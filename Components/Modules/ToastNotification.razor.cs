using Microsoft.AspNetCore.Components;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using sqlite_web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sqlite_web.Components.Modules
{
    public partial class ToastNotification : ComponentBase, IDisposable
    {
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;

        [Parameter] public List<AppBaseComponent.ToastItemModel> Items { get; set; } = new();
        [Parameter] public bool Show { get; set; }
        [Parameter] public EventCallback<bool> ShowChanged { get; set; }
        [Parameter] public string MessageKey { get; set; } = string.Empty;
        [Parameter] public object[]? MessageArgs { get; set; }
        [Parameter] public string Message { get; set; } = string.Empty;
        [Parameter] public bool IsError { get; set; } = false;
        [Parameter] public string CurrentLanguage { get; set; } = "fa";
        [Parameter] public PCalendar CalendarManager { get; set; } = new();
        [Parameter] public string Category { get; set; } = "General";
        [Parameter] public string SubCategory { get; set; } = "General";
        [Parameter] public string? EventDate { get; set; }
        [Parameter] public string? EventTime { get; set; }
        [Parameter] public int AutoCloseMilliseconds { get; set; } = 0;
        [Parameter] public bool SaveToDatabase { get; set; } = true;
        [Parameter] public int? AdminId { get; set; }
        [Parameter] public string AdminUsername { get; set; } = string.Empty;
        [Parameter] public Employee? TargetEmployee { get; set; }

        private Timer? _liveTimer;

        // بازسازی متن ترجمه‌شده برای هر آیتم اعلان در صف
        protected string GetItemHtmlMessage(AppBaseComponent.ToastItemModel item)
        {
            if (!string.IsNullOrEmpty(item.MessageKey))
            {
                var template = CalendarManager.Translate(item.MessageKey);
                return (item.MessageArgs != null && item.MessageArgs.Length > 0)
                    ? string.Format(template, item.MessageArgs)
                    : template;
            }
            return item.Message;
        }

        protected override async Task OnParametersSetAsync()
        {
            if (Items != null && Items.Any())
            {
                EnsureLiveTimerStarted();

                if (SaveToDatabase && DbContext != null)
                {
                    var unsavedItems = Items.Where(i => !i.IsSavedToDb).ToList();
                    foreach (var item in unsavedItems)
                    {
                        item.IsSavedToDb = true;
                        try
                        {
                            string htmlMsg = GetItemHtmlMessage(item);
                            var serializedArgs = (item.MessageArgs != null && item.MessageArgs.Length > 0)
                                ? JsonSerializer.Serialize(item.MessageArgs)
                                : null;

                            DbContext.AdminNotificationLogs.Add(new AdminNotificationLog
                            {
                                Category = item.Category,
                                SubCategory = item.SubCategory,
                                MessageKey = item.MessageKey,
                                MessageArgs = serializedArgs,
                                Message = Regex.Replace(htmlMsg, "<.*?>", string.Empty),
                                NotificationType = item.IsError ? "Error" : "Success",
                                CreatedAt = item.ShownAt,
                                CreatedDate = item.ShownAt.ToString("yyyy-MM-dd"),
                                CreatedTime = item.ShownAt.ToString("HH:mm:ss"),
                                EventDate = item.EventDate,
                                EventTime = item.EventTime,
                                AdminId = AdminId,
                                AdminUsername = string.IsNullOrWhiteSpace(AdminUsername) ? "Kiosk / System" : AdminUsername,
                                EmployeeId = item.TargetEmployee?.Id,
                                EmployeeFullName = item.TargetEmployee?.FullName ?? "-",
                                EmployeePersonnelCode = item.TargetEmployee?.PersonnelCode ?? "-",
                                EmployeePhone = item.TargetEmployee?.Phone ?? "-"
                            });
                        }
                        catch (Exception ex)
                        {
                            await ErrorLogger.LogBackendErrorAsync("ToastNotification.SaveLog", ex, AdminId, AdminUsername, item.TargetEmployee, CurrentLanguage);
                        }
                    }

                    if (unsavedItems.Any())
                    {
                        await DbContext.SaveChangesAsync();
                    }
                }
            }
        }

        private void EnsureLiveTimerStarted()
        {
            if (_liveTimer != null) return;
            _liveTimer = new Timer(async _ =>
            {
                if (Items != null && Items.Any())
                    await InvokeAsync(StateHasChanged);
                else
                {
                    _liveTimer?.Dispose();
                    _liveTimer = null;
                }
            }, null, 10000, 10000);
        }

        // محاسبه ترکیبی زمان سپری‌شده از لحظه نمایش اعلان (روز، ساعت و دقیقه تا بی‌نهایت روز)
        protected string GetRelativeTimeText(DateTime shownAt)
        {
            var diff = DateTime.Now - shownAt;
            if (diff.TotalMinutes < 1)
                return CalendarManager.Translate("PCal_MainHome_Now");

            var parts = new List<string>();

            if (diff.Days > 0)
                parts.Add($"{diff.Days} {CalendarManager.Translate("PCal_Time_DayUnit")}");

            if (diff.Hours > 0)
                parts.Add($"{diff.Hours} {CalendarManager.Translate("PCal_Time_HourUnit")}");

            if (diff.Minutes > 0)
                parts.Add($"{diff.Minutes} {CalendarManager.Translate("PCal_Time_MinuteUnit")}");

            string combined = string.Join(" ", parts);

            // قرارگیری پیشوند «منذ» در زبان عربی و پسوند «پیش / ago / önce» در سایر زبان‌ها
            if (CurrentLanguage == "ar")
                return $"{CalendarManager.Translate("PCal_Time_AgoSuffix")} {combined}";

            return $"{combined} {CalendarManager.Translate("PCal_Time_AgoSuffix")}";
        }


        // بستن دستی یک اعلان و بالا رفتن خودکار اعلان‌های زیرین
        protected async Task RemoveToastItemAsync(Guid itemId)
        {
            var item = Items?.FirstOrDefault(t => t.Id == itemId);
            if (item != null)
                Items!.Remove(item);

            if (Items == null || !Items.Any())
            {
                Show = false;
                _liveTimer?.Dispose();
                _liveTimer = null;
                await ShowChanged.InvokeAsync(false);
            }
        }

        public void Dispose() => _liveTimer?.Dispose();
    }
}
