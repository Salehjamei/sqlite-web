using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using System.Threading.Tasks;

namespace sqlite_web.Components
{
    /// <summary>
    /// کلاس پایه اختصاصی ما که از کامپوننت بومی بلیزور ارث‌بری می‌کند
    /// </summary>
    public class AppBaseComponent : ComponentBase
    {
        // تزریق حافظه محلی مرورگر
        [Inject] public ProtectedLocalStorage LocalStorage { get; set; } = default!;

        // نمونه‌سازی واحد از کلاس مدیریت تقویم و ویژگی‌های چندزبانه
        protected PCalendar CalendarManager { get; set; } = new();
        // متغیر کنترل زبان فعال
        protected string CurrentLanguage { get; set; } = "fa";

        /// <summary>
        /// متد مشترک برای لود هم‌زمان زبان و تقویم در تمام صفحات بدون تکرار کد
        /// </summary>
        protected async Task LoadStoredLanguageAndCalendarAsync()
        {
            try
            {
                var calendarResult = await LocalStorage.GetAsync<bool>("UsePersianCalendarSetting");
                if (calendarResult.Success)
                {
                    CalendarManager.UsePersianCalendar = calendarResult.Value;
                }

                var langResult = await LocalStorage.GetAsync<string>("SystemLanguageSetting");
                if (langResult.Success && !string.IsNullOrEmpty(langResult.Value))
                {
                    CurrentLanguage = langResult.Value;
                    CurrentLanguage = CurrentLanguage;
                }
            }
            catch
            {
                // مهار خطای لود اولیه در Blazor Server
            }
        }
        /// <summary>
        /// ToastNotification
        /// </summary> !
        // متغیرهای مشترک کنترل کامپوننت ToastNotification برای تمام صفحات
        protected bool showToast = false;
        protected bool isToastError = false;
        protected string toastCategory = "General";
        protected string toastSubCategory = "General";
        protected string toastMessageKey = string.Empty;
        protected object[]? toastMessageArgs = null;
        protected string toastMessage = string.Empty;
        protected string? toastEventDate = null;
        protected string? toastEventTime = null;
        protected Employee? toastTargetEmployee = null;

        // متد سراسری صدور اعلان چندزبانه با دریافت نام صفحه و زیردسته
        protected void TriggerToast(
            string category,
            string subCategory,
            string messageKey,
            object[]? args = null,
            bool isError = false,
            Employee? targetEmp = null,
            string? eventDate = null,
            string? eventTime = null)
        {
            toastCategory = category;
            toastSubCategory = subCategory;
            toastMessageKey = messageKey;
            toastMessageArgs = args;
            toastMessage = string.Empty;
            isToastError = isError;
            toastTargetEmployee = targetEmp;
            toastEventDate = eventDate;
            toastEventTime = eventTime;
            showToast = true;
        }


        /// <summary> ^
        /// ToastNotification
        /// </summary>
    }
}
