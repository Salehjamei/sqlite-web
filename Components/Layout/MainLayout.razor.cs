using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        [Inject] public NavigationManager MyNavigationManager { get; set; } = default!;

        protected override void OnInitialized()
        {
            // گوش دادن زنده به پالس‌های تغییر وضعیت سرویس ادمین
            AdminState.OnChange += OnStateServiceChanged;
            MyNavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Task.Delay(100); // زمان کافی برای لود حافظه مرورگر پس از ورود ادمین
                await AdminState.CheckAccessAndRedirectAsync();
            }
        }

        // 🌟 متد خروج مرکزی هدر لایوت جهت تضمین شستشوی کامل سشن و هدایت اجباری
        protected async Task HandleGlobalLogout()
        {
            await AdminState.LogoutAsync();
        }

        private void OnStateServiceChanged()
        {
            InvokeAsync(StateHasChanged); // ریفرش گرافیکی آنی هدر
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            await InvokeAsync(async () =>
            {
                await AdminState.CheckAccessAndRedirectAsync();
            });
        }

        public void Dispose()
        {
            AdminState.OnChange -= OnStateServiceChanged;
            MyNavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
