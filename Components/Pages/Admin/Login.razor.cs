using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class Login
    {
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        [Inject]
        public NavigationManager MyNavigationManager { get; set; } = default!;

        protected string localUsername { get; set; } = "";
        protected string localPassword { get; set; } = "";
        protected string feedbackMessage { get; set; } = "";

        protected async Task ExecuteServiceLogin()
        {
            if (string.IsNullOrWhiteSpace(localUsername) || string.IsNullOrWhiteSpace(localPassword))
            {
                feedbackMessage = "❌ لطفاً تمام کادرها را پر کنید!";
                return;
            }

            bool success = await AdminState.LoginAsync(localUsername, localPassword);

            if (success)
            {
                if (!AdminState.IsAuthorized)
                {
                    MyNavigationManager.NavigateTo("/", forceLoad: true);
                }
                else
                {
                    MyNavigationManager.NavigateTo("/admin", forceLoad: true);
                }
            }
            else
            {
                feedbackMessage = "❌ نام کاربری یا رمز عبور اشتباه است!";
            }
        }
    }
}
