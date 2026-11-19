using Microsoft.AspNetCore.Components;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using sqlite_web.Services;
using System;
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
        [Parameter] public string Message { get; set; } = string.Empty;
        [Parameter] public bool IsError { get; set; } = false;
        [Parameter] public string CurrentLanguage { get; set; } = "fa";
        [Parameter] public PCalendar CalendarManager { get; set; } = new();

        // دسته‌بندی بخش صادرکننده اعلان (Kiosk, AdminPanel, ManageAdmins, ...)
        [Parameter] public string Category { get; set; } = "General";

        // زمان بسته شدن خودکار به میلی‌ثانیه (مقدار 0 به معنای باز ماندن تا زمان بستن دستی است)
        [Parameter] public int AutoCloseMilliseconds { get; set; } = 0;

        // پارامترهای ثبت خودکار سابقه در دیتابیس برای مشاهده سوپر ادمین
        [Parameter] public bool SaveToDatabase { get; set; } = true;
        [Parameter] public int? AdminId { get; set; }
        [Parameter] public string AdminUsername { get; set; } = string.Empty;
        [Parameter] public Employee? TargetEmployee { get; set; }

        private DateTime _shownAt;
        private string _lastSavedMessage = string.Empty;
        private Timer? _liveTimer;

        protected override async Task OnParametersSetAsync()
        {
            if (Show && !string.IsNullOrEmpty(Message) && Message != _lastSavedMessage)
            {
                _lastSavedMessage = Message;
                _shownAt = DateTime.Now;

                // ذخیره خودکار اعلان و اطلاعات پرسنل در دیتابیس سرور
                if (SaveToDatabase && DbContext != null)
                {
                    try
                    {
                        var log = new AdminNotificationLog
                        {
                            // ثبت نام بخش صادرکننده اعلان
                            Category = Category,
                            Message = Regex.Replace(Message, "<.*?>", string.Empty),
                            NotificationType = IsError ? "Error" : "Success",
                            CreatedAt = DateTime.Now,
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
                _lastSavedMessage = string.Empty;
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
            _lastSavedMessage = string.Empty;
            _liveTimer?.Dispose();
            await ShowChanged.InvokeAsync(false);
        }

        public void Dispose()
        {
            _liveTimer?.Dispose();
        }
    }
}
