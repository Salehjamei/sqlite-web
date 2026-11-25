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
        [Inject] public GeoLocationService GeoService { get; set; } = default!;
        [Inject] public PersonnelService PersonnelSvc { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;

        private string message = "";
        private List<Employee> employees = new();
        private List<Attendance> allAttendances = new();
        private List<PersonnelCategory> allCategories = new();

        // متغیرهای نگهداری شناسه دسته‌بندی‌های انتخاب‌شده در سلکتورهای سه‌سطحی کیوسک
        private int SelectedMainCatId { get; set; } = 0;
        private int SelectedSub1CatId { get; set; } = 0;
        private int SelectedSub2CatId { get; set; } = 0;

        // واکشی لیست‌های آبشاری هر سطح از طریق متدهای سرویس مشترک پرسنل
        private List<PersonnelCategory> MainCategoriesList => PersonnelSvc.GetMainCategories(allCategories);
        private List<PersonnelCategory> Sub1CategoriesList => PersonnelSvc.GetSub1Categories(allCategories, SelectedMainCatId);
        private List<PersonnelCategory> Sub2CategoriesList => PersonnelSvc.GetSub2Categories(allCategories, SelectedSub1CatId);

        // Search
        private string searchQuery = "";

        private bool isAnyAdminLoggedIn => AdminState.IsAuthorized;

        // فیلتر هم‌زمان لیست نمایش کیوسک با استفاده از متد مشترک سرویس پرسنل
        private IEnumerable<Employee> DisplayEmployees =>
            PersonnelSvc.FilterEmployees(employees, SelectedMainCatId, SelectedSub1CatId, SelectedSub2CatId, searchQuery);

        // تغییر کتگوری اصلی و بازنشانی خودکار سلکتورهای فرزند اول و دوم
        private void OnMainCatFilterChanged(ChangeEventArgs e)
        {
            SelectedMainCatId = int.TryParse(e.Value?.ToString(), out int id) ? id : 0;
            SelectedSub1CatId = 0;
            SelectedSub2CatId = 0;
        }

        // تغییر کتگوری فرزند اول و بازنشانی خودکار سلکتور فرزند دوم
        private void OnSub1CatFilterChanged(ChangeEventArgs e)
        {
            SelectedSub1CatId = int.TryParse(e.Value?.ToString(), out int id) ? id : 0;
            SelectedSub2CatId = 0;
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

               // بارگذاری ایزوله پرسنل مجاز به نمایش در کیوسک، دسته‌بندی‌های سه‌سطحی و سوابق تردد از سرویس مشترک
        private async Task LoadData()
        {
            try
            {
                employees = await PersonnelSvc.GetEmployeesAsync(onlyVisibleInKiosk: true);
                allAttendances = await PersonnelSvc.GetAllAttendancesAsync();
                allCategories = await PersonnelSvc.GetAllCategoriesAsync();
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

        // ثبت ورود پرسنل در کیوسک همراه با اعتبارسنجی محدوده مکانی، دسته‌بندی سه‌سطحی و ثبت لاگ‌های مکانی و خطا
        private async Task ClockIn(int employeeId)
        {
            var targetEmp = employees.FirstOrDefault(e => e.Id == employeeId)
                            ?? await DbContext.Employees.FindAsync(employeeId);

            try
            {
                // جلوگیری از ثبت ورود تکراری در صورت وجود رکورد تردد باز در سیستم
                bool hasOpenRecord = await DbContext.Attendances
                    .AnyAsync(a => a.EmployeeId == employeeId && a.ClockOutTime == null);

                if (hasOpenRecord)
                {
                    TriggerToast(
                        category: "Kiosk",
                        subCategory: "Validation",
                        messageKey: "PCal_MainHome_AlreadyHasOpenRecord",
                        isError: true,
                        targetEmp: targetEmp
                    );
                    return;
                }

                // بررسی مجاز بودن موقعیت مکانی دستگاه نسبت به مکان‌های تعریف‌شده برای دسته‌بندی کارمند
                var locCheck = await GeoService.ValidateEmployeeLocationAsync(targetEmp);
                if (!locCheck.IsAllowed)
                {
                    // ثبت خطای خارج از محدوده یا عدم دسترسی موقعیت در دسته‌بندی Location و زیرکتگوری PersonnelLocation
                    TriggerToast(
                        category: "Location",
                        subCategory: "PersonnelLocation",
                        messageKey: locCheck.ErrorKey,
                        args: new object[] { targetEmp?.FullName ?? "-", locCheck.ClosestDistanceMeters },
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
                    EntrySource = "Kiosk",
                    ClockInLocation = locCheck.Coordinates,
                    LocationName = locCheck.MatchedLocationName == "FreeMode" ? null : locCheck.MatchedLocationName
                };

                DbContext.Attendances.Add(attendance);
                await DbContext.SaveChangesAsync();

                var empName = targetEmp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                await LoadData();

                // ثبت اعلان موفقیت ورود در بخش کیوسک
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

                // ثبت مستقل موقعیت جغرافیایی ثبت ورود پرسنل در تاریخچه اعلانات مکانی
                if (!string.IsNullOrEmpty(locCheck.Coordinates))
                {
                    string locTitle = locCheck.MatchedLocationName == "FreeMode" ? "-" : locCheck.MatchedLocationName;
                    TriggerToast(
                        category: "Location",
                        subCategory: "PersonnelLocation",
                        messageKey: "PCal_Loc_PersonnelClockInLog",
                        args: new object[] { empName, locTitle, locCheck.Coordinates },
                        isError: false,
                        targetEmp: targetEmp,
                        eventDate: now.ToString("yyyy-MM-dd"),
                        eventTime: now.ToString("HH:mm:ss")
                    );
                }
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("Home.ClockIn", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, targetEmp, CurrentLanguage);
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


        // ثبت خروج پرسنل در کیوسک همراه با اعتبارسنجی محدوده مکانی، دسته‌بندی سه‌سطحی و ثبت لاگ‌های مکانی و خطا
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

                    // بررسی مجاز بودن موقعیت مکانی دستگاه در لحظه ثبت خروج کارمند
                    var locCheck = await GeoService.ValidateEmployeeLocationAsync(targetEmp);
                    if (!locCheck.IsAllowed)
                    {
                        TriggerToast(
                            category: "Location",
                            subCategory: "PersonnelLocation",
                            messageKey: locCheck.ErrorKey,
                            args: new object[] { targetEmp?.FullName ?? "-", locCheck.ClosestDistanceMeters },
                            isError: true,
                            targetEmp: targetEmp
                        );
                        return;
                    }

                    var now = DateTime.Now;
                    record.ClockOutTime = now;
                    record.ClockOutLocation = locCheck.Coordinates;

                    DbContext.Attendances.Update(record);
                    await DbContext.SaveChangesAsync();

                    var empName = targetEmp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                    await LoadData();

                    // ثبت اعلان موفقیت خروج در بخش کیوسک
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

                    // ثبت مستقل موقعیت جغرافیایی ثبت خروج پرسنل در تاریخچه اعلانات مکانی
                    if (!string.IsNullOrEmpty(locCheck.Coordinates))
                    {
                        string locTitle = locCheck.MatchedLocationName == "FreeMode" ? "-" : locCheck.MatchedLocationName;
                        TriggerToast(
                            category: "Location",
                            subCategory: "PersonnelLocation",
                            messageKey: "PCal_Loc_PersonnelClockOutLog",
                            args: new object[] { empName, locTitle, locCheck.Coordinates },
                            isError: false,
                            targetEmp: targetEmp,
                            eventDate: now.ToString("yyyy-MM-dd"),
                            eventTime: now.ToString("HH:mm:ss")
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("Home.ClockOut", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, targetEmp, CurrentLanguage);
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
