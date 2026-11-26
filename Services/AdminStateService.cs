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
        private readonly SystemErrorLoggerService _errorLogger;
        // 🔘 شیء زنده ادمین آنلاین واکشی شده از دیتابیس
        public AdminUser? CurrentOnlineAdmin { get; private set; }

        // 🔘 پرچم اصلی احراز هویت (سشن فعال)
        public bool IsAuthorized { get; private set; } = false;
        public string adminUsername => CurrentOnlineAdmin?.Username ?? "";
        public bool isCurrentUserSuperAdmin => CurrentOnlineAdmin?.IsSuperAdmin ?? false;
        public bool IsChecked { get; private set; } = false;
        // پرچم جلوگیری از ثبت تکراری لاگ آنلاین شدن در طول یک نشست فعال مرورگر
        private bool _hasLoggedOnlineSession = false;

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
        // 🌟 فیلد ذخیره زنده تنظیمات کاری واکشی شده از سرور
        public WorkSettings CurrentSettings { get; set; } = new();
        private int? _editingAdminId = null;

        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime, NavigationManager navigationManager, SystemErrorLoggerService errorLogger)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
            _errorLogger = errorLogger;
        }

        public void RedirectToLogin() => _navigationManager.NavigateTo("/login", forceLoad: false);

        /// پایش مرکزی و گارد امنیتی بررسی اصالت سشن، تشخیص انقضا یا مسدودسازی توکن و ثبت رویدادهای آنلاین شدن ادمین در سرور
        public async Task CheckAccessAndRedirectAsync()
        {
            try
            {
                // واکشی توکن فعال موجود در حافظه مرورگر کلاینت
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

                    // بررسی علت نامعتبر شدن توکن ذخیره‌شده (انقضای زمانی یا محدود شدن توکن) جهت ثبت در سرور
                    if (verifiedAdmin == null)
                    {
                        var invalidAdmin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.CurrentToken == savedToken);
                        if (invalidAdmin != null)
                        {
                            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{invalidAdmin.Username}»</span>";

                            // تشخیص انقضای زمانی توکن
                            if (invalidAdmin.TokenExpireTime <= DateTime.Now)
                            {
                                await RecordSecurityLogAsync(
                                    subCategory: "TokenExpired",
                                    messageKey: "PCal_Security_Log_TokenExpired",
                                    args: new object[] { coloredName },
                                    notificationType: "Error",
                                    actorAdminId: invalidAdmin.Id,
                                    actorAdminUsername: invalidAdmin.Username
                                );
                            }
                            // تشخیص محدود شدن توکن به دلیل فریز شدن حساب یا توقف دستی
                            else if (invalidAdmin.IsFrozen || invalidAdmin.IsTokenStopped)
                            {
                                await RecordSecurityLogAsync(
                                    subCategory: "TokenRestricted",
                                    messageKey: "PCal_Security_Log_TokenRestricted",
                                    args: new object[] { coloredName },
                                    notificationType: "Error",
                                    actorAdminId: invalidAdmin.Id,
                                    actorAdminUsername: invalidAdmin.Username
                                );
                            }

                            // پاکسازی توکن منقضی یا محدودشده از جدول ادمین جهت جلوگیری از ثبت لاگ تکراری
                            invalidAdmin.CurrentToken = null;
                            invalidAdmin.IsTokenStopped = true;
                            _dbContext.Admins.Update(invalidAdmin);
                            await _dbContext.SaveChangesAsync();
                        }
                    }
                }

                // مدیریت ورود به صفحه لاگین
                if (currentUri.Contains("/login", StringComparison.OrdinalIgnoreCase))
                {
                    if (verifiedAdmin != null)
                    {
                        FillAdminData(verifiedAdmin);
                        IsChecked = true;
                        NotifyStateChanged();
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

                // تایید نشست معتبر ادمین آنلاین و ثبت رویداد آنلاین شدن در ابتدای نشست مرورگر
                if (verifiedAdmin != null)
                {
                    FillAdminData(verifiedAdmin);
                    IsChecked = true;

                    // ثبت رویداد آنلاین شدن ادمین هنگام باز کردن برنامه با توکن معتبر قبلی
                    if (!_hasLoggedOnlineSession)
                    {
                        _hasLoggedOnlineSession = true;
                        string coloredOnlineName = $"<span style='color: #dc3545; font-weight: 900;'>«{verifiedAdmin.Username}»</span>";
                        await RecordSecurityLogAsync(
                            subCategory: "AdminOnline",
                            messageKey: "PCal_Security_Log_AdminOnline",
                            args: new object[] { coloredOnlineName },
                            notificationType: "Info",
                            actorAdminId: verifiedAdmin.Id,
                            actorAdminUsername: verifiedAdmin.Username
                        );
                    }

                    NotifyStateChanged();
                    return;
                }

                // محافظت از مسیرهای مدیریتی در صورت نبود توکن معتبر
                if (currentUri.Contains("/admin", StringComparison.OrdinalIgnoreCase))
                {
                    Reset();
                    _navigationManager.NavigateTo("/login", forceLoad: false);
                }
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("statically rendered", StringComparison.OrdinalIgnoreCase)
                                         || ex.Message.Contains("JavaScript interop calls cannot be issued", StringComparison.OrdinalIgnoreCase))
            {
                // نادیده گرفتن خطای طبیعی مرحله پیش‌رندر اولیه سرور پیش از اتصال مرورگر
                return;
            }
            catch (Exception ex)
            {
                // ثبت تمامی خطاهای واقعی دیگر (از جمله خطاهای دیتابیس و InvalidOperationExceptionهای غیرمرتبط با پیش‌رندر)
                await _errorLogger.LogBackendErrorAsync("AdminStateService.CheckAccessAndRedirectAsync", ex, CurrentOnlineAdmin?.Id, adminUsername);
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
        // احراز هویت مدیر، تولید توکن نشست و ثبت رویداد ورود چندزبانه در تاریخچه اعلانات
        public async Task<bool> LoginAsync(string username, string password)
        {
            if (_dbContext == null) return false;

            // بازیابی زبان فعلی سیستم از حافظه مرورگر برای ترجمه پیام‌های بازخورد ورود
            string currentLang = "fa";
            try
            {
                var storedLang = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "SystemLanguageSetting");
                if (!string.IsNullOrWhiteSpace(storedLang))
                {
                    currentLang = storedLang.Trim('"');
                }
            }
            catch { }

            var pCal = new sqlite_web.Components.Models.Calendar.PCalendar { CurrentLanguage = currentLang };

            try
            {
                // بررسی صحت نام کاربری و رمز عبور در جدول ادمین‌ها
                var admin = await _dbContext.Admins.FirstOrDefaultAsync(a =>
                    a.Username.ToLower() == username.ToLower() && a.Password == password);

                // ثبت تلاش ورود ناموفق در صورت اشتباه بودن نام کاربری یا رمز عبور
                if (admin == null)
                {
                    FeedbackMessage = pCal.Translate("PCal_Login_InvalidCredentials");
                    string coloredAttempt = $"<span style='color: #dc3545; font-weight: 900;'>«{username}»</span>";
                    await RecordSecurityLogAsync(
                        subCategory: "AdminLogin",
                        messageKey: "PCal_Security_Log_LoginFailed",
                        args: new object[] { coloredAttempt },
                        notificationType: "Error",
                        actorAdminId: null,
                        actorAdminUsername: username
                    );
                    return false;
                }

                // ثبت تلاش ورود حساب مسدودشده (فریز) در تاریخچه امنیتی سرور
                if (admin.IsFrozen)
                {
                    FeedbackMessage = pCal.Translate("PCal_Login_AccountFrozen");
                    string coloredFrozen = $"<span style='color: #dc3545; font-weight: 900;'>«{admin.Username}»</span>";
                    await RecordSecurityLogAsync(
                        subCategory: "TokenRestricted",
                        messageKey: "PCal_Security_Log_LoginFrozenAttempt",
                        args: new object[] { coloredFrozen },
                        notificationType: "Error",
                        actorAdminId: admin.Id,
                        actorAdminUsername: admin.Username
                    );
                    return false;
                }

                // تولید توکن منحصربه‌فرد برای سشن جدید و تعیین تاریخ انقضا
                string newToken = Guid.NewGuid().ToString("N");
                admin.CurrentToken = newToken;
                admin.IsTokenStopped = false;
                admin.TokenExpireTime = DateTime.Now.AddDays(31);

                _dbContext.Admins.Update(admin);
                await _dbContext.SaveChangesAsync();

                // علامت‌گذاری نشست به عنوان ثبت‌شده جهت جلوگیری از ثبت هم‌زمان لاگ آنلاین شدن
                _hasLoggedOnlineSession = true;

                // ثبت رویداد ورود موفق ادمین در جدول تاریخچه اعلانات سرور
                string coloredLoginName = $"<span style='color: #dc3545; font-weight: 900;'>«{admin.Username}»</span>";
                await RecordSecurityLogAsync(
                    subCategory: "AdminLogin",
                    messageKey: "PCal_Security_Log_LoginSuccess",
                    args: new object[] { coloredLoginName },
                    notificationType: "Success",
                    actorAdminId: admin.Id,
                    actorAdminUsername: admin.Username
                );

                // ذخیره فیزیکی توکن در مرورگر کلاینت جهت پایش‌های بعدی
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "admin_session_token", newToken);
                FillAdminData(admin);

                FeedbackMessage = pCal.Translate("PCal_Login_SuccessFeedback");
                NotifyStateChanged();

                // انتقال مستقیم به صفحه مدیریت پرسنل
                _navigationManager.NavigateTo("/admin/panel", forceLoad: false);
                return true;
            }
            catch (Exception ex)
            {
                await _errorLogger.LogBackendErrorAsync("AdminStateService.LoginAsync", ex, null, username, null, currentLang);
                FeedbackMessage = pCal.Translate("PCal_Login_SystemError");
                return false;
            }
        }


        /// متد خروج مرکزی سیستم، ابطال همزمان سشن و ثبت رویداد خروج در تاریخچه اعلانات
        public async Task LogoutAsync()
        {
            if (CurrentOnlineAdmin != null && _dbContext != null)
            {
                int logoutAdminId = CurrentOnlineAdmin.Id;
                string logoutAdminName = CurrentOnlineAdmin.Username;
                string coloredLogoutName = $"<span style='color: #dc3545; font-weight: 900;'>«{logoutAdminName}»</span>";

                try
                {
                    var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == logoutAdminId);
                    if (admin != null)
                    {
                        admin.CurrentToken = null;
                        admin.IsTokenStopped = true;
                        admin.TokenExpireTime = null;
                        _dbContext.Admins.Update(admin);
                        await _dbContext.SaveChangesAsync();
                    }

                    // ثبت رویداد خروج دستی ادمین در جدول اعلانات سرور
                    await RecordSecurityLogAsync(
                        subCategory: "AdminLogout",
                        messageKey: "PCal_Security_Log_LogoutSuccess",
                        args: new object[] { coloredLogoutName },
                        notificationType: "Info",
                        actorAdminId: logoutAdminId,
                        actorAdminUsername: logoutAdminName
                    );
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

        public async Task<bool> SaveAdminAsync()
        {
            if (_dbContext == null) return false;
            if (string.IsNullOrWhiteSpace(FormAdminUsername) || string.IsNullOrWhiteSpace(FormAdminPassword)) return false;

            if (IsEditMode && _editingAdminId.HasValue)
            {
                var existingAdmin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == _editingAdminId.Value);
                if (existingAdmin != null)
                {
                    bool finalSuperAdminStatus = FormIsSuperAdmin;

                    // بررسی اینکه آیا کاربر قصد دارد تیک سوپر ادمین یک سوپر ادمین موجود را بردارد
                    if (!FormIsSuperAdmin && existingAdmin.IsSuperAdmin)
                    {
                        int totalSuperAdmins = await _dbContext.Admins.CountAsync(a => a.IsSuperAdmin);

                        // اگر فقط همین یک سوپر ادمین در دیتابیس وجود دارد، اجازه تغییر نمی‌دهیم
                        if (totalSuperAdmins <= 1)
                        {
                            FeedbackMessage = "❌ خطا: سیستم باید حداقل دارای یک سوپر ادمین باشد. امکان حذف رتبه تنها سوپر ادمین وجود ندارد!";
                            return false; // 🛑 متوقف کردن فوری متد و بازگرداندن وضعیت شکست عملیات
                        }
                    }

                    existingAdmin.Username = FormAdminUsername;
                    existingAdmin.Password = FormAdminPassword;
                    existingAdmin.IsSuperAdmin = finalSuperAdminStatus;
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
            FeedbackMessage = "✅ تغییرات با موفقیت ذخیره شد.";
            return true; // ✨ عملیات با موفقیت واقعی ثبت شد
        }


        public async Task ToggleFreezeAdminAsync(int id, bool freezeStatus)
        {
            // ۱. محافظت اول: ادمین جاری سیستم نباید بتواند خودش را فریز کند
            if (_dbContext == null || (CurrentOnlineAdmin != null && CurrentOnlineAdmin.Id == id)) return;

            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);

            // ۲. محافظت دوم: جایگزینی "admintop" با شرط ساختاری (هیچ سوپر ادمینی نباید فریز شود)
            if (admin != null && !admin.IsSuperAdmin)
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
            // ۱. محافظت اول: ادمین جاری سیستم نباید بتواند خودش را حذف کند
            if (_dbContext == null || (CurrentOnlineAdmin != null && CurrentOnlineAdmin.Id == id)) return;

            var admin = await _dbContext.Admins.FirstOrDefaultAsync(a => a.Id == id);

            // ۲. محافظت دوم: جایگزینی "admintop" با شرط ساختاری (هیچ سوپر ادمینی نباید حذف شود)
            if (admin != null && !admin.IsSuperAdmin)
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
        // پاکسازی وضعیت ادمین آنلاین و بازنشانی پرچم نشست مرورگر
        private void Reset()
        {
            CurrentOnlineAdmin = null;
            IsAuthorized = false;
            _hasLoggedOnlineSession = false;
        }
        /// <summary>
        /// متد آسنکرون بارگذاری یا ساخت پیش‌فرض تنظیمات کاری از بانک اطلاعاتی SQLite با ایمنی کامل
        /// </summary>
        public async Task LoadWorkSettingsAsync()
        {
            if (_dbContext == null) return;

            try
            {
                var settings = await _dbContext.Set<WorkSettings>().FirstOrDefaultAsync();
                if (settings == null)
                {
                    // ایجاد رکورد پیش‌فرض در اولین اجرای سیستم
                    settings = new WorkSettings
                    {
                        RegularStartTime = "07:00",
                        RegularEndTime = "17:30",
                        OffDaysOfWeek = "Saturday,Sunday",
                        DeficitThresholdMinutes = 15 // مقدار پیش‌فرض آستانه تهاتر کسری کار
                    };
                    _dbContext.Set<WorkSettings>().Add(settings);
                    await _dbContext.SaveChangesAsync();
                }
                CurrentSettings = settings;
            }
            catch (Exception ex)
            {
                // 🟢 گارد امنیتی: اگر جدول دیتابیس مایگریشن نداشت، مقدار پیش‌فرض را در رم مقداردهی کن تا کل پنل قطع نشود
                Console.WriteLine($"⚠️ هشدار دیتابیس در تنظیمات: {ex.Message}");

                CurrentSettings = new WorkSettings
                {
                    RegularStartTime = "07:00",
                    RegularEndTime = "17:30",
                    OffDaysOfWeek = "Saturday,Sunday",
                    DeficitThresholdMinutes = 15
                };
            }
            finally
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// متد به روزرسانی پویا پارامترهای محاسباتی و قوانین خروج خودکار توسط سوپر ادمین ارشد
        /// </summary>
        public async Task<bool> SaveWorkSettingsAsync(string startTime, string endTime, List<string> offDays)
        {
            if (_dbContext == null) return false;
            try
            {
                var settings = await _dbContext.Set<WorkSettings>().FirstOrDefaultAsync();
                if (settings != null)
                {
                    settings.RegularStartTime = startTime;
                    settings.RegularEndTime = endTime;
                    settings.OffDaysOfWeek = string.Join(",", offDays);

                    // اعمال خودکار شروط جدید خروج اتوماتیک سیستم از روی مقادیر بایند شده مدل
                    // مقادیر ApplyAutoLogout و AutoLogoutTime مستقیماً ذخیره می شوند

                    _dbContext.Set<WorkSettings>().Update(settings);
                    await _dbContext.SaveChangesAsync();
                    CurrentSettings = settings;
                    NotifyStateChanged();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                await _errorLogger.LogBackendErrorAsync("AdminStateService.SaveWorkSettingsAsync", ex, CurrentOnlineAdmin?.Id, adminUsername);
                return false;
            }

        }
        // ثبت مستقیم رویدادهای امنیتی (ورود، خروج، انقضای توکن و مسدودسازی) در جدول تاریخچه اعلانات سرور
        private async Task RecordSecurityLogAsync(
            string subCategory,
            string messageKey,
            object[]? args = null,
            string notificationType = "Info",
            int? actorAdminId = null,
            string actorAdminUsername = "System")
        {
            if (_dbContext == null) return;
            try
            {
                var pCal = new sqlite_web.Components.Models.Calendar.PCalendar { CurrentLanguage = "fa" };
                var template = pCal.Translate(messageKey);
                var formattedMsg = (args != null && args.Length > 0) ? string.Format(template, args) : template;
                var cleanMsg = System.Text.RegularExpressions.Regex.Replace(formattedMsg, "<.*?>", string.Empty);
                var now = DateTime.Now;

                _dbContext.AdminNotificationLogs.Add(new AdminNotificationLog
                {
                    Category = "Security",
                    SubCategory = subCategory,
                    MessageKey = messageKey,
                    MessageArgs = (args != null && args.Length > 0) ? System.Text.Json.JsonSerializer.Serialize(args) : null,
                    Message = cleanMsg,
                    NotificationType = notificationType,
                    CreatedAt = now,
                    CreatedDate = now.ToString("yyyy-MM-dd"),
                    CreatedTime = now.ToString("HH:mm:ss"),
                    AdminId = actorAdminId,
                    AdminUsername = string.IsNullOrWhiteSpace(actorAdminUsername) ? "System" : actorAdminUsername,
                    EmployeeFullName = "-",
                    EmployeePersonnelCode = "-",
                    EmployeePhone = "-"
                });

                await _dbContext.SaveChangesAsync();
            }
            catch { }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
        // ارسال سیگنال به‌روزرسانی به تمام کامپوننت‌های متصل (مانند هدر لایوت) جهت بازرندر آنی رابط کاربری
        public void RefreshUIState() => OnChange?.Invoke();

    }
}