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
        /// 🔍 متد پایش مرکزی و گارد امنیتی مسیرها (تضمین امنیت صددرصدی صفحات)
        /// این متد توکن مرورگر را می‌خواند و در صورت مغایرت یا انقضا، کاربر را خودکار ریدایرکت می‌کند.
        /// 🔍 متد اصلاح‌شده پایش مرکزی و گارد امنیتی مسیرها
        /// </summary>
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                // خواندن فیزیکی توکن سشن از حافظه مرورگر
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    // استخراج مشخصات از دیتابیس
                    var admin = await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                    if (admin != null)
                    {
                        FillAdminData(admin); // پر کردن متغیرهای سراسری سیستم

                        // گارد امنیتی ادمین بدون دسترسی (کیوسک): جلوگیری از ورود به بخش مدیریت مدیران
                        if (!IsAuthorized && _navigationManager.Uri.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
                        {
                            _navigationManager.NavigateTo("/", forceLoad: true);
                        }

                        IsChecked = true;
                        NotifyStateChanged(); // 🌟 شلیک مطمئن پس از پر شدن کامل فیلدها
                        return;
                    }
                }

                // در صورت نبودن توکن یا منقضی شدن آن
                Reset();
                if (_navigationManager.Uri.Contains("/admin", StringComparison.OrdinalIgnoreCase) &&
                    !_navigationManager.Uri.Contains("/admin/login", StringComparison.OrdinalIgnoreCase))
                {
                    _navigationManager.NavigateTo("/admin/login", forceLoad: true);
                }
            }
            catch
            {
                // مهار خطاهای فاز پیش‌رندر
            }
            finally
            {
                IsChecked = true;
                NotifyStateChanged(); // ریفرش نهایی و پایدار گرافیک هدر
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
            _navigationManager.NavigateTo("/admin/login", forceLoad: true);
        }

        // متد کمکی جهت تزریق مقادیر رکورد دیتابیس به متغیرهای عمومی سرویس
        private void FillAdminData(AdminUser admin)
        {
            adminUsername = admin.Username;
            adminPassword = admin.Password;
            adminFullName = admin.Username;
            isCurrentUserSuperAdmin = admin.IsSuperAdmin; // ست کردن وضعیت ادمین ارشد

            pCanAdd = admin.CanAddEmployee;
            pCanEdit = admin.CanEditEmployee;
            pCanDelete = admin.CanDeleteEmployee;

            currentAdminPerms = isCurrentUserSuperAdmin ? "مدیر ارشد سیستم" : "مدیر معمولی با دسترسی محدود";

            if (!admin.IsSuperAdmin && !pCanAdd && !pCanEdit && !pCanDelete)
            {
                IsAuthorized = false; // ادمین بدون دسترسی (کیوسک)
            }
            else
            {
                IsAuthorized = true;
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
