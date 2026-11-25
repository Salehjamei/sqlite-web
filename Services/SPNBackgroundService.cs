using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using System;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sqlite_web.Services
{
    // هسته مرکزی پردازش‌های پس‌زمینه (Background Worker) جهت اجرای وظایف زمان‌بندی‌شده بدون وابستگی به نشست کاربران
    public class SPNBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SystemErrorLoggerService _errorLogger;

        // قفل هم‌زمانی جهت جلوگیری از اجرای موازی و تداخل تراکنش‌های دیتابیس هنگام بیدار شدن هم‌زمان فرآیندها
        private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

        public SPNBackgroundService(IServiceScopeFactory scopeFactory, SystemErrorLoggerService errorLogger)
        {
            _scopeFactory = scopeFactory;
            _errorLogger = errorLogger;
        }

        // چرخه حیات اصلی سرویس پس‌زمینه شامل اجرای جبرانی در لحظه استارت و زمان‌بندی روزانه راس ساعت مقرر
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // اجرای جبرانی در لحظه راه‌اندازی یا بیدار شدن سرور از حالت خواب (Idle) برای پوشش رکوردهای جا مانده
            await ExecuteAutoLogoutWithRetryAsync(stoppingToken);

            // حلقه انتظار هوشمند روزانه که بدون ایجاد سربار کوئری مداوم، دقیقاً تا زمان اجرای بعدی در خواب می‌ماند
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan delay = await CalculateDelayUntilNextRunAsync(stoppingToken);
                await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                await ExecuteAutoLogoutWithRetryAsync(stoppingToken);
            }
        }

        // لایه تاب‌آوری در برابر خطا جهت مدیریت شکست عملیات، ثبت هشدار برای ادمین و یک‌بار تلاش مجدد پس از ۵ دقیقه
        private async Task ExecuteAutoLogoutWithRetryAsync(CancellationToken stoppingToken)
        {
            try
            {
                await ProcessAutoLogoutAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // ثبت جزئیات خطای فنی در لاگر مرکزی بک‌اند و اطلاع‌رسانی زمان تلاش مجدد به سوپر ادمین
                await _errorLogger.LogBackendErrorAsync("SPNBackgroundService.AutoLogout", ex);
                await LogSystemErrorAsync("PCal_System_AutoLogoutRetryScheduled", ex.InnerException?.Message ?? ex.Message, stoppingToken);

                // انتظار ۵ دقیقه‌ای برای رفع قفل‌های موقت دیتابیس و انجام تنها یک تلاش جبرانی
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    await ProcessAutoLogoutAsync(stoppingToken);
                }
                catch (Exception retryEx)
                {
                    // ثبت شکست قطعی پس از اتمام تلاش مجدد و خروج از چرخه خطا جهت جلوگیری از حلقه بی‌نهایت
                    await _errorLogger.LogBackendErrorAsync("SPNBackgroundService.AutoLogoutRetry", retryEx);
                    await LogSystemErrorAsync("PCal_System_AutoLogoutRetryFailed", retryEx.InnerException?.Message ?? retryEx.Message, stoppingToken);
                }
            }
        }

        // منطق تجاری شناسایی ترددهای باز گذشته و بستن خودکار آن‌ها بر اساس قوانین ساعت کاری سرور
        public async Task ProcessAutoLogoutAsync(CancellationToken stoppingToken = default)
        {
            await _syncLock.WaitAsync(stoppingToken);
            try
            {
                // ایجاد یک Scope مستقل از DbContext برای ایزوله‌سازی تراکنش پس‌زمینه از نشست‌های رابط کاربری
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var settings = await db.WorkSettings.AsNoTracking().FirstOrDefaultAsync(stoppingToken);
                bool applyAutoLogout = settings?.ApplyAutoLogout ?? true;
                if (!applyAutoLogout) return;

                string autoTimeStr = settings?.AutoLogoutTime ?? "23:59";
                if (!TimeSpan.TryParse(autoTimeStr, out var parsedAutoTime))
                {
                    parsedAutoTime = new TimeSpan(23, 59, 0);
                }

                var now = DateTime.Now;
                var today = DateTime.Today;

                // واکشی ترددهای بدون خروج از روزهای گذشته و همچنین ترددهای باز امروز پس از عبور از ساعت مقرر
                var openRecords = await db.Attendances
                    .Include(a => a.Employee)
                    .Where(a => a.ClockOutTime == null && a.ClockInTime.HasValue &&
                                (a.ClockInTime.Value.Date < today ||
                                 (a.ClockInTime.Value.Date == today && now.TimeOfDay >= parsedAutoTime)))
                    .ToListAsync(stoppingToken);

                if (!openRecords.Any()) return;

                var pCal = new PCalendar { CurrentLanguage = "fa" };

                foreach (var record in openRecords)
                {
                    var autoClockOut = record.ClockInTime!.Value.Date.Add(parsedAutoTime);

                    // جلوگیری از ثبت ساعت خروج عقب‌تر از زمان ورود در صورت ثبت تردد در دقایق پایانی شب
                    if (autoClockOut < record.ClockInTime.Value)
                    {
                        autoClockOut = record.ClockInTime.Value.Date.Add(new TimeSpan(23, 59, 0));
                    }

                    record.ClockOutTime = autoClockOut;
                    record.EntrySource = "AutoClose";
                    db.Attendances.Update(record);

                    // تولید رکورد ممیزی (Audit Log) چندزبانه برای اطلاع‌رسانی بستن خودکار تردد به سوپر ادمین
                    var empName = record.Employee?.FullName ?? "-";
                    string coloredEmpName = $"<span style='color: #dc3545; font-weight: 900;'>«{empName}»</span>";
                    var args = new object[] { coloredEmpName, autoClockOut.ToString("HH:mm") };
                    var template = pCal.Translate("PCal_System_AutoLogoutSuccess");

                    db.AdminNotificationLogs.Add(new AdminNotificationLog
                    {
                        Category = "System",
                        SubCategory = "AutoLogout",
                        MessageKey = "PCal_System_AutoLogoutSuccess",
                        MessageArgs = JsonSerializer.Serialize(args),
                        Message = Regex.Replace(string.Format(template, args), "<.*?>", string.Empty),
                        NotificationType = "Info",
                        CreatedAt = now,
                        CreatedDate = now.ToString("yyyy-MM-dd"),
                        CreatedTime = now.ToString("HH:mm:ss"),
                        EventDate = autoClockOut.ToString("yyyy-MM-dd"),
                        EventTime = autoClockOut.ToString("HH:mm:ss"),
                        AdminId = null,
                        AdminUsername = "System",
                        EmployeeId = record.EmployeeId,
                        EmployeeFullName = empName,
                        EmployeePersonnelCode = record.Employee?.PersonnelCode ?? "-",
                        EmployeePhone = record.Employee?.Phone ?? "-"
                    });
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            finally
            {
                // آزادسازی قطعی قفل هم‌زمانی در هر شرایطی جهت جلوگیری از بن‌بست (Deadlock) در اجراهای بعدی
                _syncLock.Release();
            }
        }

        // ثبت مستقیم پیام‌های هشدار و خطای عملیاتی سرویس پس‌زمینه در جدول اعلانات مدیریتی
        private async Task LogSystemErrorAsync(string messageKey, string errorDetail, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var pCal = new PCalendar { CurrentLanguage = "fa" };

                var args = new object[] { errorDetail };
                var template = pCal.Translate(messageKey);
                var now = DateTime.Now;

                db.AdminNotificationLogs.Add(new AdminNotificationLog
                {
                    Category = "System",
                    SubCategory = "AutoLogout",
                    MessageKey = messageKey,
                    MessageArgs = JsonSerializer.Serialize(args),
                    Message = Regex.Replace(string.Format(template, args), "<.*?>", string.Empty),
                    NotificationType = "Error",
                    CreatedAt = now,
                    CreatedDate = now.ToString("yyyy-MM-dd"),
                    CreatedTime = now.ToString("HH:mm:ss"),
                    AdminId = null,
                    AdminUsername = "System",
                    EmployeeFullName = "-",
                    EmployeePersonnelCode = "-",
                    EmployeePhone = "-"
                });

                await db.SaveChangesAsync(stoppingToken);
            }
            catch { }
        }     
        // محاسبه دقیق بازه زمانی انتظار تا سررسید بعدی بر اساس ساعت خروج خودکار ذخیره‌شده در تنظیمات سرور
        private async Task<TimeSpan> CalculateDelayUntilNextRunAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var settings = await db.WorkSettings.AsNoTracking().FirstOrDefaultAsync(stoppingToken);

                string autoTimeStr = settings?.AutoLogoutTime ?? "23:59";
                if (!TimeSpan.TryParse(autoTimeStr, out var targetTime))
                {
                    targetTime = new TimeSpan(23, 59, 0);
                }

                var now = DateTime.Now;
                var nextRun = now.Date.Add(targetTime);

                // انتقال زمان اجرای بعدی به روز فردا در صورت عبور ساعت فعلی از سررسید امروز
                if (nextRun <= now)
                {
                    nextRun = nextRun.AddDays(1);
                }

                // افزودن ۵ ثانیه حاشیه اطمینان جهت جلوگیری از بیدار شدن زودهنگام در میلی‌ثانیه‌های مرزی
                return (nextRun - now).Add(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                await _errorLogger.LogBackendErrorAsync("SPNBackgroundService.CalculateDelay", ex);
                return TimeSpan.FromHours(1);
            }
        }
    }
}