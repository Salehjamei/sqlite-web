using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin;

namespace sqlite_web.Services
{
    /// <summary>
    /// سرویس متمرکز مدیریت وضعیت، سطوح دسترسی و عملیات دیتابیسی مدیران سیستم
    /// </summary>
    public class AdminStateService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        // 🔘 شیء زنده ادمین آنلاین واکشی شده از دیتابیس
        public AdminUser? CurrentOnlineAdmin { get; private set; }

        // 🔘 پرچم اصلی احراز هویت (سشن فعال)
        public bool IsAuthorized { get; private set; } = false;
        public string adminUsername => CurrentOnlineAdmin?.Username ?? "";
        public bool isCurrentUserSuperAdmin => CurrentOnlineAdmin?.IsSuperAdmin ?? false;
        public bool IsChecked { get; private set; } = false;

        // رویداد مطلع‌سازی کامپوننت‌ها (مانند هدر لایوت) از تغییرات سشن ادمین
        public event Action? OnChange;

        // 🌟 متغیرهای فرم مدیریت دسترسی ادمین‌ها
        public List<AdminUser> AdminList { get; set; } = new();
        public bool IsEditMode { get; set; } = false;
        public string FeedbackMessage { get; set; } = string.Empty;

        // فیلدهای موقت بایند کادرهای ورودی فرم ثبت/ویرایش مدیر جدید
        public string FormAdminUsername { get; set; } = string.Empty;
        public string FormAdminPassword { get; set; } = string.Empty;
        public bool FormIsSuperAdmin { get; set; } = false;
        public bool FormCanAdd { get; set; } = false;
        public bool FormCanEdit { get; set; } = false;
        public bool FormCanDelete { get; set; } = false;

        private int? _editingAdminId = null;

        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        public void RedirectToLogin() => _navigationManager.NavigateTo("/login", forceLoad: false);

        /// <summary>
        /// 🔍 پایش مرکزی و گارد امنیتی بررسی اصالت سشن و سلامت توکن‌ها در دیتابیس
        /// </summary>
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                // واکشی توکن فعال موجود در لایو حافظه مرورگر کلاینت
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");
                var currentUri = _navigationManager.Uri;

