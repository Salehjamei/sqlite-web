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
    /// سرویس متمرکز مدیریت وضعیت، سطوح دسترسی و تمام عملیات دیتابیسی مدیران سیستم
    /// </summary>
    public class AdminStateService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        // 🔘 شیء زنده ادمین آنلاین واکشی شده از دیتابیس
        public AdminUser? CurrentOnlineAdmin { get; private set; }

        // 🔘 متغیرهای عمومی احراز هویت مورد نیاز هدر لایوت و صفحات
        public bool IsAuthorized { get; set; } = true;
        public string adminUsername => CurrentOnlineAdmin?.Username ?? "";
        public bool isCurrentUserSuperAdmin => CurrentOnlineAdmin?.IsSuperAdmin ?? false;
        public bool IsChecked { get; private set; } = false;
        public event Action? OnChange;

        // 🌟 متغیرهای فرم مدیریت ادمین‌ها (انتقال یافته به سرویس جهت یکنواختی کامل)
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

        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }
        /// <summary>
        /// 🌟 متد عمومی و پرکاربرد انتقال خودکار به صفحه ورود کلاینت
        /// این متد در تمام صفحات آینده به صورت یکپارچه برای اخراج کلاینت‌های غیرمجاز صدا زده می‌شود.
        /// </summary>
        public void RedirectToLogin()
        {
            _navigationManager.NavigateTo("/login", forceLoad: false);
        }
        /// <summary>
        /// 🔍 پایش مرکزی و گارد امنیتی مسیرهای پنل مدیریت
        /// </summary>
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    var admin = await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.CurrentToken == savedToken);
                    if (admin != null)
                    {
                        FillAdminData(admin);

                        if (!IsAuthorized && _navigationManager.Uri.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
                        {
                            _navigationManager.NavigateTo("/", forceLoad: true);
                            return;
                        }

                        if (_navigationManager.Uri.Contains("/login", StringComparison.OrdinalIgnoreCase))
                        {
                            IsChecked = true;
                            NotifyStateChanged();
                            _navigationManager.NavigateTo("/admin", forceLoad: false);
                            return;
                        }

                        IsChecked = true;
                        NotifyStateChanged();
                        return;
                    }
                }

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
        /// <summary>
        /// 👑 متد متمرکز مقداردهی اولیه و ساخت اولین سوپر ادمین ارشد سیستم در SQLite
        /// توضیحات کد: این متد تضمین می‌کند که فیلدهای امنیتی توکن و وضعیت فریز ادمین ارشد در بدو ساخت کاملاً تراز باشند.
        /// </summary>
        public async Task InitializeDefaultSuperAdminAsync()
        {
            if (_dbContext == null) return;

            // اطمینان از ساخت فیزیکی فایل دیتابیس و جداول SQLite روی هارد دیسک
            await _dbContext.Database.EnsureCreatedAsync();

            // اگر جدول ادمین‌ها کاملاً خالی بود، اولین مدیر ارشد سیستم را با دسترسی تفکیکی کامل بساز
            if (!await _dbContext.Admins.AnyAsync())
            {
                var initialSuperAdmin = new sqlite_web.Components.Models.Admin.AdminUser
                {
                    Username = "AdminTop",
                    Password = "AdminTop", // رمز عبور پیش‌فرض شما
                    IsSuperAdmin = true,   // 👑 دارای دسترسی ارشد مدیریت مدیران
                    CanAddEmployee = true, // ➕ دارای دسترسی ثبت کارمندان
                    CanEditEmployee = true,// ✏️ دارای دسترسی ویرایش کارمندان
                    CanDeleteEmployee = true, // ❌ دارای دسترسی حذف کارمندان
                    IsFrozen = false,      // اکانت فعال است و فریز نیست
                    IsTokenStopped = false, // توکن متوقف نشده است
                    CurrentToken = null,   // توکن در اولین ورود پس از ثبت فرم صادر خواهد شد
                    TokenExpireTime = null
                };

                _dbContext.Admins.Add(initialSuperAdmin);
                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// 🔑 متد ورود مرکزی سیستم و تولید توکن معتبر
        /// </summary>
        public async Task<bool> LoginAsync(string inputUsername, string inputPassword)
        {
            if (_dbContext == null || string.IsNullOrWhiteSpace(inputUsername) || string.IsNullOrWhiteSpace(inputPassword))
                return false;

            var admin = await _dbContext.Admins
                .FirstOrDefaultAsync(a => a.Username.ToLower() == inputUsername.ToLower() && a.Password == inputPassword);

            if (admin != null)
            {
                if (admin.IsFrozen)
                    return false;

                string uniqueToken = Guid.NewGuid().ToString("N");
                admin.CurrentToken = uniqueToken;
                admin.IsTokenStopped = false;
                admin.TokenExpireTime = DateTime.Now.AddDays(7);

                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();

                CurrentOnlineAdmin = admin;

                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "admin_session_token", uniqueToken);
                }
                catch { }

                IsChecked = true;
                NotifyStateChanged();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 🏃‍♂️ متد خروج مرکزی سیستم و ابطال سشن
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
                        admin.IsTokenStopped = true;
                        _dbContext.Admins.Update(admin);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                catch { }
            }

            Reset();
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
            }
            catch { }

            NotifyStateChanged();
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }

        // =========================================================================
        // 👑 بخش متدها و عملیات مدیریت ادمین‌ها (پوشش کامل نیازمندی صفحه ManageAdmins)
        // =========================================================================

        /// <summary>
        /// 💾 بارگذاری فیزیکی لیست مدیران از دیتابیس SQLite
        /// </summary>
        public async Task LoadAdminsDataAsync()
        {
            if (_dbContext != null)
            {
                AdminList = await _dbContext.Admins.AsNoTracking().ToListAsync();
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// 💾 ثبت یا ویرایش اطلاعات مدیر در دیتابیس
        /// </summary>
        public async Task SaveAdminAsync()
        {
            if (_dbContext == null) return;

            if (string.IsNullOrWhiteSpace(FormAdminUsername) || string.IsNullOrWhiteSpace(FormAdminPassword))
            {
                FeedbackMessage = "❌ لطفاً نام کاربری و رمز عبور را وارد کنید!";
                return;
            }

            if (IsEditMode)
            {
                var existingAdmin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Username == FormAdminUsername);
                if (existingAdmin != null)
                {
                    existingAdmin.Password = FormAdminPassword;
                    existingAdmin.IsSuperAdmin = FormIsSuperAdmin;
                    existingAdmin.CanAddEmployee = FormCanAdd;
                    existingAdmin.CanEditEmployee = FormCanEdit;
                    existingAdmin.CanDeleteEmployee = FormCanDelete;

                    _dbContext.Admins.Update(existingAdmin);
                    FeedbackMessage = "✅ مشخصات و دسترسی‌های مدیر با موفقیت ویرایش شد.";
                }
            }
            else
            {
                var checkDuplicate = await _dbContext.Admins.AnyAsync(a => a.Username.ToLower() == FormAdminUsername.ToLower());
                if (checkDuplicate)
                {
                    FeedbackMessage = "❌ این نام کاربری قبلاً ثبت شده است!";
                    return;
                }

                var newAdmin = new AdminUser
                {
                    Username = FormAdminUsername,
                    Password = FormAdminPassword,
                    IsSuperAdmin = FormIsSuperAdmin,
                    CanAddEmployee = FormCanAdd,
                    CanEditEmployee = FormCanEdit,
                    CanDeleteEmployee = FormCanDelete,
                    IsFrozen = false,
                    IsTokenStopped = false
                };

                _dbContext.Admins.Add(newAdmin);
                FeedbackMessage = "✅ مدیر جدید با موفقیت در سیستم ثبت شد.";
            }

            await _dbContext.SaveChangesAsync();
            await LoadAdminsDataAsync();
            ClearForm();
        }

        /// <summary>
        /// ❄️ فریز / فعال‌سازی موقت سشن ادمین‌ها در دیتابیس
        /// </summary>
        public async Task ToggleFreezeAdminAsync(int id, bool freezeStatus)
        {
            if (_dbContext == null) return;

            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null)
            {
                if (admin.Username.ToLower() == "admintop")
                {
                    FeedbackMessage = "❌ اکانت ادمین اصلی سیستم قابل فریز شدن نیست!";
                    return;
                }
                admin.IsFrozen = freezeStatus;
                if (freezeStatus)
                {
                    admin.IsTokenStopped = true;
                    admin.CurrentToken = null;
                }
                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();
                FeedbackMessage = freezeStatus ? "❄️ ادمین فریز شد و سشن او منقضی گردید." : "🔥 ادمین از حالت فریز خارج و فعال شد.";
            }
            await LoadAdminsDataAsync();
        }
        /// 
        /// 🛑 ابطال و متوقف کردن دستی توکن یک ادمین بدون فریز کاربری
        /// 
        public async Task StopAdminTokenAsync(int id)
        {
            if (_dbContext == null) return;
            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null)
            {
                admin.IsTokenStopped = true;
                admin.CurrentToken = null;
                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();
                FeedbackMessage = "🛑 توکن فعال ادمین انتخاب شده متوقف و باطل شد.";
            }
            await LoadAdminsDataAsync();
        }
        /// 
        /// ✏️ بارگذاری مشخصات مدیر در فرم جهت ویرایش
        /// 
        public void StartEdit(AdminUser admin)
        {
            IsEditMode = true;
            FeedbackMessage = string.Empty;
            FormAdminUsername = admin.Username;
            FormAdminPassword = admin.Password;
            FormIsSuperAdmin = admin.IsSuperAdmin;
            FormCanAdd = admin.CanAddEmployee;
            FormCanEdit = admin.CanEditEmployee;
            FormCanDelete = admin.CanDeleteEmployee;
            NotifyStateChanged();
        }
        /// 
        /// ↩️ انصراف از ویرایش و پاک‌سازی فرم
        /// 
        public void CancelEdit()
        {
            IsEditMode = false;
            FeedbackMessage = string.Empty;
            ClearForm();
            NotifyStateChanged();
        }
        /// 
        /// 🗑️ حذف فیزیکی کامل ادمین از سیستم همراه با لایه محافظتی اکانت ارشد
        /// 
        public async Task DeleteAdminAsync(int id)
        {
            if (_dbContext == null) return;
            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);
            if (admin != null)
            {
                if (admin.Username.ToLower() == "admintop")
                {
                    FeedbackMessage = "❌ مدیر اصلی سیستم (AdminTop) قابل حذف نیست!";
                    return;
                }
                _dbContext.Admins.Remove(admin);
                await _dbContext.SaveChangesAsync();
                FeedbackMessage = "🗑️ مدیر انتخاب شده با موفقیت از دیتابیس حذف شد.";
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
        }
        private void FillAdminData(AdminUser admin)
        {
            CurrentOnlineAdmin = admin;
            IsAuthorized = admin.IsSuperAdmin || admin.CanAddEmployee || admin.CanEditEmployee || admin.CanDeleteEmployee;
        }
        private void Reset()
        {
            CurrentOnlineAdmin = null;
            IsAuthorized = true;
        }
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}