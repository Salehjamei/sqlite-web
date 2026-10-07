using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class AdminPanel
    {
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            // پایش آنی انقضای توکن و ریدایرکت خودکار به لاگین مجزا از درون خودِ سرویس
            await AdminState.CheckAccessAndRedirectAsync();
        }
    }
}
