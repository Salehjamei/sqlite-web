using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
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
                    CalendarManager.CurrentLanguage = CurrentLanguage;
                }
            }
            catch
            {
                // مهار خطای لود اولیه در Blazor Server
            }
        }
    }
}
