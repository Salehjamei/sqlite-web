using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using System;
using System.Collections.Generic;
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

        /// متد مشترک جهت بازیابی هم‌زمان تنظیمات زبان و نوع تقویم از حافظه مرورگر در تمام صفحات برنامه
        protected async Task LoadStoredLanguageAndCalendarAsync()
        {
            try
            {
                // بازیابی نوع تقویم سه‌گانه ذخیره‌شده (شمسی، میلادی یا هجری قمری) از حافظه مرورگر
                var calTypeResult = await LocalStorage.GetAsync<string>("SystemCalendarTypeSetting");
                if (calTypeResult.Success && !string.IsNullOrEmpty(calTypeResult.Value) && CalendarManager != null)
                {
                    CalendarManager.CalendarType = calTypeResult.Value;
                }
                else
                {
                    // بازیابی تنظیمات تقویم از کلید قدیمی در صورت عدم وجود تنظیمات جدید در حافظه مرورگر
                    var calendarResult = await LocalStorage.GetAsync<bool>("UsePersianCalendarSetting");
                    if (calendarResult.Success && CalendarManager != null)
                    {
                        CalendarManager.UsePersianCalendar = calendarResult.Value;
                    }
                }

                // بازیابی زبان فعال سیستم (فارسی، انگلیسی، ترکی یا عربی) از حافظه مرورگر
                var langResult = await LocalStorage.GetAsync<string>("SystemLanguageSetting");
                if (langResult.Success && !string.IsNullOrEmpty(langResult.Value))
                {
                    CurrentLanguage = langResult.Value;
                }
            }
            catch
            {
                // مهار خطاهای احتمالی ارتباط با حافظه مرورگر در زمان پیش‌رندر اولیه سرور
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

        // صدور اعلان چندزبانه و افزودن آن به صف نمایش حداکثر ۳ تایی
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
            if (ActiveToastList.Count >= 3)
            {
                ActiveToastList.RemoveAt(0);
            }

            ActiveToastList.Add(new ToastItemModel
            {
                Category = category,
                SubCategory = subCategory,
                MessageKey = messageKey,
                MessageArgs = args,
                Message = string.Empty,
                IsError = isError,
                TargetEmployee = targetEmp,
                EventDate = eventDate,
                EventTime = eventTime,
                ShownAt = DateTime.Now
            });

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
        // مدل نگهداری مشخصات هر اعلان در صف نمایش حداکثر ۳ تایی
        public class ToastItemModel
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Category { get; set; } = "General";
            public string SubCategory { get; set; } = "General";
            public string MessageKey { get; set; } = string.Empty;
            public object[]? MessageArgs { get; set; }
            public string Message { get; set; } = string.Empty;
            public string? EventDate { get; set; }
            public string? EventTime { get; set; }
            public bool IsError { get; set; } = false;
            public Employee? TargetEmployee { get; set; }
            public DateTime ShownAt { get; set; } = DateTime.Now;
            public bool IsSavedToDb { get; set; } = false;
        }

        // صف زنده اعلان‌های باز در صفحه (حداکثر ۳ عدد به ترتیب ورود)
        protected List<ToastItemModel> ActiveToastList { get; set; } = new();

        /// <summary> ^
        /// ToastNotification
        /// </summary>

    }
}
