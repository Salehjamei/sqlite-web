using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin; // کلاس مدل ادمین شما

namespace sqlite_web.Services
{
    public class AdminStateService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        // 🔘 متغیرهای عمومی احراز هویت مورد نیاز هدر و تمام صفحات سیستم
        public bool IsAuthorized { get; set; } = true; // سطح دسترسی (true = مدیر ارشد یا دارای دسترسی / false = ادمین بدون دسترسی)
        public string adminFullName { get; set; } = ""; // نام کامل مدیر آنلاین
        public string adminUsername { get; set; } = ""; // نام کاربری مدیر آنلاین (توکن فعال)
        public string adminPassword { get; set; } = ""; // رمز عبور مدیر آنلاین
        public bool isCurrentUserSuperAdmin { get; set; } = false; // 🌟 پرکاربرد: تشخیص مستقیم ادمین ارشد (SuperAdmin) برای پایداری منوها

        // 🛡️ فیلدهای سه‌گانه سطوح دسترسی تفکیکی ادمین آنلاین
        public bool pCanAdd { get; set; } = false; // مجوز ثبت کارمند جدید
        public bool pCanEdit { get; set; } = false; // مجوز ویرایش کارمندان
        public bool pCanDelete { get; set; } = false; // مجوز حذف کارمندان

