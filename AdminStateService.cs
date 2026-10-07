using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin;

namespace sqlite_web.Services
{
    public class AdminStateService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        // متغیرهای عمومی و یکپارچه سیستم شما
        public bool IsAuthorized { get; set; } = true;
        public string adminFullName { get; set; } = "";
        public string adminUsername { get; set; } = "";
        public string adminPassword { get; set; } = "";

        public bool pCanAdd { get; set; } = false;
        public bool pCanEdit { get; set; } = false;
        public bool pCanDelete { get; set; } = false;

        public bool IsChecked { get; private set; } = false;
        public event Action? OnChange;

        // تزریق دیتابیس، ناوبری و جاوااسکریپت به درون سرویس
        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        // ۱. متد پایش مرکزی: اسکن توکن و اعمال گارد امنیتی ریدایرکت بر روی صفحات با دسترسی حساس
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    var admin = await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                    if (admin != null)
                    {
                        FillAdminData(admin);

                        // 🛡️ لایه محافظتی: اگر ادمین معمولی بدون دسترسی بخواهد دستی وارد صفحه مدیریت مدیران شود، او را شوت کن به کیوسک
                        if (!IsAuthorized && _navigationManager.Uri.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
                        {
                            _navigationManager.NavigateTo("/", forceLoad: true);
                        }

                        IsChecked = true;
                        NotifyStateChanged();
                        return;
                    }
                }

                // 🔒 اگر توکن منقضی یا خالی بود و کاربر در صفحات حساس ادمین بود، انتقال خودکار به لاگین مجزا
                Reset();
                if (_navigationManager.Uri.Contains("/admin", StringComparison.OrdinalIgnoreCase) &&
                    !_navigationManager.Uri.Contains("/admin/login", StringComparison.OrdinalIgnoreCase))
                {
                    _navigationManager.NavigateTo("/admin/login", forceLoad: true);
                }
            }
            catch
            {
                // مهار خطای فاز پیش‌رندر سرور
            }
            finally
            {
                IsChecked = true;
                NotifyStateChanged();
            }
        }

        // ۲. متد ورود مرکزی
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

        // ۳. متد خروج مرکزی
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

        private void FillAdminData(AdminUser admin)
        {
            adminUsername = admin.Username;
            adminPassword = admin.Password;
            adminFullName = admin.Username;

            pCanAdd = admin.CanAddEmployee;
            pCanEdit = admin.CanEditEmployee;
            pCanDelete = admin.CanDeleteEmployee;

            if (!admin.IsSuperAdmin && !pCanAdd && !pCanEdit && !pCanDelete)
            {
                IsAuthorized = false; // ادمین بدون دسترسی (کیوسک)
            }
            else
            {
                IsAuthorized = true; // ادمین مجاز ارشد یا دارای سطح دسترسی پایه
            }
        }

        private void Reset()
        {
            adminUsername = "";
            adminFullName = "";
            adminPassword = "";
            IsAuthorized = true;
            pCanAdd = false;
            pCanEdit = false;
            pCanDelete = false;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
