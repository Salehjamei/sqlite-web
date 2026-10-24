using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models;
using sqlite_web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sqlite_web.Components.Pages
{
    /// <summary>
    /// کلاس جزئی مدیریت منطق پشت‌صحنه پنل پرسنل و کارکردها
    /// </summary>
    public partial class AdminPanel : ComponentBase
    {
        // 💾 تزریق کانتکست دیتابیس بومی پروژه برای ارتباط با SQLite
        [Inject] public AppDbContext DbContext { get; set; } = default!;

        // 🛡️ تزریق سرویس پایش اصالت سشن و متغیرهای هویت آنلاین ادمین
        [Inject] public AdminStateService AdminState { get; set; } = default!;

        // 📅 نمونه‌سازی از کلاس مستقل و جدید مدیریت تقویم و ویژگی‌های داینامیک
        protected sqlite_web.Components.Models.Calendar.PCalendar CalendarManager { get; set; } = new();

        // 👑 پرچم‌های هوشمند بررسی سطوح دسترسی ادمین آنلاین
        protected bool canAddPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanAddEmployee ?? false);
        protected bool canEditPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanEditEmployee ?? false);
        protected bool canDeletePerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanDeleteEmployee ?? false);

        // 🔔 رشته متنی نگه‌داری پیام‌های زنده سیستم
        protected string SystemMessage { get; set; } = string.Empty;

        // 📅 پرچم وضعیت سوییچ بین حالت ثبت جدید ساعت کاری یا ویرایش
        protected bool isAttEditMode { get; set; } = false;
        private int? _editingAttendanceId = null;
        protected string TotalMonthlyHours { get; set; } = "00:00 ساعت";

        // 🛠️ متغیرهای جدید جهت مهار مودال پویا تنظیمات ساعات موظفی و تعطیلات سرور
        protected bool showSettingsModal { get; set; } = false;
        protected string settingsStartTime { get; set; } = "07:00";
        protected string settingsEndTime { get; set; } = "17:30";

        // 📅 لیست روزهای هفته انتخاب شده برای تعطیلات
        protected List<string> selectedOffDays { get; set; } = new();

        // فیلدهای بایند شده به فرم ثبت و ویرایش کارمندان
        protected string newEmpName { get; set; } = string.Empty;
        protected string newEmpCode { get; set; } = string.Empty;
        protected string newEmpPhone { get; set; } = string.Empty;
        protected bool isEmpEditMode { get; set; } = false;
        private int? _editingEmployeeId = null;

        // متغیرهای مدیریت پنجره‌های بازشو (Modal) و حذف
        protected bool showModal { get; set; } = false;
        protected bool showManualAddModal { get; set; } = false;
        protected bool showDeleteModal { get; set; } = false;

        // فیلدهای کنترلی فرم ثبت دستی تردد پرسنل
        protected Employee? selectedEmployee { get; set; }
        protected DateTime manualDate { get; set; } = DateTime.Today;
        protected DateTime manualOutDate { get; set; } = DateTime.Today; // 🟢 فیلد جدید ثبت روز خروج جهت بررسی ولیدیشن انطباق
        protected string manualInTime { get; set; } = "07:00";
        protected string manualOutTime { get; set; } = "17:30";

        protected List<Employee> employees { get; set; } = new();
        protected string SearchQuery { get; set; } = string.Empty;

        // پروپرتی فیلتر کننده کارمندان بر اساس نام یا کد ملی
        protected List<Employee> FilteredEmployees => string.IsNullOrWhiteSpace(SearchQuery)
            ? (employees ?? new())
            : (employees ?? new()).Where(e => (e.FullName != null && e.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                              (e.PersonnelCode != null && e.PersonnelCode.Contains(SearchQuery))).ToList();

        protected List<Attendance> allAttendances { get; set; } = new();
        protected string SelectedMonthFilter { get; set; } = string.Empty;
        protected string TotalRegularHours { get; set; } = "00:00";
        protected string TotalOvertimeHours { get; set; } = "00:00";

        public List<MonthOption> AvailableMonths { get; set; } = new();
        protected int? ActiveMobileDropdownId { get; set; } = null;
        public bool showEmpFormModal { get; set; } = false;
        public string previewImageUrl { get; set; } = string.Empty;

        // متغیرهای مورد نیاز برای مدیریت هر دو نوع حذف مشترک
        protected string deleteModalTitle { get; set; } = string.Empty;
        protected string targetDeleteName { get; set; } = string.Empty;
        protected string deleteTargetMessage { get; set; } = string.Empty;
        protected string deleteModalSubMessage { get; set; } = string.Empty;
        private int _targetIdToDelete = 0;
        private string _deleteType = string.Empty;

        // متد باز کردن مودال ثبت تردد دستی جدید
        protected void OpenManualAddModal()
        {
            CancelAttEdit();
            if (CalendarManager != null) CalendarManager.GetType().GetProperty("AttendanceValidationMessage")?.SetValue(CalendarManager, string.Empty);
            showManualAddModal = true;
        }

        // بستن مودال ثبت تردد دستی
        protected void CloseManualAddModal()
        {
            showManualAddModal = false;
            CancelAttEdit();
        }

        // متد باز و بسته کردن منوی آبشاری مستقل موبایل کارمندان
        protected void ToggleMobileMenuDropdown(int employeeId)
        {
            if (ActiveMobileDropdownId == employeeId) ActiveMobileDropdownId = null;
            else ActiveMobileDropdownId = employeeId;
        }

        // متد متمرکز پاکسازی و بستن منوهای آبشاری موبایل
        protected void CloseAllMobileDropdowns() => ActiveMobileDropdownId = null;

        public class MonthOption
        {
            public string Value { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        // مقداردهی اولیه و واکشی داده‌ها قبل از رندر شدن جدول
        protected override async Task OnInitializedAsync()
        {
            try
            {
                await AdminState.CheckAccessAndRedirectAsync();

                if (AdminState.IsAuthorized)
                {
                    await AdminState.LoadWorkSettingsAsync();

                    // 🟢 گارد امنیتی: اگر تنظیمات بانک اطلاعاتی خالی بود، مقادیر پیش‌فرض ست شود تا سرور کرش نکند
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
                Console.WriteLine($"❌ خطا در متد OnInitializedAsync: {ex.Message}");
            }        
        }

        // متد رندر تعاملی جهت فعال کردن رویدادهای دکمه‌ها در مرورگر
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender) await InvokeAsync(StateHasChanged);
        }

        // تازه سازی آنی لیست کارمندان و کل ترددهای ثبت شده از دیتابیس SQLite
        private async Task RefreshDataAsync()
        {
            if (DbContext == null) return;

            try
            {
                employees = await DbContext.Employees.AsNoTracking().ToListAsync() ?? new();
                allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync() ?? new();

                // 🟢 گارد امنیتی دوم: بازشماری کارکردها تنها در صورتی مجاز است که یک کارمند به طور واقعی انتخاب شده باشد
                if (selectedEmployee != null && selectedEmployee.Id > 0)
                {
                    LoadAvailableMonths(selectedEmployee.Id);
                    CalculateFilteredHours(selectedEmployee.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطا در متد RefreshDataAsync: {ex.Message}");
            }
        }

        // باز کردن مودال فرم افزودن پرسنل جدید
        public void OpenAddEmployeeModal()
        {
            CloseAllMobileDropdowns();
            CancelEmpEdit();
            showEmpFormModal = true;
        }

        // بستن مودال فرم پرسنل
        public void CloseEmpFormModal()
        {
            showEmpFormModal = false;
            CancelEmpEdit();
        }

        // فشرده‌سازی و تبدیل آنلاین تصویر پرسنل به فرمت Base64
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
                SystemMessage = $"❌ خطا در بارگذاری فایل تصویر: {ex.Message}";
            }
        }

        // مدیریت عملیات ذخیره‌سازی اطلاعات کارمند جدید یا ثبت ویرایش مشخصات
        protected async Task SaveEmployeeData()
        {
            if (string.IsNullOrWhiteSpace(newEmpName) || string.IsNullOrWhiteSpace(newEmpCode))
            {
                SystemMessage = "❌ نام کارمند و کد پرسنلی الزامی است!";
                return;
            }
            if (newEmpName.Length > 40 || newEmpCode.Length != 10 || (!string.IsNullOrWhiteSpace(newEmpPhone) && newEmpPhone.Length > 15))
            {
                SystemMessage = "❌ خطای امنیتی سرور در طول کاراکترها!";
                return;
            }

            if (isEmpEditMode && _editingEmployeeId.HasValue)
            {
                if (!canEditPerm && !AdminState.isCurrentUserSuperAdmin) return;
                var emp = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _editingEmployeeId.Value);
                if (emp != null)
                {
                    emp.FullName = newEmpName;
                    emp.Phone = newEmpPhone;
                    emp.PersonnelCode = newEmpCode;
                    emp.ImagePath = previewImageUrl;
                    DbContext.Employees.Update(emp);
                    SystemMessage = $"✅ مشخصات کارمند ویرایش شد.";
                }
            }
            else
            {
                if (!canAddPerm && !AdminState.isCurrentUserSuperAdmin) return;
                DbContext.Employees.Add(new Employee { FullName = newEmpName, Phone = newEmpPhone, PersonnelCode = newEmpCode, ImagePath = previewImageUrl });
                SystemMessage = $"✅ کارمند جدید ثبت شد.";
            }
            await DbContext.SaveChangesAsync();
            await RefreshDataAsync();
            CloseEmpFormModal();
        }
        // سوییچ وضعیت فرم به حالت ویرایش پرونده کارمند
        protected void StartEmpEdit(Employee emp)
        {
            ActiveMobileDropdownId = null;
            selectedEmployee = emp;
            isEmpEditMode = true;
            _editingEmployeeId = emp.Id;
            newEmpName = emp.FullName ?? string.Empty;
            newEmpCode = emp.PersonnelCode ?? string.Empty;
            newEmpPhone = emp.Phone ?? string.Empty;
            previewImageUrl = emp.ImagePath;
            showEmpFormModal = true;
        }
        // متد پاکسازی متغیرهای محلی و ریست کردن کادرهای فرم ثبت پرسنل
        protected void CancelEmpEdit()
        {
            isEmpEditMode = false;
            _editingEmployeeId = null;
            newEmpName = string.Empty;
            newEmpPhone = string.Empty;
            newEmpCode = string.Empty;
            previewImageUrl = string.Empty;
        }
        // راه‌اندازی و باز کردن مودال تایید نهایی حذف کارمند
        protected void TriggerDeleteEmployee(int id, string name)
        {
            if (!canDeletePerm) return;
            _targetIdToDelete = id;
            _deleteType = "Employee";
            deleteModalTitle = "تاییدیه حذف قطعی پرونده پرسنلی";
            targetDeleteName = name;
            deleteTargetMessage = $"آیا از حذف کامل پرونده پرسنلی کارمند با نام «{name}» مطمئن هستید؟";
            deleteModalSubMessage = "توجه: کلیه سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد.";
            ActiveMobileDropdownId = null;
            showDeleteModal = true;
        }
        // حاضر کردن سریع کارمند از درون جدول یا منوی موبایل
        protected async Task ForcePresent(int empId)
        {
            if (!canEditPerm) return;
            DbContext.Attendances.Add(new Attendance { EmployeeId = empId, ClockInTime = DateTime.Now });
            await DbContext.SaveChangesAsync();
            await RefreshDataAsync();
        }
        // متد ثبت ورود سریع پرسنل از طریق پنل ادمین
        protected async Task StartAttendanceNow(int employeeId)
        {
            try
            {
                DbContext.Attendances.Add(new Attendance { EmployeeId = employeeId, ClockInTime = DateTime.Now, ClockOutTime = null });
                await DbContext.SaveChangesAsync();
                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                AdminState.FeedbackMessage = $"❌ خطا در ثبت ورود: {ex.Message}";
            }
            await InvokeAsync(StateHasChanged);
        }
        // متد ثبت خروج سریع پرسنل و مهار تاریخ روز خروج
        protected async Task StopAttendanceNow(int attendanceId)
        {
            try
            {
                var attendance = await DbContext.Attendances.FindAsync(attendanceId);
                if (attendance != null)
                {
                    attendance.ClockOutTime = DateTime.Now;
                    DbContext.Attendances.Update(attendance);
                    await DbContext.SaveChangesAsync();
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                AdminState.FeedbackMessage = $"❌ خطا در ثبت خروج: {ex.Message}";
            }
            await InvokeAsync(StateHasChanged);
        }
        // 🔐 ثبت دستی یا ویرایش ساعت کاری مجهز به شروط دقیق ولیدیشن انطباق روز و تقدم زمان
        // متد ثبت دستی یا ویرایش دوکاره ساعت کاری پرسنل توسط ادمین سیستم
        // متد ثبت دستی یا ویرایش دوکاره ساعت کاری پرسنل بر اساس ولیدیشن انطباق روز و تقدم ساعت
        protected async Task AddManualAttendance()
        {
            if (!canEditPerm) return;
            if (selectedEmployee == null) return;

            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            try
            {
                // 🟢 همگام‌سازی آنی متغیرهای بومی تاریخ از روی منوهای آبشاری فرم کلاینت قبل از شروع عملیات پارس
                manualDate = CalendarManager.ParseFormFieldsToDateTime(CalendarManager.ManualInYear, CalendarManager.ManualInMonth, CalendarManager.ManualInDay, "00:00");
                manualOutDate = CalendarManager.ParseFormFieldsToDateTime(CalendarManager.ManualOutYear, CalendarManager.ManualOutMonth, CalendarManager.ManualOutDay, "00:00");

                // ترکیب کردن فیلدهای مستقل تاریخ روز ورود و خروج با فیلدهای متنی ساعت با حفظ ساختار بومی شما
                var inTime = DateTime.Parse($"{manualDate:yyyy-MM-dd} {manualInTime}");
                var outTime = DateTime.Parse($"{manualOutDate:yyyy-MM-dd} {manualOutTime}");

                // گارد اول: تاریخ روز خروج باید دقیقاً مساوی با تاریخ روز ورود باشد
                if (inTime.Date != outTime.Date)
                {
                    CalendarManager.GetType().GetProperty("AttendanceValidationMessage")?.SetValue(CalendarManager, "❌ خطا: تاریخ خروج باید دقیقاً همان روز تاریخ ورود باشد! امکان ثبت تردد در روزهای متفاوت وجود ندارد.");
                    return;
                }

                // گارد دوم: ساعت خروج نباید قبل‌تر یا جلوتر از ساعت ورود تنظیم شده باشد
                if (outTime < inTime)
                {
                    CalendarManager.GetType().GetProperty("AttendanceValidationMessage")?.SetValue(CalendarManager, "❌ خطا: ساعت خروج نمی‌تواند قبل‌تر از ساعت ورود تنظیم شود!");
                    return;
                }

                if (isAttEditMode && _editingAttendanceId.HasValue)
                {
                    var att = await DbContext.Attendances.FindAsync(_editingAttendanceId.Value);
                    if (att != null)
                    {
                        att.ClockInTime = inTime;
                        att.ClockOutTime = outTime;
                        DbContext.Attendances.Update(att);
                    }
                }
                else
                {
                    DbContext.Attendances.Add(new Attendance { EmployeeId = selectedEmployee.Id, ClockInTime = inTime, ClockOutTime = outTime });
                }

                await DbContext.SaveChangesAsync();
                showManualAddModal = false;

                await RefreshDataAsync();
                CalculateFilteredHours(selectedEmployee.Id);
                CancelAttEdit();
            }
            catch
            {
                CalendarManager.GetType().GetProperty("AttendanceValidationMessage")?.SetValue(CalendarManager, "❌ فرمت زمان وارد شده اشتباه است (مثال معتبر: 07:00 یا 17:30)");
            }
        }


        protected void CloseModal() { showModal = false; selectedEmployee = null; }
        protected void ClearSystemMessage() => SystemMessage = string.Empty;
        // محاسبه تفاضل ورود و خروج به رشته متنی فرمت شده استاندارد HH:mm
        protected string GetSingleDuration(Attendance att)
        {
            if (att.ClockInTime == null || att.ClockOutTime == null) return "در حال تردد";
            var diff = att.ClockOutTime.Value - att.ClockInTime.Value;
            return $"{(int)diff.TotalHours:00}:{diff.Minutes:00}";
        }
        // فعال‌سازی وضعیت ویرایش ساعت کاری یک ردیف
        // فعال‌سازی ادیتور فیلدهای ساعت کاری یک ردیف خاص و همگام‌سازی سلکتورهای آبشاری تاریخ کاری
        protected void StartAttEdit(Attendance att)
        {
            isAttEditMode = true;
            _editingAttendanceId = att.Id;
            manualInTime = att.ClockInTime.HasValue ? att.ClockInTime.Value.ToString("HH:mm") : "07:00";
            manualOutTime = att.ClockOutTime.HasValue ? att.ClockOutTime.Value.ToString("HH:mm") : "17:30";

            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            // 🟢 همگام‌سازی فیلدهای داخلی منوهای آبشاری تقویم جدید کلاس با زمان‌های واقعی ردیف
            CalendarManager.SyncFormCalendarFields(att.ClockInTime ?? DateTime.Today, att.ClockOutTime ?? DateTime.Today);
            CalendarManager.AttendanceValidationMessage = string.Empty;

            showManualAddModal = true;
        }

        // انصراف از ویرایش فیلد ردیف ساعت کاری
        protected void CancelAttEdit()
        {
            isAttEditMode = false;
            _editingAttendanceId = null;
            manualDate = DateTime.Today;
            manualOutDate = DateTime.Today;
            manualInTime = "07:00";
            manualOutTime = "17:30";
            if (CalendarManager != null) CalendarManager.GetType().GetProperty("AttendanceValidationMessage")?.SetValue(CalendarManager, string.Empty);
        }
        // استخراج تمام ماه‌های دارای سابقه تردد به صورت داینامیک متناسب با متد تقویم جدید شما
        private void LoadAvailableMonths(int employeeId)
        {
            AvailableMonths.Clear();
            var now = DateTime.Now;

            if (allAttendances == null) allAttendances = new();
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            // استخراج سال و ماه‌های رکوردهای تردد به صورت متمایز و مرتب‌شده
            var dates = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue)
                .Select(a => new { a.ClockInTime!.Value.Year, a.ClockInTime.Value.Month })
                .Distinct()
                .OrderByDescending(d => d.Year)
                .ThenByDescending(d => d.Month)
                .ToList();

            foreach (var d in dates)
            {
                // 🟢 فراخوانی کاملاً مستقیم و امن متد متنی ماه بدون استفاده از رفلکشن
                string monthText = CalendarManager.GetDynamicMonthText(d.Year, d.Month);
                AvailableMonths.Add(new MonthOption { Value = $"{d.Year}-{d.Month}", Text = monthText });
            }

            // بررسی و درج ماه جاری در صورت عدم وجود سابقه در لیست
            var currentMonthValue = $"{now.Year}-{now.Month}";
            if (!AvailableMonths.Any(m => m.Value == currentMonthValue))
            {
                string monthText = CalendarManager.GetDynamicMonthText(now.Year, now.Month);
                AvailableMonths.Insert(0, new MonthOption { Value = currentMonthValue, Text = monthText });
            }

            if (string.IsNullOrEmpty(SelectedMonthFilter)) SelectedMonthFilter = currentMonthValue;
        }

        // تغییر فیلتر ماهانه کارکرد جدول
        protected void OnMonthSelectorChanged(ChangeEventArgs e)
        {
            SelectedMonthFilter = e.Value?.ToString() ?? string.Empty;
            if (selectedEmployee != null) CalculateFilteredHours(selectedEmployee.Id);
        }
        // مفسر اصلاح‌شده رویدادهای منوی تفکیکی موبایل کارمندان
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
        // بیدار کردن مودال محاسبات کارکرد ماهانه پرسنل
        protected void OpenReportModal(Employee emp)
        {
            selectedEmployee = emp;
            showModal = true;
            LoadAvailableMonths(emp.Id);
            CalculateFilteredHours(emp.Id);
        }
        // بیدار کردن مودال حذف برای ساعت کاری
        protected void TriggerDeleteTimeRecord(Attendance att)
        {
            if (att == null) return;
            if (CalendarManager == null) CalendarManager = new sqlite_web.Components.Models.Calendar.PCalendar();

            _targetIdToDelete = att.Id;
            _deleteType = "Attendance";
            deleteModalTitle = "تاییدیه حذف قطعی رکورد تردد پرسنل";

            // 🟢 فراخوانی مستقیم متد مبدل تاریخ بدون سربار رفلکشن
            string dateStr = CalendarManager.GetDynamicFormattedDate(att.ClockInTime);
            string timeStr = att.ClockInTime.HasValue ? att.ClockInTime.Value.ToString("HH:mm") : "00:00";

            targetDeleteName = $"تردد مورخ {dateStr} ساعت {timeStr}";
            deleteTargetMessage = $"آیا از حذف کامل این رکورد تردد کاری پرسنل در تاریخ {dateStr} مطمئن هستید؟";
            deleteModalSubMessage = "توجه: با تایید نهایی، این ساعت ورود و خروج خاص به طور کامل پاک شده و در محاسبات این ماه لحاظ نخواهد شد.";
            showDeleteModal = true;
        }
        protected void CancelDeleteModal() => showDeleteModal = false;
        // متد نهایی اجرای حذف دیتابیس بر اساس نوع کلیک
        protected async Task ExecuteConfirmedDelete()
        {
            showDeleteModal = false;
            if (_targetIdToDelete <= 0 || DbContext == null) return;
            try
            {
                if (_deleteType == "Employee")
                {
                    var employee = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _targetIdToDelete);
                    if (employee != null)
                    {
                        var attendances = DbContext.Attendances.Where(a => a.EmployeeId == _targetIdToDelete);
                        DbContext.Attendances.RemoveRange(attendances);
                        DbContext.Employees.Remove(employee);
                        SystemMessage = $"✅ پرونده پرسنلی حذف شد.";
                    }
                }
                else if (_deleteType == "Attendance")
                {
                    var att = await DbContext.Attendances.FirstOrDefaultAsync(a => a.Id == _targetIdToDelete);
                    if (att != null)
                    {
                        DbContext.Attendances.Remove(att);
                        SystemMessage = $"✅ رکورد تردد با موفقیت ابطال شد.";
                    }
                }
                await DbContext.SaveChangesAsync();
            }
            catch (Exception ex) { SystemMessage = $"❌ خطا در حذف: {ex.Message}"; }
            if (selectedEmployee != null && _deleteType == "Attendance") CalculateFilteredHours(selectedEmployee.Id);
            await RefreshDataAsync();
            await InvokeAsync(StateHasChanged);
        }






        // موتور هوشمند محاسبه کارکرد و تفکیک کامل روزهای موظفی و اضافه کاری ماه انتخابی
        private void CalculateFilteredHours(int employeeId)
        {
            if (string.IsNullOrEmpty(SelectedMonthFilter)) return;

            var parts = SelectedMonthFilter.Split('-');
            int targetYear = int.Parse(parts[0]);
            int targetMonth = int.Parse(parts[1]);

            // ۱. بررسی و آپدیت خودکار ترددهایی که روزشان گذشته و خروج نخورده‌اند به ۲۳:۵۹
            var openAttendances = DbContext.Attendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue && !a.ClockOutTime.HasValue)
                .ToList();

            bool physicalDbChanged = false;
            foreach (var openAtt in openAttendances)
            {
                // اگر تاریخ ورود قبل از امروز است یعنی روز تمام شده و باید ۲۳:۵۹ ثبت شود
                if (openAtt.ClockInTime.Value.Date < DateTime.Today)
                {
                    openAtt.ClockOutTime = openAtt.ClockInTime.Value.Date.AddHours(23).AddMinutes(59);
                    DbContext.Attendances.Update(openAtt);
                    physicalDbChanged = true;
                }
            }
            if (physicalDbChanged) DbContext.SaveChanges();

            // واکشی کل ترددهای فیلتر شده ماه
            var monthlyRecords = DbContext.Attendances.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue
                       && a.ClockInTime.Value.Year == targetYear && a.ClockInTime.Value.Month == targetMonth).ToList();

            double totalRegularMinutes = 0;
            double totalOvertimeMinutes = 0;
            double totalDeficitMinutes = 0; // مجموع کسری کار عادی پرسنل در این ماه

            // 🟢 مهار فیزیکی نال بودن متغیر تنظیمات سرور جهت جلوگیری از قطع شدن ارتباط کلاینت
            var settings = AdminState.CurrentSettings;

            // واکشی امن مقادیر فیلدها با در نظر گرفتن مقادیر پیش‌فرض محلی سیستم
            string startTimeStr = settings?.RegularStartTime ?? "07:00";
            string endTimeStr = settings?.RegularEndTime ?? "17:30";
            string offDaysStr = settings?.OffDaysOfWeek ?? "Saturday,Sunday";
            int thresholdMinutes = settings?.GetType().GetProperty("DeficitThresholdMinutes")?.GetValue(settings) as int? ?? 15;

            TimeSpan regStart = TimeSpan.Parse(startTimeStr);
            TimeSpan regEnd = TimeSpan.Parse(endTimeStr);
            double maxExpectedRegularMinutes = (regEnd - regStart).TotalMinutes; // مجموع کل دقایق موظفی یک روز عادی (مثلاً ۶۳۰ دقیقه)
            var offDaysList = offDaysStr.Split(',').ToList();

            foreach (var rec in monthlyRecords)
            {
                DateTime clockIn = rec.ClockInTime!.Value;

                // اگر هنوز خروج نزده و مال امروز است، زمان حال را برای محاسبات موقت در نظر بگیرد
                DateTime clockOut = rec.ClockOutTime ?? DateTime.Now;

                // اگر روز تعطیل رسمی هفته بود، کل کارکرد مستقیماً به اضافه کاری می‌رود
                if (offDaysList.Contains(clockIn.DayOfWeek.ToString()))
                {
                    totalOvertimeMinutes += (clockOut - clockIn).TotalMinutes;
                    continue;
                }

                DateTime regularStartThreshold = clockIn.Date.Add(regStart);
                DateTime regularEndThreshold = clockIn.Date.Add(regEnd);

                // محاسبه اضافه کاری صبح
                if (clockIn < regularStartThreshold)
                {
                    var endOfEarlyOvertime = clockOut < regularStartThreshold ? clockOut : regularStartThreshold;
                    totalOvertimeMinutes += (endOfEarlyOvertime - clockIn).TotalMinutes;
                }

                // محاسبه کارکرد واقعی درون بازه موظفی
                DateTime effectiveIn = clockIn < regularStartThreshold ? regularStartThreshold : clockIn;
                DateTime effectiveOut = clockOut > regularEndThreshold ? regularEndThreshold : clockOut;

                double dayRegularMinutes = 0;
                if (effectiveIn < effectiveOut)
                {
                    dayRegularMinutes = (effectiveOut - effectiveIn).TotalMinutes;
                    totalRegularMinutes += dayRegularMinutes;
                }

                // ⚠️ محاسبه کسری کار روزانه: اگر کارکرد داخل مرز موظفی کمتر از استاندارد باشد
                if (rec.ClockOutTime.HasValue && dayRegularMinutes < maxExpectedRegularMinutes)
                {
                    totalDeficitMinutes += (maxExpectedRegularMinutes - dayRegularMinutes);
                }

                // محاسبه اضافه کاری عصر و شب
                if (clockOut > regularEndThreshold)
                {
                    DateTime effectiveOvertimeStart = clockIn > regularEndThreshold ? clockIn : regularEndThreshold;
                    totalOvertimeMinutes += (clockOut - effectiveOvertimeStart).TotalMinutes;
                }
            }

            // 🧮 موتور تهاتر هوشمند: اگر کسری کار از آستانه تعیین شده ادمین بیشتر بود
            if (totalDeficitMinutes >= thresholdMinutes)
            {
                // اگر اضافه کاری داشتیم، تا جای ممکن کسری موظفی را از اضافه کاری کم کرده و به موظفی انتقال می‌دهیم
                double compensation = Math.Min(totalOvertimeMinutes, totalDeficitMinutes);
                totalOvertimeMinutes -= compensation;
                totalRegularMinutes += compensation;
            }

            // تبدیل دقایق نهایی حاصل از تهاتر به فرمت HH:mm جهت نمایش در باکس‌ها
            int regHours = (int)(totalRegularMinutes / 60);
            int regMins = (int)(totalRegularMinutes % 60);
            TotalRegularHours = $"{regHours:00}:{regMins:00}";

            int overHours = (int)(totalOvertimeMinutes / 60);
            int overMins = (int)(totalOvertimeMinutes % 60);
            TotalOvertimeHours = $"{overHours:00}:{overMins:00}";
        }

        // باز کردن پاپ‌آپ تنظیمات سرور و بارگذاری مقادیر ساعات کاری از SQLite
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
        // متد ذخیره نهایی پارامترهای جدید محاسباتی در سرور
        protected async Task SaveSettingsData()
        {
            bool success = await AdminState.SaveWorkSettingsAsync(settingsStartTime, settingsEndTime, selectedOffDays);
            if (success) showSettingsModal = false;
            await InvokeAsync(StateHasChanged);
        }
        // متد مدیریت روشن/خاموش کردن چک‌باکس روزهای هفته
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