        // ⚙️ پرچم‌های کنترلی چرخه حیات بلایزر
        public bool IsChecked { get; private set; } = false; // تایید اتمام اسکن توکن مرورگر
        public string currentAdminPerms { get; set; } = "ادمین معمولی"; // 🌟 پرکاربرد: نمایش متنی سطح دسترسی در پنل‌ها
        public event Action? OnChange; // رویداد مرکزی جهت مطلع‌سازی آنی هدر برای رندر دکمه خروج

        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }
        /// <summary>
        /// 🔍 متد پایش متمرکز امنیتی مسیرها با شرط‌های نفوذناپذیر دیتابیس لوکال
        /// توضیحات کد: این متد جلوی لوپ ریدایرکت‌های اشتباه ادمین ارشد را در فاز پیش‌رندر سرور می‌گیرد.
        /// </summary>
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                // خواندن فیزیکی توکن سشن فعال از روی لوکال استوریج مرورگر کلاینت
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    // کوئری مستقیم روی دیتابیس SQLite بر اساس نام کاربری ادمین
                    var admin = await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                    if (admin != null)
                    {
                        FillAdminData(admin); // لود کامل فیلدها و سطوح دسترسی (IsAuthorized و superadmin)

                        // 🛡️ ۱. شرط نجات از صفحه لاگین: اگر ادمین مجاز آنلاین است و سیستم به اشتباه در مسیر لاگین مانده، او را به پنل هدایت کن
                        if (_navigationManager.Uri.Contains("/admin/login", StringComparison.OrdinalIgnoreCase))
                        {
                            IsChecked = true;
                            NotifyStateChanged();
                            _navigationManager.NavigateTo("/admin", forceLoad: false); // هدایت روان بدون ریفرش تخریب‌کننده کش
                            return;
                        }

                        // 🛡️ ۲. گارد امنیتی ادمین کیوسک: جلوگیری از دسترسی مدیران معمولی به بخش مدیریت مدیران
                        if (!IsAuthorized && _navigationManager.Uri.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
                        {
                            _navigationManager.NavigateTo("/", forceLoad: true);
                        }

                        IsChecked = true;
                        NotifyStateChanged(); // فعال‌سازی فوری دکمه‌های هدر
                        return;
                    }
                }

                // 🔒 ۳. فیکس طلایی: شرط ریدایرکت به لاگین مستقل؛ فقط و فقط زمانی مجاز است ریدایرکت کند که
                // اولاً فاز پیش‌رندر سرور تمام شده باشد (IsChecked شده باشد) و ثانیاً توکن "واقعاً در مرورگر خالی باشد"
                if (IsChecked && string.IsNullOrEmpty(savedToken))
                {
                    Reset(); // شستشوی متغیرها فقط در صورت نبودن واقعی توکن
                    if (_navigationManager.Uri.Contains("/admin", StringComparison.OrdinalIgnoreCase) &&
                        !_navigationManager.Uri.Contains("/admin/login", StringComparison.OrdinalIgnoreCase))
                    {
                        _navigationManager.NavigateTo("/admin/login", forceLoad: false);
                    }
                }
            }
            catch
            {
                // مهار خطاهای تعامل با لایه اسکریپتی مرورگر در فاز پیش‌رندر سرور (Prerendering)
                // 🔒 فیکس کلیدی: در این بخش هرگز پرچم IsChecked را true نمی‌کنیم تا سیستم شروط ریدایرکت را عجولانه اجرا نکند
                return;
            }
            finally
            {
                // پرچم اطمینان از اتمام اسکن فقط پس از لود کلاینت و اتصال زنده مرورگر فعال می‌شود
                if (!IsChecked)
                {
                    IsChecked = true;
                    NotifyStateChanged(); // بیدار کردن نهایی گرافیک منوهای هدر
                }
            }
        }




        /// <summary>
        /// 🔑 متد ورود مرکزی (Login) مستقر در هسته سرویس
        /// </summary>
        public async Task<bool> LoginAsync(string inputUsername, string inputPassword)
        {
            if (_dbContext == null || string.IsNullOrWhiteSpace(inputUsername) || string.IsNullOrWhiteSpace(inputPassword))
                return false;

            var admin = await _dbContext.Admins.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username.ToLower() == inputUsername.ToLower() && a.Password == inputPassword);

            if (admin != null)
            {
                FillAdminData(admin);

                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "admin_session_token", admin.Username);
                }
                catch { }

                IsChecked = true;
                NotifyStateChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 🏃‍♂️ متد خروج مرکزی (Logout) و شستشوی کامل سشن‌ها برای هدر سراسری
        /// </summary>
        public async Task LogoutAsync()
        {
            Reset();
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
            }
            catch { }

            NotifyStateChanged();
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        // متد کمکی جهت تزریق مقادیر رکورد دیتابیس به متغیرهای عمومی سرویس
        private void FillAdminData(AdminUser admin)
        {
            adminUsername = admin.Username;
            adminPassword = admin.Password;
            adminFullName = admin.Username;
            isCurrentUserSuperAdmin = admin.IsSuperAdmin; // 👑 لود فیزیکی تیک سوپر ادمین از روی دیتابیس SQLite

            // لود مستقل و بدون واسطه تیک‌های عملیاتی پرسنل از روی هارد دیتابیس
            pCanAdd = admin.CanAddEmployee;
            pCanEdit = admin.CanEditEmployee;
            pCanDelete = admin.CanDeleteEmployee;

            currentAdminPerms = isCurrentUserSuperAdmin ? "سوپر ادمین ارشد" : "مدیر معمولی";

            // 🛡️ منطق گارد امنیتی هدر لایوت:
            // الف) دکمه رفتن به کیوسک یا پنل مدیریت (IsAuthorized) روشن می‌شود اگر کاربر سوپر ادمین باشد "یا" حداقل یکی از تیک‌های عادی کارمندان را داشته باشد.
            if (isCurrentUserSuperAdmin || pCanAdd || pCanEdit || pCanDelete)
            {
                IsAuthorized = true;
            }
            else
            {
                IsAuthorized = false; // ادمین بدون هیچ دسترسی (ادمین کیوسک) که دکمه ورود به پنل برایش مسدود است
            }
        }


        private void Reset()
        {
            adminUsername = "";
            adminFullName = "";
            adminPassword = "";
            IsAuthorized = true;
            isCurrentUserSuperAdmin = false;
            pCanAdd = false;
            pCanEdit = false;
            pCanDelete = false;
            currentAdminPerms = "بدون هویت";
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
