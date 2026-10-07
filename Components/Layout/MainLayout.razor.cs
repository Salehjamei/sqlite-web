using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Layout
{
    public partial class MainLayout
    {
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        [Inject]
        public NavigationManager MyNavigationManager { get; set; } = default!;

        // متد خروج سراسری و مرکزی هدر لایوت
        protected async Task HandleGlobalLogout()
        {
            // شستشوی توکن از روی هارد لوکال مرورگر از طریق سرویس
            await AdminState.LogoutAsync();

            // هدایت اجباری به لاگین مجزا همراه با ریفرش سشن‌ها
            MyNavigationManager.NavigateTo("/admin/login", forceLoad: true);
        }
    }
}
