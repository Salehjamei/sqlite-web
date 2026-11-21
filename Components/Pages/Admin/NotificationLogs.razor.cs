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
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public AdminStateService AdminState { get; set; } = default!;

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

                // اعمال فیلتر تاریخ روی لیست اعلان‌ها بر اساس فرمت استاندارد سال-ماه-روز
                if (FilterDate.HasValue)
                {
                    string targetDateStr = FilterDate.Value.ToString("yyyy-MM-dd");
                    query = query.Where(l => l.CreatedDate == targetDateStr);
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

        // واکشی لاگ‌ها از سرور و علامت‌گذاری موارد خوانده‌نشده به عنوان خوانده‌شده
        protected async Task LoadLogsAsync()
        {
            if (DbContext == null) return;
            try
            {
                allLogs = await DbContext.AdminNotificationLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                // علامت‌گذاری خودکار تمام اعلان‌های جدید به عنوان خوانده‌شده توسط سوپر ادمین
                var unreadLogs = allLogs.Where(l => !l.IsReadBySuperAdmin).ToList();
                if (unreadLogs.Any())
                {
                    foreach (var item in unreadLogs)
                    {
                        item.IsReadBySuperAdmin = true;
                    }
                    await DbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading notification logs: {ex.Message}");
            }
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
                            return string.Format(template, args);
                        }
                    }
                    catch { }
                }
                return template;
            }
            return log.Message;
        }

        // تغییر دسته‌بندی اصلی و بازنشانی زیردسته
        protected void OnCategoryChanged(ChangeEventArgs e)
        {
            SelectedCategory = e.Value?.ToString() ?? "ALL";
            SelectedSubCategory = "ALL";
        }

        // پاکسازی تمام فیلترها
        protected void ResetFilters()
        {
            SearchQuery = string.Empty;
            SelectedCategory = "ALL";
            SelectedSubCategory = "ALL";
            SelectedType = "ALL";
            FilterDate = null;
            FilterTimeFrom = string.Empty;
            FilterTimeTo = string.Empty;
        }

        // حذف تکی یک رکورد لاگ
        protected async Task DeleteSingleLogAsync(int id)
        {
            if (DbContext == null) return;
            var item = await DbContext.AdminNotificationLogs.FindAsync(id);
            if (item != null)
            {
                DbContext.AdminNotificationLogs.Remove(item);
                await DbContext.SaveChangesAsync();
                allLogs.Remove(item);
            }
        }

        // پاکسازی کامل تاریخچه اعلانات
        protected async Task ExecuteClearAllLogsAsync()
        {
            showClearConfirmModal = false;
            if (DbContext == null) return;

            DbContext.AdminNotificationLogs.RemoveRange(DbContext.AdminNotificationLogs);
            await DbContext.SaveChangesAsync();
            allLogs.Clear();
            await InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            AdminState.OnChange -= HandleStateChanged;
        }
    }
}
