using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class ManageAdmins : IDisposable
    {
        [Inject]
        public AdminStateService AdminState { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            // ۱. ثبت رویداد ریفرش گرافیکی صفحه به محض تغییر وضعیت متغیرها در سرویس
            AdminState.OnChange += OnAdminStateChanged;

            // ۲. اجرای موتور پایش مرکزی امنیتی مسیرها و واکشی اطلاعات
            await AdminState.CheckAccessAndRedirectAsync();
            await AdminState.LoadAdminsDataAsync();
        }

        private void OnAdminStateChanged()
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            AdminState.OnChange -= OnAdminStateChanged;
        }
    }
}