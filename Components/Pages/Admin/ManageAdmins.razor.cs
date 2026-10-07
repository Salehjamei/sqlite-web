using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class ManageAdmins
    {
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            // 🚀 تمام منطق پایش مسیر، لایه امنیتی توکن و ریدایرکت ادمین‌ها به عهده یک خط کد در سرویس مرکزی است
            await AdminState.CheckAccessAndRedirectAsync();
        }
    }
}
