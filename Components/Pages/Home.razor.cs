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

        private string message = "";
        private List<Employee> employees = new();
        private List<Attendance> allAttendances = new();

        // Search
        private string searchQuery = "";

        // Toast
        private bool showToast = false;
        private string toastMessage = "";
        private System.Threading.CancellationTokenSource? _toastCts;

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

                    // ۳. 🟢 تزریق مستقیم و اجباری زبان و تنظیمات تقویم به شیء مدیریت تقویم شما
                    if (CalendarManager != null)
                    {
                        // همگام‌سازی زبان جاری با موتور مترجم
                        CalendarManager.GetType().GetProperty("CurrentLanguage")?.SetValue(CalendarManager, CurrentLanguage);

                        // بازیابی وضعیت فعال بودن تقویم فارسی از حافظه مرورگر
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
                    employees = await DbContext.Employees.AsNoTracking().ToListAsync();
                    allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync();
                }
            }
            catch (Exception ex)
            {
                string errorFormat = CalendarManager.Translate("PCal_MainHome_LoadError");
                message = string.Format(errorFormat, ex.Message);
            }
        }       

        private async Task AdminLogoutFromHome()
        {
            await AdminState.LogoutAsync();
            message = CalendarManager.Translate("PCal_MainHome_KioskLocked");
            StateHasChanged();
        }

        private void ClearSearch()
        {
            searchQuery = "";
        }

        private async Task ShowToastAsync(string text, int milliseconds = 3500)
        {
            try
            {
                // ۱. اگر از قبل تایمری در حال اجراست، آن را متوقف و ابطال کن
                if (_toastCts != null)
                {
                    _toastCts.Cancel();
                    _toastCts.Dispose();
                }

                // ۲. ساخت یک توکن اختصاصی جدید برای پیام فعلی
                _toastCts = new System.Threading.CancellationTokenSource();
                var currentToken = _toastCts.Token;

                toastMessage = text;
                showToast = true;
                StateHasChanged();

                // ۳. صبر کردن با قابلیت لغو شدن؛ اگر دکمه دیگری زده شود، این خط خطای لغو صادر می‌کند
                await Task.Delay(milliseconds, currentToken);

                // ۴. اگر تا پایان زمان تعیین‌شده لغو نشد، کادر را ببند
                if (!currentToken.IsCancellationRequested)
                {
                    showToast = false;
                    StateHasChanged();
                }
            }
            catch (TaskCanceledException)
            {
                // این بخش زمانی اجرا می‌شود که عملیات قبلی لغو شده است؛ عمداً خالی می‌گذاریم تا پیام جدید مدیریت را به دست بگیرد
            }
        }
        private async Task ClockIn(int employeeId)
        {
            try
            {
                // ۱. بررسی وضعیت و بستن خودکار ترددهای باز روزهای گذشته بر اساس تنظیمات سیستم
                var settings = AdminState.CurrentSettings;
                bool applyAutoLogout = settings?.ApplyAutoLogout ?? true;
                string autoTimeStr = settings?.AutoLogoutTime ?? "23:59";

                var unfinishedRecords = await DbContext.Attendances
                    .Where(a => a.EmployeeId == employeeId && a.ClockOutTime == null)
                    .ToListAsync();

                foreach (var oldRecord in unfinishedRecords)
                {
                    if (oldRecord.ClockInTime.HasValue && oldRecord.ClockInTime.Value.Date < DateTime.Today)
                    {
                        if (applyAutoLogout)
                        {
                            var parsedTime = TimeSpan.Parse(autoTimeStr);
                            oldRecord.ClockOutTime = oldRecord.ClockInTime.Value.Date.Add(parsedTime);
                        }
                        else
                        {
                            oldRecord.ClockOutTime = oldRecord.ClockInTime.Value.Date.Add(TimeSpan.Parse("23:59"));
                        }
                        oldRecord.EntrySource = "AutoClose";
                        DbContext.Attendances.Update(oldRecord);
                    }
                }

                // ۲. ثبت رکورد ورود جدید با تفکیک منبع کیوسک دستگاه
                var attendance = new Attendance
                {
                    EmployeeId = employeeId,
                    ClockInTime = DateTime.Now,
                    CreationDate = DateTime.Now,
                    EntrySource = "Kiosk"
                };

                DbContext.Attendances.Add(attendance);
                await DbContext.SaveChangesAsync();

                var empName = employees.FirstOrDefault(e => e.Id == employeeId)?.FullName;
                if (string.IsNullOrEmpty(empName))
                {
                    var emp = await DbContext.Employees.FindAsync(employeeId);
                    empName = emp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                }

                await LoadData();
                string successFormat = CalendarManager.Translate("PCal_MainHome_ClockInSuccess");
                await ShowToastAsync(string.Format(successFormat, empName));
            }
            catch (Exception ex)
            {
                string errFormat = CalendarManager.Translate("PCal_MainHome_ClockInError");
                message = string.Format(errFormat, ex.InnerException?.Message ?? ex.Message);
            }
        }

        private async Task ClockOut(int attendanceId)
        {
            try
            {
                var record = await DbContext.Attendances.FindAsync(attendanceId);
                if (record != null)
                {
                    record.ClockOutTime = DateTime.Now;

                    DbContext.Attendances.Update(record);
                    await DbContext.SaveChangesAsync();

                    var empName = employees.FirstOrDefault(e => e.Id == record.EmployeeId)?.FullName;
                    if (string.IsNullOrEmpty(empName))
                    {
                        var emp = await DbContext.Employees.FindAsync(record.EmployeeId);
                        empName = emp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                    }

                    await LoadData();
                    string successFormat = CalendarManager.Translate("PCal_MainHome_ClockOutSuccess");
                    await ShowToastAsync(string.Format(successFormat, empName));
                }
            }
            catch (Exception ex)
            {
                string errFormat = CalendarManager.Translate("PCal_MainHome_ClockOutError");
                message = string.Format(errFormat, ex.InnerException?.Message ?? ex.Message);
            }
        }

    }
}