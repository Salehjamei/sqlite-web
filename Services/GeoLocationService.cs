using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;

namespace sqlite_web.Services
{
    public class DeviceLocationResult
    {
        public bool Success { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string FormattedCoordinates => Success
            ? $"{Latitude.ToString("F6", CultureInfo.InvariantCulture)}, {Longitude.ToString("F6", CultureInfo.InvariantCulture)}"
            : string.Empty;
    }

    public class LocationValidationResult
    {
        public bool IsAllowed { get; set; }
        public string MatchedLocationName { get; set; } = string.Empty;
        public string Coordinates { get; set; } = string.Empty;
        public double ClosestDistanceMeters { get; set; }
        public string ErrorKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// سرویس مرکزی دریافت GPS دستگاه، محاسبه فاصله جغرافیایی و اعتبارسنجی محدوده مجاز بر اساس دسته‌بندی پرسنل
    /// </summary>
    public class GeoLocationService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;

        public GeoLocationService(AppDbContext dbContext, IJSRuntime jsRuntime)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
        }

        // دریافت مختصات جغرافیایی لحظه‌ای از مرورگر دستگاه
        public async Task<DeviceLocationResult> GetCurrentDeviceLocationAsync()
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<DeviceLocationResult>("getDeviceLocation");
                return result ?? new DeviceLocationResult { Success = false, ErrorCode = "UNKNOWN" };
            }
            catch
            {
                return new DeviceLocationResult { Success = false, ErrorCode = "JS_ERROR" };
            }
        }

        // محاسبه فاصله دقیق بین دو نقطه جغرافیایی بر حسب متر (فرمول استاندارد Haversine)
        public double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // شعاع کره زمین به متر
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        // بررسی مجاز بودن تردد کارمند در موقعیت فعلی با توجه به مکان‌های فعال و دسته‌بندی سه‌سطحی کارمند
        public async Task<LocationValidationResult> ValidateEmployeeLocationAsync(Employee? employee)
        {
            var activeLocations = await _dbContext.AllowedLocations
                .AsNoTracking()
                .Where(l => l.IsActive)
                .ToListAsync();

            // اگر هیچ مکان مجازی در سیستم تعریف نشده باشد، دریافت مختصات جهت لاگ انجام می‌شود اما مانع تردد نمی‌شود
            var deviceLoc = await GetCurrentDeviceLocationAsync();

            if (!activeLocations.Any())
            {
                return new LocationValidationResult
                {
                    IsAllowed = true,
                    MatchedLocationName = "FreeMode",
                    Coordinates = deviceLoc.FormattedCoordinates
                };
            }

            // در صورت فعال بودن مکان‌های مجاز، دریافت موقعیت دستگاه اجباری است
            if (!deviceLoc.Success)
            {
                return new LocationValidationResult
                {
                    IsAllowed = false,
                    ErrorKey = deviceLoc.ErrorCode == "PERMISSION_DENIED"
                        ? "PCal_Loc_PermissionDenied"
                        : "PCal_Loc_Unavailable"
                };
            }

            // فیلتر کردن مکان‌های مجاز متناسب با دسته‌بندی سه‌سطحی کارمند
            var eligibleLocations = activeLocations.Where(loc =>
                (loc.AllowedMainCategoryId == null || loc.AllowedMainCategoryId == employee?.MainCategoryId) &&
                (loc.AllowedSubCategory1Id == null || loc.AllowedSubCategory1Id == employee?.SubCategory1Id) &&
                (loc.AllowedSubCategory2Id == null || loc.AllowedSubCategory2Id == employee?.SubCategory2Id)
            ).ToList();

            if (!eligibleLocations.Any())
            {
                return new LocationValidationResult
                {
                    IsAllowed = false,
                    Coordinates = deviceLoc.FormattedCoordinates,
                    ErrorKey = "PCal_Loc_NoCategoryAccess"
                };
            }

            double minDistance = double.MaxValue;
            foreach (var loc in eligibleLocations)
            {
                double distance = CalculateDistanceMeters(deviceLoc.Latitude, deviceLoc.Longitude, loc.Latitude, loc.Longitude);
                if (distance < minDistance) minDistance = distance;

                if (distance <= loc.RadiusMeters)
                {
                    return new LocationValidationResult
                    {
                        IsAllowed = true,
                        MatchedLocationName = loc.Name,
                        Coordinates = deviceLoc.FormattedCoordinates,
                        ClosestDistanceMeters = Math.Round(distance)
                    };
                }
            }

            return new LocationValidationResult
            {
                IsAllowed = false,
                Coordinates = deviceLoc.FormattedCoordinates,
                ClosestDistanceMeters = Math.Round(minDistance),
                ErrorKey = "PCal_Loc_OutOfRange"
            };
        }
        // ثبت بی‌صدای رویداد موقعیت مکانی در جدول تاریخچه اعلانات سرور بدون نمایش بنر در رابط کاربری
        public async Task LogLocationAuditSilentlyAsync(
            string subCategory,
            string messageKey,
            object[] args,
            int? actorAdminId = null,
            string actorAdminUsername = "Kiosk",
            Employee? targetEmp = null,
            string? eventDate = null,
            string? eventTime = null)
        {
            try
            {
                var pCal = new sqlite_web.Components.Models.Calendar.PCalendar { CurrentLanguage = "fa" };
                var template = pCal.Translate(messageKey);
                var formattedMsg = (args != null && args.Length > 0) ? string.Format(template, args) : template;
                var cleanMsg = System.Text.RegularExpressions.Regex.Replace(formattedMsg, "<.*?>", string.Empty);
                var now = DateTime.Now;

                _dbContext.AdminNotificationLogs.Add(new AdminNotificationLog
                {
                    Category = "Location",
                    SubCategory = subCategory,
                    MessageKey = messageKey,
                    MessageArgs = (args != null && args.Length > 0) ? System.Text.Json.JsonSerializer.Serialize(args) : null,
                    Message = cleanMsg,
                    NotificationType = "Info",
                    CreatedAt = now,
                    CreatedDate = now.ToString("yyyy-MM-dd"),
                    CreatedTime = now.ToString("HH:mm:ss"),
                    EventDate = eventDate,
                    EventTime = eventTime,
                    AdminId = actorAdminId,
                    AdminUsername = string.IsNullOrWhiteSpace(actorAdminUsername) ? "Kiosk" : actorAdminUsername,
                    EmployeeId = targetEmp?.Id,
                    EmployeeFullName = targetEmp?.FullName ?? "-",
                    EmployeePersonnelCode = targetEmp?.PersonnelCode ?? "-",
                    EmployeePhone = targetEmp?.Phone ?? "-"
                });

                await _dbContext.SaveChangesAsync();
            }
            catch { }
        }
    }
}
