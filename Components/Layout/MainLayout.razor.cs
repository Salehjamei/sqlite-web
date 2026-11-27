using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using sqlite_web.Services;
namespace sqlite_web.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        // 💾 تزریق سرویس حافظه محلی مرورگر در بالای کلاس لایوت برای لود زبان
        [Inject] public Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedLocalStorage LocalStorage { get; set; } = default!;
        [Inject] public Microsoft.Extensions.DependencyInjection.IServiceScopeFactory ScopeFactory { get; set; } = default!;

        // نگهداری آخرین نشانی ثبت‌شده جهت جلوگیری از ثبت لاگ تکراری برای یک صفحه
        private string _lastLoggedUri = string.Empty;

        // 📅 نمونه‌سازی از کلاس مستقل و جدید مدیریت تقویم و ویژگی‌های داینامیک
        protected sqlite_web.Components.Models.Calendar.PCalendar CalendarManager { get; set; } = new();
        // 🟢 خصوصیت جدید جهت مهار زبان فعال در کل لایه منطق برنامه (fa، en، tr)
        protected string CurrentLanguage { get; set; } = "fa";

        protected bool ShowAccessDeniedModal { get; set; } = false;
        protected bool ShowUrlHijackModal { get; set; } = false;
        protected bool ShowLogoutConfirmModal { get; set; } = false;

        // متغیر کنترل وضعیت بک‌گراند دکمه فعال
        protected string ActivePage { get; set; } = "home";
        // وضعیت باز یا بسته بودن منوی کشویی زیرمجموعه دکمه سطوح دسترسی
        protected bool IsAccessDropdownOpen { get; set; } = false;
        // تغییر وضعیت باز و بسته شدن منوی کشویی سطوح دسترسی
        protected void ToggleAccessDropdown()
        {
            IsAccessDropdownOpen = !IsAccessDropdownOpen;
        }

        // بستن منوی کشویی سطوح دسترسی
        protected void CloseAccessDropdown()
        {
            IsAccessDropdownOpen = false;
        }
        // کنترل وضعیت باز یا بسته بودن زیرمنوی کشویی سطوح دسترسی در هدر
        protected bool isAccessSubMenuOpen = false;

        protected void ToggleAccessSubMenu()
        {
            isAccessSubMenuOpen = !isAccessSubMenuOpen;
        }
        protected bool IsMobileMenuOpen { get; set; } = false;

        protected void OpenLogoutModal()
        {
            IsMobileMenuOpen = false;
            ShowLogoutConfirmModal = true;
        }

        protected void CloseLogoutModal() => ShowLogoutConfirmModal = false;

        protected async Task ExecuteConfirmedLogout()
        {
            ShowLogoutConfirmModal = false;
            await AdminState.LogoutAsync();
        }

        protected void ToggleMobileMenu()
        {
            IsMobileMenuOpen = !IsMobileMenuOpen;
            InvokeAsync(StateHasChanged);
        }

       
        private bool IsCurrentlyInAdminPanel => NavigationManager.Uri.Contains("/admin", StringComparison.OrdinalIgnoreCase);

        protected override void OnInitialized()
        {
            AdminState.OnChange += OnAdminStateChanged;
            NavigationManager.LocationChanged += OnLocationChanged;

            // 🟢 فیکس باگ اکتیو صفحه: پایش آدرس دقیق به محض مقداردهی اولیه کامپوننت لایوت
            UpdateActivePageIndicator(NavigationManager.Uri);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // بازیابی خودکار و ماندگار زبان ذخیره‌شده کاربر از حافظه مرورگر کلاینت
                try
                {
                    var langResult = await LocalStorage.GetAsync<string>("SystemLanguageSetting");
                    if (langResult.Success && !string.IsNullOrEmpty(langResult.Value))
                    {
                        CurrentLanguage = langResult.Value;

                        if (CalendarManager != null)
                        {
                            CalendarManager.CurrentLanguage = CurrentLanguage;
                        }
                    }
                }
                catch { }

                // اجرای گارد دسترسی و پایش امنیتی آدرس
                await AdminState.CheckAccessAndRedirectAsync();
                EvaluateUrlSecurity(NavigationManager.Uri);

                // ثبت ورود اولیه ادمین آنلاین به صفحه فعلی در تاریخچه اعلانات
                await LogPageNavigationAsync(NavigationManager.Uri);

                // تازه‌سازی مجدد وضعیت دکمه‌های فعال هدر
                UpdateActivePageIndicator(NavigationManager.Uri);
                await InvokeAsync(StateHasChanged);
            }
        }

        // واکنش به تغییر نشانی صفحه، به‌روزرسانی دکمه فعال و ثبت بازدید صفحه در سرور
        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            UpdateActivePageIndicator(e.Location);
            EvaluateUrlSecurity(e.Location);
            await LogPageNavigationAsync(e.Location);
            await InvokeAsync(StateHasChanged);
        }

        // ثبت رویداد ورود ادمین آنلاین به هر یک از صفحات برنامه در جدول تاریخچه اعلانات سرور
        private async Task LogPageNavigationAsync(string url)
        {
            if (AdminState.CurrentOnlineAdmin == null) return;
            if (url.Contains("/login", StringComparison.OrdinalIgnoreCase)) return;

            var uriPath = new Uri(url).AbsolutePath.TrimEnd('/').ToLowerInvariant();
            if (string.IsNullOrEmpty(uriPath)) uriPath = "/";

            // جلوگیری از ثبت مجدد همان صفحه در صورت رفرش لحظه‌ای وضعیت
            if (_lastLoggedUri == uriPath) return;
            _lastLoggedUri = uriPath;

            // نگاشت مسیر صفحه به کلید ترجمه چندزبانه متناظر
            string pageKey = uriPath switch
            {
                "/" => "KioskBtn",
                "/admin/panel" => "PersonnelManagementBtn",
                "/admin/manage-admins" => "AccessPageTitle",
                "/admin/locations" => "PCal_Loc_PageTitle",
                "/admin/notifications" or "/admin/notificationlogs" => "PCal_AdminNotif_ModalTitle",
                _ => uriPath
            };

            try
            {
                using var scope = ScopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var admin = AdminState.CurrentOnlineAdmin;
                string coloredAdmin = $"<span style='color: #dc3545; font-weight: 900;'>«{admin.Username}»</span>";
                string pageNameFa = CalendarManager.Translate(pageKey);
                string coloredPage = $"<span style='color: #0d6efd; font-weight: 900;'>«{pageNameFa}»</span>";

                // ذخیره کلید ترجمه صفحه در آرگومان دوم جهت بازسازی چندزبانه در پنل سوپر ادمین
                var storageArgs = new object[] { coloredAdmin, pageKey };
                var displayArgs = new object[] { coloredAdmin, coloredPage };
                var template = CalendarManager.Translate("PCal_Navigation_PageVisitLog");
                var now = DateTime.Now;

                db.AdminNotificationLogs.Add(new sqlite_web.Components.Models.AdminNotificationLog
                {
                    Category = "Navigation",
                    SubCategory = "PageVisit",
                    MessageKey = "PCal_Navigation_PageVisitLog",
                    MessageArgs = System.Text.Json.JsonSerializer.Serialize(storageArgs),
                    Message = System.Text.RegularExpressions.Regex.Replace(string.Format(template, displayArgs), "<.*?>", string.Empty),
                    NotificationType = "Info",
                    CreatedAt = now,
                    CreatedDate = now.ToString("yyyy-MM-dd"),
                    CreatedTime = now.ToString("HH:mm:ss"),
                    AdminId = admin.Id,
                    AdminUsername = admin.Username,
                    EmployeeFullName = "-",
                    EmployeePersonnelCode = "-",
                    EmployeePhone = "-"
                });

                await db.SaveChangesAsync();
            }
            catch { }
        }

        protected void CloseUrlHijackModal() => ShowUrlHijackModal = false;

        // هدایت هوشمند و امن ادمین آنلاین از صفحه کیوسک به داشبورد مدیریتی متناسب با سطح دسترسی او
        protected void NavigateToDashboardSafe()
        {
            // در صورت نبود نشست معتبر برای ادمین، عملیات متوقف می‌شود
            if (AdminState.CurrentOnlineAdmin == null) return;
            var currentAdmin = AdminState.CurrentOnlineAdmin;

            // اولویت اول: هدایت سوپر ادمین یا مدیران دارای مجوزهای پرسنلی به پنل مدیریت پرسنل
            if (currentAdmin.IsSuperAdmin || currentAdmin.CanAddEmployee || currentAdmin.CanEditEmployee || currentAdmin.CanDeleteEmployee)
            {
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
            }
            // اولویت دوم: هدایت مدیرانی که فقط مجوز مدیریت مکان‌ها و دسته‌بندی‌ها را دارند به صفحه مکان‌ها
            else if (currentAdmin.CanManageLocations)
            {
                NavigationManager.NavigateTo("/admin/locations", forceLoad: false);
            }
            // اولویت سوم: نمایش پنجره هشدار عدم دسترسی در صورت نداشتن هیچ‌گونه مجوز مدیریتی
            else
            {
                ShowAccessDeniedModal = true;
                InvokeAsync(StateHasChanged);
            }
        }
        protected void CloseAccessModal() => ShowAccessDeniedModal = false;

        // متد گوش دادن بومی به تغییرات وضعیت سرویس مرکزی جهت به‌روزرسانی آنی زبان و دکمه‌های هدر
        private async void OnAdminStateChanged()
        {
            try
            {
                // 🟢 واکشی آنی آخرین زبان ذخیره شده از حافظه مرورگر به محض دریافت سیگنال از پنل تنظیمات
                var langResult = await LocalStorage.GetAsync<string>("SystemLanguageSetting");
                if (langResult.Success && !string.IsNullOrEmpty(langResult.Value))
                {
                    CurrentLanguage = langResult.Value;

                    // اعمال تغییر زبان روی موتور مترجم کپسوله‌شده در هدر لایوت
                    if (CalendarManager != null)
                    {
                        CalendarManager.CurrentLanguage = CurrentLanguage;
                    }
                }
            }
            catch
            {
                // مهار خطاهای احتمالی ارتباط با سشن مرورگر
            }

            // 🔄 اجرای منطق بومی قدیمی شما جهت به‌روزرسانی وضعیت دکمه فعال صفحه
            UpdateActivePageIndicator(NavigationManager.Uri);

            // بازرندر آنی و لحظه‌ای کل هدر برنامه بدون نیاز به رفرش فیزیکی صفحه
            await InvokeAsync(StateHasChanged);
        }
        public void Dispose()
        {
            AdminState.OnChange -= OnAdminStateChanged;
            NavigationManager.LocationChanged -= OnLocationChanged;
        }

        // بستن هم‌زمان منوی کشویی موبایل و زیرمنوی سطوح دسترسی و بازرندر رابط کاربری
        protected void CloseMobileMenu()
        {
            IsMobileMenuOpen = false;
            IsAccessDropdownOpen = false;
            InvokeAsync(StateHasChanged);
        }

        // بستن منوهای باز هدر و هدایت پویا به مسیر مقصد بدون بارگذاری مجدد صفحه
        protected void NavigateToPage(string targetUrl)
        {
            IsMobileMenuOpen = false;
            IsAccessDropdownOpen = false;
            NavigationManager.NavigateTo(targetUrl, forceLoad: false);
        }

        // ارزیابی سطح دسترسی مدیر آنلاین به آدرس فعلی و جلوگیری از ورود غیرمجاز از طریق نوار آدرس
        private void EvaluateUrlSecurity(string url)
        {
            if (AdminState.CurrentOnlineAdmin == null) return;
            var user = AdminState.CurrentOnlineAdmin;

            // بررسی دسترسی اختصاصی سوپر ادمین برای صفحات مدیریت مدیران و تاریخچه اعلانات
            if ((url.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase) ||
                 url.Contains("/admin/notifications", StringComparison.OrdinalIgnoreCase)) && !user.IsSuperAdmin)
            {
                ShowUrlHijackModal = true;
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
                return;
            }

            // بررسی دسترسی به صفحه مکان‌ها و دسته‌بندی‌ها (مجاز برای سوپر ادمین یا مدیران دارای تیک CanManageLocations)
            if (url.Contains("/admin/locations", StringComparison.OrdinalIgnoreCase) && !user.IsSuperAdmin && !user.CanManageLocations)
            {
                ShowUrlHijackModal = true;
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
                return;
            }

            // بررسی حداقل مجوزهای پرسنلی برای ورود مدیران محدود به پنل مدیریت پرسنل
            if (url.Contains("/admin/panel", StringComparison.OrdinalIgnoreCase) && !user.IsSuperAdmin)
            {
                if (!user.CanAddEmployee && !user.CanEditEmployee && !user.CanDeleteEmployee)
                {
                    if (user.CanManageLocations)
                    {
                        NavigationManager.NavigateTo("/admin/locations", forceLoad: false);
                    }
                    else
                    {
                        ShowUrlHijackModal = true;
                        NavigationManager.NavigateTo("/", forceLoad: false);
                    }
                }
            }
        }


        // تشخیص صفحه فعال بر اساس نشانی فعلی مرورگر جهت اعمال حاشیه سبز روی دکمه مربوطه در هدر
        private void UpdateActivePageIndicator(string url)
        {
            // بررسی فعال بودن صفحه مدیریت پرسنل
            if (url.Contains("/admin/panel", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "panel";
            }
            // بررسی فعال بودن صفحه مدیریت سطوح دسترسی و مدیران
            else if (url.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "manage";
            }
            // بررسی فعال بودن صفحه مدیریت مکان‌ها و دسته‌بندی پرسنل
            else if (url.Contains("/admin/locations", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "locations";
            }
            // بررسی فعال بودن صفحه تاریخچه اعلانات سیستم
            else if (url.Contains("/admin/notifications", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "notifications";
            }
            // حالت پیش‌فرض برای صفحه اصلی کیوسک تردد
            else
            {
                ActivePage = "home";
            }
        }


    }
}
