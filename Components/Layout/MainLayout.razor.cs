using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using System;
using System.Threading.Tasks;

namespace sqlite_web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject]
        public NavigationManager MyNavigationManager { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        public AppDbContext DbContext { get; set; } = default!;

        protected bool hasToken = false;
        protected string adminUsername = "";
        protected bool isAuthorizedAdmin = false;
        protected bool isChecked = false;

        protected override void OnInitialized()
        {
            MyNavigationManager.LocationChanged += OnLocationChanged;
        }

        // متد مرکزی اسکن توکن و اعمال ریفرش آنی گرافیک هدر
        private async Task CheckTokenAndPermissionsAsync()
        {
            try
            {
                var savedToken = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(savedToken))
                {
                    hasToken = true;
                    adminUsername = savedToken;

                    if (DbContext != null)
                    {
                        var admin = await DbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                        if (admin != null)
                        {
                            if (admin.IsSuperAdmin || admin.CanAddEmployee || admin.CanEditEmployee || admin.CanDeleteEmployee)
                            {
                                isAuthorizedAdmin = true;
                            }
                            else
                            {
                                isAuthorizedAdmin = false; // ادمین بدون دسترسی (کیوسک)
                            }
                        }
                    }
                }
                else
                {
                    hasToken = false;
                    adminUsername = "";
                    isAuthorizedAdmin = false;
                }
            }
            catch
            {
                // مهار خطای جاوااسکریپت در اولین رندر سرور
            }
            finally
            {
                isChecked = true;
                StateHasChanged(); // 🌟 فرمان لود فوری و اجباری دکمه خروج به مرورگر
            }
        }

        // 🚀 فیکس کلیدی: بیدار کردن و فراخوانی متد اسکن در هر دو لایف‌سیکل سرور و مرورگر
        protected override async Task OnInitializedAsync()
        {
            await Task.Yield(); // آزادسازی ریسورس سرور
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // وقفه فوق‌العاده کوتاه برای پایداری کانال ارتباطی SignalR
                await Task.Delay(50);
                await CheckTokenAndPermissionsAsync();
            }
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            await InvokeAsync(async () =>
            {
                await CheckTokenAndPermissionsAsync();
            });
        }

        protected async Task GlobalLogout()
        {
            hasToken = false;
            adminUsername = "";
            isAuthorizedAdmin = false;
            isChecked = false;

            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
            MyNavigationManager.NavigateTo("/admin", forceLoad: true);
        }

        public void Dispose()
        {
            MyNavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
