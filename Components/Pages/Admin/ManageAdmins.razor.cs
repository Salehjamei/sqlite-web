using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using sqlite_web.Components.Models.Admin; // فضای نام مدل جدول AdminUser شما
using sqlite_web.Services; // فضای نام لایه سرویس مرکزی ادمین
using sqlite_web.Components.Models; // فضای نام دیتابیس AppDbContext پروژه

namespace sqlite_web.Components.Pages.Admin
{
    /// <summary>
    /// کلاس بک‌اَند تفکیک‌شده صفحه کنترل و اعطای سطوح دسترسی مدیران سیستم
    /// </summary>
    public partial class ManageAdmins
    {
        // 🌟 تزریق سرویس مدیریت وضعیت سراسری ادمین‌ها
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        // 🌟 تزریق دیتابیس پروژه جهت ذخیره‌سازی فیزیکی اطلاعات در SQLite
        [Inject]
        public AppDbContext DbContext { get; set; } = default!;

        // 🔘 متغیرهای محلی و اختصاصی فرانت‌اَند جهت رندر جدول و فرم
        protected List<AdminUser> adminList { get; set; } = new(); // لیست مدیران برای جدول پایینی
        protected bool isEditMode { get; set; } = false; // وضعیت فرم (true = ویرایش / false = ثبت جدید)
        protected string feedbackMessage { get; set; } = ""; // پیام‌های وضعیت فرم به کاربر

        // ✏️ فیلدهای موقت متصل به کادرهای ورودی فرم ثبت/ویرایش مدیر جدید
        protected string adminFullName { get; set; } = "";
        protected string adminUsername { get; set; } = "";
        protected string adminPassword { get; set; } = "";
        protected bool pCanAdd { get; set; } = false;
        protected bool pCanEdit { get; set; } = false;
        protected bool pCanDelete { get; set; } = false;

        // 👑 فیکس جدید: متغیر متصل به چک‌باکس جدید سوپر ادمین ارشد در فرانت‌اَند شما
        protected bool pIsSuperAdmin { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            // 🛡️ اجرای موتور پایش امنیتی مسیر در سرویس مرکزی؛ اگر ادمین غیرمجاز باشد خودکار اخراج می‌شود
            await AdminState.CheckAccessAndRedirectAsync();

            // بارگذاری لیست مدیران از دیتابیس برای نمایش در جدول پایینی
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

        /// <summary>
        /// 💾 متد ذخیره و ویرایش اطلاعات مدیر جدید در دیتابیس SQLite
        /// </summary>
        protected async Task SaveAdmin()
        {
            if (DbContext == null) return;

            // اعتبارسنجی اولیه کادرهای ورودی فرم
            if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            {
                feedbackMessage = "❌ لطفاً نام کاربری و رمز عبور را وارد کنید!";
                return;
            }

            if (isEditMode)
            {
                // 🟢 حالت الف) ویرایش مشخصات ادمین قدیمی موجود در دیتابیس
                var existingAdmin = await DbContext.Admins.FirstOrDefaultAsync(a => a.Username == adminUsername);
                if (existingAdmin != null)
                {
                    existingAdmin.Password = adminPassword;
                    existingAdmin.IsSuperAdmin = pIsSuperAdmin; // ویرایش وضعیت سوپر ادمین
                    existingAdmin.CanAddEmployee = pCanAdd;
                    existingAdmin.CanEditEmployee = pCanEdit;
                    existingAdmin.CanDeleteEmployee = pCanDelete;

                    feedbackMessage = "✅ مشخصات مدیر با موفقیت ویرایش شد.";
                }
            }
            else
            {
                // 🔵 حالت ب) ثبت یک مدیر جدید در دیتابیس با دسترسی‌های تفکیکی تفویض شده
                // ابتدا بررسی تکراری نبودن نام کاربری در سیستم
                var checkDuplicate = await DbContext.Admins.AnyAsync(a => a.Username.ToLower() == adminUsername.ToLower());
                if (checkDuplicate)
                {
                    feedbackMessage = "❌ این نام کاربری قبلاً در سیستم ثبت شده است!";
                    return;
                }

                var newAdmin = new AdminUser
                {
                    Username = adminUsername,
                    Password = adminPassword,
                    IsSuperAdmin = pIsSuperAdmin, // 👑 ذخیره فیزیکی تیک سوپر ادمینی در دیتابیس SQLite
                    CanAddEmployee = pCanAdd,
                    CanEditEmployee = pCanEdit,
                    CanDeleteEmployee = pCanDelete
                };

                DbContext.Admins.Add(newAdmin);
                feedbackMessage = "✅ مدیر جدید با موفقیت در سیستم ثبت شد.";
            }

            // ذخیره‌سازی نهایی تغییرات در هارد دیسک لوکال
            await DbContext.SaveChangesAsync();

            // بارگذاری مجدد لیست ادمین‌ها برای به‌روزرسانی جدول فرانت‌اَند
            await LoadAdminsDataAsync();

            // ریست کردن و پاک‌سازی کادرهای فرم پس از عملیات موفق
            pIsSuperAdmin = false; pCanAdd = false; pCanEdit = false; pCanDelete = false;
            adminUsername = ""; adminPassword = ""; isEditMode = false;
        }

        // متد کلیک دکمه ویرایش یکی از مدیران جدول
        protected void StartEdit(AdminUser admin)
        {
            isEditMode = true;
            feedbackMessage = "";
            adminUsername = admin.Username;
            adminPassword = admin.Password;
            pIsSuperAdmin = admin.IsSuperAdmin; // بارگذاری تیک سوپر ادمین در فرم
            pCanAdd = admin.CanAddEmployee;
            pCanEdit = admin.CanEditEmployee;
            pCanDelete = admin.CanDeleteEmployee;
        }

        // متد کلیک دکمه انصراف از ویرایش
        protected void CancelEdit()
        {
            isEditMode = false;
            feedbackMessage = "";
            pIsSuperAdmin = false; pCanAdd = false; pCanEdit = false; pCanDelete = false;
            adminUsername = ""; adminPassword = "";
        }

        // متد کلیک دکمه حذف فیزیکی یک مدیر از سیستم
        protected async Task DeleteAdmin(int id)
        {
            if (DbContext == null) return;

            var admin = await DbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null)
            {
                // جلوگیری از حذف خودکار اکانت ادمین اصلی سیستم
                if (admin.Username.ToLower() == "admintop")
                {
                    feedbackMessage = "❌ مدیر اصلی سیستم (AdminTop) قابل حذف نیست!";
                    return;
                }

                DbContext.Admins.Remove(admin);
                await DbContext.SaveChangesAsync();
                feedbackMessage = "🗑️ مدیر انتخاب شده با موفقیت از سیستم حذف شد.";
            }

            await LoadAdminsDataAsync(); // ریفرش جدول پایینی
        }
    }
}
