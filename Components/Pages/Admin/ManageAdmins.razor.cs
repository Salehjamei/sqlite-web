using Microsoft.AspNetCore.Components;
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
    public partial class ManageAdmins : ComponentBase, IDisposable
    {
        // 🛡️ تزریق مستقیم هسته متمرکز سرویس مدیریت ادمین‌ها
        [Inject] public AdminStateService AdminState { get; set; } = default!;

        // 🔍 متغیر جدید ذخیره عبارت جستجوی مدیران سیستم
        protected string SearchQuery { get; set; } = string.Empty;

        // 🟢 تراز متغیر جستجو با نام لیست رسمی سرویس شما (AdminList) جهت فیکس ارورها
        protected List<AdminUser> FilteredAdmins => string.IsNullOrWhiteSpace(SearchQuery)
            ? (AdminState.AdminList ?? new())
            : (AdminState.AdminList ?? new()).Where(a => a.Username != null && a.Username.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        // 🟢 متغیر کنترل شناسه منوی آبشاری فعال موبایل بدون بسته‌شدن خودکار
        protected int? ActiveMobileDropdownId { get; set; } = null;

        // پرچم‌های بولین مدیریت باز یا بسته بودن پنجره‌های مودال فرم و حذف تعاملی ماژولار
        protected bool showFormModal { get; set; } = false;
        protected bool showDeleteModal { get; set; } = false;
        protected string deleteTargetMessage { get; set; } = string.Empty;
        protected int targetDeleteAdminId = 0;

        /// <summary>
        /// متد آسنکرون مقداردهی اولیه کامپوننت مدیریت ادمین‌ها
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            // 🟢 فیکس قطعی باگ: پاکسازی پیغام‌های مرده صفحات قبل در «اولین ثانیه ورود» به صفحه
            AdminState.FeedbackMessage = string.Empty;

            // ۱. اتصال و گوش دادن به رویداد تغییرات سشن در سرویس
            AdminState.OnChange += OnAdminStateChanged;

            // ۲. پایش گارد امنیتی اصالت توکن و سشن سوپر ادمین ارشد
            await AdminState.CheckAccessAndRedirectAsync();

            // ۳. فراخوانی متد رسمی بومی سرویس شما جهت واکشی اطلاعات از SQLite
            await AdminState.LoadAdminsDataAsync();
        }


        private void OnAdminStateChanged()
        {
            InvokeAsync(StateHasChanged);
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
            AdminState.CancelEdit(); // فراخوانی متد بومی ریست فرم سرویس شما
            showFormModal = true;
        }

        protected void CloseFormModal()
        {
            showFormModal = false;
            AdminState.CancelEdit(); // لغو وضعیت ادیت در سرویس
        }

        protected void CancelDeleteModal()
        {
            showDeleteModal = false;
            targetDeleteAdminId = 0;
        }
        /// <summary>
        /// فرستادن اطلاعات فیلتر شده به متد رسمی ذخیره‌سازی سرور و فعال‌سازی آلرت پویا با رنگ قرمز نام مدیر
        /// </summary>
        protected async Task HandleSaveAdmin()
        {
            if (string.IsNullOrWhiteSpace(AdminState.FormAdminUsername)) return;

            string targetAdminName = AdminState.FormAdminUsername;
            bool isModeEdit = AdminState.IsEditMode;

            await AdminState.SaveAdminAsync();

            // 🔴 تزریق استایل رنگ قرمز به نام ادمین جهت تفکیک بصری کامل
            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";

            AdminState.FeedbackMessage = isModeEdit
                ? $"مشخصات و مجوزهای حساب کاربری مدیر سیستم با نام {coloredName} با موفقیت اصلاح شد."
                : $"حساب کاربری مدیر جدید سیستم با نام {coloredName} با موفقیت در سرور تعریف و ثبت گردید.";

            showFormModal = false;
            CloseAllMobileDropdowns();
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// فعال‌سازی آلرت برای عملیات فریز همراه با نمایش نام قرمز رنگ مدیر هدف
        /// </summary>
        protected async Task HandleToggleFreeze(int id, bool freezeStatus)
        {
            CloseAllMobileDropdowns();

            string targetAdminName = AdminState.AdminList?.FirstOrDefault(a => a.Id == id)?.Username ?? "مدیر سیستم";

            await AdminState.ToggleFreezeAdminAsync(id, freezeStatus);

            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";

            AdminState.FeedbackMessage = freezeStatus
                ? $"حساب کاربری مدیر سیستم با نام {coloredName} با موفقیت مسدود فریز شد و سشن‌های او ابطال گردید."
                : $"حساب کاربری مدیر سیستم با نام {coloredName} با موفقیت فعال و رفع مسدودیت شد.";

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// فعال‌سازی آلرت برای عملیات ابطال توکن همراه با نمایش نام قرمز رنگ مدیر هدف
        /// </summary>
        protected async Task HandleStopToken(int id)
        {
            CloseAllMobileDropdowns();

            string targetAdminName = AdminState.AdminList?.FirstOrDefault(a => a.Id == id)?.Username ?? "مدیر سیستم";

            await AdminState.StopAdminTokenAsync(id);

            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";

            AdminState.FeedbackMessage = $"توکن زنده عملیاتی مدیر سیستم با نام {coloredName} ابطال شد و کاربر فوراً از برنامه اخراج گردید.";

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// فعال‌سازی حالت ویرایش و بارگذاری مشخصات در فیلدهای بومی سرویس شما
        /// </summary>
        protected void TriggerStartEdit(AdminUser admin)
        {
            AdminState.StartEdit(admin); // لود داده‌ها در فیلدهای FormAdminUsername و... سرویس شما
            CloseAllMobileDropdowns();
            showFormModal = true; // نمایش پاپ‌آ‌پ ویرایش
        }

        /// <summary>
        /// آماده‌سازی زیرساخت حذف و مهار نام ادمین برای پاپ‌آپ مودال تاییدیه حذف
        /// </summary>
        protected void TriggerDeleteAdmin(int id, string name)
        {
            if (AdminState.CurrentOnlineAdmin?.Id == id) return;

            targetDeleteAdminId = id;
            deleteTargetMessage = $"آیا از حذف کامل حساب کاربری مدیر سیستم با نام {name} مطمئن هستید(؟";
            CloseAllMobileDropdowns();
            showDeleteModal = true;
        }

        /// <summary>
        /// اجرای متد رسمی حذف فیزیکی ادمین و صدور آلرت زنده با نام قرمز رنگ
        /// </summary>
        protected async Task ExecuteConfirmedAdminDelete()
        {
            showDeleteModal = false;

            string targetAdminName = AdminState.AdminList?.FirstOrDefault(a => a.Id == targetDeleteAdminId)?.Username ?? "مدیر سیستم";

            await AdminState.DeleteAdminAsync(targetDeleteAdminId);

            string coloredName = $"<span style='color: #dc3545; font-weight: 900;'>«{targetAdminName}»</span>";

            AdminState.FeedbackMessage = $"حساب کاربری مدیر سیستم با نام {coloredName} به همراه کلیه سوابق و مجوزهای او با موفقیت حذف گردید.";

            targetDeleteAdminId = 0;
            await InvokeAsync(StateHasChanged);
        }




    }
}