using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sqlite_web.Components.Models;

namespace sqlite_web.Services
{
    // سرویس مرکزی و ایزوله مدیریت خواندن، علامت‌گذاری خوانده‌شده، ثبت و حذف تاریخچه اعلانات سرور
    public class NotificationLogService
    {
        // تزریق کارخانه ساخت اسکوپ دیتابیس جهت اجرای ایزوله کوئری‌ها بدون تداخل DbContext
        private readonly IServiceScopeFactory _scopeFactory;

        // تزریق سرویس ثبت خطاهای سیستمی در سرور جهت ثبت خطاهای احتمالی عملیات اعلانات
        private readonly SystemErrorLoggerService _errorLogger;

        public NotificationLogService(IServiceScopeFactory scopeFactory, SystemErrorLoggerService errorLogger)
        {
            _scopeFactory = scopeFactory;
            _errorLogger = errorLogger;
        }

        // واکشی لیست کامل اعلانات از دیتابیس به ترتیب جدیدترین زمان ثبت و علامت‌گذاری خودکار موارد خوانده‌نشده
        public async Task<List<AdminNotificationLog>> GetAndMarkAllAsReadAsync(int? actorAdminId = null, string? actorAdminUsername = null, string currentLanguage = "fa")
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // دریافت تمام لاگ‌های ثبت‌شده از جدیدترین به قدیمی‌ترین
                var logs = await db.AdminNotificationLogs
                    .OrderByDescending(l => l.CreatedAt)
                    .ToListAsync();

                // شناسایی اعلان‌های خوانده‌نشده و تغییر وضعیت آن‌ها به خوانده‌شده توسط سوپر ادمین
                var unreadLogs = logs.Where(l => !l.IsReadBySuperAdmin).ToList();
                if (unreadLogs.Any())
                {
                    foreach (var item in unreadLogs)
                    {
                        item.IsReadBySuperAdmin = true;
                    }
                    await db.SaveChangesAsync();
                }

                return logs;
            }
            catch (Exception ex)
            {
                // ثبت جزئیات خطای فنی در جدول اعلانات سرور
                await _errorLogger.LogBackendErrorAsync("NotificationLogService.GetAndMarkAllAsReadAsync", ex, actorAdminId, actorAdminUsername ?? "System", null, currentLanguage);
                return new List<AdminNotificationLog>();
            }
        }

        // افزودن یک رکورد اعلان جدید به جدول تاریخچه اعلانات سرور به صورت ایزوله
        public async Task AddLogAsync(AdminNotificationLog logEntry)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.AdminNotificationLogs.Add(logEntry);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _errorLogger.LogBackendErrorAsync("NotificationLogService.AddLogAsync", ex, logEntry.AdminId, logEntry.AdminUsername);
            }
        }

        // حذف تکی یک رکورد اعلان بر اساس شناسه از دیتابیس به صورت ایزوله
        public async Task<bool> DeleteSingleLogAsync(int logId, int? actorAdminId = null, string? actorAdminUsername = null, string currentLanguage = "fa")
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var item = await db.AdminNotificationLogs.FirstOrDefaultAsync(l => l.Id == logId);
                if (item == null) return false;

                db.AdminNotificationLogs.Remove(item);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _errorLogger.LogBackendErrorAsync("NotificationLogService.DeleteSingleLogAsync", ex, actorAdminId, actorAdminUsername ?? "System", null, currentLanguage);
                return false;
            }
        }

        // پاکسازی کامل تمامی سوابق جدول تاریخچه اعلانات از دیتابیس به صورت ایزوله
        public async Task<bool> ClearAllLogsAsync(int? actorAdminId = null, string? actorAdminUsername = null, string currentLanguage = "fa")
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                db.AdminNotificationLogs.RemoveRange(db.AdminNotificationLogs);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await _errorLogger.LogBackendErrorAsync("NotificationLogService.ClearAllLogsAsync", ex, actorAdminId, actorAdminUsername ?? "System", null, currentLanguage);
                return false;
            }
        }
    }
}
