using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;

namespace sqlite_web.Services
{
    // سرویس مرکزی ثبت خطاهای سیستمی و سروری در تاریخچه اعلانات با پشتیبانی کامل از موتور ترجمه چهارزبانه
    public class SystemErrorLoggerService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SystemErrorLoggerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // ثبت ایزوله خطای فنی بک‌اند همراه با کلید ترجمه چندزبانه و آرگومان‌های پویا در دیتابیس
        public async Task LogBackendErrorAsync(
            string sourceLocation,
            Exception ex,
            int? actorAdminId = null,
            string actorAdminUsername = "System",
            Employee? targetEmp = null,
            string? currentLanguage = "fa")
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                string rawDetail = ex.InnerException?.Message ?? ex.Message;
                string coloredSource = $"<span style='color: #0d6efd; font-weight: 900;'>[{sourceLocation}]</span>";
                string coloredError = $"<span style='color: #dc3545; font-weight: 700;'>{rawDetail}</span>";

                // تولید متن ترجمه‌شده بر اساس زبان فعال کاربر (fa, en, tr, ar) علاوه بر ذخیره کلید چندزبانه
                string activeLang = string.IsNullOrWhiteSpace(currentLanguage) ? "fa" : currentLanguage;
                var pCal = new PCalendar { CurrentLanguage = activeLang };
                var args = new object[] { coloredSource, coloredError };
                var template = pCal.Translate("PCal_System_BackendErrorLog");
                var now = DateTime.Now;

                db.AdminNotificationLogs.Add(new AdminNotificationLog
                {
                    Category = "System",
                    SubCategory = "BackendError",
                    MessageKey = "PCal_System_BackendErrorLog",
                    MessageArgs = JsonSerializer.Serialize(args),
                    Message = Regex.Replace(string.Format(template, args), "<.*?>", string.Empty),
                    NotificationType = "Error",
                    CreatedAt = now,
                    CreatedDate = now.ToString("yyyy-MM-dd"),
                    CreatedTime = now.ToString("HH:mm:ss"),
                    AdminId = actorAdminId,
                    AdminUsername = string.IsNullOrWhiteSpace(actorAdminUsername) ? "System" : actorAdminUsername,
                    EmployeeId = targetEmp?.Id,
                    EmployeeFullName = targetEmp?.FullName ?? "-",
                    EmployeePersonnelCode = targetEmp?.PersonnelCode ?? "-",
                    EmployeePhone = targetEmp?.Phone ?? "-"
                });

                await db.SaveChangesAsync();
            }
            catch (Exception internalEx)
            {
                Console.WriteLine($"System Error Logger Failure: {internalEx.Message}");
            }
        }
    }
}
