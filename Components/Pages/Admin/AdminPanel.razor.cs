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
        // 🟢 متغیر جدید جهت کنترل وضعیت باز یا بسته بودن مودال ثانویه ثبت دستی ساعت کاری
        protected bool showManualAddModal { get; set; } = false;
        // متدهای جدید کنترل وضعیت مودال ثبت دستی
        protected void OpenManualAddModal()
        {
            CancelAttEdit();
            showManualAddModal = true;
        }

        protected void CloseManualAddModal()
        {
            showManualAddModal = false;
            CancelAttEdit();
        }
        private string deleteType { get; set; } = string.Empty;
        private int _targetDeleteId = 0;

        protected Employee? selectedEmployee { get; set; }
        protected DateTime manualDate { get; set; } = DateTime.Today;
        protected string manualInTime { get; set; } = "07:00";
        protected string manualOutTime { get; set; } = "17:30";
        // 🔍 متغیر جدید ذخیره رشته عبارت جستجوی پرسنل
        protected List<Employee> employees { get; set; } = new();

        // رشته متنی ذخیره عبارت جستجو
        protected string SearchQuery { get; set; } = string.Empty;

        // پروپرتی فیلتر کننده (نام آن باید FilteredEmployees باشد تا با لیست اصلی تداخل نکند)
        // 🟢 فیکس نهایی واکشی: تراز کامل حروف با متغیر بومی دیتابیس شما (employees با e کوچک)
        protected List<Employee> FilteredEmployees => string.IsNullOrWhiteSpace(SearchQuery)
            ? (employees ?? new())
            : (employees ?? new()).Where(e => (e.FullName != null && e.FullName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) ||
                                              (e.PersonnelCode != null && e.PersonnelCode.Contains(SearchQuery))).ToList();



        protected List<Attendance> allAttendances { get; set; } = new();
        protected string SelectedMonthFilter { get; set; } = string.Empty;
        protected string TotalRegularHours { get; set; } = "00:00";
        protected string TotalOvertimeHours { get; set; } = "00:00";

        // 🎯 فیکس باگ: عمومی کردن لایه لیست جهت شناسایی مفسر فرانت رازور
        public List<MonthOption> AvailableMonths { get; set; } = new();

        // 🟢 پروپرتی کنترل شناسه منوی آبشاری فعال موبایل
        protected int? ActiveMobileDropdownId { get; set; } = null;
        /// <summary>
        /// 🟢 متد باز و بسته کردن (تاتل) منوی آبشاری مستقل موبایل با حروف همگام
        /// </summary>
        protected void ToggleMobileMenuDropdown(int employeeId)
        {
            if (ActiveMobileDropdownId == employeeId)
            {
                ActiveMobileDropdownId = null;
            }
            else
            {
                ActiveMobileDropdownId = employeeId;
            }
        }

        /// <summary>
        /// 🟢 متد متمرکز پاکسازی و بستن منوهای آبشاری موبایل به محض لمس صفحه یا باز کردن مودال‌ها
        /// </summary>
        protected void CloseAllMobileDropdowns()
        {
            ActiveMobileDropdownId = null;
        }

        /// <summary>
        /// کلاس بومی ساختاردهی و پر کردن سلکتور گزارشات تفکیکی ماه‌های سال
        /// </summary>
        public class MonthOption
        {
            public string Value { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        /// <summary>
        /// 🟢 گام اول: مقداردهی اولیه و واکشی ۱۰۰٪ داده‌ها قبل از رندر شدن جدول جهت نمایش قطعی لیست پرسنل
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            try
            {
                // ۱. اجرای گارد امنیتی و بررسی اصالت سشن ادمین آنلاین
                await AdminState.CheckAccessAndRedirectAsync();

                // ۲. اگر ادمین تایید صلاحیت شده است، دیتای قوانین کاری و کارمندان همگام‌سازی شود
                if (AdminState.IsAuthorized)
                {
                    // بارگذاری مشخصات تنظیمات ساعات موظفی از SQLite
                    await AdminState.LoadWorkSettingsAsync();

                    // واکشی لیست فیزیکی کارمندان و رکوردهای تردد امروز
                    await RefreshDataAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ خطا در متد OnInitializedAsync پنل ادمین: {ex.Message}");
            }
        }

        /// <summary>
        /// 🟢 گام دوم: متد رندر تعاملی جهت باز کردن خودکار قفل کلیک رویدادها و بیدار کردن مودال‌ها
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // اجبار کامپوننت به تازه‌سازی نهایی و فعال کردن رویدادهای دکمه‌ها در مرورگر
                await InvokeAsync(StateHasChanged);
            }
        }



        /// <summary>
        /// 🔄 متد متمرکز و آسنکرون تازه‌سازی آنی لیست کارمندان و کل ترددهای ثبت شده از دیتابیس SQLite
        /// </summary>
        private async Task RefreshDataAsync()
        {
            if (DbContext == null) return;

            // واکشی کل رکوردها به صورت AsNoTracking جهت بالا رفتن سرعت لود و کاهش مصرف رم
            employees = await DbContext.Employees.AsNoTracking().ToListAsync() ?? new();
            allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync() ?? new();

            // در صورتی که پنجره کارنامه تفکیکی کارمندی باز است، ساعات کارکرد او آنی بازشماری شود
            if (selectedEmployee != null)
            {
                LoadAvailableMonths(selectedEmployee.Id);
                CalculateFilteredHours(selectedEmployee.Id);
            }
        }
        // 🟢 متغیرهای مدیریت مودال فرم ثبت‌نام و ویرایش کارمندان
        public bool showEmpFormModal { get; set; } = false;

        // رشته متنی نگه‌داری آدرس استاندارد Base64 تصویر جهت نمایش زنده پیش‌نمایش در فرم
        public string previewImageUrl { get; set; } = string.Empty;

        /// <summary>
        /// متد باز کردن مودال فرم افزودن پرسنل جدید و پاکسازی مقادیر کادرهای ورودی قبلی
        /// </summary>
        public void OpenAddEmployeeModal()
        {
            CloseAllMobileDropdowns(); // بستن منوهای آبشاری موبایل جهت جلوگیری از تداخل لایه‌ای
            CancelEmpEdit(); // ریست کردن کامل متغیرهای بایند شده
            showEmpFormModal = true; // نمایش فیزیکی مودال فرم
        }

        /// <summary>
        /// متد بستن مودال فرم پرسنل و لغو کامل وضعیت ویرایش پرونده
        /// </summary>
        public void CloseEmpFormModal()
        {
            showEmpFormModal = false;
            CancelEmpEdit();
        }

        /// <summary>
        /// متد ارتقایافته و آسنکرون بارگذاری، فشرده‌سازی و تبدیل آنلاین تصویر پرسنل به فرمت Base64 بدون ارور هاب
        /// </summary>
        public async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    // 📸 فشرده‌سازی هوشمند به ابعاد ۵۰۰x۵۰۰ پیکسل با فرمت image/jpeg جهت تضمین عدم کرش سیستم حضور غیاب
                    var highQualFile = await file.RequestImageFileAsync("image/jpeg", 500, 500);

                    // ایجاد آرایه بایت متناسب با حجم فیزیکی فایل فشرده شده جدید
                    var buffer = new byte[highQualFile.Size];

                    // باز کردن جریان ایمن خواندن فایل با سقف بافر ۵ مگابایتی
                    using (var stream = highQualFile.OpenReadStream(5120000))
                    {
                        int bytesRead = 0;
                        while (bytesRead < buffer.Length)
                        {
                            int read = await stream.ReadAsync(buffer, bytesRead, buffer.Length - bytesRead);
                            if (read == 0) break; // اتمام فایل
                            bytesRead += read;
                        }
                    }

                    // تولید دیتا یوآرآی استاندارد جهت ذخیره مستقیم در فیلد ImagePath بانک اطلاعاتی
                    previewImageUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(buffer)}";
                    SystemMessage = "📸 تصویر پرسنلی با کیفیت بالا و ابعاد استاندارد بارگذاری شد.";
                }
            }
            catch (Exception ex)
            {
                SystemMessage = $"❌ خطا در پردازش و بارگذاری فایل تصویر: {ex.Message}";
            }
        }
        /// <summary>
        /// مدیریت عملیات ذخیره‌سازی اطلاعات کارمند جدید یا ثبت ویرایش مشخصات به همراه راست‌آزمایی لایه امنیتی دسترسی‌ها و گارد طول کاراکتر بک‌آند
        /// </summary>
        protected async Task SaveEmployeeData()
        {
            // 🛡️ ۱. گارد بررسی فیلدهای اجباری
            if (string.IsNullOrWhiteSpace(newEmpName) || string.IsNullOrWhiteSpace(newEmpCode))
            {
                SystemMessage = "❌ نام کارمند و کد پرسنلی الزامی است!";
                return;
            }

            // 🛡️ ۲. لایه امنیتی بک‌آند: مهار تقلب در فرانت و کنترل فیزیکی طول کاراکترها در سرور

            // بررسی سقف ۴۰ کاراکتر برای نام و نام خانوادگی
            if (newEmpName.Length > 40)
            {
                SystemMessage = "❌ خطای امنیتی سرور: نام و نام خانوادگی نمی‌تواند بیشتر از ۴۰ کاراکتر باشد!";
                return;
            }

            // بررسی قالب دقیق ۱۰ رقمی برای کد ملی (کد پرسنلی)
            if (newEmpCode.Length != 10)
            {
                SystemMessage = "❌ خطای امنیتی سرور: کد ملی وارد شده باید دقیقاً ۱۰ رقم باشد!";
                return;
            }

            // بررسی سقف ۱۵ کاراکتر برای شماره تماس در صورت وارد شدن
            if (!string.IsNullOrWhiteSpace(newEmpPhone) && newEmpPhone.Length > 15)
            {
                SystemMessage = "❌ خطای امنیتی سرور: شماره تماس نمی‌تواند بیشتر از ۱۵ رقم باشد!";
                return;
            }

            // 🛡️ ۳. بررسی مجوزها و اعمال فیزیکی دیتابیس بر اساس وضعیت ثبت یا ویرایش
            if (isEmpEditMode && _editingEmployeeId.HasValue)
            {
                // گارد امنیتی بررسی مجوز ویرایش مشخصات پرسنلی
                if (!canEditPerm && !AdminState.isCurrentUserSuperAdmin)
                {
                    SystemMessage = "❌ خطای امنیتی: شما مجوز ویرایش اطلاعات پرسنل را ندارید!";
                    return;
                }

                var emp = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _editingEmployeeId.Value);
                if (emp != null)
                {
                    emp.FullName = newEmpName;
                    emp.Phone = newEmpPhone;
                    emp.PersonnelCode = newEmpCode;
                    emp.ImagePath = previewImageUrl; // به‌روزرسانی نهایی آدرس عکس جدید یا قبلی در دیتابیس

                    DbContext.Employees.Update(emp);

                    // 🔴 تفکیک گرافیکی: تزریق رنگ قرمز به نام پرسنل در حالت ویرایش پرونده
                    string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{newEmpName}»</span>";
                    SystemMessage = $"✅ مشخصات و تصویر پرسنل با نام {coloredName} با موفقیت ویرایش و اصلاح شد.";
                }
            }
            else
            {
                // گارد امنیتی بررسی مجوز افزودن کارمند جدید به سیستم حضور و غیاب
                if (!canAddPerm && !AdminState.isCurrentUserSuperAdmin)
                {
                    SystemMessage = "❌ خطای امنیتی: حساب کاربری شما فاقد مجوز ثبت کارمند جدید است!";
                    return;
                }

                DbContext.Employees.Add(new Employee
                {
                    FullName = newEmpName,
                    Phone = newEmpPhone,
                    PersonnelCode = newEmpCode,
                    ImagePath = previewImageUrl
                });

                // 🔴 تفکیک گرافیکی: تزریق رنگ قرمز به نام پرسنل در حالت ثبت پرونده جدید
                string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{newEmpName}»</span>";
                SystemMessage = $"✅ مشخصات کارمند جدید با نام {coloredName} با موفقیت به همراه تصویر در سیستم ثبت شد.";
            }

            await DbContext.SaveChangesAsync(); // اعمال فیزیکی کل تغییرات در SQLite
            await RefreshDataAsync(); // تازه‌سازی آنی لایوت جدول اصلی
            CloseEmpFormModal(); // بستن فرم پاپ‌آپ
        }



        /// <summary>
        /// متد سوییچ وضعیت فرم به حالت ویرایش پرونده کارمند و لود مشخصات قبلی او در کادرهای ورودی فرم
        /// </summary>
        protected void StartEmpEdit(Employee emp)
        {
            // بستن قطعی منوی آبشاری موبایل به محض لمس دکمه ویرایش
            ActiveMobileDropdownId = null;

            selectedEmployee = emp;
            isEmpEditMode = true; // یا isEditMode بر اساس متغیر شما
            _editingEmployeeId = emp.Id;

            // بایند مقادیر قبلی به فیلدهای مودال
            newEmpName = emp.FullName ?? string.Empty;
            newEmpCode = emp.PersonnelCode ?? string.Empty;
            newEmpPhone = emp.Phone ?? string.Empty;
            previewImageUrl = emp.ImagePath;

            showEmpFormModal = true;
        }

        /// <summary>
        /// متد پاکسازی متغیرهای محلی و ریست کردن کادرهای فرم ثبت پرسنل
        /// </summary>
        protected void CancelEmpEdit()
        {
            isEmpEditMode = false;
            _editingEmployeeId = null;
            newEmpName = string.Empty;
            newEmpPhone = string.Empty;
            newEmpCode = string.Empty;
            previewImageUrl = string.Empty;
        }

        /// <summary>
        /// راه‌اندازی و باز کردن مودال تایید نهایی حذف کارمند و بررسی سطح دسترسی canDeletePerm
        /// </summary>
        protected void TriggerDeleteEmployee(int id, string name)
        {
            if (!canDeletePerm) { SystemMessage = "❌ خطای امنیتی: حساب کاربری شما مجوز حذف پرسنل را ندارد!"; return; }
            deleteType = "EMP";
            _targetDeleteId = id;
            deleteTargetMessage = $"آیا از حذف کامل پرونده پرسنلی «{name}» و کل تاریخچه ترددهای او مطمئن هستید؟";
            showDeleteModal = true;
        }    

        /// <summary>
        /// حاضر کردن سریع کارمند از درون جدول یا منوی موبایل (تزریق زنده DateTime.Now به فیلد ورود)
        /// </summary>
        protected async Task ForcePresent(int empId)
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: حساب شما فاقد مجوز ثبت تردد سریع است!"; return; }
            DbContext.Attendances.Add(new Attendance { EmployeeId = empId, ClockInTime = DateTime.Now });
            await DbContext.SaveChangesAsync();
            SystemMessage = "🟢 وضعیت پرسنل انتخاب شده با موفقیت به حاضر تغییر یافت.";
            await RefreshDataAsync();
        }    
        /// <summary>
        /// ثبت دستی یا ویرایش دوکاره ساعت کاری روزهای گذشته یا امروز بر اساس فرمت معتبر زمانی
        /// </summary>
        protected async Task AddManualAttendance()
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: فاقد مجوز اصلاح دیتای ترددها!"; return; }
            if (selectedEmployee == null) return;

            try
            {
                var inTime = DateTime.Parse($"{manualDate:yyyy-MM-dd} {manualInTime}");
                var outTime = DateTime.Parse($"{manualDate:yyyy-MM-dd} {manualOutTime}");

                if (isAttEditMode && _editingAttendanceId.HasValue)
                {
                    // حالت ویرایش ردیف ساعت کاری ثبت شده
                    var att = await DbContext.Attendances.FindAsync(_editingAttendanceId.Value);
                    if (att != null)
                    {
                        att.ClockInTime = inTime;
                        att.ClockOutTime = outTime;
                        DbContext.Attendances.Update(att);
                        SystemMessage = "✅ ردیف ساعت کاری انتخاب شده با موفقیت ویرایش و اصلاح شد.";
                    }
                }
                else
                {
                    // حالت ثبت ردیف تردد دستی جدید در دیتابیس
                    DbContext.Attendances.Add(new Attendance { EmployeeId = selectedEmployee.Id, ClockInTime = inTime, ClockOutTime = outTime });
                    SystemMessage = "✅ ردیف ساعت کاری دستی جدید با موفقیت در سیستم ثبت گردید.";
                }

                await DbContext.SaveChangesAsync();
                showManualAddModal = false; // بستن خودکار پاپ‌آپ پس از ذخیره موفق
                await RefreshDataAsync();
                CancelAttEdit(); // بازنشانی فیلدهای فرم ساعت به حالت پیش‌فرض (۰۷:۰۰ تا ۱۷:۳۰)
            }
            catch
            {
                SystemMessage = "❌ فرمت زمان وارد شده اشتباه است (مثال معتبر: 07:00 یا 17:30)";
            }
        }

        protected void CloseModal() { showModal = false; selectedEmployee = null; }
        protected void ClearSystemMessage() => SystemMessage = string.Empty;

        /// <summary>
        /// محاسبه و تبدیل تفاضل ورود و خروج به رشته متنی فرمت شده استاندارد HH:mm جهت نمایش در جدول
        /// </summary>
        protected string GetSingleDuration(Attendance att)
        {
            if (att.ClockInTime == null || att.ClockOutTime == null) return "در حال تردد";
            var diff = att.ClockOutTime.Value - att.ClockInTime.Value;
            return $"{diff.Hours:00}:{diff.Minutes:00}";
        }

        /// <summary>
        /// 🧮 محاسبه ناخالص مجموع کل کارکرد پرسنل در ماه جاری سیستم جهت پردازش آماری سریع
        /// </summary>
        private void CalculateMonthlyTotalHours(int employeeId)
        {
            var now = DateTime.Now;
            var currentMonthRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue && a.ClockOutTime.HasValue && a.ClockInTime.Value.Year == now.Year && a.ClockInTime.Value.Month == now.Month).ToList();

            double totalMinutes = 0;
            foreach (var rec in currentMonthRecords) totalMinutes += (rec.ClockOutTime!.Value - rec.ClockInTime!.Value).TotalMinutes;

            int hours = (int)(totalMinutes / 60);
            int minutes = (int)(totalMinutes % 60);
            TotalMonthlyHours = $"{hours:00}:{minutes:00} ساعت";
        }

        protected void StartAttEdit(Attendance att)
        {
            isAttEditMode = true;
            _editingAttendanceId = att.Id;
            manualDate = att.ClockInTime ?? DateTime.Today; // تاریخ ورود مبنا قرار می‌گیرد

            // 🟢 فیکس باگ ویرایش: استخراج فرمت دقیق ساعت و دقیقه ورود و خروج بدون نشت تاریخ
            manualInTime = att.ClockInTime.HasValue ? att.ClockInTime.Value.ToString("HH:mm") : "07:00";
            manualOutTime = att.ClockOutTime.HasValue ? att.ClockOutTime.Value.ToString("HH:mm") : "17:30";

            showManualAddModal = true;
        }


        protected void CancelAttEdit()
        {
            isAttEditMode = false;
            _editingAttendanceId = null;
            manualDate = DateTime.Today;
            manualInTime = "07:00";
            manualOutTime = "17:30";
        }

        /// <summary>
        /// استخراج تمام ماه‌هایی که این کارمند در آن‌ها سابقه تردد ثبت شده دارد جهت پر کردن داینامیک سلکتور فیلتر
        /// </summary>
        private void LoadAvailableMonths(int employeeId)
        {
            AvailableMonths.Clear();
            var now = DateTime.Now;

            var dates = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue)
                .Select(a => new { a.ClockInTime!.Value.Year, a.ClockInTime.Value.Month }).Distinct().OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).ToList();

            foreach (var d in dates)
            {
                var dt = new DateTime(d.Year, d.Month, 1);
                AvailableMonths.Add(new MonthOption { Value = $"{d.Year}-{d.Month}", Text = dt.ToString("MMMM yyyy") });
            }

            var currentMonthValue = $"{now.Year}-{now.Month}";
            if (!AvailableMonths.Any(m => m.Value == currentMonthValue)) AvailableMonths.Insert(0, new MonthOption { Value = currentMonthValue, Text = now.ToString("MMMM yyyy") });
            if (string.IsNullOrEmpty(SelectedMonthFilter)) SelectedMonthFilter = currentMonthValue;
        }

        protected void OnMonthSelectorChanged(ChangeEventArgs e)
        {
            SelectedMonthFilter = e.Value?.ToString() ?? string.Empty;
            if (selectedEmployee != null) CalculateFilteredHours(selectedEmployee.Id);
        }

        /// <summary>
        /// 🟢 مفسر نهایی رویدادهای منوی انتخابی مستقل موبایل (با زدن گزینه‌ها منو بسته نمی‌شود و باز می‌ماند)
        /// 
        /// <summary>
        /// 🟢 مفسر اصلاح‌شده رویدادهای منوی تفکیکی موبایل: بستن خودکار در ویرایش/حذف و باز ماندن در حضور/توقف
        /// </summary>
        protected async Task HandleMobileActionChanged(string actionType, Employee emp, int activeAttendanceId)
        {
            if (string.IsNullOrEmpty(actionType)) return;

            switch (actionType)
            {
                case "REPORT":
                    OpenReportModal(emp);
                    CloseAllMobileDropdowns(); // 👈 بسته شدن منو با زدن کارنامه تفکیکی
                    break;

                case "START_ATT":
                    if (canEditPerm) await ForcePresent(emp.Id);
                    // 🎯 توجه: متد CloseAllMobileDropdowns در اینجا فراخوانی نمی‌شود تا منوی حاضر کردن باز بماند
                    break;

                case "STOP_ATT":
                    if (canEditPerm && activeAttendanceId > 0) await StopAttendanceNow(activeAttendanceId);
                    // 🎯 توجه: متد CloseAllMobileDropdowns در اینجا فراخوانی نمی‌شود تا منوی توقف تردد باز بماند
                    break;

                case "EDIT_EMP":
                    if (canEditPerm)
                    {
                        StartEmpEdit(emp);
                        CloseAllMobileDropdowns(); // 👈 فیکس نیازمندی: منو فوراً با زدن دکمه ویرایش ناپدید می‌شود
                    }
                    break;

                case "DELETE_EMP":
                    if (canDeletePerm)
                    {
                        TriggerDeleteEmployee(emp.Id, emp.FullName);
                        CloseAllMobileDropdowns(); // 👈 فیکس نیازمندی: منو فوراً با زدن دکمه حذف ناپدید می‌شود
                    }
                    break;
            }

            await InvokeAsync(StateHasChanged);
        }
        /// <summary>
        /// 🧮 موتور هوشمند محاسبه کارکرد بر اساس تنظیمات پویا دیتابیس و تفکیک کامل روزهای تعطیل هفته
        /// </summary>
        private void CalculateFilteredHours(int employeeId)
        {
            if (string.IsNullOrEmpty(SelectedMonthFilter)) return;

            var parts = SelectedMonthFilter.Split('-');
            int targetYear = int.Parse(parts[0]);
            int targetMonth = int.Parse(parts[1]);

            var monthlyRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue && a.ClockOutTime.HasValue
                       && a.ClockInTime.Value.Year == targetYear && a.ClockInTime.Value.Month == targetMonth).ToList();

            double totalRegularMinutes = 0;
            double totalOvertimeMinutes = 0;

            // واکشی پارامترهای زنده از تنظیمات سرور
            var settings = AdminState.CurrentSettings;
            TimeSpan regStart = TimeSpan.Parse(settings.RegularStartTime); // 07:00
            TimeSpan regEnd = TimeSpan.Parse(settings.RegularEndTime);     // 17:30
            var offDaysList = settings.OffDaysOfWeek.Split(',').ToList();

            foreach (var rec in monthlyRecords)
            {
                DateTime clockIn = rec.ClockInTime!.Value;
                DateTime clockOut = rec.ClockOutTime!.Value; // 🟢 حفظ کامل ساختار تاریخ و ساعت خروج

                // 🎯 شرط اول: اگر روز تردد جزو روزهای تعطیل هفته (مثلا شنبه یا یکشنبه) باشد، کل کارکرد اضافه کاری است
                if (offDaysList.Contains(clockIn.DayOfWeek.ToString()))
                {
                    totalOvertimeMinutes += (clockOut - clockIn).TotalMinutes;
                    continue;
                }

                // 🎯 شرط دوم: تفکیک بازه موظفی و اضافه کاری در روزهای عادی هفته
                DateTime regularStartThreshold = clockIn.Date.Add(regStart);
                DateTime regularEndThreshold = clockIn.Date.Add(regEnd);

                // محاسبه زمان قبل از ساعت موظفی (اضافه کاری صبح)
                if (clockIn < regularStartThreshold)
                {
                    var endOfEarlyOvertime = clockOut < regularStartThreshold ? clockOut : regularStartThreshold;
                    totalOvertimeMinutes += (endOfEarlyOvertime - clockIn).TotalMinutes;
                }

                // محاسبه زمان داخل بازه موظفی (ساعات کارکرد عادی)
                DateTime effectiveIn = clockIn < regularStartThreshold ? regularStartThreshold : clockIn;
                DateTime effectiveOut = clockOut > regularEndThreshold ? regularEndThreshold : clockOut;

                if (effectiveIn < effectiveOut)
                {
                    totalRegularMinutes += (effectiveOut - effectiveIn).TotalMinutes;
                }

                // محاسبه زمان بعد از ساعت موظفی (اضافه کاری عصر و شب با حفظ تاریخ خروج)
                if (clockOut > regularEndThreshold)
                {
                    DateTime effectiveOvertimeStart = clockIn > regularEndThreshold ? clockIn : regularEndThreshold;
                    totalOvertimeMinutes += (clockOut - effectiveOvertimeStart).TotalMinutes;
                }
            }

            int regHours = (int)(totalRegularMinutes / 60);
            int regMins = (int)(totalRegularMinutes % 60);
            TotalRegularHours = $"{regHours:00}:{regMins:00}";

            int overHours = (int)(totalOvertimeMinutes / 60);
            int overMins = (int)(totalOvertimeMinutes % 60);
            TotalOvertimeHours = $"{overHours:00}:{overMins:00}";
        }
        /// <summary>
        /// 🟢 متد ثبت ورود سریع پرسنل از طریق پنل ادمین در دیتابیس SQLite
        /// </summary>
        protected async Task StartAttendanceNow(int employeeId)
        {
            try
            {
                // ایجاد یک رکورد تردد جدید با ساعت و تاریخ فعلی سرور
                var newAttendance = new Attendance
                {
                    EmployeeId = employeeId,
                    ClockInTime = DateTime.Now,
                    ClockOutTime = null
                };

                DbContext.Attendances.Add(newAttendance);
                await DbContext.SaveChangesAsync(); // ذخیره فیزیکی در دیتابیس

                await RefreshDataAsync(); // تازه‌سازی آنی لیست پرسنل و وضعیت جدول
                AdminState.FeedbackMessage = "🟢 وضعیت ورود کارمند با موفقیت و به صورت زنده ثبت شد.";
            }
            catch (Exception ex)
            {
                AdminState.FeedbackMessage = $"❌ خطا در ثبت ورود: {ex.Message}";
            }
            await InvokeAsync(StateHasChanged);
        }
        /// <summary>
        /// 🛑 متد ثبت خروج سریع پرسنل و مهار تاریخ روز خروج در دیتابیس SQLite
        /// </summary>
        protected async Task StopAttendanceNow(int attendanceId)
        {
            try
            {
                // واکشی رکورد تردد باز کارمند جهت بستن ساعت خروج
                var attendance = await DbContext.Attendances.FindAsync(attendanceId);
                if (attendance != null)
                {
                    // 🟢 فیکس اصولی باگ: ثبت کامل تاریخ روز به همراه ساعت و دقیقه خروج فعلی
                    attendance.ClockOutTime = DateTime.Now;

                    DbContext.Attendances.Update(attendance);
                    await DbContext.SaveChangesAsync();

                    await RefreshDataAsync(); // رفرش خودکار وضعیت فرانت صفحه
                    AdminState.FeedbackMessage = "🛑 وضعیت خروج کارمند با موفقیت و با حفظ کامل تاریخ ثبت شد.";
                }
            }
            catch (Exception ex)
            {
                AdminState.FeedbackMessage = $"❌ خطا در ثبت خروج: {ex.Message}";
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 🗑️ متد حذف فیزیکی پرونده کارمند و ابطال تمام رکوردهای تردد او از SQLite
        /// </summary>
        protected async Task TriggerDelete(int employeeId, string fullName)
        {
            try
            {
                // ۱. واکشی کارمند از جدول کارمندان
                var employee = await DbContext.Employees.FindAsync(employeeId);
                if (employee != null)
                {
                    // ۲. حذف موازی تمام رکوردهای تردد ثبت شده برای این کارمند جهت حفظ یکپارچگی (Cascade Delete)
                    var empAttendances = DbContext.Attendances.Where(a => a.EmployeeId == employeeId);
                    DbContext.Attendances.RemoveRange(empAttendances);

                    // ۳. حذف فیزیکی خود پرونده کارمند
                    DbContext.Employees.Remove(employee);
                    await DbContext.SaveChangesAsync();

                    await RefreshDataAsync(); // تازه‌سازی فوری جدول ادمین‌پنل
                    AdminState.FeedbackMessage = $"🗑️ پرونده پرسنلی «{fullName}» به همراه کلیه سوابق تردد او با موفقیت حذف گردید.";
                }
            }
            catch (Exception ex)
            {
                AdminState.FeedbackMessage = $"❌ خطا در حذف پرونده: {ex.Message}";
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// متد باز کردن پاپ‌آپ تنظیمات سرور و بارگذاری مقادیر زنده از دیتابیس SQLite
        /// </summary>
        protected void OpenSettingsModal()
        {
            if (!canEditPerm || !AdminState.isCurrentUserSuperAdmin) return;

            CloseAllMobileDropdowns();

            // واکشی مشخصات زنده از لایه سرویس متمرکز شما
            var current = AdminState.CurrentSettings;
            settingsStartTime = current.RegularStartTime;
            settingsEndTime = current.RegularEndTime;

            // تبدیل رشته کاما دار دیتابیس به لیست سی‌شارپ جهت فعال‌سازی چک‌باکس‌ها
            selectedOffDays = current.OffDaysOfWeek.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            showSettingsModal = true;
        }

        /// <summary>
        /// متد ذخیره نهایی پارامترهای جدید محاسباتی در سرور و بستن مودال
        /// </summary>
        protected async Task SaveSettingsData()
        {
            bool success = await AdminState.SaveWorkSettingsAsync(settingsStartTime, settingsEndTime, selectedOffDays);
            if (success)
            {
                showSettingsModal = false;
                // آپدیت آنی بنر فیدبک سرویس جهت نمایش آلرت سبز موفقیت در بالای پنل
                AdminState.FeedbackMessage = "✅ قوانین محاسباتی ساعات موظفی و روزهای تعطیل هفته با موفقیت در سرور به‌روزرسانی شد.";
            }
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

        // 🟢 تراز قطعی امضای متد با لایه فرانت رازور
        protected void OpenReportModal(Employee emp)
        {
            selectedEmployee = emp;
            showModal = true;
            LoadAvailableMonths(emp.Id);
            CalculateFilteredHours(emp.Id);
        }
        // 🛠️ کل متغیرهای مورد نیاز برای مدیریت هر دو نوع حذف (بسیار مینی‌مال)
        protected bool showDeleteModal { get; set; } = false;
        protected string deleteModalTitle { get; set; } = string.Empty;
        protected string targetDeleteName { get; set; } = string.Empty;
        protected string deleteTargetMessage { get; set; } = string.Empty;
        protected string deleteModalSubMessage { get; set; } = string.Empty;

        private int _targetIdToDelete = 0;
        private string _deleteType = string.Empty; // "Employee" یا "Attendance"

        // 🗑️ متد مشترک ۱: بیدار کردن مودال حذف برای پرسنل
        protected void TriggerDeleteAttendance(int employeeId)
        {
            var emp = employees?.FirstOrDefault(e => e.Id == employeeId);
            if (emp == null) return;

            _targetIdToDelete = employeeId;
            _deleteType = "Employee";
            deleteModalTitle = "تاییدیه حذف قطعی پرونده پرسنلی";
            targetDeleteName = emp.FullName ?? string.Empty;
            deleteTargetMessage = $"آیا از حذف کامل پرونده پرسنلی کارمند با نام «{targetDeleteName}» مطمئن هستید؟";
            deleteModalSubMessage = "توجه: با تایید نهایی، کلیه رکوردهای حضور و غیاب و سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد.";

            ActiveMobileDropdownId = null;
            showDeleteModal = true;
        }

        // 🗑️ متد مشترک ۲: بیدار کردن مودال حذف برای ساعت کاری (بدون کدهای اضافه)
        protected void TriggerDeleteTimeRecord(Attendance att)
        {
            if (att == null) return;

            _targetIdToDelete = att.Id;
            _deleteType = "Attendance";
            deleteModalTitle = "تاییدیه حذف قطعی رکورد تردد پرسنل";

            string dateStr = att.ClockInTime?.ToString("yyyy/MM/dd") ?? "تاریخ نامشخص";
            string timeStr = att.ClockInTime?.ToString("HH:mm") ?? "00:00";

            targetDeleteName = $"تردد مورخ {dateStr} ساعت {timeStr}";
            deleteTargetMessage = $"آیا از حذف کامل این رکورد تردد کاری پرسنل در تاریخ {dateStr} مطمئن هستید؟";
            deleteModalSubMessage = "توجه: با تایید نهایی، این ساعت ورود و خروج خاص به طور کامل پاک شده و در محاسبات این ماه لحاظ نخواهد شد.";

            showDeleteModal = true;
        }

        // 🗑️ متد انصراف مشترک برای هر دو حالت
        protected void CancelDeleteModal() => showDeleteModal = false;

        // 🎯 متد نهایی و فوق‌العاده هوشمند اجرای حذف دیتابیس بر اساس نوع کلیک
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
                        SystemMessage = $"✅ پرونده پرسنلی <span style='color: #dc3545; font-weight: 900;'>«{targetDeleteName}»</span> با موفقیت حذف شد.";
                    }
                }
                else if (_deleteType == "Attendance")
                {
                    var att = await DbContext.Attendances.FirstOrDefaultAsync(a => a.Id == _targetIdToDelete);
                    if (att != null)
                    {
                        DbContext.Attendances.Remove(att);
                        SystemMessage = $"✅ رکورد {targetDeleteName} برای کارمند <span style='color: #dc3545; font-weight: 900;'>«{selectedEmployee?.FullName}»</span> با موفقیت ابطال شد.";
                    }
                }
                await DbContext.SaveChangesAsync();
            }
            catch (Exception ex) { SystemMessage = $"❌ خطا در حذف: {ex.Message}"; }

            if (selectedEmployee != null && _deleteType == "Attendance") CalculateFilteredHours(selectedEmployee.Id);
            await RefreshDataAsync();
            await InvokeAsync(StateHasChanged);
        }

    }
}