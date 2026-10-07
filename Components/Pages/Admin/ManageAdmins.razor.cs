using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using sqlite_web.Components.Models.Admin; // کلاس مدل دیتابیس شما
using sqlite_web.Services;
using sqlite_web.Components.Models;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class ManageAdmins
    {
        // 🌟 تزریق سرویس مدیریت وضعیت مرکزی ادمین
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        // 🌟 تزریق دیتابیس برای فرآیندهای ثبت و حذف داخلی صفحه
        [Inject]
        public AppDbContext DbContext { get; set; } = default!;

        // 🔘 متغیرهای محلی و اختصاصی فرانت‌اَند جهت رندر صحیح فرم و جدول (رفع خطاهای تصویر اول)
        protected List<AdminUser> adminList { get; set; } = new(); // لیست مدیران جهت نمایش در جدول پایینی
        protected bool isEditMode { get; set; } = false; // پرچم وضعیت فرم (true = در حال ویرایش / false = ثبت جدید)
        protected string feedbackMessage { get; set; } = ""; // پیام‌های وضعیت فرم به کارمندان

        // ✏️ فیلدهای موقت متصل به کادرهای ورودی ثبت مدیر جدید در فرانت‌اَند
        protected string adminFullName { get; set; } = "";
        protected string adminUsername { get; set; } = "";
        protected string adminPassword { get; set; } = "";
        protected bool pCanAdd { get; set; } = false;
        protected bool pCanEdit { get; set; } = false;
        protected bool pCanDelete { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            // 🛡️ اجرای آنی موتور پایش امنیتی مسیر در سرویس مرکزی؛ اگر ادمین غیرمجاز باشد خودکار اخراج می‌شود
            await AdminState.CheckAccessAndRedirectAsync();

            // بارگذاری فیزیکی لیست مدیران از دیتابیس
            await LoadAdminsDataAsync();
        }

        // متد واکشی لیست ادمین‌ها از دیتابیس SQLite
        protected async Task LoadAdminsDataAsync()
        {
            if (DbContext != null)
            {
                adminList = await DbContext.Admins.AsNoTracking().ToListAsync();
            }
        }

        // متدهای دکمه‌های فرم فرانت‌اَند شما که نباید خالی بمانند
        protected async Task SaveAdmin()
        {
            // منطق ذخیره/ویرایش ادمین در دیتابیس شما در این قسمت اجرا می‌شود
            await LoadAdminsDataAsync();
        }

        protected void StartEdit(AdminUser admin)
        {
            isEditMode = true;
            adminUsername = admin.Username;
            // پر کردن بقیه فیلدها جهت ویرایش...
        }

        protected void CancelEdit()
        {
            isEditMode = false;
            adminUsername = "";
            adminPassword = "";
        }

        protected async Task DeleteAdmin(int id)
        {
            // منطق حذف رکورد ادمین از دیتابیس شما...
            await LoadAdminsDataAsync();
        }
    }
}
