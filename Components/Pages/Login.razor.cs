using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages
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

        private async Task ExecuteServiceLogin()
        {
            if (string.IsNullOrWhiteSpace(localUsername) || string.IsNullOrWhiteSpace(localPassword))
            {
                feedbackMessage = "❌ لطفاً تمام کادرها را پر کنید!";
                return;
            }

            bool success = await AdminState.LoginAsync(localUsername, localPassword);

            if (success)
            {
                // فیکس ریدایرکت: هدایت روان بدون ریفرش تخریب‌کننده کش مرورگر
                if (!AdminState.IsAuthorized)
                {
                    MyNavigationManager.NavigateTo("/", forceLoad: false); // انتقال ادمین معمولی به کیوسک
                }
                else
                {
                    MyNavigationManager.NavigateTo("/admin", forceLoad: false); // انتقال سوپر ادمین به پنل اصلی
                }
            }
            else
            {
                feedbackMessage = "❌ نام کاربری یا رمز عبور اشتباه است!";
            }
        }

    }
}
