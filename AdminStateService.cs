using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin; // فضای نام مدل جدول AdminUser شما

namespace sqlite_web.Services
{
    public class AdminStateService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        // 🌟 فیکس بنیادی: متغیری که کل اطلاعات مدیر آنلاین را مستقیم از روی رکورد دیتابیس نگه می‌دارد
        public AdminUser? CurrentOnlineAdmin { get; private set; }

        // متغیرهای عمومی احراز هویت که هدر و صفحات به آن نیاز دارند (دیگر به متغیرهای صفحه لاگین ربطی ندارند)
        public bool IsAuthorized { get; set; } = true;
        public string adminFullName => CurrentOnlineAdmin?.Username ?? ""; // واکشی زنده نام از دیتابیس
        public string adminUsername => CurrentOnlineAdmin?.Username ?? ""; // واکشی زنده نام کاربری
        public string adminPassword => CurrentOnlineAdmin?.Password ?? "";

        // فیلدهای سه‌گانه سطوح دسترسی عملیاتی پرسنل استخراج شده از روی سشن واقعی
        public bool pCanAdd => CurrentOnlineAdmin?.CanAddEmployee ?? false;
        public bool pCanEdit => CurrentOnlineAdmin?.CanEditEmployee ?? false;
        public bool pCanDelete => CurrentOnlineAdmin?.CanDeleteEmployee ?? false;
        public bool isCurrentUserSuperAdmin => CurrentOnlineAdmin?.IsSuperAdmin ?? false;

        public bool IsChecked { get; private set; } = false;
        public string currentAdminPerms => isCurrentUserSuperAdmin ? "سوپر ادمین ارشد" : "مدیر معمولی";
        public event Action? OnChange;

        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        /// <summary>
        /// 🔍 متد پایش زنده: واکشی نام ادمین از روی توکن واقعی هدر مرورگر و اتصال به SQLite
        /// </summary>
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                // خواندن فیزیکی توکن ادمینی که لاگین کرده از روی LocalStorage مرورگر کلاینت
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    // 🔍 اسکن مستقیم جدول ادمین‌ها در دیتابیس بر اساس توکن فعال مرورگر
                    var admin = await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                    if (admin != null)
                    {
                        CurrentOnlineAdmin = admin; // 🌟 جفت کردن هدر با رکورد واقعی دیتابیس به جای متغیرهای اشتباه

                        // تنظیم مستقل سطوح دسترسی تفکیکی
                        if (admin.IsSuperAdmin || admin.CanAddEmployee || admin.CanEditEmployee || admin.CanDeleteEmployee)
                        {
                            IsAuthorized = true;
                        }
                        else
                        {
                            IsAuthorized = false; // ادمین بدون دسترسی کیوسک
                        }

                        // اگر ادمین سشن فعال دارد و اشتباهاً در صفحه لاگین مانده، هدایت خودکار به پنل مدیریت
                        if (_navigationManager.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
                        {
                            IsChecked = true;
                            NotifyStateChanged();
                            _navigationManager.NavigateTo("/admin", forceLoad: false);
                            return;
                        }

                        // گارد امنیتی ادمین معمولی برای صفحه مدیریت ادمین‌ها
                        if (!IsAuthorized && _navigationManager.Uri.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
                        {
                            _navigationManager.NavigateTo("/", forceLoad: true);
                            return;
                        }

                        IsChecked = true;
                        NotifyStateChanged();
                        return;
                    }
                }

                // اگر توکن منقضی یا خالی بود و کاربر خواست فیزیکی وارد پنل‌ها شود
                if (IsChecked && string.IsNullOrEmpty(savedToken))
                {
                    Reset();
                    if (_navigationManager.Uri.Contains("/admin", StringComparison.OrdinalIgnoreCase))
                    {
                        _navigationManager.NavigateTo("/login", forceLoad: false);
                    }
                }
            }
            catch
            {
                return;
            }
            finally
            {
                if (!IsChecked)
                {
                    IsChecked = true;
                    NotifyStateChanged();
                }
            }
        }

        public async Task<bool> LoginAsync(string inputUsername, string inputPassword)
        {
            if (_dbContext == null || string.IsNullOrWhiteSpace(inputUsername) || string.IsNullOrWhiteSpace(inputPassword))
                return false;

            var admin = await _dbContext.Admins.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Username.ToLower() == inputUsername.ToLower() && a.Password == inputPassword);

            if (admin != null)
            {
                CurrentOnlineAdmin = admin; // ست کردن شیء ادمین در لحظه لاگین موفق

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

        private void Reset()
        {
            CurrentOnlineAdmin = null; // شستشوی کامل رکورد ادمین آنلاین
            IsAuthorized = true;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
