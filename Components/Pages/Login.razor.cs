using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace sqlite_web.Components.Pages
{
    public partial class Login
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        // رفع وارنینگ‌های BL0008 با حذف مقداردهی اولیه مستقیم فیلدهای فرم
        [SupplyParameterFromForm]
        public string? Username { get; set; }

        [SupplyParameterFromForm]
        public string? Password { get; set; }

        /// <summary>
        /// انتقال منطق بررسی توکن به محیط تعاملی بعد از رندر شدن فیزیکی صفحه در مرورگر
        /// </summary>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await AdminState.CheckAccessAndRedirectAsync();

                if (AdminState.IsAuthorized)
                {
                    NavigationManager.NavigateTo("/", forceLoad: false);
                }

                StateHasChanged();
            }
        }

        private async Task HandleLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                AdminState.FeedbackMessage = "❌ لطفا نام کاربری و رمز عبور را وارد نمایید.";
                return;
            }

            // فراخوانی سرویس مرکزی با مقادیر پر شده
            bool loginResult = await AdminState.LoginAsync(Username, Password);

            if (loginResult)
            {
                NavigationManager.NavigateTo("/", forceLoad: false);
            }
        }
    }
}
