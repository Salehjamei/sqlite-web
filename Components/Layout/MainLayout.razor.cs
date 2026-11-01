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

        protected void CloseMobileMenu()
        {
            IsMobileMenuOpen = false;
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

        private void EvaluateUrlSecurity(string url)
        {
            if (AdminState.CurrentOnlineAdmin == null) return;
            var user = AdminState.CurrentOnlineAdmin;

            if (url.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase) && !user.IsSuperAdmin)
            {
                ShowUrlHijackModal = true;
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
            }

            if (url.Contains("/admin/panel", StringComparison.OrdinalIgnoreCase) && !user.IsSuperAdmin)
            {
                if (!user.CanAddEmployee && !user.CanEditEmployee && !user.CanDeleteEmployee)
                {
                    ShowUrlHijackModal = true;
                    NavigationManager.NavigateTo("/", forceLoad: false);
                }
            }
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


        /// <summary>
        /// 🟢 بازنویسی متد پایش آدرس فعال با حذف کامل تداخل حروف کوچک و بزرگ برای تثبیت حاشیه سبز دکمه‌ها
        /// </summary>
        private void UpdateActivePageIndicator(string url)
        {
            if (url.Contains("/admin/panel", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "panel";
            }
            else if (url.Contains("/admin/manage-admins", StringComparison.OrdinalIgnoreCase))
            {
                ActivePage = "manage";
            }
            else
            {
                ActivePage = "home";
            }
        }

        public void Dispose()
        {
            AdminState.OnChange -= OnAdminStateChanged;
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
        /// <summary>
        /// 🟢 متد جدید هدایت هوشمند آدرس مرورگر به محض فشردن دکمه‌های فیزیکی هدر سیستم
        /// </summary>
        protected void NavigateToPage(string targetUrl)
        {
            IsMobileMenuOpen = false; // منوی همبرگری موبایل را در صورت باز بودن ببند
            NavigationManager.NavigateTo(targetUrl, forceLoad: false); // تغییر آدرس زنده صفحه بدون لود مجدد کل مرورگر
        }
          
    }
}
