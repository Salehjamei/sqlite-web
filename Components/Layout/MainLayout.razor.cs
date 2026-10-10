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
                await AdminState.CheckAccessAndRedirectAsync();
                EvaluateUrlSecurity(NavigationManager.Uri);

                // 🟢 تازه‌سازی مجدد وضعیت دکمه‌ها پس از اولین رندر فیزیکی لایوت در مرورگر کلاینت
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

        private void OnAdminStateChanged()
        {
            UpdateActivePageIndicator(NavigationManager.Uri);
            InvokeAsync(StateHasChanged);
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
