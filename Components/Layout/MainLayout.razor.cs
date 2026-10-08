using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace sqlite_web.Components.Layout
{
    public partial class MainLayout : IDisposable
    {
        [Inject] public NavigationManager MyNavigationManager { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public sqlite_web.AppDbContext DbContext { get; set; } = default!; // اتصال مستقیم به دیتابیس پروژه شما

        // 🔘 متغیرهای بومی و اختصاصی لایوت جهت رندر بدون باگ دکمه‌ها
        protected bool hasToken = false;
        protected bool IsAuthorized = false;
        protected string adminUsername = "";

        protected override void OnInitialized()
        {
            // اتصال به رویداد ناوبری برای پایش مداوم آدرس صفحات
            MyNavigationManager.LocationChanged += OnLocationChanged;
        }

        /// <summary>
        /// 🕒 اجرای تضمینی اسکن توکن در فاز بومی OnAfterRender بلایزر
        /// این متد مشکل Prerendering سرور را کاملاً حل کرده و منوها را فوراً روشن می‌کند.
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Task.Delay(50); // وقفه فوق‌العاده کوتاه برای ثبات کدهای جاوااسکریپت
                await CheckOnlineSessionAsync();
            }
        }

        // متد مرکزی اسکن حافظه مرورگر و واکشی زنده سطوح دسترسی از SQLite
        private async Task CheckOnlineSessionAsync()
        {
            try
            {
                var token = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");

                if (!string.IsNullOrEmpty(token) && DbContext != null)
                {
                    // اسکن مستقیم جدول ادمین‌ها در دیتابیس لوکال شما
                    var admin = await DbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == token);
                    if (admin != null)
                    {
                        hasToken = true;
                        adminUsername = admin.Username;

                        // تفکیک دقیق دسترسی سوپر ادمین و ادمین کیوسک بر پایه دیتابیس شما
                        if (admin.IsSuperAdmin || admin.CanAddEmployee || admin.CanEditEmployee || admin.CanDeleteEmployee)
                        {
                            IsAuthorized = true; // سوپر ادمین یا مجاز
                        }
                        else
                        {
                            IsAuthorized = false; // ادمین کیوسک بدون دسترسی
                        }

                        StateHasChanged(); // فرمان بومی بلایزر جهت رندر آنی دکمه خروج و رفتن به کیوسک
                        return;
                    }
                }

                // اگر توکنی یافت نشد، متغیرها ریست شوند
                hasToken = false;
                adminUsername = "";
                IsAuthorized = false;
                StateHasChanged();
            }
            catch
            {
                // مهار خطاهای تعامل در فاز پیش‌رندر سرور
            }
        }

        /// <summary>
        /// 🏃‍♂️ متد خروج بومی و قطعی هدر لایوت
        /// </summary>
        protected async Task ExecuteGlobalLogout()
        {
            hasToken = false;
            adminUsername = "";
            IsAuthorized = false;

            try
            {
                // پاک کردن فیزیکی توکن سشن از روی حافظه لوکال کلاینت
                await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
            }
            catch { }

            // هدایت اجباری به صفحه لاگین مجزا همراه با ریفرش کامل سشن‌ها
            MyNavigationManager.NavigateTo("/login", forceLoad: true);
        }

        private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            await InvokeAsync(async () =>
            {
                await CheckOnlineSessionAsync();
            });
        }

        public void Dispose()
        {
            MyNavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
