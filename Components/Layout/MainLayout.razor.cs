using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        [Inject]
        public NavigationManager MyNavigationManager { get; set; } = default!;

        protected override void OnInitialized()
        {
            // گوش دادن به تغییرات وضعیت ادمین در سراسر برنامه
            AdminState.OnChange += StateHasChanged;
            MyNavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // مقداردهی اولیه سرویس فقط یک‌بار در لایه کلاینت مرورگر
                await AdminState.InitializeAsync();
            }
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            await InvokeAsync(async () =>
            {
                await AdminState.InitializeAsync();
            });
        }

        protected async Task HandleGlobalLogout()
        {
            await AdminState.LogoutAsync();
            MyNavigationManager.NavigateTo("/admin", forceLoad: true);
        }

        public void Dispose()
        {
            AdminState.OnChange -= StateHasChanged;
            MyNavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
