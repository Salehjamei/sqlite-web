using Microsoft.AspNetCore.Components;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using sqlite_web.Services;
using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sqlite_web.Components.Modules
{
    public partial class ToastNotification : ComponentBase, IDisposable
    {
        [Inject] public AppDbContext DbContext { get; set; } = default!;

        // پارامترهای کنترل نمایش و محتوای اعلان
        [Parameter] public bool Show { get; set; }
        [Parameter] public EventCallback<bool> ShowChanged { get; set; }

        // کلید ترجمه پیام در کلاس PCalendar (اولویت اول برای چندزبانه‌سازی)
        [Parameter] public string MessageKey { get; set; } = string.Empty;

        // پارامترهای متغیر داخل متن پیام (مثلاً نام کارمند)
        [Parameter] public object[]? MessageArgs { get; set; }

        // متن مستقیم (برای سازگاری با بخش‌هایی که هنوز از روش قبلی استفاده می‌کنند)
        [Parameter] public string Message { get; set; } = string.Empty;

        [Parameter] public bool IsError { get; set; } = false;
        [Parameter] public string CurrentLanguage { get; set; } = "fa";
        [Parameter] public PCalendar CalendarManager { get; set; } = new();

        // دسته‌بندی اصلی و زیردسته بخش صادرکننده اعلان (ثبت مستقیم به انگلیسی در سرور)
        [Parameter] public string Category { get; set; } = "General";
        [Parameter] public string SubCategory { get; set; } = "General";

        // پارامترهای اختیاری تاریخ و ساعت رویداد هدف (مثلاً در زمان ثبت دستی تردد)
        [Parameter] public string? EventDate { get; set; }
        [Parameter] public string? EventTime { get; set; }

        // زمان بسته شدن خودکار به میلی‌ثانیه (مقدار 0 به معنای باز ماندن تا زمان بستن دستی است)
        [Parameter] public int AutoCloseMilliseconds { get; set; } = 0;

        // پارامترهای ثبت خودکار سابقه در دیتابیس برای مشاهده سوپر ادمین
        [Parameter] public bool SaveToDatabase { get; set; } = true;
        [Parameter] public int? AdminId { get; set; }
        [Parameter] public string AdminUsername { get; set; } = string.Empty;
        [Parameter] public Employee? TargetEmployee { get; set; }

        private DateTime _shownAt;
        private string _lastSavedSignature = string.Empty;
        private Timer? _liveTimer;

        // تولید متن نهایی و ترجمه‌شده برای نمایش در کامپوننت Toast
        protected string DisplayMessage
        {
            get
            {
                if (!string.IsNullOrEmpty(MessageKey))
                {
                    var template = CalendarManager.Translate(MessageKey);
                    return (MessageArgs != null && MessageArgs.Length > 0)
                        ? string.Format(template, MessageArgs)
                        : template;
                }
                return Message;
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            var serializedArgs = (MessageArgs != null && MessageArgs.Length > 0)
                ? JsonSerializer.Serialize(MessageArgs)
                : string.Empty;

            var currentSignature = $"{Category}|{SubCategory}|{MessageKey}|{Message}|{serializedArgs}";

            if (Show && !string.IsNullOrEmpty(DisplayMessage) && currentSignature != _lastSavedSignature)
            {
                _lastSavedSignature = currentSignature;
                var now = DateTime.Now;
                _shownAt = now;

                // ذخیره خودکار اعلان و اطلاعات پرسنل در دیتابیس سرور
                if (SaveToDatabase && DbContext != null)
                {
                    try
                    {
                        var log = new AdminNotificationLog
                        {
                            // ثبت نام بخش و زیردسته صادرکننده اعلان به صورت انگلیسی
                            Category = Category,
                            SubCategory = SubCategory,

                            // ذخیره کلید ترجمه و پارامترها برای نمایش چندزبانه در پنل سوپر ادمین
                            MessageKey = MessageKey,
                            MessageArgs = string.IsNullOrEmpty(serializedArgs) ? null : serializedArgs,

                            // ذخیره متن خام بدون تگ HTML به عنوان پشتیبان
                            Message = Regex.Replace(DisplayMessage, "<.*?>", string.Empty),

                            NotificationType = IsError ? "Error" : "Success",

                            // ثبت زمان کامل به همراه فیلدهای مجزای تاریخ و ساعت
                            CreatedAt = now,
                            CreatedDate = now.ToString("yyyy-MM-dd"),
                            CreatedTime = now.ToString("HH:mm:ss"),
                            EventDate = EventDate,
                            EventTime = EventTime,

                            AdminId = AdminId,
                            AdminUsername = string.IsNullOrWhiteSpace(AdminUsername) ? "Kiosk / System" : AdminUsername,
                            EmployeeId = TargetEmployee?.Id,
                            EmployeeFullName = TargetEmployee?.FullName ?? "-",
                            EmployeePersonnelCode = TargetEmployee?.PersonnelCode ?? "-",
                            EmployeePhone = TargetEmployee?.Phone ?? "-"
                        };
                        DbContext.AdminNotificationLogs.Add(log);
                        await DbContext.SaveChangesAsync();
                    }
                    catch { }
                }

                // راه‌اندازی تایمر زنده برای محاسبه «چند دقیقه پیش» و بسته شدن خودکار (در صورت تنظیم)
                _liveTimer?.Dispose();
                _liveTimer = new Timer(async _ =>
                {
                    var elapsed = DateTime.Now - _shownAt;
                    if (AutoCloseMilliseconds > 0 && elapsed.TotalMilliseconds >= AutoCloseMilliseconds)
                    {
                        await InvokeAsync(CloseToastAsync);
                    }
                    else
                    {
                        await InvokeAsync(StateHasChanged);
                    }
                }, null, 10000, 10000);
            }
            else if (!Show)
            {
                _lastSavedSignature = string.Empty;
                _liveTimer?.Dispose();
            }
        }

        // محاسبه زمان سپری شده از لحظه نمایش اعلان
        protected string GetRelativeTimeText()
        {
            var diff = DateTime.Now - _shownAt;
            if (diff.TotalMinutes < 1)
            {
                return CalendarManager.Translate("PCal_MainHome_Now");
            }
            if (diff.TotalHours < 1)
            {
                return string.Format(CalendarManager.Translate("PCal_Toast_MinutesAgo"), (int)diff.TotalMinutes);
            }
            return string.Format(CalendarManager.Translate("PCal_Toast_HoursAgo"), (int)diff.TotalHours);
        }

        // بستن دستی یا خودکار اعلان و اطلاع‌رسانی به صفحه والد
        protected async Task CloseToastAsync()
        {
            Show = false;
            _lastSavedSignature = string.Empty;
            _liveTimer?.Dispose();
            await ShowChanged.InvokeAsync(false);
        }

        public void Dispose()
        {
            _liveTimer?.Dispose();
        }
    }
}
