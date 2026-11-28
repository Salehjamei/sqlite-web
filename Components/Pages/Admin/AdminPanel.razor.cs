using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sqlite_web.Components.Pages
{
    public partial class AdminPanel : AppBaseComponent, IDisposable
    {
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public GeoLocationService GeoService { get; set; } = default!;
        [Inject] public PersonnelService PersonnelSvc { get; set; } = default!;
        [Inject] public Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;
        // تزریق سرویس متمرکز ذخیره‌سازی تصاویر پرسنلی
        [Inject] public ImageStorageService ImageStorage { get; set; } = default!;

        // نگهداری نام فیلدی که دچار خطای اعتبارسنجی شده است ("Name", "Code", "Phone")
        protected string EmpErrorField { get; set; } = string.Empty;
        // ثبت نام فیلد دارای خطا و فوکوس خودکار روی المنت HTML مربوطه
        private async Task SetEmpValidationErrorAsync(string fieldName, string elementId, string messageKey)
        {
            EmpErrorField = fieldName;

            // صدور اعلان خطای اعتبارسنجی فرم با زیردسته Validation و ثبت در سرور
            TriggerToast(
                category: "AdminPanel",
                subCategory: "Validation",
                messageKey: messageKey,
                isError: true
            );

            await InvokeAsync(StateHasChanged);

            try
            {
                await JSRuntime.InvokeVoidAsync("eval", $"setTimeout(function() {{ var el = document.getElementById('{elementId}'); if(el) {{ el.focus(); el.select(); }} }}, 50);");
            }
            catch { }
        }

        protected bool canAddPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanAddEmployee ?? false);
        protected bool canEditPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanEditEmployee ?? false);
        protected bool canDeletePerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanDeleteEmployee ?? false);
        // بررسی مجوز مشاهده نقشه موقعیت مکانی ورود و خروج پرسنل در کارنامه
        protected bool canViewMapPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanViewLocationMap ?? false);

        protected string SystemMessage { get; set; } = string.Empty;

        protected bool isAttEditMode { get; set; } = false;
        private int? _editingAttendanceId = null;
        protected string TotalMonthlyHours { get; set; } = "00:00";

        protected bool showSettingsModal { get; set; } = false;
        protected string settingsStartTime { get; set; } = "07:00";
        protected string settingsEndTime { get; set; } = "17:30";
        protected List<string> selectedOffDays { get; set; } = new();

        protected string newEmpName { get; set; } = string.Empty;
        protected string newEmpCode { get; set; } = string.Empty;


        protected string newEmpPhone { get; set; } = string.Empty;
        protected bool newEmpHiddenFromKiosk { get; set; } = false;
        protected bool isEmpEditMode { get; set; } = false;
        private int? _editingEmployeeId = null;
        // نگهداری لیست کامل دسته‌بندی‌های سه‌سطحی و شناسه‌های انتخاب‌شده در فیلتر جدول پرسنل
        protected List<PersonnelCategory> allCategories { get; set; } = new();
        protected int FilterMainCatId { get; set; } = 0;
        protected int FilterSub1CatId { get; set; } = 0;
        protected int FilterSub2CatId { get; set; } = 0;

        // متغیرهای نگهداری دسته‌بندی سه‌سطحی انتخاب‌شده در فرم ثبت و ویرایش کارمند
        protected int newEmpMainCatId { get; set; } = 0;
        protected int newEmpSub1CatId { get; set; } = 0;
        protected int newEmpSub2CatId { get; set; } = 0;


        protected bool showModal { get; set; } = false;
        protected bool showManualAddModal { get; set; } = false;
        protected bool showDeleteModal { get; set; } = false;

        protected Employee? selectedEmployee { get; set; }
        protected DateTime manualDate { get; set; } = DateTime.Today;
        protected DateTime manualOutDate { get; set; } = DateTime.Today;
        protected string manualInTime { get; set; } = "07:00";
        protected string manualOutTime { get; set; } = "17:30";

        protected List<Employee> employees { get; set; } = new();
        protected string SearchQuery { get; set; } = string.Empty;

        // لیست فیلترشده پرسنل متصل به متد مشترک سرویس پرسنل (ترکیب دسته‌بندی سه‌سطحی و عبارت جستجو)
        protected IEnumerable<Employee> FilteredEmployees =>
            PersonnelSvc.FilterEmployees(employees, FilterMainCatId, FilterSub1CatId, FilterSub2CatId, SearchQuery);
        // تغییر فیلتر کتگوری اصلی جدول پرسنل، بازنشانی فرزندان و ذخیره دائمی در حافظه مرورگر
        protected async Task OnFilterMainCatChanged(ChangeEventArgs e)
        {
            FilterMainCatId = int.TryParse(e.Value?.ToString(), out int id) ? id : 0;
            FilterSub1CatId = 0;
            FilterSub2CatId = 0;

            try
            {
                await LocalStorage.SetAsync("AdminPanelSelectedMainCatId", FilterMainCatId);
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("AdminPanel.SaveMainCategoryStorage", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }


        // تغییر فیلتر کتگوری فرزند اول جدول پرسنل و بازنشانی فرزند دوم
        protected void OnFilterSub1CatChanged(ChangeEventArgs e)
        {
            FilterSub1CatId = int.TryParse(e.Value?.ToString(), out int id) ? id : 0;
            FilterSub2CatId = 0;
        }
        protected List<Attendance> allAttendances { get; set; } = new();
        protected string SelectedMonthFilter { get; set; } = string.Empty;
        protected string TotalRegularHours { get; set; } = "00:00";
        protected string TotalOvertimeHours { get; set; } = "00:00";

        public List<MonthOption> AvailableMonths { get; set; } = new();
        protected int? ActiveMobileDropdownId { get; set; } = null;
        public bool showEmpFormModal { get; set; } = false;
        public string previewImageUrl { get; set; } = string.Empty;

        protected string deleteModalTitle { get; set; } = string.Empty;
        protected string targetDeleteName { get; set; } = string.Empty;
        protected string deleteTargetMessage { get; set; } = string.Empty;
        protected string deleteModalSubMessage { get; set; } = string.Empty;
        private int _targetIdToDelete = 0;
        private string _deleteType = string.Empty;

        public class MonthOption
        {
            public string Value { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }
        // پرچم کنترل وضعیت باز یا بسته بودن مودال نقشه موقعیت جغرافیایی تردد در کارنامه
        protected bool showAttMapModal { get; set; } = false;

        // نگهداری رکورد تردد انتخاب‌شده جهت نمایش موقعیت ورود و خروج آن روی نقشه
        protected Attendance? mapTargetAttendance { get; set; }

        // بررسی اینکه آیا برای رکورد تردد حداقل یکی از مختصات ورود یا خروج ثبت شده است یا خیر
        protected bool HasValidCoordinates(Attendance att)
        {
            return !string.IsNullOrWhiteSpace(att.ClockInLocation) || !string.IsNullOrWhiteSpace(att.ClockOutLocation);
        }

        // تجزیه رشته مختصات جغرافیایی با فرمت "عرض,طول" به دو عدد اعشاری استاندارد برای ارسال به ماژول نقشه
        protected (double? lat, double? lng) ParseCoordPair(string? coord)
        {
            // در صورت خالی بودن رشته مختصات، مقادیر تهی برگردانده می‌شود
            if (string.IsNullOrWhiteSpace(coord)) return (null, null);

            // جداسازی عرض و طول جغرافیایی با جداکننده کاما
            var parts = coord.Split(',');

            // اعتبارسنجی دو بخشی بودن مختصات و تبدیل امن آن‌ها به عدد اعشاری با استاندارد انگلیسی
            if (parts.Length == 2 &&
                double.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                return (lat, lng);
            }

            return (null, null);
        }

        // باز کردن مودال نقشه تعاملی برای نمایش نقاط ثبت ورود و خروج رکورد تردد انتخاب‌شده
        protected void OpenAttendanceMapModal(Attendance att)
        {
            // ثبت رکورد تردد هدف و فعال‌سازی نمایش مودال نقشه
            mapTargetAttendance = att;
            showAttMapModal = true;
        }

        // بستن مودال نقشه تعاملی و پاکسازی رکورد انتخاب‌شده از حافظه
        protected void CloseAttendanceMapModal()
        {
            // غیرفعال کردن نمایش مودال و آزادسازی مرجع رکورد تردد
            showAttMapModal = false;
            mapTargetAttendance = null;
        }

        protected override async Task OnInitializedAsync()
        {
            // اتصال صفحه به رویداد تغییر وضعیت مرکزی برای همگام‌سازی لحظه‌ای زبان
            AdminState.OnChange += HandleStateChanged;

            try
            {
                await AdminState.CheckAccessAndRedirectAsync();
                if (AdminState.IsAuthorized)
                {
                    await AdminState.LoadWorkSettingsAsync();
                    if (AdminState.CurrentSettings == null)
                    {
                        AdminState.CurrentSettings = new sqlite_web.Components.Models.Admin.WorkSettings
                        {
                            RegularStartTime = "07:00",
                            RegularEndTime = "17:30",
                            OffDaysOfWeek = "Saturday,Sunday",
                            DeficitThresholdMinutes = 15
                        };
                    }
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnInitializedAsync: {ex.Message}");
            }
        }
        // استخراج نام کاربری ادمین ثبت‌کننده یا آخرین ویرایش‌کننده رکورد تردد برای نمایش در کارنامه
        protected string GetAttendanceAdminInfo(Attendance att)
        {
            int? targetAdminId = att.LastModifiedByAdminId ?? att.CreatedByAdminId;
            if (!targetAdminId.HasValue || targetAdminId.Value <= 0)
                return string.Empty;

            var admin = AdminState.AdminList?.FirstOrDefault(a => a.Id == targetAdminId.Value);
            if (admin != null && !string.IsNullOrWhiteSpace(admin.Username))
                return admin.Username;

            if (AdminState.CurrentOnlineAdmin != null && AdminState.CurrentOnlineAdmin.Id == targetAdminId.Value)
                return AdminState.CurrentOnlineAdmin.Username;

            return $"Admin #{targetAdminId.Value}";
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // همگام‌سازی تنظیمات زبان و نوع تقویم ذخیره‌شده در مرورگر
                await SyncLanguageFromStorageAsync();

                // بازیابی دسته اصلی انتخاب‌شده قبلی در فیلتر پنل پرسنل از حافظه مرورگر
                try
                {
                    var savedAdminMainCat = await LocalStorage.GetAsync<int>("AdminPanelSelectedMainCatId");
                    if (savedAdminMainCat.Success && savedAdminMainCat.Value > 0 && allCategories.Any(c => c.Level == 1 && c.Id == savedAdminMainCat.Value))
                    {
                        FilterMainCatId = savedAdminMainCat.Value;
                        await InvokeAsync(StateHasChanged);
                    }
                }
                catch (Exception ex)
                {
                    // ثبت خطای احتمالی خواندن دسته اصلی از حافظه مرورگر در سرور
                    await ErrorLogger.LogBackendErrorAsync("AdminPanel.LoadMainCategoryStorage", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
                }
            }
        }


        // متد اختصاصی برای خواندن زبان از حافظه مرورگر و اعمال اجباری روی مترجم صفحه
        private async Task SyncLanguageFromStorageAsync()
        {
            try
            {
                await LoadStoredLanguageAndCalendarAsync();

                if (CalendarManager != null && !string.IsNullOrEmpty(CurrentLanguage))
                {
                    CalendarManager.CurrentLanguage = CurrentLanguage;
                }

                if (selectedEmployee != null)
                {
                    LoadAvailableMonths(selectedEmployee.Id);
                    CalculateFilteredHours(selectedEmployee.Id);
                }
            }
            catch { }

            await InvokeAsync(StateHasChanged);
        }

        // واکنش لحظه‌ای به تغییر زبان از هدر یا بخش تنظیمات
        private async void HandleStateChanged()
        {
            await SyncLanguageFromStorageAsync();
        }

        // آزادسازی رویداد هنگام خروج از صفحه
        public void Dispose()
        {
            AdminState.OnChange -= HandleStateChanged;
        }
        protected async Task SetSystemLanguage(string lang)
        {
            CurrentLanguage = lang;
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();
            CalendarManager.CurrentLanguage = lang;

            try { await LocalStorage.SetAsync("SystemLanguageSetting", lang); } catch { }

            // اطلاع‌رسانی آنی به هدر سایت (MainLayout) جهت تغییر هم‌زمان زبان دکمه‌های نوار بالا
            AdminState?.RefreshUIState();

            if (selectedEmployee != null)
            {
                LoadAvailableMonths(selectedEmployee.Id);
                CalculateFilteredHours(selectedEmployee.Id);
            }
            await InvokeAsync(StateHasChanged);
        }

        // تنظیم نوع تقویم سیستم (شمسی، میلادی یا هجری قمری) و ذخیره ماندگار در حافظه مرورگر
        protected async Task SetCalendarType(string calendarType)
        {
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();
            CalendarManager.CalendarType = calendarType;

            try { await LocalStorage.SetAsync("SystemCalendarTypeSetting", calendarType); } catch { }

            if (selectedEmployee != null)
            {
                LoadAvailableMonths(selectedEmployee.Id);
                CalculateFilteredHours(selectedEmployee.Id);
            }
            await InvokeAsync(StateHasChanged);
        }
        // واکشی ایزوله اطلاعات پرسنل، دسته‌بندی‌های سه‌سطحی و سوابق تردد از سرویس مشترک
        private async Task RefreshDataAsync()
        {
            try
            {
                employees = await PersonnelSvc.GetEmployeesAsync(onlyVisibleInKiosk: false);
                allAttendances = await PersonnelSvc.GetAllAttendancesAsync();
                allCategories = await PersonnelSvc.GetAllCategoriesAsync();

                if (selectedEmployee != null && selectedEmployee.Id > 0)
                {
                    LoadAvailableMonths(selectedEmployee.Id);
                    CalculateFilteredHours(selectedEmployee.Id);
                }
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("AdminPanel.RefreshDataAsync", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }

        // مدیریت مودال های فرم پرسنل و منوهای آبشاری
        public void OpenAddEmployeeModal()
        {
            CloseAllMobileDropdowns();
            CancelEmpEdit();
            showEmpFormModal = true;
        }

        public void CloseEmpFormModal()
        {
            showEmpFormModal = false;
            CancelEmpEdit();
        }

        protected void OpenManualAddModal()
        {
            CancelAttEdit();
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();
            CalendarManager.SyncFormCalendarFields(DateTime.Today, DateTime.Today);
            CalendarManager.AttendanceValidationMessage = string.Empty;
            showManualAddModal = true;
        }

        protected void CloseManualAddModal()
        {
            showManualAddModal = false;
            CancelAttEdit();
        }

        protected void ToggleMobileMenuDropdown(int employeeId)
        {
            if (ActiveMobileDropdownId == employeeId) ActiveMobileDropdownId = null;
            else ActiveMobileDropdownId = employeeId;
        }

        protected void CloseAllMobileDropdowns() => ActiveMobileDropdownId = null;

        // آپلود و فشرده‌سازی تصویر پرسنل تا سقف ۲۰۰ مگابایت از طریق سرویس متمرکز ImageStorageService
        public async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            var result = await ImageStorage.SaveCompressedImageAsync(
                file: e.File,
                subFolder: "employees",
                filePrefix: "emp",
                previousImageUrl: previewImageUrl,
                currentLanguage: CurrentLanguage,
                actorAdminId: AdminState.CurrentOnlineAdmin?.Id,
                actorAdminUsername: AdminState.adminUsername
            );

            if (result.Success)
            {
                previewImageUrl = result.ImageUrl;
            }
            else if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                TriggerToast(
                    category: "AdminPanel",
                    subCategory: "EmployeeInfo",
                    messageKey: "PCal_AdminPanel_ImageUploadError",
                    args: new object[] { result.ErrorMessage },
                    isError: true
                );
            }
        }


        // اعتبارسنجی مجزای فیلدها و ذخیره یا ویرایش اطلاعات کارمند از طریق سرویس پرسنل همراه با ثبت اعلان‌ها
        protected async Task SaveEmployeeData()
        {
            // پاکسازی وضعیت خطای قبلی از روی تمام فیلدهای فرم پیش از بررسی مجدد
            EmpErrorField = string.Empty;

            // بررسی پر بودن فیلد اجباری نام و فوکوس روی آن در صورت خالی بودن
            if (string.IsNullOrWhiteSpace(newEmpName))
            {
                await SetEmpValidationErrorAsync("Name", "empNameInput", "PCal_AdminPanel_EmpNameRequired");
                return;
            }

            // بررسی پر بودن فیلد اجباری کد ملی و فوکوس روی آن در صورت خالی بودن
            if (string.IsNullOrWhiteSpace(newEmpCode))
            {
                await SetEmpValidationErrorAsync("Code", "empCodeInput", "PCal_AdminPanel_EmpCodeRequired");
                return;
            }

            // کنترل سقف مجاز ۴۰ کاراکتر برای نام و نام خانوادگی همراه با قرمز کردن کادر
            if (newEmpName.Length > 40)
            {
                await SetEmpValidationErrorAsync("Name", "empNameInput", "PCal_AdminPanel_ErrNameLength");
                return;
            }

            // کنترل بازه مجاز طول کد ملی (حداقل ۱۰ و حداکثر ۲۰ رقم) همراه با فوکوس
            if (newEmpCode.Length < 10 || newEmpCode.Length > 20)
            {
                await SetEmpValidationErrorAsync("Code", "empCodeInput", "PCal_AdminPanel_ErrCodeLength");
                return;
            }

            // کنترل سقف مجاز ۱۵ رقم برای شماره تماس در صورت وارد شدن همراه با فوکوس
            if (!string.IsNullOrWhiteSpace(newEmpPhone) && newEmpPhone.Length > 15)
            {
                await SetEmpValidationErrorAsync("Phone", "empPhoneInput", "PCal_AdminPanel_ErrPhoneLength");
                return;
            }

            try
            {
                // بررسی سطح دسترسی مدیر متناسب با حالت ویرایش یا افزودن پرسنل جدید
                if (isEmpEditMode && _editingEmployeeId.HasValue)
                {
                    if (!canEditPerm && !AdminState.isCurrentUserSuperAdmin) return;
                }
                else
                {
                    if (!canAddPerm && !AdminState.isCurrentUserSuperAdmin) return;
                }

                // بررسی تکراری نبودن کد ملی در دیتابیس از طریق سرویس پرسنل
                int? excludeId = isEmpEditMode ? _editingEmployeeId : null;
                bool isCodeDuplicate = await PersonnelSvc.IsPersonnelCodeDuplicateAsync(newEmpCode, excludeId);
                if (isCodeDuplicate)
                {
                    string dupKey = isEmpEditMode ? "PCal_AdminPanel_DuplicateCodeEdit" : "PCal_AdminPanel_DuplicateCodeAdd";
                    await SetEmpValidationErrorAsync("Code", "empCodeInput", dupKey);
                    return;
                }

                // ارسال اطلاعات فرم به سرویس پرسنل جهت ثبت رکورد جدید یا به‌روزرسانی پرونده موجود به صورت ایزوله
                var result = await PersonnelSvc.SaveOrUpdateEmployeeAsync(
                    editingId: excludeId,
                    fullName: newEmpName,
                    phone: newEmpPhone,
                    personnelCode: newEmpCode,
                    imagePath: previewImageUrl,
                    isHiddenFromKiosk: newEmpHiddenFromKiosk,
                    mainCatId: newEmpMainCatId > 0 ? newEmpMainCatId : null,
                    sub1CatId: newEmpSub1CatId > 0 ? newEmpSub1CatId : null,
                    sub2CatId: newEmpSub2CatId > 0 ? newEmpSub2CatId : null
                );

                // در صورت انجام موفق عملیات در سرویس، اعلان مناسب صادر می‌شود
                if (result.SavedEmployee != null)
                {
                    string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{newEmpName}»</span>";

                    if (isEmpEditMode)
                    {
                        // بررسی تغییر وضعیت پنهان‌سازی کارمند در کیوسک جهت صدور لاگ اختصاصی
                        if (result.VisibilityChanged)
                        {
                            string visibilityKey = newEmpHiddenFromKiosk
                                ? "PCal_AdminPanel_EmpHiddenFromKioskLog"
                                : "PCal_AdminPanel_EmpShownInKioskLog";

                            TriggerToast(
                                category: "AdminPanel",
                                subCategory: "KioskVisibility",
                                messageKey: visibilityKey,
                                args: new object[] { coloredEmpName },
                                isError: false,
                                targetEmp: result.SavedEmployee
                            );
                        }
                        else
                        {
                            TriggerToast(
                                category: "AdminPanel",
                                subCategory: "EmployeeInfo",
                                messageKey: "PCal_AdminPanel_EmpEditSuccess",
                                args: new object[] { coloredEmpName },
                                isError: false,
                                targetEmp: result.SavedEmployee
                            );
                        }
                    }
                    else
                    {
                        TriggerToast(
                            category: "AdminPanel",
                            subCategory: "EmployeeInfo",
                            messageKey: "PCal_AdminPanel_EmpAddSuccess",
                            args: new object[] { coloredEmpName },
                            isError: false,
                            targetEmp: result.SavedEmployee
                        );
                    }
                }

                // بارگذاری مجدد لیست پرسنل در جدول صفحه و بستن مودال فرم
                await RefreshDataAsync();
                CloseEmpFormModal();
            }
            catch (Exception ex)
            {
                // ثبت جزئیات فنی خطای رخ‌داده در تاریخچه اعلانات سرور جهت بررسی سوپر ادمین
                await ErrorLogger.LogBackendErrorAsync("AdminPanel.SaveEmployeeData", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
            }
        }


        // مقداردهی اولیه فیلدهای فرم برای ویرایش اطلاعات یک کارمند مشخص
        protected void StartEmpEdit(Employee emp)
        {
            ActiveMobileDropdownId = null;
            selectedEmployee = emp;
            isEmpEditMode = true;
            _editingEmployeeId = emp.Id;

            // بارگذاری مقادیر فعلی کارمند درون متغیرهای فرم
            newEmpName = emp.FullName ?? string.Empty;
            newEmpCode = emp.PersonnelCode ?? string.Empty;
            newEmpPhone = emp.Phone ?? string.Empty;
            previewImageUrl = emp.ImagePath;
            newEmpHiddenFromKiosk = emp.IsHiddenFromKiosk;
            newEmpMainCatId = emp.MainCategoryId ?? 0;
            newEmpSub1CatId = emp.SubCategory1Id ?? 0;
            newEmpSub2CatId = emp.SubCategory2Id ?? 0;
            // نمایش پنجره مودال فرم
            showEmpFormModal = true;
        }
        // بازنشانی و پاکسازی کامل متغیرهای فرم ثبت و ویرایش کارمند
        protected void CancelEmpEdit()
        {
            isEmpEditMode = false;
            _editingEmployeeId = null;
            newEmpName = string.Empty;
            newEmpPhone = string.Empty;
            newEmpCode = string.Empty;
            previewImageUrl = string.Empty;
            newEmpHiddenFromKiosk = false;
            newEmpMainCatId = 0;
            newEmpSub1CatId = 0;
            newEmpSub2CatId = 0;
            EmpErrorField = string.Empty;
        }

        protected void TriggerDeleteEmployee(int id, string name)
        {
            if (!canDeletePerm) return;
            _targetIdToDelete = id;
            _deleteType = "Employee";
            deleteModalTitle = CalendarManager.Translate("PCal_AdminPanel_DelEmpTitle");
            targetDeleteName = name;
            deleteTargetMessage = string.Format(CalendarManager.Translate("PCal_AdminPanel_DelEmpMsg"), name);
            deleteModalSubMessage = CalendarManager.Translate("PCal_AdminPanel_DelEmpSubMsg");
            ActiveMobileDropdownId = null;
            showDeleteModal = true;
        }
        // ثبت حضور فوری کارمند از طریق سرویس پرسنل در صورت داشتن مجوز ویرایش
        protected async Task ForcePresent(int empId)
        {
            // بررسی سطح دسترسی ادمین برای ویرایش تردد پرسنل
            if (!canEditPerm) return;

            // ثبت رکورد ورود جدید با منبع مدیریتی در دیتابیس از طریق سرویس پرسنل
            await PersonnelSvc.RecordClockInAsync(
                employeeId: empId,
                clockInTime: DateTime.Now,
                entrySource: "AdminFast",
                coordinates: null,
                locationName: null,
                adminId: AdminState.CurrentOnlineAdmin?.Id
            );

            // بارگذاری مجدد داده‌های جدول جهت نمایش وضعیت جدید کارمند
            await RefreshDataAsync();
        }

        // ثبت سریع ورود کارمند از جدول پرسنل از طریق سرویس پرسنل همراه با ثبت بی‌صدای مختصات جغرافیایی ادمین
        protected async Task StartAttendanceNow(int employeeId)
        {
            // کنترل مجوز ویرایش تردد ادمین آنلاین
            if (!canEditPerm) return;

            try
            {
                // دریافت شناسه، نام کاربری ادمین فعلی و زمان دقیق سرور
                int? currentAdminId = AdminState.CurrentOnlineAdmin?.Id;
                string currentAdminName = AdminState.CurrentOnlineAdmin?.Username ?? "Admin";
                var now = DateTime.Now;

                // دریافت مختصات جغرافیایی لحظه‌ای دستگاه ادمین از مرورگر
                var adminLoc = await GeoService.GetCurrentDeviceLocationAsync();

                // ارسال اطلاعات به سرویس پرسنل جهت ثبت ایزوله رکورد ورود سریع در دیتابیس
                var result = await PersonnelSvc.RecordClockInAsync(
                    employeeId: employeeId,
                    clockInTime: now,
                    entrySource: "AdminFast",
                    coordinates: adminLoc.FormattedCoordinates,
                    locationName: null,
                    adminId: currentAdminId
                );

                // در صورت وجود تردد باز قبلی برای کارمند، پیام هشدار نمایش داده می‌شود
                if (!result.Success)
                {
                    var existingEmp = employees.FirstOrDefault(e => e.Id == employeeId);
                    TriggerToast(
                        category: "AdminPanel",
                        subCategory: "QuickAttendance",
                        messageKey: "PCal_MainHome_AlreadyHasOpenRecord",
                        isError: true,
                        targetEmp: existingEmp
                    );
                    return;
                }

                // استخراج نام کارمند و قالب‌بندی رنگی آن جهت نمایش در بنر اعلان
                var targetEmp = employees.FirstOrDefault(e => e.Id == employeeId);
                string empName = targetEmp?.FullName ?? string.Empty;
                string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";

                // نمایش اعلان شیشه‌ای ساده ثبت ورود سریع به ادمین بدون افشای مختصات روی صفحه
                TriggerToast(
                    category: "AdminPanel",
                    subCategory: "QuickAttendance",
                    messageKey: "PCal_AdminPanel_FastInSuccess",
                    args: new object[] { coloredEmpName },
                    isError: false,
                    targetEmp: targetEmp,
                    eventDate: now.ToString("yyyy-MM-dd"),
                    eventTime: now.ToString("HH:mm:ss")
                );

                // ثبت بی‌صدای مختصات مکانی ادمین در تاریخچه اعلانات سرور با زیردسته AdminLocation
                if (adminLoc.Success && !string.IsNullOrEmpty(adminLoc.FormattedCoordinates))
                {
                    await GeoService.LogLocationAuditSilentlyAsync(
                        subCategory: "AdminLocation",
                        messageKey: "PCal_Loc_AdminFastInLocLog",
                        args: new object[] { coloredEmpName, adminLoc.FormattedCoordinates },
                        actorAdminId: currentAdminId,
                        actorAdminUsername: currentAdminName,
                        targetEmp: targetEmp,
                        eventDate: now.ToString("yyyy-MM-dd"),
                        eventTime: now.ToString("HH:mm:ss")
                    );
                }

                // بارگذاری مجدد داده‌های جدول پرسنل
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // ثبت جزئیات فنی خطا در لاگر مرکزی و نمایش پیام خطای ساده در رابط کاربری
                await ErrorLogger.LogBackendErrorAsync("AdminPanel.StartAttendanceNow", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
                TriggerToast("AdminPanel", "QuickAttendance", "PCal_AdminPanel_FastInError", new object[] { ex.Message }, isError: true);
            }

            await InvokeAsync(StateHasChanged);
        }

        // ثبت سریع خروج کارمند از جدول پرسنل از طریق سرویس پرسنل همراه با ثبت بی‌صدای مختصات جغرافیایی ادمین
        protected async Task StopAttendanceNow(int attendanceId)
        {
            // کنترل مجوز ویرایش تردد ادمین آنلاین
            if (!canEditPerm) return;

            try
            {
                // دریافت شناسه، نام کاربری ادمین فعلی و زمان دقیق سرور
                int? currentAdminId = AdminState.CurrentOnlineAdmin?.Id;
                string currentAdminName = AdminState.CurrentOnlineAdmin?.Username ?? "Admin";
                var now = DateTime.Now;

                // دریافت مختصات جغرافیایی لحظه‌ای دستگاه ادمین از مرورگر
                var adminLoc = await GeoService.GetCurrentDeviceLocationAsync();

                // ارسال دستور ثبت خروج به سرویس پرسنل جهت به‌روزرسانی ایزوله رکورد تردد باز
                var updatedAttendance = await PersonnelSvc.RecordClockOutAsync(
                    attendanceId: attendanceId,
                    clockOutTime: now,
                    coordinates: adminLoc.FormattedCoordinates,
                    adminId: currentAdminId
                );

                // در صورت یافتن و به‌روزرسانی موفق رکورد تردد، اعلان‌ها صادر می‌شوند
                if (updatedAttendance != null)
                {
                    var targetEmp = employees.FirstOrDefault(e => e.Id == updatedAttendance.EmployeeId);
                    string empName = targetEmp?.FullName ?? string.Empty;
                    string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";

                    // نمایش اعلان شیشه‌ای ساده ثبت خروج سریع به ادمین بدون افشای مختصات روی صفحه
                    TriggerToast(
                        category: "AdminPanel",
                        subCategory: "QuickAttendance",
                        messageKey: "PCal_AdminPanel_FastOutSuccess",
                        args: new object[] { coloredEmpName },
                        isError: false,
                        targetEmp: targetEmp,
                        eventDate: now.ToString("yyyy-MM-dd"),
                        eventTime: now.ToString("HH:mm:ss")
                    );

                    // ثبت بی‌صدای مختصات مکانی ادمین در تاریخچه اعلانات سرور با زیردسته AdminLocation
                    if (adminLoc.Success && !string.IsNullOrEmpty(adminLoc.FormattedCoordinates))
                    {
                        await GeoService.LogLocationAuditSilentlyAsync(
                            subCategory: "AdminLocation",
                            messageKey: "PCal_Loc_AdminFastOutLocLog",
                            args: new object[] { coloredEmpName, adminLoc.FormattedCoordinates },
                            actorAdminId: currentAdminId,
                            actorAdminUsername: currentAdminName,
                            targetEmp: targetEmp,
                            eventDate: now.ToString("yyyy-MM-dd"),
                            eventTime: now.ToString("HH:mm:ss")
                        );
                    }

                    // بارگذاری مجدد داده‌های جدول پرسنل
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // ثبت جزئیات فنی خطا در لاگر مرکزی و نمایش پیام خطای ساده در رابط کاربری
                await ErrorLogger.LogBackendErrorAsync("AdminPanel.StopAttendanceNow", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername, null, CurrentLanguage);
                TriggerToast("AdminPanel", "QuickAttendance", "PCal_AdminPanel_FastOutError", new object[] { ex.Message }, isError: true);
            }

            await InvokeAsync(StateHasChanged);
        }

        // ثبت دستی یا ویرایش ساعت کاری پرسنل از طریق سرویس پرسنل همراه با ذخیره تاریخچه تغییرات ادمین
        protected async Task AddManualAttendance()
        {
            // بررسی سطح دسترسی مدیر و انتخاب شدن کارمند هدف
            if (!canEditPerm) return;
            if (selectedEmployee == null) return;

            // اطمینان از مقداردهی اولیه شیء مدیریت تقویم
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            try
            {
                // تبدیل فیلدهای سال، ماه و روز انتخاب شده در فرم به تاریخ استاندارد میلادی
                manualDate = CalendarManager.ParseFormFieldsToDateTime(CalendarManager.ManualInYear, CalendarManager.ManualInMonth, CalendarManager.ManualInDay, "00:00");
                manualOutDate = CalendarManager.ParseFormFieldsToDateTime(CalendarManager.ManualOutYear, CalendarManager.ManualOutMonth, CalendarManager.ManualOutDay, "00:00");

                // ترکیب تاریخ و ساعت وارد شده برای ایجاد زمان دقیق ورود و خروج
                var inTime = DateTime.Parse($"{manualDate:yyyy-MM-dd} {manualInTime}");
                var outTime = DateTime.Parse($"{manualOutDate:yyyy-MM-dd} {manualOutTime}");

                // ۱. بررسی یکسان بودن تاریخ روز ورود و روز خروج
                if (inTime.Date != outTime.Date)
                {
                    CalendarManager.AttendanceValidationMessage = CalendarManager.Translate("PCal_AdminPanel_ManualDateMismatch");

                    // ثبت خطای عدم تطابق تاریخ در تاریخچه اعلان‌های سرور با زیردسته ReportCard
                    TriggerToast(
                        category: "AdminPanel",
                        subCategory: "ReportCard",
                        messageKey: "PCal_AdminPanel_ManualDateMismatch",
                        isError: true,
                        targetEmp: selectedEmployee
                    );
                    return;
                }

                // ۲. جلوگیری از ثبت ساعت خروج زودتر از ساعت ورود
                if (outTime < inTime)
                {
                    CalendarManager.AttendanceValidationMessage = CalendarManager.Translate("PCal_AdminPanel_ManualTimeOrderError");

                    // ثبت خطای ترتیب ساعت در تاریخچه اعلان‌های سرور با زیردسته ReportCard
                    TriggerToast(
                        category: "AdminPanel",
                        subCategory: "ReportCard",
                        messageKey: "PCal_AdminPanel_ManualTimeOrderError",
                        isError: true,
                        targetEmp: selectedEmployee
                    );
                    return;
                }

                // دریافت مشخصات ادمین آنلاین جهت ثبت در تاریخچه و لاگ سیستم
                int? currentAdminId = AdminState.CurrentOnlineAdmin?.Id;
                string currentAdminName = AdminState.CurrentOnlineAdmin?.Username ?? "Admin";

                // ارسال اطلاعات به سرویس پرسنل جهت ثبت یا ویرایش ایزوله تردد همراه با لاگ ممیزی
                int? editId = isAttEditMode ? _editingAttendanceId : null;
                bool saved = await PersonnelSvc.SaveOrUpdateManualAttendanceAsync(
                    editingAttendanceId: editId,
                    employeeId: selectedEmployee.Id,
                    inTime: inTime,
                    outTime: outTime,
                    adminId: currentAdminId,
                    adminUsername: currentAdminName,
                    auditReason: CalendarManager.Translate("PCal_AdminPanel_AuditEditReason")
                );

                if (saved)
                {
                    // قالب‌بندی نام کارمند با رنگ قرمز و انتخاب کلید ترجمه مناسب برای ویرایش یا افزودن
                    string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{selectedEmployee.FullName}»</span>";
                    string msgKey = isAttEditMode ? "PCal_AdminPanel_ManualEditSuccess" : "PCal_AdminPanel_ManualAddSuccess";

                    // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته ReportCard و زمان رویداد
                    TriggerToast(
                        category: "AdminPanel",
                        subCategory: "ReportCard",
                        messageKey: msgKey,
                        args: new object[] { coloredEmpName },
                        isError: false,
                        targetEmp: selectedEmployee,
                        eventDate: inTime.ToString("yyyy-MM-dd"),
                        eventTime: $"{inTime:HH:mm} - {outTime:HH:mm}"
                    );
                }

                // بستن مودال ثبت دستی، بازمحاسبه ساعات کارنامه و بازنشانی فرم
                showManualAddModal = false;
                await RefreshDataAsync();
                CalculateFilteredHours(selectedEmployee.Id);
                CancelAttEdit();
            }
            catch
            {
                // ۳. نمایش خطا در صورت نامعتبر بودن فرمت ساعت تایپ شده
                CalendarManager.AttendanceValidationMessage = CalendarManager.Translate("PCal_AdminPanel_InvalidTimeFormat");

                // ثبت خطای فرمت زمان در تاریخچه اعلان‌های سرور با زیردسته ReportCard
                TriggerToast(
                    category: "AdminPanel",
                    subCategory: "ReportCard",
                    messageKey: "PCal_AdminPanel_InvalidTimeFormat",
                    isError: true,
                    targetEmp: selectedEmployee
                );
            }
        }

        protected void CloseModal() { showModal = false; selectedEmployee = null; }
        protected void ClearSystemMessage() => SystemMessage = string.Empty;

        protected string GetSingleDuration(Attendance att)
        {
            if (att.ClockInTime == null || att.ClockOutTime == null)
                return CalendarManager.Translate("PCal_AdminPanel_ActiveAttendance");

            var diff = att.ClockOutTime.Value - att.ClockInTime.Value;
            return $"{(int)diff.TotalHours:00}:{diff.Minutes:00}";
        }

        protected void StartAttEdit(Attendance att)
        {
            isAttEditMode = true;
            _editingAttendanceId = att.Id;
            manualInTime = att.ClockInTime.HasValue ? att.ClockInTime.Value.ToString("HH:mm") : "07:00";
            manualOutTime = att.ClockOutTime.HasValue ? att.ClockOutTime.Value.ToString("HH:mm") : "17:30";

            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            CalendarManager.SyncFormCalendarFields(att.ClockInTime ?? DateTime.Today, att.ClockOutTime ?? DateTime.Today);
            CalendarManager.AttendanceValidationMessage = string.Empty;

            showManualAddModal = true;
        }

        protected void CancelAttEdit()
        {
            isAttEditMode = false;
            _editingAttendanceId = null;
            manualInTime = "07:00";
            manualOutTime = "17:30";

            if (CalendarManager != null)
            {
                CalendarManager.SyncFormCalendarFields(DateTime.Today, DateTime.Today);
                CalendarManager.AttendanceValidationMessage = string.Empty;
            }
        }

        // واکشی لیست ماه‌های دارای سابقه تردد برای پرسنل انتخاب‌شده بر اساس نوع تقویم فعال (شمسی، قمری یا میلادی)
        private void LoadAvailableMonths(int employeeId)
        {
            AvailableMonths.Clear();

            // افزودن گزینه پیش‌فرض نمایش تمامی ماه‌ها در ابتدای لیست
            AvailableMonths.Add(new MonthOption
            {
                Value = "ALL",
                Text = CalendarManager.Translate("PCal_AdminPanel_AllMonths")
            });

            // فیلتر کردن رکوردهای تردد دارای تاریخ ورود معتبر برای کارمند انتخاب‌شده
            var allRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue).ToList();

            // استخراج سال و ماه یکتا بر اساس تقویم هجری شمسی
            if (CalendarManager.CalendarType == "Persian")
            {
                System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
                var pDates = allRecords.Select(a => new
                {
                    Year = pc.GetYear(a.ClockInTime!.Value),
                    Month = pc.GetMonth(a.ClockInTime!.Value)
                }).Distinct().OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).ToList();

                foreach (var d in pDates)
                {
                    string monthText = CalendarManager.GetDynamicMonthText(d.Year, d.Month);
                    AvailableMonths.Add(new MonthOption { Value = $"{d.Year}-{d.Month}", Text = monthText });
                }
            }
            // استخراج سال و ماه یکتا بر اساس تقویم هجری قمری
            else if (CalendarManager.CalendarType == "Hijri")
            {
                System.Globalization.HijriCalendar hc = new System.Globalization.HijriCalendar();
                var hDates = allRecords.Select(a => new
                {
                    Year = hc.GetYear(a.ClockInTime!.Value),
                    Month = hc.GetMonth(a.ClockInTime!.Value)
                }).Distinct().OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).ToList();

                foreach (var d in hDates)
                {
                    string monthText = CalendarManager.GetDynamicMonthText(d.Year, d.Month);
                    AvailableMonths.Add(new MonthOption { Value = $"{d.Year}-{d.Month}", Text = monthText });
                }
            }
            // استخراج سال و ماه یکتا بر اساس تقویم میلادی
            else
            {
                var mDates = allRecords.Select(a => new
                {
                    Year = a.ClockInTime!.Value.Year,
                    Month = a.ClockInTime!.Value.Month
                }).Distinct().OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).ToList();

                foreach (var d in mDates)
                {
                    string monthText = CalendarManager.GetDynamicMonthText(d.Year, d.Month);
                    AvailableMonths.Add(new MonthOption { Value = $"{d.Year}-{d.Month}", Text = monthText });
                }
            }

            // تنظیم حالت پیش‌فرض روی نمایش همه ماه‌ها در صورت خالی بودن فیلتر
            if (string.IsNullOrEmpty(SelectedMonthFilter)) SelectedMonthFilter = "ALL";
        }

        protected void OnMonthSelectorChanged(ChangeEventArgs e)
        {
            SelectedMonthFilter = e.Value?.ToString() ?? string.Empty;
            if (selectedEmployee != null) CalculateFilteredHours(selectedEmployee.Id);
        }

        protected async Task HandleMobileActionChanged(string actionType, Employee emp, int activeAttendanceId)
        {
            if (string.IsNullOrEmpty(actionType)) return;
            switch (actionType)
            {
                case "REPORT":
                    OpenReportModal(emp);
                    CloseAllMobileDropdowns();
                    break;
                case "START_ATT":
                    if (canEditPerm) await ForcePresent(emp.Id);
                    break;
                case "STOP_ATT":
                    if (canEditPerm && activeAttendanceId > 0) await StopAttendanceNow(activeAttendanceId);
                    break;
                case "EDIT_EMP":
                    if (canEditPerm) { StartEmpEdit(emp); CloseAllMobileDropdowns(); }
                    break;
                case "DELETE_EMP":
                    if (canDeletePerm) { TriggerDeleteEmployee(emp.Id, emp.FullName); CloseAllMobileDropdowns(); }
                    break;
            }
            await InvokeAsync(StateHasChanged);
        }

        protected void OpenReportModal(Employee emp)
        {
            selectedEmployee = emp;
            showModal = true;
            LoadAvailableMonths(emp.Id);
            CalculateFilteredHours(emp.Id);
        }

        protected void TriggerDeleteTimeRecord(Attendance att)
        {
            if (att == null) return;
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            _targetIdToDelete = att.Id;
            _deleteType = "Attendance";
            deleteModalTitle = CalendarManager.Translate("PCal_AdminPanel_DelAttTitle");

            string dateStr = CalendarManager.GetDynamicFormattedDate(att.ClockInTime);
            string timeStr = att.ClockInTime.HasValue ? att.ClockInTime.Value.ToString("HH:mm") : "00:00";

            targetDeleteName = string.Format(CalendarManager.Translate("PCal_AdminPanel_DelAttTargetName"), dateStr, timeStr);
            deleteTargetMessage = string.Format(CalendarManager.Translate("PCal_AdminPanel_DelAttMsg"), dateStr);
            deleteModalSubMessage = CalendarManager.Translate("PCal_AdminPanel_DelAttSubMsg");
            showDeleteModal = true;
        }
        protected void CancelDeleteModal() => showDeleteModal = false;
        // اجرای عملیات حذف تایید شده (پرونده پرسنل یا رکورد تردد) از طریق سرویس پرسنل و پاکسازی فایل تصویر
        protected async Task ExecuteConfirmedDelete()
        {
            // بستن پنجره مودال تاییدیه حذف
            showDeleteModal = false;

            // بررسی معتبر بودن شناسه هدف
            if (_targetIdToDelete <= 0) return;

            try
            {
                // حالت اول: حذف کامل پرونده یک کارمند، کلیه ترددهای او و فایل تصویر پروفایل
                if (_deleteType == "Employee")
                {
                    var deletedEmployee = await PersonnelSvc.DeleteEmployeeWithAttendancesAsync(_targetIdToDelete);
                    if (deletedEmployee != null)
                    {
                        // پاکسازی فایل تصویر کارمند از پوشه wwwroot سرور جهت جلوگیری از انباشت فایل‌ها
                        ImageStorage.DeleteImageFile(deletedEmployee.ImagePath);

                        // قالب‌بندی نام کارمند با رنگ قرمز و فونت ضخیم جهت نمایش در اعلان بالای صفحه
                        string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{deletedEmployee.FullName}»</span>";

                        // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته EmployeeInfo
                        TriggerToast(
                            category: "AdminPanel",
                            subCategory: "EmployeeInfo",
                            messageKey: "PCal_AdminPanel_EmpDeletedMsg",
                            args: new object[] { coloredEmpName },
                            isError: false,
                            targetEmp: deletedEmployee
                        );
                    }
                }
                // حالت دوم: حذف یک رکورد تردد خاص از کارنامه کارمند
                else if (_deleteType == "Attendance")
                {
                    var deletedAtt = await PersonnelSvc.DeleteAttendanceRecordAsync(_targetIdToDelete);
                    if (deletedAtt != null)
                    {
                        // واکشی شیء کارمند مرتبط با این تردد
                        var targetEmp = selectedEmployee ?? employees.FirstOrDefault(e => e.Id == deletedAtt.EmployeeId);
                        string empName = targetEmp?.FullName ?? string.Empty;

                        // قالب‌بندی نام کارمند با رنگ قرمز و فونت ضخیم جهت نمایش در اعلان
                        string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";

                        // استخراج تاریخ و ساعت رکورد تردد پیش از حذف جهت ثبت در لاگ رویداد
                        string? evDate = deletedAtt.ClockInTime?.ToString("yyyy-MM-dd");
                        string? evTime = deletedAtt.ClockInTime?.ToString("HH:mm");

                        // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته ReportCard
                        TriggerToast(
                            category: "AdminPanel",
                            subCategory: "ReportCard",
                            messageKey: "PCal_AdminPanel_AttDeletedMsg",
                            args: new object[] { coloredEmpName },
                            isError: false,
                            targetEmp: targetEmp,
                            eventDate: evDate,
                            eventTime: evTime
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                // نمایش پیغام خطا در صورت بروز مشکل در عملیات حذف
                string subCat = _deleteType == "Employee" ? "EmployeeInfo" : "ReportCard";
                TriggerToast("AdminPanel", subCat, "PCal_AdminPanel_DeleteError", new object[] { ex.Message }, isError: true);
            }

            // بازمحاسبه ساعات کاری ماهانه در صورت حذف شدن یک تردد از داخل کارنامه
            if (selectedEmployee != null && _deleteType == "Attendance") CalculateFilteredHours(selectedEmployee.Id);

            // به‌روزرسانی لیست‌های صفحه و بازرندر رابط کاربری
            await RefreshDataAsync();
            await InvokeAsync(StateHasChanged);
        }

        // محاسبه تفکیک شده ساعات موظفی و اضافه کاری در بازه ماهانه انتخاب‌شده با پشتیبانی از تقویم‌های شمسی، قمری و میلادی
        private void CalculateFilteredHours(int employeeId)
        {
            var allRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue).ToList();
            List<Attendance> monthlyRecords = new List<Attendance>();

            // اعمال فیلتر ماه و سال بر اساس تقویم انتخاب‌شده در تنظیمات سیستم
            if (!string.IsNullOrEmpty(SelectedMonthFilter) && SelectedMonthFilter != "ALL")
            {
                var parts = SelectedMonthFilter.Split('-');
                int targetYear = int.Parse(parts[0]);
                int targetMonth = int.Parse(parts[1]);

                // جداسازی رکوردهای ماه هدف در تقویم شمسی
                if (CalendarManager.CalendarType == "Persian")
                {
                    System.Globalization.PersianCalendar pc = new System.Globalization.PersianCalendar();
                    monthlyRecords = allRecords.Where(a => pc.GetYear(a.ClockInTime!.Value) == targetYear
                        && pc.GetMonth(a.ClockInTime!.Value) == targetMonth).ToList();
                }
                // جداسازی رکوردهای ماه هدف در تقویم هجری قمری
                else if (CalendarManager.CalendarType == "Hijri")
                {
                    System.Globalization.HijriCalendar hc = new System.Globalization.HijriCalendar();
                    monthlyRecords = allRecords.Where(a => hc.GetYear(a.ClockInTime!.Value) == targetYear
                        && hc.GetMonth(a.ClockInTime!.Value) == targetMonth).ToList();
                }
                // جداسازی رکوردهای ماه هدف در تقویم میلادی
                else
                {
                    monthlyRecords = allRecords.Where(a => a.ClockInTime!.Value.Year == targetYear
                        && a.ClockInTime!.Value.Month == targetMonth).ToList();
                }
            }
            else
            {
                monthlyRecords = allRecords;
            }

            double totalRegularMinutes = 0;
            double totalOvertimeMinutes = 0;
            double totalDeficitMinutes = 0;

            // دریافت قوانین ساعت کاری و روزهای تعطیل از تنظیمات سرور
            var settings = AdminState.CurrentSettings;
            string startTimeStr = settings?.RegularStartTime ?? "07:00";
            string endTimeStr = settings?.RegularEndTime ?? "17:30";
            string offDaysStr = settings?.OffDaysOfWeek ?? "Saturday,Sunday";
            int thresholdMinutes = settings?.DeficitThresholdMinutes ?? 15;

            TimeSpan regStart = TimeSpan.Parse(startTimeStr);
            TimeSpan regEnd = TimeSpan.Parse(endTimeStr);
            double maxExpectedRegularMinutes = (regEnd - regStart).TotalMinutes;
            var offDaysList = offDaysStr.Split(',').ToList();

            // پیمایش رکوردهای ماهانه جهت محاسبه دقیق دقایق موظفی، اضافه کاری و کسری کار
            foreach (var rec in monthlyRecords)
            {
                DateTime clockIn = rec.ClockInTime!.Value;
                DateTime clockOut = rec.ClockOutTime ?? DateTime.Now;
                double totalDuration = (clockOut - clockIn).TotalMinutes;

                // نادیده گرفتن ردیف‌های با کارکرد صفر در محاسبات
                if (totalDuration <= 0)
                {
                    continue;
                }

                // احتساب کل زمان کارکرد روزهای تعطیل هفته به عنوان ۱۰۰٪ اضافه کاری
                if (offDaysList.Contains(clockIn.DayOfWeek.ToString()))
                {
                    totalOvertimeMinutes += totalDuration;
                    continue;
                }

                DateTime regularStartThreshold = clockIn.Date.Add(regStart);
                DateTime regularEndThreshold = clockIn.Date.Add(regEnd);

                // محاسبه اضافه کاری پیش از شروع رسمی شیفت
                if (clockIn < regularStartThreshold)
                {
                    var endOfEarlyOvertime = clockOut < regularStartThreshold ? clockOut : regularStartThreshold;
                    totalOvertimeMinutes += (endOfEarlyOvertime - clockIn).TotalMinutes;
                }

                // محاسبه کارکرد عادی در بازه مجاز ساعت موظفی
                DateTime effectiveIn = clockIn < regularStartThreshold ? regularStartThreshold : clockIn;
                DateTime effectiveOut = clockOut > regularEndThreshold ? regularEndThreshold : clockOut;
                double dayRegularMinutes = 0;
                if (effectiveIn < effectiveOut)
                {
                    dayRegularMinutes = (effectiveOut - effectiveIn).TotalMinutes;
                    totalRegularMinutes += dayRegularMinutes;
                }

                // محاسبه کسری کار تنها برای روزهایی که کارکرد واقعی انجام شده است
                if (rec.ClockOutTime.HasValue && dayRegularMinutes > 0 && dayRegularMinutes < maxExpectedRegularMinutes)
                {
                    totalDeficitMinutes += (maxExpectedRegularMinutes - dayRegularMinutes);
                }

                // محاسبه اضافه کاری پس از پایان رسمی شیفت
                if (clockOut > regularEndThreshold)
                {
                    DateTime effectiveOvertimeStart = clockIn > regularEndThreshold ? clockIn : regularEndThreshold;
                    totalOvertimeMinutes += (clockOut - effectiveOvertimeStart).TotalMinutes;
                }
            }

            // اعمال تهاتر کسری کار از اضافه کاری در صورت رسیدن مجموع کسری به آستانه تعیین‌شده
            if (totalDeficitMinutes >= thresholdMinutes)
            {
                double compensation = Math.Min(totalOvertimeMinutes, totalDeficitMinutes);
                totalOvertimeMinutes -= compensation;
                totalRegularMinutes += compensation;
            }

            // قالب‌بندی خروجی نهایی ساعات موظفی و اضافه کاری به فرمت ساعت و دقیقه
            int regHours = (int)(totalRegularMinutes / 60);
            int regMins = (int)(totalRegularMinutes % 60);
            TotalRegularHours = $"{regHours:00}:{regMins:00}";

            int overHours = (int)(totalOvertimeMinutes / 60);
            int overMins = (int)(totalOvertimeMinutes % 60);
            TotalOvertimeHours = $"{overHours:00}:{overMins:00}";
        }
        protected void OpenSettingsModal()
        {
            if (!canEditPerm || !AdminState.isCurrentUserSuperAdmin) return;
            CloseAllMobileDropdowns();
            var current = AdminState.CurrentSettings;
            settingsStartTime = current.RegularStartTime;
            settingsEndTime = current.RegularEndTime;
            selectedOffDays = current.OffDaysOfWeek.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            showSettingsModal = true;
        }
        protected async Task SaveSettingsData()
        {
            bool success = await AdminState.SaveWorkSettingsAsync(settingsStartTime, settingsEndTime, selectedOffDays);
            if (success)
            {
                showSettingsModal = false;
                TriggerToast(
                    category: "AdminPanel",
                    subCategory: "Settings",
                    messageKey: "PCal_AdminPanel_SettingsSavedSuccess",
                    isError: false
                );
            }
            await InvokeAsync(StateHasChanged);
        }

        protected void ToggleOffDaySelection(string dayName, object? checkedValue)
        {
            bool isChecked = checkedValue is bool b && b;
            if (isChecked)
            {
                if (!selectedOffDays.Contains(dayName)) selectedOffDays.Add(dayName);
            }
            else
            {
                if (selectedOffDays.Contains(dayName)) selectedOffDays.Remove(dayName);
            }
        }
    }
}