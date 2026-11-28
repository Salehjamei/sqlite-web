using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models;
using sqlite_web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class NotificationLogs : AppBaseComponent, IDisposable
    {
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        // تزریق سرویس ایزوله مدیریت تاریخچه اعلانات سرور
        [Inject] public NotificationLogService NotifLogSvc { get; set; } = default!;
        // لیست کل لاگ‌های واکشی شده از دیتابیس
        protected List<AdminNotificationLog> allLogs = new();

        // متغیرهای فیلتر و جستجو
        protected string SearchQuery { get; set; } = string.Empty;
        protected string SelectedCategory { get; set; } = "ALL";
        protected string SelectedSubCategory { get; set; } = "ALL";
        protected string SelectedType { get; set; } = "ALL";
        // فیلتر تاریخ انتخاب‌شده در کادر تاریخ جهت جستجوی رکوردها
        protected DateTime? FilterDate { get; set; } = null; // فرمت yyyy-MM-dd
        protected string FilterTimeFrom { get; set; } = string.Empty; // فرمت HH:mm
        protected string FilterTimeTo { get; set; } = string.Empty;   // فرمت HH:mm
        // متغیرهای فیلتر تاریخ پویا متصل به تقویم فعال سیستم (شمسی، قمری یا میلادی)
        protected string FilterYear { get; set; } = string.Empty;
        protected string FilterMonth { get; set; } = string.Empty;
        protected string FilterDay { get; set; } = string.Empty;

        // تبدیل تاریخ رویداد ثبت‌شده در سرور به فرمت تقویم فعال کاربر (شمسی، قمری یا میلادی)
        protected string GetFormattedEventDate(string? eventDateStr)
        {
            if (string.IsNullOrWhiteSpace(eventDateStr)) return string.Empty;
            if (DateTime.TryParse(eventDateStr, out var parsedDate) && CalendarManager != null)
            {
                return CalendarManager.GetDynamicFormattedDate(parsedDate);
            }
            return eventDateStr;
        }

        // کنترل مودال تاییدیه پاکسازی تاریخچه
        protected bool showClearConfirmModal { get; set; } = false;

        // لیست زیردسته‌های یکتا موجود در دیتابیس برای پر کردن دراپ‌داون فیلتر
        protected IEnumerable<string> AvailableSubCategories => allLogs
            .Where(l => SelectedCategory == "ALL" || l.Category == SelectedCategory)
            .Select(l => l.SubCategory)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s);

        // اعمال هم‌زمان تمام فیلترهای ترکیبی روی لیست اعلان‌ها و مرتب‌سازی نزولی بر اساس زمان ثبت
        protected IEnumerable<AdminNotificationLog> FilteredLogs
        {
            get
            {
                var query = allLogs.AsEnumerable();

                // فیلتر کردن رکوردها بر اساس بخش اصلی صادرکننده اعلان (Category)
                if (SelectedCategory != "ALL")
                    query = query.Where(l => l.Category == SelectedCategory);

                // فیلتر کردن رکوردها بر اساس موضوع فرعی رویداد (SubCategory)
                if (SelectedSubCategory != "ALL")
                    query = query.Where(l => l.SubCategory == SelectedSubCategory);

                // فیلتر کردن رکوردها بر اساس وضعیت اعلان (موفق، خطا یا سیستمی)
                if (SelectedType != "ALL")
                    query = query.Where(l => l.NotificationType == SelectedType);

                // اعمال فیلتر تاریخ بر اساس سال، ماه و روز انتخاب‌شده در تقویم فعال سیستم (شمسی، قمری یا میلادی)
                if (!string.IsNullOrEmpty(FilterYear) && !string.IsNullOrEmpty(FilterMonth) && !string.IsNullOrEmpty(FilterDay) && CalendarManager != null)
                {
                    try
                    {
                        DateTime targetGregorianDate = CalendarManager.ParseFormFieldsToDateTime(FilterYear, FilterMonth, FilterDay, "00:00");
                        string targetDateStr = targetGregorianDate.ToString("yyyy-MM-dd");
                        query = query.Where(l => l.CreatedDate == targetDateStr);
                    }
                    catch { }
                }

                // فیلتر کردن رکوردها از ساعت شروع مشخص‌شده به بعد
                if (!string.IsNullOrWhiteSpace(FilterTimeFrom))
                    query = query.Where(l => string.Compare(l.CreatedTime, FilterTimeFrom.Trim(), StringComparison.Ordinal) >= 0);

                // فیلتر کردن رکوردها تا سقف ساعت پایان مشخص‌شده
                if (!string.IsNullOrWhiteSpace(FilterTimeTo))
                    query = query.Where(l => string.Compare(l.CreatedTime, FilterTimeTo.Trim() + ":59", StringComparison.Ordinal) <= 0);

                // جستجوی متنی زنده در نام کارمند، کد ملی، نام کاربری ادمین و متن پیام اعلان
                if (!string.IsNullOrWhiteSpace(SearchQuery))
                {
                    var q = SearchQuery.Trim();
                    query = query.Where(l =>
                        (!string.IsNullOrEmpty(l.EmployeeFullName) && l.EmployeeFullName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(l.EmployeePersonnelCode) && l.EmployeePersonnelCode.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(l.AdminUsername) && l.AdminUsername.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(l.Message) && l.Message.Contains(q, StringComparison.OrdinalIgnoreCase))
                    );
                }

                // مرتب‌سازی نهایی لیست از جدیدترین به قدیمی‌ترین اعلان
                return query.OrderByDescending(l => l.CreatedAt);
            }
        }

        // پاکسازی تمام فیلترهای متنی، دسته‌بندی، تاریخ و ساعت
        protected void ResetFilters()
        {
            SearchQuery = string.Empty;
            SelectedCategory = "ALL";
            SelectedSubCategory = "ALL";
            SelectedType = "ALL";
            FilterYear = string.Empty;
            FilterMonth = string.Empty;
            FilterDay = string.Empty;
            FilterTimeFrom = string.Empty;
            FilterTimeTo = string.Empty;
        }



        protected override async Task OnInitializedAsync()
        {
            AdminState.OnChange += HandleStateChanged;
            await AdminState.CheckAccessAndRedirectAsync();

            if (AdminState.IsAuthorized && AdminState.isCurrentUserSuperAdmin)
            {
                await LoadLogsAsync();
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await SyncLanguageAsync();
            }
        }

        private async void HandleStateChanged()
        {
            await SyncLanguageAsync();
        }

        private async Task SyncLanguageAsync()
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
        // بازسازی و ترجمه هوشمند متن هر اعلان به زبان فعلی سوپر ادمین
        protected string GetLocalizedLogMessage(AdminNotificationLog log)
        {
            if (!string.IsNullOrEmpty(log.MessageKey) && CalendarManager != null)
            {
                var template = CalendarManager.Translate(log.MessageKey);
                if (!string.IsNullOrEmpty(log.MessageArgs))
                {
                    try
                    {
                        var args = JsonSerializer.Deserialize<string[]>(log.MessageArgs);
                        if (args != null && args.Length > 0)
                        {
                            // ترجمه پویای نام صفحه مقصد در لاگ‌های جابه‌جایی بین صفحات
                            if (log.MessageKey == "PCal_Navigation_PageVisitLog" && args.Length > 1)
                            {
                                string translatedPageName = CalendarManager.Translate(args[1]);
                                args[1] = $"<span style='color: #0d6efd; font-weight: 900;'>«{translatedPageName}»</span>";
                            }
                            return string.Format(template, args);
                        }
                    }
                    catch { }
                }
                return template;
            }
            return log.Message;
        }
        // واکشی لاگ‌ها از سرویس مرکزی و علامت‌گذاری خودکار موارد خوانده‌نشده
        protected async Task LoadLogsAsync()
        {
            allLogs = await NotifLogSvc.GetAndMarkAllAsReadAsync(
                actorAdminId: AdminState.CurrentOnlineAdmin?.Id,
                actorAdminUsername: AdminState.adminUsername,
                currentLanguage: CurrentLanguage
            );
        }

        // حذف تکی یک رکورد لاگ از طریق سرویس مرکزی اعلانات
        protected async Task DeleteSingleLogAsync(int id)
        {
            bool deleted = await NotifLogSvc.DeleteSingleLogAsync(
                logId: id,
                actorAdminId: AdminState.CurrentOnlineAdmin?.Id,
                actorAdminUsername: AdminState.adminUsername,
                currentLanguage: CurrentLanguage
            );

            if (deleted)
            {
                var item = allLogs.FirstOrDefault(l => l.Id == id);
                if (item != null) allLogs.Remove(item);
            }
        }

        // پاکسازی کامل تاریخچه اعلانات از طریق سرویس مرکزی اعلانات
        protected async Task ExecuteClearAllLogsAsync()
        {
            showClearConfirmModal = false;
            bool cleared = await NotifLogSvc.ClearAllLogsAsync(
                actorAdminId: AdminState.CurrentOnlineAdmin?.Id,
                actorAdminUsername: AdminState.adminUsername,
                currentLanguage: CurrentLanguage
            );

            if (cleared)
            {
                allLogs.Clear();
                await InvokeAsync(StateHasChanged);
            }
        }

        // تغییر دسته‌بندی اصلی و بازنشانی زیردسته
        protected void OnCategoryChanged(ChangeEventArgs e)
        {
            SelectedCategory = e.Value?.ToString() ?? "ALL";
            SelectedSubCategory = "ALL";
        }
        public void Dispose()
        {
            AdminState.OnChange -= HandleStateChanged;
        }
        // پرچم کنترل وضعیت نمایش مودال نقشه موقعیت جغرافیایی در صفحه تاریخچه اعلانات
        protected bool showLogMapModal { get; set; } = false;

        // نگهداری شیء موقت تردد جهت ارسال مختصات استخراج‌شده از متن لاگ به ماژول نقشه
        protected Attendance? logMapAttendance { get; set; }

        // نگهداری نام کارمند یا عنوان رویداد جهت نمایش در هدر مودال نقشه
        protected string logMapTitleName { get; set; } = string.Empty;

        // بررسی اینکه آیا در متن پیام لاگ مختصات جغرافیایی به فرمت (lat, lng) وجود دارد یا خیر
        protected bool HasLogCoordinates(AdminNotificationLog log)
        {
            if (log == null || log.Category != "Location" || string.IsNullOrWhiteSpace(log.Message))
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(log.Message, @"\(\s*-?\d+\.\d+\s*,\s*-?\d+\.\d+\s*\)");
        }

        // استخراج مختصات و نام مکان از متن لاگ اعلان و باز کردن مودال نقشه تعاملی
        protected void OpenLogMapModal(AdminNotificationLog log)
        {
            if (log == null || string.IsNullOrWhiteSpace(log.Message)) return;

            // استخراج رشته مختصات داخل پرانتز از متن پیام لاگ
            var coordMatch = System.Text.RegularExpressions.Regex.Match(log.Message, @"\(\s*(-?\d+\.\d+\s*,\s*-?\d+\.\d+)\s*\)");
            if (!coordMatch.Success) return;

            string extractedCoords = coordMatch.Groups[1].Value;

            // استخراج نام مکان داخل گیومه «...» از متن پیام در صورت وجود
            string? extractedLocName = null;
            var locNameMatch = System.Text.RegularExpressions.Regex.Match(log.Message, @"«([^»]+)»");
            if (locNameMatch.Success)
            {
                extractedLocName = locNameMatch.Groups[1].Value;
            }

            // ساخت یک شیء موقت Attendance برای نمایش در ماژول استاندارد InteractiveMap
            logMapAttendance = new Attendance
            {
                Id = log.Id,
                ClockInTime = log.CreatedAt,
                ClockInLocation = extractedCoords,
                LocationName = extractedLocName,
                EntrySource = log.SubCategory == "AdminLocation" ? "AdminFast" : "Kiosk",
                CreatedByAdminId = log.SubCategory == "AdminLocation" ? log.AdminId : null
            };

            logMapTitleName = !string.IsNullOrWhiteSpace(log.EmployeeFullName) && log.EmployeeFullName != "-"
                ? log.EmployeeFullName
                : log.AdminUsername;

            showLogMapModal = true;
        }

    }
}
