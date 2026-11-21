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
                // 🟢 گام اول: بازیابی خودکار و ماندگار زبان ذخیره‌شده کاربر از حافظه مرورگر کلاینت
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
                catch
                {
                    // مهار خطاهای احتمالی ارتباط اولیه سشن بلیزور
                }

                // 🛡️ گام دوم: اجرای گارد دسترسی و پایش امنیتی آدرس بومی پروژه شما
                await AdminState.CheckAccessAndRedirectAsync();
                EvaluateUrlSecurity(NavigationManager.Uri);

                // 🔄 گام سوم: تازه‌سازی مجدد وضعیت دکمه‌ها و فعال‌سازی تعاملی رویدادها در مرورگر
                UpdateActivePageIndicator(NavigationManager.Uri);
                await InvokeAsync(StateHasChanged);
            }
        }


        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            UpdateActivePageIndicator(e.Location);
            EvaluateUrlSecurity(e.Location);
            InvokeAsync(StateHasChanged);
        }      
        protected void CloseUrlHijackModal() => ShowUrlHijackModal = false;

        protected void NavigateToDashboardSafe()
        {
            if (AdminState.CurrentOnlineAdmin == null) return;
            var currentAdmin = AdminState.CurrentOnlineAdmin;

            if (currentAdmin.IsSuperAdmin || currentAdmin.CanAddEmployee || currentAdmin.CanEditEmployee || currentAdmin.CanDeleteEmployee)
            {
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
            }
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

            // بررسی دسترسی سوپر ادمین برای صفحات مدیریت مدیران و تاریخچه اعلانات
            if ((url.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase) ||
                 url.Contains("/admin/notifications", StringComparison.OrdinalIgnoreCase)) && !user.IsSuperAdmin)
            {
                ShowUrlHijackModal = true;
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
            }

            // بررسی حداقل مجوزهای پرسنلی برای ورود مدیران محدود به پنل مدیریت پرسنل
            if (url.Contains("/admin/panel", StringComparison.OrdinalIgnoreCase) && !user.IsSuperAdmin)
            {
                if (!user.CanAddEmployee && !user.CanEditEmployee && !user.CanDeleteEmployee)
                {
                    ShowUrlHijackModal = true;
                    NavigationManager.NavigateTo("/", forceLoad: false);
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
