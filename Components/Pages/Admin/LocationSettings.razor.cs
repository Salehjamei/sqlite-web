using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using sqlite_web.Components.Models;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages.Admin
{
    // منطق پشت‌صحنه مدیریت درخت دسته‌بندی سه‌سطحی پرسنل و تعریف مکان‌های جغرافیایی مجاز (بدون وابستگی مستقیم به دیتابیس)
    public partial class LocationSettings : AppBaseComponent, IDisposable
    {
        // تزریق سرویس‌های مرکزی سیستم جهت مدیریت وضعیت ادمین، موقعیت مکانی، پرسنل و ثبت خطاها
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public GeoLocationService GeoService { get; set; } = default!;
        [Inject] public PersonnelService PersonnelSvc { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;

        // لیست کامل دسته‌بندی‌های سه‌سطحی و مکان‌های مجاز واکشی‌شده از سرویس‌ها
        protected List<PersonnelCategory> AllCategories { get; set; } = new();
        protected List<AllowedLocation> LocationsList { get; set; } = new();

        // لیست‌های فیلترشده بر اساس سطح درخت دسته‌بندی از طریق سرویس مشترک پرسنل
        protected List<PersonnelCategory> MainCategories => PersonnelSvc.GetMainCategories(AllCategories);
        protected List<PersonnelCategory> Sub1Categories => AllCategories.Where(c => c.Level == 2).ToList();
        protected List<PersonnelCategory> Sub2Categories => AllCategories.Where(c => c.Level == 3).ToList();

        // متغیرهای فرم افزودن دسته‌بندی جدید در هر یک از سه سطح
        protected string NewMainCategoryTitle { get; set; } = string.Empty;
        protected int SelectedParentForSub1 { get; set; } = 0;
        protected string NewSub1CategoryTitle { get; set; } = string.Empty;
        protected int SelectedParentForSub2 { get; set; } = 0;
        protected string NewSub2CategoryTitle { get; set; } = string.Empty;

        // متغیرهای فرم ثبت و ویرایش مکان مجاز
        protected bool ShowLocationModal { get; set; } = false;
        protected bool IsLocationEditMode { get; set; } = false;
        protected int EditingLocationId { get; set; } = 0;

        protected string LocName { get; set; } = string.Empty;
        protected string LocLatitudeStr { get; set; } = string.Empty;
        protected string LocLongitudeStr { get; set; } = string.Empty;
        protected int LocRadiusMeters { get; set; } = 100;
        protected bool LocIsActive { get; set; } = true;

        // محدودیت‌های دسته‌بندی مجاز برای مکان انتخاب‌شده (0 به معنای آزاد برای همه زیرمجموعه‌ها)
        protected int LocAllowedMainCatId { get; set; } = 0;
        protected int LocAllowedSub1CatId { get; set; } = 0;
        protected int LocAllowedSub2CatId { get; set; } = 0;
        protected bool IsFetchingGps { get; set; } = false;

        // متغیرهای مودال تاییدیه حذف مکان یا دسته‌بندی
        protected bool ShowDeleteModal { get; set; } = false;
        protected string DeleteTargetType { get; set; } = string.Empty;
        protected int DeleteTargetId { get; set; } = 0;
        protected string DeleteTargetName { get; set; } = string.Empty;

        // تبدیل رشته مختصات فرم به عدد اعشاری استاندارد برای ارسال به ماژول نقشه تعاملی
        protected double ParsedMapLat => double.TryParse(LocLatitudeStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) ? lat : 40.765400;
        protected double ParsedMapLng => double.TryParse(LocLongitudeStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lng) ? lng : 29.940800;

        // دریافت آنی مختصات جدید هنگام کلیک ادمین روی نقشه در ماژول InteractiveMap
        protected void HandleMapLocationPicked((double Lat, double Lng) coords)
        {
            LocLatitudeStr = coords.Lat.ToString("F6", CultureInfo.InvariantCulture);
            LocLongitudeStr = coords.Lng.ToString("F6", CultureInfo.InvariantCulture);
        }
        // مقداردهی اولیه صفحه، بررسی سطح دسترسی و بارگذاری داده‌های مکانی و دسته‌بندی‌ها
        protected override async Task OnInitializedAsync()
        {
            AdminState.OnChange += OnAdminStateChanged;
            await AdminState.CheckAccessAndRedirectAsync();
            await LoadAllDataAsync();
        }

        // واکنش لحظه‌ای به تغییر زبان یا وضعیت سشن ادمین در هدر
        private async void OnAdminStateChanged()
        {
            await LoadStoredLanguageAndCalendarAsync();
            if (CalendarManager != null && !string.IsNullOrEmpty(CurrentLanguage))
                CalendarManager.CurrentLanguage = CurrentLanguage;
            await InvokeAsync(StateHasChanged);
        }

        // واکشی ایزوله دسته‌بندی‌های درختی و مکان‌های مجاز از سرویس‌های مربوطه
        protected async Task LoadAllDataAsync()
        {
            try
            {
                AllCategories = await PersonnelSvc.GetAllCategoriesAsync();
                LocationsList = await GeoService.GetAllAllowedLocationsAsync();
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("LocationSettings.LoadAllDataAsync", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }

        // افزودن کتگوری اصلی (سطح ۱) از طریق سرویس موقعیت مکانی و ثبت در اعلانات
        protected async Task AddMainCategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewMainCategoryTitle)) return;
            try
            {
                string title = NewMainCategoryTitle.Trim();
                await GeoService.AddPersonnelCategoryAsync(title, level: 1, parentId: null);

                NewMainCategoryTitle = string.Empty;
                await LoadAllDataAsync();

                string coloredTitle = $"<span style='color: #dc3545; font-weight: 900;'>«{title}»</span>";
                TriggerToast("Location", "AllowedLocation", "PCal_Loc_CategoryAddedLog", new object[] { coloredTitle }, false);
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("LocationSettings.AddMainCategory", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }

        // افزودن کتگوری فرزند اول (سطح ۲) زیرمجموعه یک کتگوری اصلی از طریق سرویس
        protected async Task AddSub1CategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSub1CategoryTitle) || SelectedParentForSub1 <= 0) return;
            try
            {
                string title = NewSub1CategoryTitle.Trim();
                await GeoService.AddPersonnelCategoryAsync(title, level: 2, parentId: SelectedParentForSub1);

                NewSub1CategoryTitle = string.Empty;
                await LoadAllDataAsync();

                string coloredTitle = $"<span style='color: #dc3545; font-weight: 900;'>«{title}»</span>";
                TriggerToast("Location", "AllowedLocation", "PCal_Loc_CategoryAddedLog", new object[] { coloredTitle }, false);
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("LocationSettings.AddSub1Category", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }

        // افزودن کتگوری فرزند دوم (سطح ۳ - عنوان کاری) زیرمجموعه کتگوری فرزند اول از طریق سرویس
        protected async Task AddSub2CategoryAsync()
        {
            if (string.IsNullOrWhiteSpace(NewSub2CategoryTitle) || SelectedParentForSub2 <= 0) return;
            try
            {
                string title = NewSub2CategoryTitle.Trim();
                await GeoService.AddPersonnelCategoryAsync(title, level: 3, parentId: SelectedParentForSub2);

                NewSub2CategoryTitle = string.Empty;
                await LoadAllDataAsync();

                string coloredTitle = $"<span style='color: #dc3545; font-weight: 900;'>«{title}»</span>";
                TriggerToast("Location", "AllowedLocation", "PCal_Loc_CategoryAddedLog", new object[] { coloredTitle }, false);
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("LocationSettings.AddSub2Category", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }
        // باز کردن مودال ثبت مکان مجاز جدید و بازنشانی مقادیر فرم
        protected void OpenAddLocationModal()
        {
            IsLocationEditMode = false;
            EditingLocationId = 0;
            LocName = string.Empty;
            LocLatitudeStr = string.Empty;
            LocLongitudeStr = string.Empty;
            LocRadiusMeters = 100;
            LocIsActive = true;
            LocAllowedMainCatId = 0;
            LocAllowedSub1CatId = 0;
            LocAllowedSub2CatId = 0;
            ShowLocationModal = true;
        }

        // باز کردن مودال ویرایش مکان مجاز موجود و بارگذاری مقادیر فعلی آن
        protected void OpenEditLocationModal(AllowedLocation loc)
        {
            IsLocationEditMode = true;
            EditingLocationId = loc.Id;
            LocName = loc.Name;
            LocLatitudeStr = loc.Latitude.ToString("F6", CultureInfo.InvariantCulture);
            LocLongitudeStr = loc.Longitude.ToString("F6", CultureInfo.InvariantCulture);
            LocRadiusMeters = loc.RadiusMeters;
            LocIsActive = loc.IsActive;
            LocAllowedMainCatId = loc.AllowedMainCategoryId ?? 0;
            LocAllowedSub1CatId = loc.AllowedSubCategory1Id ?? 0;
            LocAllowedSub2CatId = loc.AllowedSubCategory2Id ?? 0;
            ShowLocationModal = true;
        }

        // بستن پنجره مودال ثبت یا ویرایش مکان
        protected void CloseLocationModal() => ShowLocationModal = false;

        // دریافت مختصات جغرافیایی فعلی دستگاه ادمین از طریق مرورگر جهت ثبت سریع نقطه مرکزی
        protected async Task FetchCurrentGpsForFormAsync()
        {
            IsFetchingGps = true;
            await InvokeAsync(StateHasChanged);

            var gps = await GeoService.GetCurrentDeviceLocationAsync();
            IsFetchingGps = false;

            if (gps.Success)
            {
                LocLatitudeStr = gps.Latitude.ToString("F6", CultureInfo.InvariantCulture);
                LocLongitudeStr = gps.Longitude.ToString("F6", CultureInfo.InvariantCulture);
            }
            else
            {
                string errKey = gps.ErrorCode == "PERMISSION_DENIED" ? "PCal_Loc_PermissionDenied" : "PCal_Loc_Unavailable";
                TriggerToast("Location", "AllowedLocation", errKey, null, true);
            }
        }

        // ذخیره یا ویرایش مکان مجاز از طریق سرویس موقعیت مکانی همراه با ثبت اعلان در زیرکتگوری AllowedLocation
        protected async Task SaveLocationAsync()
        {
            if (string.IsNullOrWhiteSpace(LocName)) return;
            if (!double.TryParse(LocLatitudeStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(LocLongitudeStr.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
            {
                TriggerToast("Location", "AllowedLocation", "PCal_Loc_InvalidCoords", null, true);
                return;
            }

            if (LocRadiusMeters < 10) LocRadiusMeters = 10;

            try
            {
                string cleanName = LocName.Trim();
                string coloredLoc = $"<span style='color: #dc3545; font-weight: 900;'>«{cleanName}»</span>";

                int? editId = (IsLocationEditMode && EditingLocationId > 0) ? EditingLocationId : null;
                bool saved = await GeoService.SaveOrUpdateAllowedLocationAsync(
                    editingId: editId,
                    name: cleanName,
                    latitude: lat,
                    longitude: lon,
                    radiusMeters: LocRadiusMeters,
                    isActive: LocIsActive,
                    mainCatId: LocAllowedMainCatId > 0 ? LocAllowedMainCatId : null,
                    sub1CatId: LocAllowedSub1CatId > 0 ? LocAllowedSub1CatId : null,
                    sub2CatId: LocAllowedSub2CatId > 0 ? LocAllowedSub2CatId : null
                );

                if (saved)
                {
                    string logKey = editId.HasValue ? "PCal_Loc_LocationUpdatedLog" : "PCal_Loc_LocationAddedLog";
                    TriggerToast("Location", "AllowedLocation", logKey, new object[] { coloredLoc, LocRadiusMeters }, false);
                }

                ShowLocationModal = false;
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("LocationSettings.SaveLocationAsync", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }

        // باز کردن پنجره تاییدیه حذف مکان یا دسته‌بندی
        protected void PromptDelete(string type, int id, string name)
        {
            DeleteTargetType = type;
            DeleteTargetId = id;
            DeleteTargetName = name;
            ShowDeleteModal = true;
        }

        // بستن پنجره تاییدیه حذف
        protected void CancelDelete()
        {
            ShowDeleteModal = false;
            DeleteTargetId = 0;
        }

        // اجرای حذف قطعی مکان یا دسته‌بندی از طریق سرویس موقعیت مکانی و ثبت در تاریخچه اعلانات
        protected async Task ConfirmDeleteAsync()
        {
            ShowDeleteModal = false;
            try
            {
                string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{DeleteTargetName}»</span>";

                if (DeleteTargetType == "Location")
                {
                    bool deleted = await GeoService.DeleteAllowedLocationAsync(DeleteTargetId);
                    if (deleted)
                    {
                        TriggerToast("Location", "AllowedLocation", "PCal_Loc_LocationDeletedLog", new object[] { coloredName }, false);
                    }
                }
                else if (DeleteTargetType == "Category")
                {
                    bool deleted = await GeoService.DeletePersonnelCategoryAsync(DeleteTargetId);
                    if (deleted)
                    {
                        TriggerToast("Location", "AllowedLocation", "PCal_Loc_CategoryDeletedLog", new object[] { coloredName }, false);
                    }
                }

                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("LocationSettings.ConfirmDeleteAsync", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }

        // بازیابی زبان و نوع تقویم ذخیره‌شده از حافظه مرورگر در اولین رندر صفحه
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadStoredLanguageAndCalendarAsync();
                if (CalendarManager != null && !string.IsNullOrEmpty(CurrentLanguage))
                    CalendarManager.CurrentLanguage = CurrentLanguage;
                await InvokeAsync(StateHasChanged);
            }
        }

        // آزادسازی رویداد هنگام خروج از صفحه
        public void Dispose()
        {
            AdminState.OnChange -= OnAdminStateChanged;
        }
    }
}
