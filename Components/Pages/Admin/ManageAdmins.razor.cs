using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin;
using sqlite_web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sqlite_web.Components.Pages.Admin
{
    /// <summary>
    /// کلاس منطق پشت‌صحنه (Code-Behind) مدیریت سطوح دسترسی، متصل به متدهای بومی سرویس مرکزی
    /// </summary>
    public partial class ManageAdmins : AppBaseComponent, IDisposable
    {
        // تزریق مستقیم هسته متمرکز سرویس مدیریت ادمین‌ها
        [Inject] public AdminStateService AdminState { get; set; } = default!;

        // متغیر جدید ذخیره عبارت جستجوی مدیران سیستم
        protected string SearchQuery { get; set; } = string.Empty;

        // لیست فیلتر شده مدیران سیستم بر اساس عبارت جستجو
        protected List<AdminUser> FilteredAdmins => string.IsNullOrWhiteSpace(SearchQuery)
            ? (AdminState.AdminList ?? new())
            : (AdminState.AdminList ?? new()).Where(a => a.Username != null && a.Username.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        // متغیر کنترل شناسه منوی آبشاری فعال موبایل بدون بسته‌شدن خودکار
        protected int? ActiveMobileDropdownId { get; set; } = null;

        // پرچم‌های بولین مدیریت باز یا بسته بودن پنجره‌های مودال فرم و حذف تعاملی ماژولار
        protected bool showFormModal { get; set; } = false;
        protected bool showDeleteModal { get; set; } = false;
        protected int targetDeleteAdminId = 0;

        /// <summary>
        /// متد آسنکرون مقداردهی اولیه کامپوننت مدیریت ادمین‌ها
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            // پاکسازی پیغام‌های خطای احتمالی قبلی در اولین لحظه ورود به صفحه
            AdminState.FeedbackMessage = string.Empty;

            // ۱. اتصال و گوش دادن به رویداد تغییرات سشن در سرویس
            AdminState.OnChange += OnAdminStateChanged;

            // ۲. پایش گارد امنیتی اصالت توکن و سشن سوپر ادمین ارشد
            await AdminState.CheckAccessAndRedirectAsync();

            // ۳. فراخوانی متد رسمی سرویس جهت واکشی اطلاعات از SQLite
            await AdminState.LoadAdminsDataAsync();
        }

        private async void OnAdminStateChanged()
        {
            await SyncLanguageFromStorageAsync();
        }

        private async Task SyncLanguageFromStorageAsync()
        {
            try
            {
                await LoadStoredLanguageAndCalendarAsync();

                if (CalendarManager != null && !string.IsNullOrEmpty(CurrentLanguage))
                {
                    CalendarManager.CurrentLanguage = CurrentLanguage;
                }
            }
            catch { }

            await InvokeAsync(StateHasChanged);
        }

        protected void ToggleMobileMenuDropdown(int adminId)
        {
            if (ActiveMobileDropdownId == adminId) ActiveMobileDropdownId = null;
            else ActiveMobileDropdownId = adminId;
        }

        protected void CloseAllMobileDropdowns() => ActiveMobileDropdownId = null;

        public void Dispose()
        {
            AdminState.OnChange -= OnAdminStateChanged;
        }

        // متدهای کنترل وضعیت بازشوهای مستقل فرم ثبت و ویرایش
        protected void OpenAddAdminModal()
        {
            CloseAllMobileDropdowns();
            AdminState.CancelEdit();
            showFormModal = true;
        }

        protected void CloseFormModal()
        {
            showFormModal = false;
            AdminState.CancelEdit();
        }

        protected void CancelDeleteModal()
        {
            showDeleteModal = false;
            targetDeleteAdminId = 0;
        }

        /// <summary>
        /// فرستادن اطلاعات فرم به متد ذخیره‌سازی سرور و صدور اعلان شیشه‌ای چندزبانه (موفق یا خطا)
        /// </summary>
        protected async Task HandleSaveAdmin()
        {
            if (string.IsNullOrWhiteSpace(AdminState.FormAdminUsername)) return;

            string targetAdminName = AdminState.FormAdminUsername;
            bool isModeEdit = AdminState.IsEditMode;

            // پاکسازی پیام خطای قبلی پیش از تلاش برای ذخیره
            AdminState.FeedbackMessage = string.Empty;

            // دریافت نتیجه موفقیت یا شکست عملیات از سرویس مرکزی
            bool isSuccess = await AdminState.SaveAdminAsync();

            if (isSuccess)
            {
                // قالب‌بندی نام مدیر با رنگ قرمز جهت نمایش در اعلان
                string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";
                string msgKey = isModeEdit ? "PCal_ManageAdmins_EditSuccess" : "PCal_ManageAdmins_AddSuccess";

                // صدور اعلان موفق و ثبت در تاریخچه سوپر ادمین با زیردسته AdminManagement
                TriggerToast(
                    category: "ManageAdmins",
                    subCategory: "AdminManagement",
                    messageKey: msgKey,
                    args: new object[] { coloredName },
                    isError: false
                );

                // بستن مودال فرم فقط در صورت موفقیت
                showFormModal = false;
                CloseAllMobileDropdowns();
            }
            else
            {
                // صدور اعلان خطای قرمز رنگ (مثلاً تلاش برای حذف رتبه تنها سوپر ادمین) و ثبت در سرور
                string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";
                TriggerToast(
                    category: "ManageAdmins",
                    subCategory: "AdminSecurity",
                    messageKey: "PCal_ManageAdmins_LastSuperAdminError",
                    args: new object[] { coloredName },
                    isError: true
                );
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task HandleToggleFreeze(int id, bool freezeStatus)
        {
            CloseAllMobileDropdowns();

            string targetAdminName = AdminState.AdminList?.FirstOrDefault(a => a.Id == id)?.Username ?? "Admin";

            // اعمال تغییر وضعیت فریز در دیتابیس
            await AdminState.ToggleFreezeAdminAsync(id, freezeStatus);

            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";
            string msgKey = freezeStatus ? "PCal_ManageAdmins_FreezeSuccess" : "PCal_ManageAdmins_UnfreezeSuccess";

            // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته AdminSecurity
            TriggerToast(
                category: "ManageAdmins",
                subCategory: "AdminSecurity",
                messageKey: msgKey,
                args: new object[] { coloredName },
                isError: false
            );

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// ابطال توکن زنده عملیاتی مدیر و ثبت در تاریخچه اعلان‌های سرور
        /// </summary>
        protected async Task HandleStopToken(int id)
        {
            CloseAllMobileDropdowns();

            string targetAdminName = AdminState.AdminList?.FirstOrDefault(a => a.Id == id)?.Username ?? "Admin";

            // ابطال توکن در دیتابیس
            await AdminState.StopAdminTokenAsync(id);

            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";

            // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته AdminSecurity
            TriggerToast(
                category: "ManageAdmins",
                subCategory: "AdminSecurity",
                messageKey: "PCal_ManageAdmins_RevokeTokenSuccess",
                args: new object[] { coloredName },
                isError: false
            );

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// فعال‌سازی حالت ویرایش و بارگذاری مشخصات مدیر در فیلدهای فرم
        /// </summary>
        protected void TriggerStartEdit(AdminUser admin)
        {
            AdminState.FeedbackMessage = string.Empty;
            AdminState.StartEdit(admin);
            CloseAllMobileDropdowns();
            showFormModal = true;
        }
        // ارسال فایل تصویر انتخاب‌شده به سرویس مدیریت ادمین‌ها جهت ذخیره در سرور
        protected async Task HandleAdminImageUpload(InputFileChangeEventArgs e)
        {
            var result = await AdminState.UploadAdminAvatarAsync(e.File);
            if (!result.Success && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                TriggerToast(
                    category: "ManageAdmins",
                    subCategory: "Validation",
                    messageKey: "PCal_AdminPanel_ImageUploadError",
                    args: new object[] { result.ErrorMessage },
                    isError: true
                );
            }
        }
        /// آماده‌سازی پنجره مودال تاییدیه حذف مدیر
        protected void TriggerDeleteAdmin(int id, string name)
        {
            if (AdminState.CurrentOnlineAdmin?.Id == id) return;

            targetDeleteAdminId = id;
            CloseAllMobileDropdowns();
            showDeleteModal = true;
        }
        /// اجرای حذف قطعی مدیر و صدور اعلان شیشه‌ای چندزبانه همراه با ثبت در تاریخچه سرور     
        protected async Task ExecuteConfirmedAdminDelete()
        {
            showDeleteModal = false;

            string targetAdminName = AdminState.AdminList?.FirstOrDefault(a => a.Id == targetDeleteAdminId)?.Username ?? "Admin";

            // حذف مدیر از دیتابیس
            await AdminState.DeleteAdminAsync(targetDeleteAdminId);

            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";

            // صدور اعلان شیشه‌ای چندزبانه و ثبت در تاریخچه سوپر ادمین با زیردسته AdminManagement
            TriggerToast(
                category: "ManageAdmins",
                subCategory: "AdminManagement",
                messageKey: "PCal_ManageAdmins_DeleteSuccess",
                args: new object[] { coloredName },
                isError: false
            );

            targetDeleteAdminId = 0;
            await InvokeAsync(StateHasChanged);
        }

        // متد چرخه حیات رندر تعاملی صفحه مدیریت مدیران جهت بارگذاری خودکار زبان سیستم
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await SyncLanguageFromStorageAsync();
            }
        }
    }
}
