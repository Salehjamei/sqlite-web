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
    // 👈 فیکس باگ کلیدی: افزودن ارث‌بری از ComponentBase جهت فعالسازی StateHasChanged و لایف‌سایکل کامپوننت
    public partial class AdminPanel : ComponentBase
    {
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public AdminStateService AdminState { get; set; } = default!;

        // 🔘 پروپرتی‌های هوشمند سطوح دسترسی تفکیکی متصل به هسته سرویس مرکزی
        protected bool canAddPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanAddEmployee ?? false);
        protected bool canEditPerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanEditEmployee ?? false);
        protected bool canDeletePerm => AdminState.isCurrentUserSuperAdmin || (AdminState.CurrentOnlineAdmin?.CanDeleteEmployee ?? false);

        protected string TxtManageEmpTitle { get; set; } = "تعریف و مدیریت اطلاعات کل پرسنل";
        protected string SystemMessage { get; set; } = string.Empty;
        // فیلدها و متغیرهای جدید برای مدیریت ویرایش ساعت کاری و کارکرد ماهانه
        protected bool isAttEditMode { get; set; } = false;
        private int? _editingAttendanceId = null;
        protected string TotalMonthlyHours { get; set; } = "00:00 ساعت";
        // فیلدهای بایند شده به فرم ثبت و ویرایش کارمندان
        protected string newEmpName { get; set; } = string.Empty;
        protected string newEmpCode { get; set; } = string.Empty;
        protected string newEmpPhone { get; set; } = string.Empty;
        protected bool isEmpEditMode { get; set; } = false;
        private int? _editingEmployeeId = null;

        // متغیرهای مدیریت پنجره‌های بازشو (Modal) و ترددها
        protected bool showModal { get; set; } = false;
        protected bool showDeleteModal { get; set; } = false;
        protected string deleteTargetMessage { get; set; } = string.Empty;
        private string deleteType { get; set; } = string.Empty; // مقادیر: "EMP" یا "ATT"
        private int _targetDeleteId = 0;

        protected Employee? selectedEmployee { get; set; }
        // ساعت پیش‌فرض جدید سیستم از ۰۷:۰۰ صبح تا ۱۷:۳۰ عصر
        protected DateTime manualDate { get; set; } = DateTime.Today;
        protected string manualInTime { get; set; } = "07:00";
        protected string manualOutTime { get; set; } = "17:30";

        protected List<Employee> employees { get; set; } = new();
        protected List<Attendance> allAttendances { get; set; } = new();
        // فیلترها و پروپرتی‌های تفکیک ساعت کاری و اضافه کاری
        protected string SelectedMonthFilter { get; set; } = string.Empty;
        protected string TotalRegularHours { get; set; } = "00:00";
        protected string TotalOvertimeHours { get; set; } = "00:00";
        protected List<MonthOption> AvailableMonths { get; set; } = new();

        // کلاس کمکی برای پر کردن سلکتور ماه‌ها
        public class MonthOption
        {
            public string Value { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        /// <summary>
        /// متد لایف‌سایکل اجرا شده پس از رندر فیزیکی صفحه در مرورگر کلاینت
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // اجرای گارد امنیتی بررسی اصالت توکن
                await AdminState.CheckAccessAndRedirectAsync();

                if (AdminState.IsAuthorized)
                {
                    await RefreshDataAsync();
                }
                StateHasChanged();
            }
        }

        /// <summary>
        /// واکشی و تازه‌سازی فیزیکی لیست کارمندان و کل ترددها از SQLite
        /// </summary>
        /// <summary>
        /// 🔄 متد متمرکز تازه‌سازی اطلاعات دیتابیس و بازنویسی آنی کادر کارکرد ماهانه درون مودال
        /// </summary>
        private async Task RefreshDataAsync()
        {
            if (DbContext == null) return;
            employees = await DbContext.Employees.AsNoTracking().ToListAsync() ?? new();
            allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync() ?? new();

            if (selectedEmployee != null)
            {
                LoadAvailableMonths(selectedEmployee.Id);
                CalculateFilteredHours(selectedEmployee.Id); // بازشماری خودکار آنی پس از ثبت دستی، حذف یا توقف تردد
            }
        }

        /// <summary>
        /// مدیریت ذخیره اطلاعات یا ثبت تغییرات پرسنل همراه با اعتبارسنجی لایه دسترسی‌ها
        /// </summary>
            // 🟢 متغیرهای جدید مدیریت مودال فرم کارمندان و عکس
        protected bool showEmpFormModal { get; set; } = false;
        protected string previewImageUrl { get; set; } = string.Empty;

        protected void OpenAddEmployeeModal()
        {
            CancelEmpEdit();
            showEmpFormModal = true;
        }

        protected void CloseEmpFormModal()
        {
            showEmpFormModal = false;
            CancelEmpEdit();
        }

        /// <summary>
        /// متد آسنکرون تبدیل تصویر به فرمت استاندارد Base64 جهت ذخیره‌سازی در فیلد ImagePath دیتابیس
        /// </summary>
        /// <summary>
        /// متد اصلاح شده تبدیل و فشرده‌سازی آنلاین تصویر جهت مهار خطای کرش هاب تعاملی
        /// </summary>
        /// <summary>
        /// متد ارتقایافته بارگذاری تصویر پرسنل با کیفیت بالا و ابعاد استاندارد HD بدون خطر کرش سیستم
        /// </summary>
        protected async Task HandleImageUpload(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    // 🟢 افزایش ابعاد به 500x500 پیکسل جهت خروجی کاملاً شفاف و باکیفیت
                    // تبدیل خودکار تمام فرمت‌ها به image/jpeg تایید شده برای پایداری بیشتر
                    var highQualFile = await file.RequestImageFileAsync("image/jpeg", 500, 500);

                    // تخصیص آرایه بایت متناسب با حجم فایل جدید
                    var buffer = new byte[highQualFile.Size];

                    // باز کردن جریان استریم با بافر امن ۵ مگابایتی
                    using (var stream = highQualFile.OpenReadStream(5120000))
                    {
                        await stream.ReadAsync(buffer);
                    }

                    // تولید دیتا یوآرآی تصویر با رزولوشن ایده‌آل و کیفیت بالا
                    previewImageUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(buffer)}";
                    SystemMessage = "📸 تصویر با رزولوشن بالا و کیفیت عالی بارگذاری شد.";
                }
            }
            catch (Exception ex)
            {
                SystemMessage = $"❌ خطا در لود تصویر باکیفیت: {ex.Message}";
            }
        }



        protected async Task SaveEmployeeData()
        {
            if (string.IsNullOrWhiteSpace(newEmpName) || string.IsNullOrWhiteSpace(newEmpCode))
            {
                SystemMessage = "❌ نام کارمند و کد پرسنلی الزامی است!";
                return;
            }

            if (isEmpEditMode && _editingEmployeeId.HasValue)
            {
                if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: مجوز ویرایش ندارید!"; return; }
                var emp = await DbContext.Employees.FirstOrDefaultAsync(e => e.Id == _editingEmployeeId.Value);
                if (emp != null)
                {
                    emp.FullName = newEmpName;
                    emp.Phone = newEmpPhone;
                    emp.PersonnelCode = newEmpCode;
                    emp.ImagePath = previewImageUrl; // 👈 اعمال عکس جدید در ویرایش پرسنل
                    DbContext.Employees.Update(emp);
                    SystemMessage = "✅ مشخصات و تصویر پرسنل با موفقیت ویرایش شد.";
                }
            }
            else
            {
                if (!canAddPerm) { SystemMessage = "❌ خطای امنیتی: مجوز ثبت کارمند جدید را ندارید!"; return; }
                // 👈 ثبت کارمند جدید به همراه عکس فیزیکی در دیتابیس
                DbContext.Employees.Add(new Employee { FullName = newEmpName, Phone = newEmpPhone, PersonnelCode = newEmpCode, ImagePath = previewImageUrl });
                SystemMessage = "✅ کارمند جدید با موفقیت به همراه تصویر ثبت شد.";
            }

            await DbContext.SaveChangesAsync();
            await RefreshDataAsync();
            CloseEmpFormModal();
        }

        protected void StartEmpEdit(Employee emp)
        {
            isEmpEditMode = true;
            _editingEmployeeId = emp.Id;
            newEmpName = emp.FullName;
            newEmpPhone = emp.Phone ?? string.Empty;
            newEmpCode = emp.PersonnelCode ?? string.Empty;
            previewImageUrl = emp.ImagePath ?? string.Empty; // لود عکس قبلی پرسنل در فرم کادر پیش‌نمایش
            showEmpFormModal = true; // باز شدن خودکار فرم مودال به محض کلیک روی مداد ویرایش جدول
        }

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
        /// راه‌اندازی و باز کردن مودال تایید حذف کارمند (نیاز به دسترسی حذف)
        /// </summary>
        protected void TriggerDeleteEmployee(int id, string name)
        {
            if (!canDeletePerm) { SystemMessage = "❌ خطای امنیتی: شما مجوز حذف پرسنل را ندارید!"; return; }
            deleteType = "EMP";
            _targetDeleteId = id;
            deleteTargetMessage = $"آیا از حذف کامل پرونده پرسنلی «{name}» و کل تاریخچه ترددهای او مطمئن هستید؟";
            showDeleteModal = true;
        }

        /// <summary>
        /// راه‌اندازی و باز کردن مودال تایید حذف ساعت کاری خاص (نیاز به دسترسی ویرایش)
        /// </summary>
        protected void TriggerDeleteAttendance(int id)
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: برای حذف ساعت کاری به مجوز ویرایش نیاز دارید!"; return; }
            deleteType = "ATT";
            _targetDeleteId = id;
            deleteTargetMessage = "آیا از حذف این ردیف ساعت کاری (ورود/خروج) انتخاب شده مطمئن هستید؟";
            showDeleteModal = true;
        }

        /// <summary>
        /// اجرای عملیات نهایی حذف قطعی پس از تایید اپراتور در مودال تعاملی
        /// </summary>
        protected async Task ExecuteConfirmedDelete()
        {
            showDeleteModal = false;

            if (deleteType == "EMP")
            {
                var emp = await DbContext.Employees.FindAsync(_targetDeleteId);
                if (emp != null) DbContext.Employees.Remove(emp);
                SystemMessage = "🗑️ پرونده پرسنلی با موفقیت حذف شد.";
            }
            else if (deleteType == "ATT")
            {
                var att = await DbContext.Attendances.FindAsync(_targetDeleteId);
                if (att != null) DbContext.Attendances.Remove(att);
                SystemMessage = "🗑️ ردیف ساعت کاری انتخاب شده با موفقیت حذف گردید.";
            }

            await DbContext.SaveChangesAsync();

            // 🟢 این فراخوانی باعث می‌شود کادر سبز رنگ بعد از حذف ردیف فوراً آپدیت شود
            await RefreshDataAsync();
        }


        /// <summary>
        /// حاضر کردن سریع کارمند از جدول (نیاز به دسترسی ویرایش ساعت کاری)
        /// </summary>
        protected async Task ForcePresent(int empId)
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: فاقد مجوز ثبت تردد سریع!"; return; }
            DbContext.Attendances.Add(new Attendance { EmployeeId = empId, ClockInTime = DateTime.Now });
            await DbContext.SaveChangesAsync();
            SystemMessage = "🟢 وضعیت پرسنل به حاضر تغییر یافت.";
            await RefreshDataAsync();
        }

        /// <summary>
        /// متوقف کردن سریع تردد کارمند از جدول (نیاز به دسترسی ویرایش ساعت کاری)
        /// </summary>
        protected async Task StopAttendanceNow(int attId)
        {
            if (!canEditPerm) { SystemMessage = "❌ خطای امنیتی: Fاقد مجوز ثبت تردد سریع!"; return; }
            var att = await DbContext.Attendances.FindAsync(attId);
            if (att != null)
            {
                att.ClockOutTime = DateTime.Now;
                SystemMessage = "🛑 تردد پرسنل متوقف شد.";
                await DbContext.SaveChangesAsync();
                await RefreshDataAsync();
            }
        }

        /// <summary>
        /// ثبت دستی و آفلاین ساعت کاری روزهای گذشته (نیاز به دسترسی ویرایش ساعت کاری)
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
                    // حالت ویرایش ساعت کاری ثبت شده پرسنل
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
                    // حالت ثبت جدید
                    DbContext.Attendances.Add(new Attendance
                    {
                        EmployeeId = selectedEmployee.Id,
                        ClockInTime = inTime,
                        ClockOutTime = outTime
                    });
                    SystemMessage = "✅ ردیف ساعت کاری دستی جدید با موفقیت ثبت گردید.";
                }

                await DbContext.SaveChangesAsync();
                await RefreshDataAsync();
                CancelAttEdit();
            }
            catch
            {
                SystemMessage = "❌ فرمت زمان اشتباه است (مثال معتبر: 07:00 یا 17:30)";
            }
        }

        protected void CloseModal() { showModal = false; selectedEmployee = null; }
        protected void CancelDeleteModal() { showDeleteModal = false; }
        protected void ClearSystemMessage() => SystemMessage = string.Empty;
        protected string GetSingleDuration(Attendance att)
        {
            if (att.ClockInTime == null || att.ClockOutTime == null) return "در حال تردد";
            var diff = att.ClockOutTime.Value - att.ClockInTime.Value;
            return $"{diff.Hours:00}:{diff.Minutes:00}";
        }
        /// <summary>
        /// 🧮 محاسبه هوشمند مجموع کل کارکرد پرسنل در ماه جاری
        /// </summary>
        private void CalculateMonthlyTotalHours(int employeeId)
        {
            var now = DateTime.Now;
            var currentMonthRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId
                         && a.ClockInTime.HasValue
                         && a.ClockOutTime.HasValue
                         && a.ClockInTime.Value.Year == now.Year
                         && a.ClockInTime.Value.Month == now.Month)
                .ToList();

            double totalMinutes = 0;
            foreach (var rec in currentMonthRecords)
            {
                totalMinutes += (rec.ClockOutTime!.Value - rec.ClockInTime!.Value).TotalMinutes;
            }

            int hours = (int)(totalMinutes / 60);
            int minutes = (int)(totalMinutes % 60);
            TotalMonthlyHours = $"{hours:00}:{minutes:00} ساعت";
        }

        // متدهای سوییچ وضعیت فرم به حالت ویرایش ساعت کاری ردیف
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
        /// استخراج تمام ماه‌هایی که این پرسنل در آن‌ها سابقه تردد ثبت شده دارد
        /// </summary>
        private void LoadAvailableMonths(int employeeId)
        {
            AvailableMonths.Clear();
            var now = DateTime.Now;

            // ماه‌های دارای رکورد را پیدا کن
            var dates = allAttendances
                .Where(a => a.EmployeeId == employeeId && a.ClockInTime.HasValue)
                .Select(a => new { a.ClockInTime!.Value.Year, a.ClockInTime.Value.Month })
                .Distinct()
                .OrderByDescending(d => d.Year).ThenByDescending(d => d.Month)
                .ToList();

            foreach (var d in dates)
            {
                var dt = new DateTime(d.Year, d.Month, 1);
                AvailableMonths.Add(new MonthOption
                {
                    Value = $"{d.Year}-{d.Month}",
                    Text = dt.ToString("MMMM yyyy") // نمایش نام ماه و سال به صورت خوانا
                });
            }

            // اگر ماه جاری در لیست نبود، خودکار به عنوان گزینه پیش‌فرض اضافه شود
            var currentMonthValue = $"{now.Year}-{now.Month}";
            if (!AvailableMonths.Any(m => m.Value == currentMonthValue))
            {
                AvailableMonths.Insert(0, new MonthOption { Value = currentMonthValue, Text = now.ToString("MMMM yyyy") });
            }

            // اگر فیلتر هنوز خالی است، روی ماه جاری تنظیم شود
            if (string.IsNullOrEmpty(SelectedMonthFilter))
            {
                SelectedMonthFilter = currentMonthValue;
            }
        }
        /// <summary>
        /// 🧮 متد محاسبه تفکیکی ساعات موظف (۷ تا ۱۷:۳۰) و اضافه کاری (بعد از ۱۷:۳۰) بر اساس ماه انتخابی
        /// </summary>
        private void CalculateFilteredHours(int employeeId)
        {
            if (string.IsNullOrEmpty(SelectedMonthFilter)) return;

            var parts = SelectedMonthFilter.Split('-');
            int targetYear = int.Parse(parts[0]);
            int targetMonth = int.Parse(parts[1]);

            // واکشی ترددهای تایید شده کارمند در ماه و سال فیلتر شده
            var monthlyRecords = allAttendances
                .Where(a => a.EmployeeId == employeeId
                         && a.ClockInTime.HasValue
                         && a.ClockOutTime.HasValue
                         && a.ClockInTime.Value.Year == targetYear
                         && a.ClockInTime.Value.Month == targetMonth)
                .ToList();

            double totalRegularMinutes = 0;
            double totalOvertimeMinutes = 0;

            foreach (var rec in monthlyRecords)
            {
                DateTime clockIn = rec.ClockInTime!.Value;
                DateTime clockOut = rec.ClockOutTime!.Value;

                // تعیین مرز دقیق پایان ساعت کاری موظف در روز تردد (ساعت ۱۷:۳۰ عصر)
                DateTime overtimeThreshold = clockIn.Date.AddHours(17).AddMinutes(30);

                if (clockIn < overtimeThreshold)
                {
                    if (clockOut <= overtimeThreshold)
                    {
                        // کل تردد در بازه موظف بوده است
                        totalRegularMinutes += (clockOut - clockIn).TotalMinutes;
                    }
                    else
                    {
                        // تردد مرز ۱۷:۳۰ را رد کرده است؛ بخش اول موظف، بخش دوم اضافه کاری
                        totalRegularMinutes += (overtimeThreshold - clockIn).TotalMinutes;
                        totalOvertimeMinutes += (clockOut - overtimeThreshold).TotalMinutes;
                    }
                }
                else
                {
                    // شروع و پایان تردد کلاً بعد از ۱۷:۳۰ عصر بوده است (تماماً اضافه کاری)
                    totalOvertimeMinutes += (clockOut - clockIn).TotalMinutes;
                }
            }

            // تبدیل دقایق موظف به فرمت HH:mm
            int regHours = (int)(totalRegularMinutes / 60);
            int regMins = (int)(totalRegularMinutes % 60);
            TotalRegularHours = $"{regHours:00}:{regMins:00}";

            // تبدیل دقایق اضافه کاری به فرمت HH:mm
            int overHours = (int)(totalOvertimeMinutes / 60);
            int overMins = (int)(totalOvertimeMinutes % 60);
            TotalOvertimeHours = $"{overHours:00}:{overMins:00}";
        }
        // تغییر فیلتر ماه توسط اپراتور و بازشماری زنده
        protected void OnMonthSelectorChanged(ChangeEventArgs e)
        {
            SelectedMonthFilter = e.Value?.ToString() ?? string.Empty;
            if (selectedEmployee != null)
            {
                CalculateFilteredHours(selectedEmployee.Id);
            }
        }

        // ارتقای متد باز کردن مودال کارنامه
        protected void OpenReportModal(Employee emp)
        {
            selectedEmployee = emp;
            showModal = true;
            LoadAvailableMonths(emp.Id); // ۱. لود ماه‌های دارای سابقه تردد
            CalculateFilteredHours(emp.Id); // ۲. محاسبه ساعت کاری ماه انتخابی
        }

        // ارتقای متد ریفرش دیتابیس پنل
      

    }
}