                // بررسی و راستی‌آزمایی توکن در دیتابیس با تمام شروط امنیتی سیستم
                AdminUser? verifiedAdmin = null;
                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    verifiedAdmin = await _dbContext.Admins.AsNoTracking()
                        .FirstOrDefaultAsync(a => a.CurrentToken == savedToken
                                               && !a.IsFrozen
                                               && !a.IsTokenStopped
                                               && a.TokenExpireTime > DateTime.Now);
                }

                // 🔘 مدیریت هوشمند ورود به صفحه لاگین
                if (currentUri.Contains("/login", StringComparison.OrdinalIgnoreCase))
                {
                    if (verifiedAdmin != null)
                    {
                        FillAdminData(verifiedAdmin);
                        IsChecked = true;
                        NotifyStateChanged();
                        // هدایت مستقیم به اولین صفحه پنل
                        _navigationManager.NavigateTo("/admin/panel", forceLoad: false);
                        return;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(savedToken))
                        {
                            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
                        }
                        Reset();
                        IsChecked = true;
                        NotifyStateChanged();
                        return;
                    }
                }

                // 🔘 اگر ادمین معتبر در سیستم یافت شد
                if (verifiedAdmin != null)
                {
                    FillAdminData(verifiedAdmin);
                    IsChecked = true;
                    NotifyStateChanged();
                    return; // اجازه بده لایوت فرآیند کنترل آدرس (هک لینک) را به صورت تعاملی با مودال جلو ببرد
                }

                // 🔘 لایه محافظتی: اگر توکن نامعتبر بود یا وجود نداشت و کاربر سعی داشت مسیرهای گارد شده /admin را باز کند
                if (currentUri.Contains("/admin", StringComparison.OrdinalIgnoreCase))
                {
                    Reset();
                    _navigationManager.NavigateTo("/login", forceLoad: false);
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

        /// <summary>
        /// 🔐 متد احراز هویت، تولید توکن امن و ایجاد سشن فعال ادمین
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_dbContext == null) return false;
            try
            {
                // بررسی صحت نام کاربری و رمز عبور در جدول ادمین‌ها
                var admin = await _dbContext.Admins.FirstOrDefaultAsync(a =>
                    a.Username.ToLower() == username.ToLower() && a.Password == password);

                if (admin == null)
                {
                    FeedbackMessage = "❌ نام کاربری یا رمز عبور اشتباه است!";
                    return false;
                }

                // بررسی وضعیت انجماد و قفل بودن حساب
                if (admin.IsFrozen)
                {
                    FeedbackMessage = "❄️ حساب کاربری شما فریز شده است و اجازه ورود ندارید!";
                    return false;
                }

                // تولید توکن منحصربه‌فرد برای سشن جدید
                string newToken = Guid.NewGuid().ToString("N");
                admin.CurrentToken = newToken;
                admin.IsTokenStopped = false; // بازنشانی پرچم متوقف شده
                admin.TokenExpireTime = DateTime.Now.AddDays(7); // سشن تا ۷ روز معتبر است

                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();

                // ذخیره فیزیکی توکن در مرورگر کلاینت جهت پایش‌های بعدی
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "admin_session_token", newToken);
                FillAdminData(admin);

                FeedbackMessage = "✅ ورود با موفقیت انجام شد.";
                NotifyStateChanged();

                // 🟢 فیکس باگ هدایت ناوبری: انتقال مستقیم به اولین صفحه داشبورد
                _navigationManager.NavigateTo("/admin/panel", forceLoad: false);
                return true;
            }
            catch
            {
                FeedbackMessage = "❌ خطایی در فرآیند ورود رخ داد!";
                return false;
            }
        }

        /// <summary>
        /// 👑 مقداردهی اولیه و ساخت مدیر ارشد سیستم در اولین اجرای پروژه
        /// </summary>
        public async Task InitializeDefaultSuperAdminAsync()
        {
            if (_dbContext == null) return;
            await _dbContext.Database.EnsureCreatedAsync();
            if (!await _dbContext.Admins.AnyAsync())
            {
                _dbContext.Admins.Add(new AdminUser
                {
                    Username = "AdminTop",
                    Password = "AdminTop",
                    IsSuperAdmin = true,
                    CanAddEmployee = true,
                    CanEditEmployee = true,
                    CanDeleteEmployee = true,
                    IsFrozen = false,
                    IsTokenStopped = false
                });
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 🏃‍♂️ متد خروج مرکزی سیستم و ابطال همزمان سشن در دیتابیس و کلاینت
        /// </summary>
        public async Task LogoutAsync()
        {
            if (CurrentOnlineAdmin != null && _dbContext != null)
            {
                try
                {
                    var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == CurrentOnlineAdmin.Id);
                    if (admin != null)
                    {
                        admin.CurrentToken = null;
                        admin.IsTokenStopped = true; // ابطال سشن
                        admin.TokenExpireTime = null;
                        _dbContext.Admins.Update(admin);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch { }
            }
            Reset();
            try { await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token"); } catch { }
            NotifyStateChanged();
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        public async Task LoadAdminsDataAsync()
        {
            if (_dbContext != null)
            {
                AdminList = await _dbContext.Admins.AsNoTracking().ToListAsync();
                NotifyStateChanged();
            }
        }

        public async Task SaveAdminAsync()
        {
            if (_dbContext == null) return;
            if (string.IsNullOrWhiteSpace(FormAdminUsername) || string.IsNullOrWhiteSpace(FormAdminPassword)) return;

            if (IsEditMode && _editingAdminId.HasValue)
            {
                var existingAdmin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == _editingAdminId.Value);
                if (existingAdmin != null)
                {
                    existingAdmin.Username = FormAdminUsername;
                    existingAdmin.Password = FormAdminPassword;
                    existingAdmin.IsSuperAdmin = FormIsSuperAdmin;
                    existingAdmin.CanAddEmployee = FormCanAdd;
                    existingAdmin.CanEditEmployee = FormCanEdit;
                    existingAdmin.CanDeleteEmployee = FormCanDelete;
                    _dbContext.Admins.Update(existingAdmin);
                    if (CurrentOnlineAdmin != null && CurrentOnlineAdmin.Id == existingAdmin.Id)
                    {
                        CurrentOnlineAdmin = existingAdmin;
                    }
                }
            }
            else
            {
                _dbContext.Admins.Add(new AdminUser
                {
                    Username = FormAdminUsername,
                    Password = FormAdminPassword,
                    IsSuperAdmin = FormIsSuperAdmin,
                    CanAddEmployee = FormCanAdd,
                    CanEditEmployee = FormCanEdit,
                    CanDeleteEmployee = FormCanDelete
                });
            }
            await _dbContext.SaveChangesAsync();
            await LoadAdminsDataAsync();
            ClearForm();
        }
        public async Task ToggleFreezeAdminAsync(int id, bool freezeStatus)
        {
            if (_dbContext == null || (CurrentOnlineAdmin != null && CurrentOnlineAdmin.Id == id)) return;
            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null && admin.Username.ToLower() != "admintop")
            {
                admin.IsFrozen = freezeStatus;
                if (freezeStatus)
                {
                    admin.IsTokenStopped = true;
                    admin.CurrentToken = null;
                    admin.TokenExpireTime = null;
                }
                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();
            }
            await LoadAdminsDataAsync();
        }
        public async Task StopAdminTokenAsync(int id)
        {
            if (_dbContext == null || (CurrentOnlineAdmin != null && CurrentOnlineAdmin.Id == id)) return;
            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null)
            {
                admin.IsTokenStopped = true;
                admin.CurrentToken = null;
                admin.TokenExpireTime = null;
                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();
            }
            await LoadAdminsDataAsync();
        }
        public void StartEdit(AdminUser admin)
        {
            IsEditMode = true;
            _editingAdminId = admin.Id;
            FormAdminUsername = admin.Username;
            FormAdminPassword = admin.Password;
            FormIsSuperAdmin = admin.IsSuperAdmin;
            FormCanAdd = admin.CanAddEmployee;
            FormCanEdit = admin.CanEditEmployee;
            FormCanDelete = admin.CanDeleteEmployee;
            NotifyStateChanged();
        }
        public void CancelEdit()
        {
            IsEditMode = false;
            ClearForm();
            NotifyStateChanged();
        }
        public async Task DeleteAdminAsync(int id)
        {
            if (_dbContext == null) return;
            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null && admin.Username.ToLower() != "admintop")
            {
                _dbContext.Admins.Remove(admin);
                await _dbContext.SaveChangesAsync();
            }
            await LoadAdminsDataAsync();
        }
        private void ClearForm()
        {
            FormAdminUsername = string.Empty;
            FormAdminPassword = string.Empty;
            FormIsSuperAdmin = false;
            FormCanAdd = false;
            FormCanEdit = false;
            FormCanDelete = false;
            _editingAdminId = null;
        }
        private void FillAdminData(AdminUser admin)
        {
            CurrentOnlineAdmin = admin;
            IsAuthorized = true;
        }
        private void Reset()
        {
            CurrentOnlineAdmin = null;
            IsAuthorized = false;
        }
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}