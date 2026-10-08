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

        // متد خروج مرکزی هدر مجهز به هدایت اجباری به لاگین مجزا
        protected async Task HandleGlobalLogout()
        {
            await AdminState.LogoutAsync();
        }
    }
}
