using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages
{
    public partial class Home : AppBaseComponent
    {
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;

        private string message = "";
        private List<Employee> employees = new();
        private List<Attendance> allAttendances = new();

        // Search
        private string searchQuery = "";

        private bool isAnyAdminLoggedIn => AdminState.IsAuthorized;

        private IEnumerable<Employee> DisplayEmployees
        {
            get
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                    return employees.OrderBy(e => e.FullName);
                var q = searchQuery.Trim();
                return employees.Where(e =>
                    (!string.IsNullOrEmpty(e.FullName) && e.FullName.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                    || (!string.IsNullOrEmpty(e.PersonnelCode) && e.PersonnelCode.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                ).OrderBy(e => e.FullName);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // ۱. بررسی دسترسی و لایه امنیتی ادمین به صفحه اصلی
                await AdminState.CheckAccessAndRedirectAsync();

                try
                {
                    // ۲. فراخوانی متد لود زبان و تقویم از کلاس پایه اختصاصی
                    await LoadStoredLanguageAndCalendarAsync();

                    // ۳. تزریق مستقیم زبان و تنظیمات تقویم به شیء مدیریت تقویم
                    if (CalendarManager != null)
                    {
                        CalendarManager.GetType().GetProperty("CurrentLanguage")?.SetValue(CalendarManager, CurrentLanguage);

                        var calendarResult = await LocalStorage.GetAsync<bool>("UsePersianCalendarSetting");
                        if (calendarResult.Success)
                        {
                            CalendarManager.UsePersianCalendar = calendarResult.Value;
                        }
                    }
                }
                catch
                {
                    // مهار خطاهای احتمالی ارتباط اولیه با لوکال استوریج در رندر اولیه
                }

                // ۴. بارگذاری اطلاعات کارمندان و رکوردها پس از همگام‌سازی کامل زبان
                if (AdminState.IsAuthorized)
                {
                    await LoadData();
                }

                // ۵. اجبار کامل کامپوننت به بازرندری و رندر متون ترجمه‌شده جدید
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task LoadData()
        {
            try
            {
                if (DbContext != null)
                {
                    var settings = AdminState.CurrentSettings;
                    bool applyAutoLogout = settings?.ApplyAutoLogout ?? true;
                    string autoTimeStr = settings?.AutoLogoutTime ?? "23:59";

                    // واکشی ترددهای باز گذشته همراه با اطلاعات کارمند جهت ثبت در لاگ سیستم
                    var overdueRecords = await DbContext.Attendances
                        .Include(a => a.Employee)
                        .Where(a => a.ClockOutTime == null && a.ClockInTime.HasValue && a.ClockInTime.Value.Date < DateTime.Today)
                        .ToListAsync();

                    var now = DateTime.Now;

                    foreach (var oldRecord in overdueRecords)
                    {
                        var parsedTime = applyAutoLogout ? TimeSpan.Parse(autoTimeStr) : TimeSpan.Parse("23:59");
                        var autoClockOut = oldRecord.ClockInTime!.Value.Date.Add(parsedTime);

                        oldRecord.ClockOutTime = autoClockOut;
                        oldRecord.EntrySource = "AutoClose";
                        DbContext.Attendances.Update(oldRecord);

                        // ثبت مستقیم لاگ خروج خودکار در سرور بدون نام ادمین (توسط System)
                        var empName = oldRecord.Employee?.FullName ?? "-";
                        string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";
                        var args = new object[] { coloredEmpName, autoClockOut.ToString("HH:mm") };
                        var template = CalendarManager.Translate("PCal_System_AutoLogoutSuccess");

                        DbContext.AdminNotificationLogs.Add(new AdminNotificationLog
                        {
                            Category = "System",
                            SubCategory = "AutoLogout",
                            MessageKey = "PCal_System_AutoLogoutSuccess",
                            MessageArgs = System.Text.Json.JsonSerializer.Serialize(args),
                            Message = System.Text.RegularExpressions.Regex.Replace(string.Format(template, args), "<.*?>", string.Empty),
                            NotificationType = "Info",
                            CreatedAt = now,
                            CreatedDate = now.ToString("yyyy-MM-dd"),
                            CreatedTime = now.ToString("HH:mm:ss"),
                            EventDate = autoClockOut.ToString("yyyy-MM-dd"),
                            EventTime = autoClockOut.ToString("HH:mm:ss"),
                            AdminId = null,
                            AdminUsername = "System",
                            EmployeeId = oldRecord.EmployeeId,
                            EmployeeFullName = empName,
                            EmployeePersonnelCode = oldRecord.Employee?.PersonnelCode ?? "-",
                            EmployeePhone = oldRecord.Employee?.Phone ?? "-"
                        });
                    }

                    if (overdueRecords.Any())
                    {
                        await DbContext.SaveChangesAsync();
                    }

                    employees = await DbContext.Employees
                        .Where(e => !e.IsHiddenFromKiosk)
                        .AsNoTracking()
                        .ToListAsync();
                    allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync();
                }
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("Home.LoadData", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
                TriggerToast(
                    category: "Kiosk",
                    subCategory: "SystemError",
                    messageKey: "PCal_MainHome_LoadError",
                    args: new object[] { ex.InnerException?.Message ?? ex.Message },
                    isError: true
                );
            }

        }


        private void ClearSearch()
        {
            searchQuery = "";
        }

        private async Task ClockIn(int employeeId)
        {
            var targetEmp = employees.FirstOrDefault(e => e.Id == employeeId)
                            ?? await DbContext.Employees.FindAsync(employeeId);

            try
            {
                bool hasOpenRecord = await DbContext.Attendances
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ClockOutTime == null);

                if (hasOpenRecord)
                {
                    // ثبت و نمایش خطای تردد باز قبلی با زیردسته Validation
                    TriggerToast(
                        category: "Kiosk",
                        subCategory: "Validation",
                        messageKey: "PCal_MainHome_AlreadyHasOpenRecord",
                        isError: true,
                        targetEmp: targetEmp
                    );
                    return;
                }

                var now = DateTime.Now;
                var attendance = new Attendance
                {
                    EmployeeId = employeeId,
                    ClockInTime = now,
                    CreationDate = now,
                    EntrySource = "Kiosk"
                };

                DbContext.Attendances.Add(attendance);
                await DbContext.SaveChangesAsync();

                var empName = targetEmp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                await LoadData();

                TriggerToast(
                    category: "Kiosk",
                    subCategory: "Attendance",
                    messageKey: "PCal_MainHome_ClockInSuccess",
                    args: new object[] { empName },
                    isError: false,
                    targetEmp: targetEmp,
                    eventDate: now.ToString("yyyy-MM-dd"),
                    eventTime: now.ToString("HH:mm:ss")
                );
            }
            catch (Exception ex)
            {
                TriggerToast(
                    category: "Kiosk",
                    subCategory: "Attendance",
                    messageKey: "PCal_MainHome_ClockInError",
                    args: new object[] { ex.InnerException?.Message ?? ex.Message },
                    isError: true,
                    targetEmp: targetEmp
                );
            }
        }

        private async Task ClockOut(int attendanceId)
        {
            Employee? targetEmp = null;
            try
            {
                var record = await DbContext.Attendances.FindAsync(attendanceId);
                if (record != null)
                {
                    targetEmp = employees.FirstOrDefault(e => e.Id == record.EmployeeId)
                                ?? await DbContext.Employees.FindAsync(record.EmployeeId);

                    var now = DateTime.Now;
                    record.ClockOutTime = now;

                    DbContext.Attendances.Update(record);
                    await DbContext.SaveChangesAsync();

                    var empName = targetEmp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                    await LoadData();

                    TriggerToast(
                        category: "Kiosk",
                        subCategory: "Attendance",
                        messageKey: "PCal_MainHome_ClockOutSuccess",
                        args: new object[] { empName },
                        isError: false,
                        targetEmp: targetEmp,
                        eventDate: now.ToString("yyyy-MM-dd"),
                        eventTime: now.ToString("HH:mm:ss")
                    );
                }
            }
            catch (Exception ex)
            {
                TriggerToast(
                    category: "Kiosk",
                    subCategory: "Attendance",
                    messageKey: "PCal_MainHome_ClockOutError",
                    args: new object[] { ex.InnerException?.Message ?? ex.Message },
                    isError: true,
                    targetEmp: targetEmp
                );
            }
        }

    }
}
