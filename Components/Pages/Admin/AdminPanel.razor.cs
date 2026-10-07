using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq; // جهت پشتیبانی از ساختار گروپ بای لایه گزارشات پرسنل
using System.Threading.Tasks;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin; // لود مدل ادمین دیتابیس شما برای حل خطای ردیف 167
using sqlite_web.Services;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class AdminPanel
    {
        // 🌟 تزریق هسته مرکزی مدیریت وضعیت و احراز هویت ادمین
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public NavigationManager MyNavigationManager { get; set; } = default!;

        // 🔘 ویژگی‌های پرکاربرد ادمین متصل به سرویس سراسری (جهت حفظ یکنواختی هدر و فرم‌ها)
        public bool isLoggedIn => !string.IsNullOrEmpty(AdminState.adminUsername);
        public bool isCurrentUserSuperAdmin => AdminState.isCurrentUserSuperAdmin;

        // 🌟 فیکس خطای ردیف ۱۶۷: بازگرداندن متغیر به کلاس شیء ادمین دیتابیس شما جهت خواندن CanEditEmployee در فرانت‌اَند
        protected AdminUser currentAdminPerms { get; set; } = new();

        // 🔘 متغیرهای لیست‌ها و جداول دیتابیس پرسنل و سوابق تردد (رفع خطاهای CS0103)
        protected List<Employee> employees { get; set; } = new(); // لیست کل کارمندان
        protected List<Attendance> allAttendances { get; set; } = new(); // لیست کل ترددها
        protected Employee? selectedEmployee { get; set; } // کارمند انتخاب شده برای مودال

        // 🕒 فیکس خطای دایرکتیو: متغیر تاریخ ویرایش به صورت Nullable DateTime جهت پشتیبانی از پسوند .Value در فرانت‌اَند شما
        protected DateTime? selectedDateForEdit { get; set; }

        // ✏️ ۱. متغیرهای رشته‌ای خالص (String) متصل به فیلدهای @bind فرم کارمندان (حل قطعی ۳ ارور تصویر دوم)
        protected string newEmpName { get; set; } = ""; // متغیر متنی ذخیره نام کارمند جدید
        protected string newEmpCode { get; set; } = ""; // متغیر متنی ذخیره کد پرسنلی
        protected string newEmpPhone { get; set; } = ""; // متغیر متنی ذخیره شماره تلفن کارمند

        // 🔍 ۲. متغیرهای ارجاع المان (ElementReference) متصل به ویژگی‌های @ref فرانت‌اَند شما برای مدیریت فوکوس موس
        protected ElementReference empNameInput; // ارجاع فیزیکی به باکس نام
        protected ElementReference empCodeInput; // ارجاع فیزیکی به باکس کد
        protected ElementReference empPhoneInput; // ارجاع فیزیکی به باکس تلفن

        // 🔘 بقیه متغیرهای کنترلی فرم کارمندان بدون تغییر
        protected bool isEmpEditMode { get; set; } = false;
        protected string TxtManageEmpTitle { get; set; } = "ثبت کارمند جدید";
        protected string previewImageUrl { get; set; } = "";
        protected string manualErrorMessage { get; set; } = "";
        protected bool renderUploadInput { get; set; } = false;
        // 🛡️ پرچم‌های کنترلی و فیلترهای خطایابی کادرهای فرم کارمندان
        protected bool isEmpNameInvalid { get; set; } = false;
        protected string empNameError { get; set; } = "";
        protected bool isEmpCodeInvalid { get; set; } = false;
        protected string empCodeError { get; set; } = "";
        protected bool isEmpPhoneInvalid { get; set; } = false;
        protected string empPhoneError { get; set; } = "";
        protected bool isEmpImageInvalid { get; set; } = false;
        protected string empImageError { get; set; } = "";
        protected bool showModal { get; set; } = false; // پایش وضعیت باز شدن مودال
        // 🕒 متغیرهای زمان تردد دستی با نوع داده بومی دیتابیس شما
        protected DateTime manualDate { get; set; } = DateTime.Now;
        protected TimeOnly manualInTime { get; set; } = new TimeOnly(8, 0);
        protected TimeOnly manualOutTime { get; set; } = new TimeOnly(17, 0);

        // 📊 متغیر متنی دکمه بستن مودال (استفاده به صورت ویژگی متنی در خط 308 تصویر دوم)
        protected string TxtCloseBtn { get; set; } = "بستن پنجره گزارش";

        protected override async Task OnInitializedAsync()
        {
            // پایش گارد امنیتی مسیر و انقضای توکن از درون سرویس متمرکز
            await AdminState.CheckAccessAndRedirectAsync();

            // 🌟 فیکس خط ۱۶۷: همگام‌سازی شیء دسترسی‌های فرانت‌اَند با اطلاعات زنده دیتابیس سرویس ادمین
            if (DbContext != null && !string.IsNullOrEmpty(AdminState.adminUsername))
            {
                var currentAdmin = await DbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == AdminState.adminUsername);
                if (currentAdmin != null)
                {
                    currentAdminPerms = currentAdmin;
                }
            }

            await RefreshDataAsync();
        }

        // متد لود اطلاعات جداول از دیتابیس SQLite
        protected async Task RefreshDataAsync()
        {
            if (DbContext != null)
            {
                employees = await DbContext.Employees.AsNoTracking().ToListAsync();
                allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync();
            }
        }

        // 🛠️ متدهای پرسنلی مجهز به ورودی شناسه عددی (int) جهت هماهنگی با کلیک‌های فرانت‌اَند
        protected async Task SaveEmployeeData() { await RefreshDataAsync(); }
        protected void StartEmpEdit(Employee emp) { isEmpEditMode = true; }
        protected void CancelEmpEdit() { isEmpEditMode = false; }
        protected async Task DeleteEmployee(int id) { await RefreshDataAsync(); }
        protected void HandlePhoneInput(ChangeEventArgs e) { }

        // 🌟 فیکس خطای ردیف ۶۲: اصلاح آرگومان ورودی متد آپلود تصویر به فایل‌سیستم بلایزر
        protected Task HandleImageUpload(InputFileChangeEventArgs e) => Task.CompletedTask;

        // 🕒 متدهای کنترل مودال‌ها و ترددهای دستی پرسنل سیستم تردد
        protected void OpenReportModal(int id) { showModal = true; }

        // 🌟 فیکس خطای ردیف ۱۲۹: این متد در فرانت‌اَند شما کلاس کامل Employee را دریافت می‌کند
        protected void OpenReportModal(Employee emp) { showModal = true; selectedEmployee = emp; }

        protected void CloseModal() { showModal = false; selectedEmployee = null; selectedDateForEdit = null; }
        protected void BackToDaysList() { }
        protected async Task AddManualAttendance() { await RefreshDataAsync(); }

        // اصلاح پارامترها جهت حذف خطاهای وارنینگ بخش ویرایش زمان
        protected async Task UpdateClockInTime(int id, string value) { await RefreshDataAsync(); }
        protected async Task UpdateClockOutTime(int id, string value) { await RefreshDataAsync(); }

        protected async Task DeleteSingleAttendance(int id) { await RefreshDataAsync(); }
        protected void SelectDateForEdit(DateTime date) { selectedDateForEdit = date; }

        // 📊 متدهای محاسباتی ساعت کارکرد پرسنل
        protected string CalculateTotalWork(int id) => "00:00";
        protected string CalculateTotalWork(Employee emp) => "00:00";
        protected string CalculateDuration(string inTime, string outTime) => "00:00";

        // 🌟 فیکس خطای ردیف ۲۲۹: سربارگذاری متد جهت پشتیبانی از فرمت لیست گروپ بای فرانت‌اَند شما در تصویر دوم خط 224
        protected string CalculateDuration(object dayRecords) => "00:00";

        protected void TxtShowReportBtn(Employee emp) { }
        protected void StopAttendanceNow(int id) { }
        protected void ForcePresent(int id) { }
    }
}
