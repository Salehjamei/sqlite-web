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
            // گوش دادن به رویداد تغییر وضعیت ادمین در سراسر برنامه برای ریفرش آنی هدر
            AdminState.OnChange += StateHasChanged;
            MyNavigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Task.Delay(100); // زمان کوتاه برای لود لوکال استوریج مرورگر
                await AdminState.InitializeAsync();
                await InvokeAsync(StateHasChanged); // اجبار هدر به لود دکمه خروج
            }
        }



        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            await InvokeAsync(async () =>
            {
                await AdminState.InitializeAsync();
            });
        }

        public void Dispose()
        {
            AdminState.OnChange -= StateHasChanged;
            MyNavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
