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

        // فیلدهای بایند شده به فرم ثبت و ویرایش کارمندان
        protected string newEmpName { get; set; } = string.Empty;
        protected string newEmpCode { get; set; } = string.Empty;
        protected string newEmpPhone { get; set; } = string.Empty;
        protected bool isEmpEditMode { get; set; } = false;
        private int? _editingEmployeeId = null;

        // متغیرهای مدیریت پنجره‌های بازشو (Modal) و حذف
        protected bool showModal { get; set; } = false;
        protected bool showDeleteModal { get; set; } = false;
        protected string deleteTargetMessage { get; set; } = string.Empty;
        private string deleteType { get; set; } = string.Empty;
        private int _targetDeleteId = 0;

        protected Employee? selectedEmployee { get; set; }
        protected DateTime manualDate { get; set; } = DateTime.Today;
        protected string manualInTime { get; set; } = "07:00";
        protected string manualOutTime { get; set; } = "17:30";

        protected List<Employee> employees { get; set; } = new();
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
        /// متد لایف‌سایکل اجرا شده پس از اتمام رندر فیزیکی تگ‌های صفحه در مرورگر کلاینت
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // ۱. اجرای گارد امنیتی بررسی اصالت توکن و سشن ادمین
                await AdminState.CheckAccessAndRedirectAsync();

                // ۲. اگر ادمین آنلاین مجاز است، فرآیند واکشی اطلاعات آغاز شود
                if (AdminState.IsAuthorized)
                {
                    await RefreshDataAsync();
                }

                // ۳. اعلان به کامپوننت جهت تازه‌سازی فوری وضعیت فرانت صفحه
                StateHasChanged();
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
        /// مدیریت عملیات ذخیره‌سازی اطلاعات کارمند جدید یا ثبت ویرایش مشخصات به همراه راست‌آزمایی لایه امنیتی دسترسی‌ها
        /// </summary>
        protected async Task SaveEmployeeData()
        {
            if (string.IsNullOrWhiteSpace(newEmpName) || string.IsNullOrWhiteSpace(newEmpCode))
            {
                SystemMessage = "❌ نام کارمند و کد پرسنلی الزامی است!";
                return;
            }

            if (isEmpEditMode && _editingEmployeeId.HasValue)
            {
                // گارد امنیتی بررسی مجوز ویرایش مشخصات پرسنلی
                if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: شما مجوز ویرایش اطلاعات پرسنل را ندارید!"; return; }

                var emp = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _editingEmployeeId.Value);
                if (emp != null)
                {
                    emp.FullName = newEmpName;
                    emp.Phone = newEmpPhone;
                    emp.PersonnelCode = newEmpCode;
                    emp.ImagePath = previewImageUrl; // به‌روزرسانی نهایی آدرس عکس جدید یا قبلی در دیتابیس
                    DbContext.Employees.Update(emp);
                    SystemMessage = "✅ مشخصات و تصویر پرسنل با موفقیت ویرایش و اصلاح شد.";
                }
            }
            else
            {
                // گارد امنیتی بررسی مجوز افزودن کارمند جدید به سیستم حضور و غیاب
                if (!canAddPerm) { SystemMessage = "❌ خطای امنیتی: حساب کاربری شما فاقد مجوز ثبت کارمند جدید است!"; return; }

                DbContext.Employees.Add(new Employee { FullName = newEmpName, Phone = newEmpPhone, PersonnelCode = newEmpCode, ImagePath = previewImageUrl });
                SystemMessage = "✅ مشخصات کارمند جدید با موفقیت به همراه تصویر در سیستم ثبت شد.";
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
            isEmpEditMode = true;
            _editingEmployeeId = emp.Id;
            newEmpName = emp.FullName;
            newEmpPhone = emp.Phone ?? string.Empty;
            newEmpCode = emp.PersonnelCode ?? string.Empty;
            previewImageUrl = emp.ImagePath ?? string.Empty; // نمایش عکس قبلی در کادر فرم ویرایش پرسنل
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
        /// راه‌اندازی و باز کردن مودال تایید حذف ساعت کاری خاص بر اساس شناسه فیزیکی رکورد تردد
        /// </summary>
        protected void TriggerDeleteAttendance(int id)
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: برای حذف ساعت کاری به سطح دسترسی ویرایش نیاز دارید!"; return; }
            deleteType = "ATT";
            _targetDeleteId = id;
            deleteTargetMessage = "آیا از حذف این ردیف ساعت کاری (ورود/خروج) انتخاب شده مطمئن هستید؟";
            showDeleteModal = true;
        }
        /// <summary>
        /// اجرای عملیات نهایی حذف قطعی اطلاعات پرسنل یا ساعت کاری پس از تایید اپراتور در مودال تعاملی
        /// </summary>
        protected async Task ExecuteConfirmedDelete()
        {
            showDeleteModal = false;

            if (deleteType == "EMP")
            {
                var emp = await DbContext.Employees.FindAsync(_targetDeleteId);
                if (emp != null) DbContext.Employees.Remove(emp);
                SystemMessage = "🗑️ پرونده پرسنلی و تاریخچه ترددهای کاربر با موفقیت حذف گردید.";
            }
            else if (deleteType == "ATT")
            {
                var att = await DbContext.Attendances.FindAsync(_targetDeleteId);
                if (att != null) DbContext.Attendances.Remove(att);
                SystemMessage = "🗑️ ردیف ساعت کاری انتخاب شده با موفقیت از سیستم حذف شد.";
            }

            await DbContext.SaveChangesAsync();
            await RefreshDataAsync(); // تازه‌سازی خودکار آنی جهت آپدیت کادر سبز رنگ دور ردیف‌ها
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
        /// متمتوقف کردن سریع تردد کارمند از درون جدول یا منوی موبایل (تزریق زنده DateTime.Now به فیلد خروج)
        /// </summary>
        protected async Task StopAttendanceNow(int attId)
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: حساب شما فاقد مجوز ثبت تردد سریع است!"; return; }
            var att = await DbContext.Attendances.FindAsync(attId);
            if (att != null)
            {
                att.ClockOutTime = DateTime.Now;
                SystemMessage = "🛑 تردد پرسنل متوقف شد و ساعت خروج ثبت گردید.";
                await DbContext.SaveChangesAsync();
                await RefreshDataAsync();
            }
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
                await RefreshDataAsync();
                CancelAttEdit(); // بازنشانی فیلدهای فرم ساعت به حالت پیش‌فرض (۰۷:۰۰ تا ۱۷:۳۰)
            }
            catch
            {
                SystemMessage = "❌ فرمت زمان وارد شده اشتباه است (مثال معتبر: 07:00 یا 17:30)";
            }
        }

        protected void CloseModal() { showModal = false; selectedEmployee = null; }
        protected void CancelDeleteModal() { showDeleteModal = false; }
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
            manualDate = att.ClockInTime!.Value.Date;
            manualInTime = att.ClockInTime.Value.ToString("HH:mm");
            manualOutTime = att.ClockOutTime!.Value.ToString("HH:mm");
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
        /// 🧮 متد پیشرفته محاسبه دقیق ساعات موظف (۷:۰۰ تا ۱۷:۳۰) و مجموع اضافه کاری (بعد از ۱۷:۳۰)
        /// </summary>
        private void CalculateFilteredHours(int employeeId)
        {
            if (string.IsNullOrEmpty(SelectedMonthFilter)) return;

            var parts = SelectedMonthFilter.Split('-');
            int targetYear = int.Parse(parts[0]); // 🟢 فیکس ایندکس سال
            int targetMonth = int.Parse(parts[1]); // 🟢 فیکس ایندکس ماه

            var monthlyRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue && a.ClockOutTime.HasValue && a.ClockInTime.Value.Year == targetYear && a.ClockInTime.Value.Month == targetMonth).ToList();

            double totalRegularMinutes = 0;
            double totalOvertimeMinutes = 0;

            foreach (var rec in monthlyRecords)
            {
                DateTime clockIn = rec.ClockInTime!.Value;
                DateTime clockOut = rec.ClockOutTime!.Value;
                DateTime overtimeThreshold = clockIn.Date.AddHours(17).AddMinutes(30); // مرز کاری ۱۷:۳۰ عصر

                if (clockIn < overtimeThreshold)
                {
                    if (clockOut <= overtimeThreshold) totalRegularMinutes += (clockOut - clockIn).TotalMinutes;
                    else { totalRegularMinutes += (overtimeThreshold - clockIn).TotalMinutes; totalOvertimeMinutes += (clockOut - overtimeThreshold).TotalMinutes; }
                }
                else totalOvertimeMinutes += (clockOut - clockIn).TotalMinutes;
            }

            int regHours = (int)(totalRegularMinutes / 60);
            int regMins = (int)(totalRegularMinutes % 60);
            TotalRegularHours = $"{regHours:00}:{regMins:00}";

            int overHours = (int)(totalOvertimeMinutes / 60);
            int overMins = (int)(totalOvertimeMinutes % 60);
            TotalOvertimeHours = $"{overHours:00}:{overMins:00}";
        }

        // 🟢 تراز قطعی امضای متد با لایه فرانت رازور جهت صفر شدن ارور CS0103
        protected void OpenReportModal(Employee emp)
        {
            selectedEmployee = emp;
            showModal = true;
            LoadAvailableMonths(emp.Id);
            CalculateFilteredHours(emp.Id);
        }

    }
}