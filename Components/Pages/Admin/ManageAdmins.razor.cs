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

        protected override async Task OnInitializedAsync()
        {
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
        /// 🟢 فرستادن اطلاعات فیلتر شده به متد رسمی ذخیره‌سازی سرور
        /// </summary>
        protected async Task HandleSaveAdmin()
        {
            if (string.IsNullOrWhiteSpace(AdminState.FormAdminUsername)) return;

            // ۱. اجرای خودکار متد آسنکرون ذخیره/ویرایش دیتابیس بومی سرویس شما
            await AdminState.SaveAdminAsync();

            showFormModal = false; // بستن مودال پس از ذخیره موفق
            CloseAllMobileDropdowns();
        }
         /// <summary>
        /// 🟢 فعال‌سازی آلرت برای عملیات فریز و مسدودسازی ادمین‌ها
        /// </summary>
        protected async Task HandleToggleFreeze(int id, bool freezeStatus)
        {
            CloseAllMobileDropdowns();
            
            // ۱. فراخوانی متد رسمی لایه سرویس شما
            await AdminState.ToggleFreezeAdminAsync(id, freezeStatus);
            
            // ۲. تزریق مستقیم پیام به فیلد فیدبک جهت بیدار شدن فوری دایو آلرت
            AdminState.FeedbackMessage = freezeStatus 
                ? "❄️ حساب کاربری مدیر انتخاب شده با موفقیت مسدود (فریز) و سشن او ابطال شد." 
                : "✅ حساب کاربری مدیر با موفقیت فعال گردید.";
                
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// 🟢 فعال‌سازی آلرت برای عملیات ابطال و قطع فوری توکن زنده عملیاتی
        /// </summary>
        protected async Task HandleStopToken(int id)
        {
            CloseAllMobileDropdowns();
            
            // ۱. فراخوانی متد رسمی لایه سرویس شما
            await AdminState.StopAdminTokenAsync(id);
            
            // ۲. تزریق مستقیم پیام هشدار به فیلد فیدبک جهت بیدار شدن فوری دایو آلرت
            AdminState.FeedbackMessage = "💥 توکن زنده عملیاتی مدیر ابطال و کاربر فوراً از سیستم اخراج شد.";
            
            await InvokeAsync(StateHasChanged);
        }
        /// <summary>
        /// 🟢 فعال‌سازی حالت ویرایش و بارگذاری مشخصات در فیلدهای بومی سرویس شما
        /// </summary>
        protected void TriggerStartEdit(AdminUser admin)
        {
            AdminState.StartEdit(admin); // لود داده‌ها در فیلدهای FormAdminUsername و... سرویس شما
            CloseAllMobileDropdowns();
            showFormModal = true; // نمایش پاپ‌آپ ویرایش
        }

        protected void TriggerDeleteAdmin(int id, string name)
        {
            if (AdminState.CurrentOnlineAdmin?.Id == id) return;

            targetDeleteAdminId = id;
            deleteTargetMessage = $"آیا از حذف کامل حساب کاربری مدیر سیستم با نام «{name}» مطمئن هستید؟";
            CloseAllMobileDropdowns();
            showDeleteModal = true;
        }

        /// <summary>
        /// 🟢 اجرای متد رسمی حذف فیزیکی ادمین از روی دیتابیس بومی سرویس شما
        /// </summary>
        protected async Task ExecuteConfirmedAdminDelete()
        {
            showDeleteModal = false;

            // فراخوانی متد حذف فیزیکی بومی سرویس شما
            await AdminState.DeleteAdminAsync(targetDeleteAdminId);

            targetDeleteAdminId = 0;
        }
    }
}