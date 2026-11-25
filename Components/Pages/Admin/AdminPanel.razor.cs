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
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public GeoLocationService GeoService { get; set; } = default!;
        [Inject] public PersonnelService PersonnelSvc { get; set; } = default!;
        [Inject] public Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;


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
        // تغییر فیلتر کتگوری اصلی جدول پرسنل و بازنشانی فرزندان
        protected void OnFilterMainCatChanged(ChangeEventArgs e)
        {
            FilterMainCatId = int.TryParse(e.Value?.ToString(), out int id) ? id : 0;
            FilterSub1CatId = 0;
            FilterSub2CatId = 0;
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await SyncLanguageFromStorageAsync();
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
        private async Task RefreshDataAsync()
        {
            if (DbContext == null) return;
            try
            {
                employees = await DbContext.Employees.AsNoTracking().ToListAsync() ?? new();
                allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync() ?? new();

                if (selectedEmployee != null && selectedEmployee.Id > 0)
                {
                    LoadAvailableMonths(selectedEmployee.Id);
                    CalculateFilteredHours(selectedEmployee.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RefreshDataAsync: {ex.Message}");
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

        // آپلود و فشرده سازی تصویر پرسنلی
        public async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    var highQualFile = await file.RequestImageFileAsync("image/jpeg", 500, 500);
                    var buffer = new byte[highQualFile.Size];
                    using (var stream = highQualFile.OpenReadStream(5120000))
                    {
                        int bytesRead = 0;
                        while (bytesRead < buffer.Length)
                        {
                            int read = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                            if (read == 0) break;
                            bytesRead += read;
                        }
                    }
                    previewImageUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(buffer)}";
                }
            }
            catch (Exception ex)
            {
                TriggerToast(
                    category: "AdminPanel",
                    subCategory: "EmployeeInfo",
                    messageKey: "PCal_AdminPanel_ImageUploadError",
                    args: new object[] { ex.Message },
                    isError: true
                );
            }
        }

        // اعتبارسنجی مجزای فیلدها و ذخیره یا ویرایش اطلاعات کارمند در دیتابیس همراه با ثبت در تاریخچه اعلان‌ها
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
                // ساخت نام برجسته و رنگی کارمند برای نمایش در بنر اعلان بالای صفحه
                string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{newEmpName}»</span>";

                // حالت اول: بررسی حالت ویرایش کارمند موجود
                if (isEmpEditMode && _editingEmployeeId.HasValue)
                {
                    // کنترل سطح دسترسی مدیر برای انجام عملیات ویرایش
                    if (!canEditPerm && !AdminState.isCurrentUserSuperAdmin) return;

                    // جلوگیری از ثبت کد ملی تکراری متعلق به کارمند دیگر و فوکوس روی فیلد کد ملی
                    bool isCodeDuplicate = await DbContext.Employees.AnyAsync(e => e.PersonnelCode == newEmpCode && e.Id != _editingEmployeeId.Value);
                    if (isCodeDuplicate)
                    {
                        await SetEmpValidationErrorAsync("Code", "empCodeInput", "PCal_AdminPanel_DuplicateCodeEdit");
                        return;
                    }

                    // واکشی رکورد کارمند از دیتابیس برای اعمال تغییرات
                    var emp = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _editingEmployeeId.Value);
                    if (emp != null)
                    {
                        // نگهداری وضعیت قبلی نمایش کارمند در کیوسک جهت تشخیص تغییر وضعیت پنهان‌سازی
                        bool wasHidden = emp.IsHiddenFromKiosk;

                        // به‌روزرسانی فیلدهای متنی، تصویر و وضعیت نمایش در کیوسک
                        emp.FullName = newEmpName;
                        emp.Phone = newEmpPhone;
                        emp.PersonnelCode = newEmpCode;
                        emp.ImagePath = previewImageUrl;
                        emp.IsHiddenFromKiosk = newEmpHiddenFromKiosk;

                        // ثبت تغییرات در بافت دیتابیس و ذخیره نهایی در فایل SQLite
                        DbContext.Employees.Update(emp);
                        await DbContext.SaveChangesAsync();

                        // ثبت تغییر وضعیت نمایش یا عدم نمایش کارمند در صفحه کیوسک در صورت تغییر وضعیت تیک مربوطه
                        if (wasHidden != newEmpHiddenFromKiosk)
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
                                targetEmp: emp
                            );
                        }
                        else
                        {
                            // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته EmployeeInfo
                            TriggerToast(
                                category: "AdminPanel",
                                subCategory: "EmployeeInfo",
                                messageKey: "PCal_AdminPanel_EmpEditSuccess",
                                args: new object[] { coloredEmpName },
                                isError: false,
                                targetEmp: emp
                            );
                        }
                    }
                }
                // حالت دوم: ثبت کارمند جدید در سیستم
                else
                {
                    // کنترل سطح دسترسی مدیر برای افزودن کارمند جدید
                    if (!canAddPerm && !AdminState.isCurrentUserSuperAdmin) return;

                    // جلوگیری از ثبت کد ملی تکراری در سیستم و فوکوس روی فیلد کد ملی
                    bool isCodeDuplicate = await DbContext.Employees.AnyAsync(e => e.PersonnelCode == newEmpCode);
                    if (isCodeDuplicate)
                    {
                        await SetEmpValidationErrorAsync("Code", "empCodeInput", "PCal_AdminPanel_DuplicateCodeAdd");
                        return;
                    }

                    // ایجاد شیء کارمند جدید همراه با وضعیت نمایش در کیوسک
                    var newEmp = new Employee
                    {
                        FullName = newEmpName,
                        Phone = newEmpPhone,
                        PersonnelCode = newEmpCode,
                        ImagePath = previewImageUrl,
                        IsHiddenFromKiosk = newEmpHiddenFromKiosk
                    };

                    // افزودن رکورد جدید به جدول دیتابیس و ذخیره نهایی
                    DbContext.Employees.Add(newEmp);
                    await DbContext.SaveChangesAsync();

                    // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته EmployeeInfo
                    TriggerToast(
                        category: "AdminPanel",
                        subCategory: "EmployeeInfo",
                        messageKey: "PCal_AdminPanel_EmpAddSuccess",
                        args: new object[] { coloredEmpName },
                        isError: false,
                        targetEmp: newEmp
                    );
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

        protected async Task ForcePresent(int empId)
        {
            if (!canEditPerm) return;
            DbContext.Attendances.Add(new Attendance { EmployeeId = empId, ClockInTime = DateTime.Now });
            await DbContext.SaveChangesAsync();
            await RefreshDataAsync();
        }

        // ثبت سریع ورود کارمند از جدول اصلی پنل همراه با ثبت نام کارمند در بنر اعلان و تاریخچه سرور
        protected async Task StartAttendanceNow(int employeeId)
        {
            // بررسی سطح دسترسی مدیر برای انجام عملیات ویرایش و ثبت تردد
            if (!canEditPerm) return;

            try
            {
                // دریافت شناسه ادمین آنلاین و زمان دقیق فعلی جهت ثبت در تاریخچه رکورد
                int? currentAdminId = AdminState.CurrentOnlineAdmin?.Id;
                var now = DateTime.Now;

                // ایجاد رکورد تردد جدید با منبع ثبت سریع ادمین
                var attendance = new Attendance
                {
                    EmployeeId = employeeId,
                    ClockInTime = now,
                    ClockOutTime = null,
                    CreationDate = now,
                    EntrySource = "AdminFast",
                    CreatedByAdminId = currentAdminId
                };

                // افزودن رکورد به دیتابیس و ذخیره تغییرات در فایل SQLite
                DbContext.Attendances.Add(attendance);
                await DbContext.SaveChangesAsync();

                // واکشی شیء کامل کارمند و قالب‌بندی نام او با رنگ قرمز جهت نمایش در اعلان
                var targetEmp = employees.FirstOrDefault(e => e.Id == employeeId);
                string empName = targetEmp?.FullName ?? string.Empty;
                string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";

                // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته QuickAttendance
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

                // بارگذاری مجدد داده‌های صفحه
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                // نمایش پیام خطا در صورت بروز مشکل هنگام ثبت ورود سریع
                TriggerToast("AdminPanel", "QuickAttendance", "PCal_AdminPanel_FastInError", new object[] { ex.Message }, isError: true);
            }

            // بازرندر رابط کاربری برای نمایش وضعیت جدید و بنر اعلان
            await InvokeAsync(StateHasChanged);
        }

        // ثبت سریع خروج کارمند از جدول اصلی یا کارنامه همراه با ثبت در تاریخچه سرور
        protected async Task StopAttendanceNow(int attendanceId)
        {
            // بررسی سطح دسترسی مدیر برای انجام عملیات ویرایش و ثبت خروج
            if (!canEditPerm) return;

            try
            {
                // یافتن رکورد تردد باز بر اساس شناسه دریافتی
                var attendance = await DbContext.Attendances.FindAsync(attendanceId);
                if (attendance != null)
                {
                    // دریافت شناسه ادمین آنلاین و ثبت ساعت خروج به عنوان زمان فعلی
                    int? currentAdminId = AdminState.CurrentOnlineAdmin?.Id;
                    var now = DateTime.Now;
                    attendance.ClockOutTime = now;
                    attendance.LastModifiedByAdminId = currentAdminId;

                    // به‌روزرسانی رکورد در دیتابیس و ذخیره تغییرات
                    DbContext.Attendances.Update(attendance);
                    await DbContext.SaveChangesAsync();

                    // واکشی شیء کامل کارمند و قالب‌بندی نام او با رنگ قرمز جهت نمایش در اعلان
                    var targetEmp = employees.FirstOrDefault(e => e.Id == attendance.EmployeeId);
                    string empName = targetEmp?.FullName ?? string.Empty;
                    string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";

                    // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته QuickAttendance
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

                    // بارگذاری مجدد داده‌های صفحه
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                // نمایش پیام خطا در صورت بروز مشکل هنگام ثبت خروج سریع
                TriggerToast("AdminPanel", "QuickAttendance", "PCal_AdminPanel_FastOutError", new object[] { ex.Message }, isError: true);
            }

            // بازرندر رابط کاربری برای نمایش وضعیت جدید و بنر اعلان
            await InvokeAsync(StateHasChanged);
        }
        // ثبت دستی یا ویرایش ساعت کاری پرسنل همراه با ذخیره تاریخچه تغییرات ادمین و ثبت خطاها/موفقیت در سرور
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

                // حالت اول: ویرایش یک رکورد تردد موجود
                if (isAttEditMode && _editingAttendanceId.HasValue)
                {
                    var att = await DbContext.Attendances
                        .Include(a => a.EditLogs)
                        .FirstOrDefaultAsync(a => a.Id == _editingAttendanceId.Value);

                    if (att != null)
                    {
                        // ثبت سابقه تغییر ساعت در جدول تاریخچه ویرایش‌ها (Audit Log)
                        var log = new AttendanceEditLog
                        {
                            AttendanceId = att.Id,
                            AdminId = currentAdminId ?? 0,
                            AdminUsername = currentAdminName,
                            EditDate = DateTime.Now,
                            OldClockInTime = att.ClockInTime,
                            OldClockOutTime = att.ClockOutTime,
                            NewClockInTime = inTime,
                            NewClockOutTime = outTime,
                            Reason = CalendarManager.Translate("PCal_AdminPanel_AuditEditReason")
                        };

                        DbContext.AttendanceEditLogs.Add(log);

                        // به‌روزرسانی ساعت‌های ورود و خروج و شناسه آخرین ویرایشگر
                        att.ClockInTime = inTime;
                        att.ClockOutTime = outTime;
                        att.LastModifiedByAdminId = currentAdminId;

                        DbContext.Attendances.Update(att);
                    }
                }
                // حالت دوم: ثبت یک رکورد تردد دستی جدید توسط ادمین
                else
                {
                    var newAtt = new Attendance
                    {
                        EmployeeId = selectedEmployee.Id,
                        ClockInTime = inTime,
                        ClockOutTime = outTime,
                        CreationDate = DateTime.Now,
                        EntrySource = "AdminManual",
                        CreatedByAdminId = currentAdminId,
                        LastModifiedByAdminId = currentAdminId
                    };
                    DbContext.Attendances.Add(newAtt);
                }

                // ذخیره نهایی تغییرات در فایل دیتابیس SQLite
                await DbContext.SaveChangesAsync();

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
        // اجرای عملیات حذف تایید شده (پرونده پرسنل یا رکورد تردد) و ثبت در تاریخچه اعلان‌های سرور
        protected async Task ExecuteConfirmedDelete()
        {
            // بستن پنجره مودال تاییدیه حذف
            showDeleteModal = false;

            // بررسی معتبر بودن شناسه هدف و اتصال دیتابیس
            if (_targetIdToDelete <= 0 || DbContext == null) return;

            try
            {
                // حالت اول: حذف کامل پرونده یک کارمند و کلیه ترددهای او
                if (_deleteType == "Employee")
                {
                    var employee = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _targetIdToDelete);
                    if (employee != null)
                    {
                        // قالب‌بندی نام کارمند با رنگ قرمز و فونت ضخیم جهت نمایش در اعلان بالای صفحه
                        string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{employee.FullName}»</span>";

                        // واکشی و حذف تمامی سوابق تردد مرتبط با این کارمند
                        var attendances = DbContext.Attendances.Where(a => a.EmployeeId == _targetIdToDelete);
                        DbContext.Attendances.RemoveRange(attendances);

                        // حذف رکورد اصلی کارمند از جدول پرسنل و ذخیره نهایی در دیتابیس
                        DbContext.Employees.Remove(employee);
                        await DbContext.SaveChangesAsync();

                        // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته EmployeeInfo
                        TriggerToast(
                            category: "AdminPanel",
                            subCategory: "EmployeeInfo",
                            messageKey: "PCal_AdminPanel_EmpDeletedMsg",
                            args: new object[] { coloredEmpName },
                            isError: false,
                            targetEmp: employee
                        );
                    }
                }
                // حالت دوم: حذف یک رکورد تردد خاص از کارنامه کارمند
                else if (_deleteType == "Attendance")
                {
                    var att = await DbContext.Attendances.FirstOrDefaultAsync(a => a.Id == _targetIdToDelete);
                    if (att != null)
                    {
                        // واکشی شیء کارمند مرتبط با این تردد (از کارمند انتخاب‌شده در کارنامه یا لیست کل پرسنل)
                        var targetEmp = selectedEmployee ?? employees.FirstOrDefault(e => e.Id == att.EmployeeId);
                        string empName = targetEmp?.FullName ?? string.Empty;

                        // قالب‌بندی نام کارمند با رنگ قرمز و فونت ضخیم جهت نمایش در اعلان
                        string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";

                        // استخراج تاریخ و ساعت رکورد تردد پیش از حذف جهت ثبت در لاگ رویداد
                        string? evDate = att.ClockInTime?.ToString("yyyy-MM-dd");
                        string? evTime = att.ClockInTime?.ToString("HH:mm");

                        // حذف رکورد تردد از جدول دیتابیس و ذخیره نهایی
                        DbContext.Attendances.Remove(att);
                        await DbContext.SaveChangesAsync();

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
                // نمایش پیغام خطا در صورت بروز مشکل در عملیات حذف دیتابیس
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