using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading.Tasks;

namespace sqlite_web.Components.Pages
{
    // ارث بری از AppBaseComponent جهت لود خودکار زبان و دسترسی به لایه لوکال استوریج
    public partial class Login : AppBaseComponent
    {
        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [SupplyParameterFromForm]
        public string? Username { get; set; }

        [SupplyParameterFromForm]
        public string? Password { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // فراخوانی متد بارگذاری زبان و تنظیمات ذخیره شده سیستم از کلاس پایه
                await LoadStoredLanguageAndCalendarAsync();

                await AdminState.CheckAccessAndRedirectAsync();

                if (AdminState.IsAuthorized)
                {
                    NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// متد مدیریت سوییچ آنلاین زبان جاری و ذخیره ماندگار در حافظه کلاینت پیش از لاگین ادمین
        /// </summary>
        protected async Task ChangeLoginLanguage(string lang)
        {
            CurrentLanguage = lang;
            if (CalendarManager != null)
            {
                CalendarManager.CurrentLanguage = lang;
            }
            try
            {
                await LocalStorage.SetAsync("SystemLanguageSetting", lang);
            }
            catch
            {
                // mhar khataie adame dastrasie daryaft sessional مرورگر
            }
            await InvokeAsync(StateHasChanged);
        }

        private async Task HandleLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                string requiredMsg = CalendarManager.Translate("PCal_Login_RequiredFields");
                AdminState.FeedbackMessage = $"❌ {requiredMsg}";
                return;
            }

            bool loginResult = await AdminState.LoginAsync(Username, Password);

            if (loginResult)
            {
                await AdminState.LoadWorkSettingsAsync();
                NavigationManager.NavigateTo("/admin/panel", forceLoad: false);
            }
        }
    }
}
