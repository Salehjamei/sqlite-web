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

        // تغییر کتگوری اصلی، بازنشانی فرزندان و ذخیره دائمی انتخاب در حافظه مرورگر همراه با ثبت خطای احتمالی
        private async Task OnMainCatFilterChanged(ChangeEventArgs e)
        {
            SelectedMainCatId = int.TryParse(e.Value?.ToString(), out int id) ? id : 0;
            SelectedSub1CatId = 0;
            SelectedSub2CatId = 0;

            try
            {
                await LocalStorage.SetAsync("KioskSelectedMainCatId", SelectedMainCatId);
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("Home.SaveMainCategoryStorage", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
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
                catch (Exception ex)
                {
                    // ثبت خطای ارتباط با حافظه مرورگر هنگام بازیابی تنظیمات زبان و تقویم در سرور
                    await ErrorLogger.LogBackendErrorAsync("Home.OnAfterRenderStorage", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
                }

                // ۴. بارگذاری اطلاعات کارمندان و رکوردها پس از همگام‌سازی کامل زبان
                if (AdminState.IsAuthorized)
                {
                    await LoadData();

                    // بازیابی دسته اصلی انتخاب‌شده قبلی از حافظه مرورگر در صورت وجود در لیست دسته‌بندی‌ها
                    try
                    {
                        var savedMainCat = await LocalStorage.GetAsync<int>("KioskSelectedMainCatId");
                        if (savedMainCat.Success && savedMainCat.Value > 0 && MainCategoriesList.Any(c => c.Id == savedMainCat.Value))
                        {
                            SelectedMainCatId = savedMainCat.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        // ثبت خطای بازیابی دسته اصلی انتخاب‌شده از حافظه مرورگر در سرور
                        await ErrorLogger.LogBackendErrorAsync("Home.LoadMainCategoryStorage", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
                    }
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

        // ثبت ورود پرسنل در کیوسک با بررسی محدوده مکانی، نمایش اعلان ساده ورود و ثبت بی‌صدای مختصات برای سوپر ادمین
        private async Task ClockIn(int employeeId)
        {
            // واکشی اطلاعات کارمند هدف از لیست حافظه یا دیتابیس
            var targetEmp = employees.FirstOrDefault(e => e.Id == employeeId)
                            ?? await DbContext.Employees.FindAsync(employeeId);

            try
            {
                // بررسی نبود رکورد تردد باز قبلی برای جلوگیری از ثبت ورود تکراری
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

                // اعتبارسنجی موقعیت مکانی فعلی دستگاه نسبت به مکان‌های مجاز دسته‌بندی کارمند
                var locCheck = await GeoService.ValidateEmployeeLocationAsync(targetEmp);
                if (!locCheck.IsAllowed)
                {
                    // نمایش خطای خارج از محدوده یا عدم دسترسی موقعیت به کاربر و ثبت در اعلانات مکانی
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

                // ساخت و ذخیره رکورد جدید ورود همراه با مختصات جغرافیایی و نام مکان مجاز
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

                // نمایش اعلان شیشه‌ای موفقیت ورود به کارمند بدون افشای مختصات جغرافیایی روی صفحه
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

                // ثبت بی‌صدای مختصات مکانی ورود در جدول اعلانات سرور جهت گزارش‌گیری سوپر ادمین
                if (!string.IsNullOrEmpty(locCheck.Coordinates))
                {
                    string locTitle = locCheck.MatchedLocationName == "FreeMode" ? "-" : locCheck.MatchedLocationName;
                    await GeoService.LogLocationAuditSilentlyAsync(
                        subCategory: "PersonnelLocation",
                        messageKey: "PCal_Loc_PersonnelClockInLog",
                        args: new object[] { empName, locTitle, locCheck.Coordinates },
                        actorAdminId: AdminState.CurrentOnlineAdmin?.Id,
                        actorAdminUsername: AdminState.CurrentOnlineAdmin?.Username ?? "Kiosk",
                        targetEmp: targetEmp,
                        eventDate: now.ToString("yyyy-MM-dd"),
                        eventTime: now.ToString("HH:mm:ss")
                    );
                }
            }
            catch (Exception ex)
            {
                // ثبت جزئیات خطای فنی سرور در لاگر مرکزی و نمایش هشدار خطا به کاربر
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
        // ثبت خروج پرسنل در کیوسک با بررسی محدوده مکانی، نمایش اعلان ساده خروج و ثبت بی‌صدای مختصات برای سوپر ادمین
        private async Task ClockOut(int attendanceId)
        {
            Employee? targetEmp = null;
            try
            {
                // واکشی رکورد تردد باز و اطلاعات کارمند مرتبط
                var record = await DbContext.Attendances.FindAsync(attendanceId);
                if (record != null)
                {
                    targetEmp = employees.FirstOrDefault(e => e.Id == record.EmployeeId)
                                ?? await DbContext.Employees.FindAsync(record.EmployeeId);

                    // اعتبارسنجی موقعیت مکانی فعلی دستگاه در لحظه ثبت خروج کارمند
                    var locCheck = await GeoService.ValidateEmployeeLocationAsync(targetEmp);
                    if (!locCheck.IsAllowed)
                    {
                        // نمایش خطای خارج از محدوده یا عدم دسترسی موقعیت به کاربر و ثبت در اعلانات مکانی
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

                    // به‌روزرسانی ساعت خروج و ذخیره مختصات جغرافیایی خروج در رکورد تردد
                    var now = DateTime.Now;
                    record.ClockOutTime = now;
                    record.ClockOutLocation = locCheck.Coordinates;

                    DbContext.Attendances.Update(record);
                    await DbContext.SaveChangesAsync();

                    var empName = targetEmp?.FullName ?? CalendarManager.Translate("PCal_MainHome_DefaultUser");
                    await LoadData();

                    // نمایش اعلان شیشه‌ای موفقیت خروج به کارمند بدون افشای مختصات جغرافیایی روی صفحه
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

                    // ثبت بی‌صدای مختصات مکانی خروج در جدول اعلانات سرور جهت گزارش‌گیری سوپر ادمین
                    if (!string.IsNullOrEmpty(locCheck.Coordinates))
                    {
                        string locTitle = locCheck.MatchedLocationName == "FreeMode" ? "-" : locCheck.MatchedLocationName;
                        await GeoService.LogLocationAuditSilentlyAsync(
                            subCategory: "PersonnelLocation",
                            messageKey: "PCal_Loc_PersonnelClockOutLog",
                            args: new object[] { empName, locTitle, locCheck.Coordinates },
                            actorAdminId: AdminState.CurrentOnlineAdmin?.Id,
                            actorAdminUsername: AdminState.CurrentOnlineAdmin?.Username ?? "Kiosk",
                            targetEmp: targetEmp,
                            eventDate: now.ToString("yyyy-MM-dd"),
                            eventTime: now.ToString("HH:mm:ss")
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // ثبت جزئیات خطای فنی سرور در لاگر مرکزی و نمایش هشدار خطا به کاربر
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
