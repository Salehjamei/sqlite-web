using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;

namespace sqlite_web.Services
{
    // مدل نگهداری نتیجه دریافت مختصات جغرافیایی از مرورگر دستگاه
    public class DeviceLocationResult
    {
        public bool Success { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Accuracy { get; set; }
        public string ErrorCode { get; set; } = string.Empty;

        // تولید رشته استاندارد مختصات با فرمت انگلیسی جهت ذخیره در دیتابیس
        public string FormattedCoordinates => Success
            ? $"{Latitude.ToString("F6", CultureInfo.InvariantCulture)}, {Longitude.ToString("F6", CultureInfo.InvariantCulture)}"
            : string.Empty;
    }

    // مدل نگهداری نتیجه اعتبارسنجی موقعیت مکانی پرسنل نسبت به محدوده‌های مجاز
    public class LocationValidationResult
    {
        public bool IsAllowed { get; set; }
        public string MatchedLocationName { get; set; } = string.Empty;
        public string Coordinates { get; set; } = string.Empty;
        public double ClosestDistanceMeters { get; set; }
        public string ErrorKey { get; set; } = string.Empty;
    }

    /// <summary>
    /// سرویس مرکزی دریافت GPS دستگاه، محاسبه فاصله جغرافیایی، مدیریت مکان‌ها و اعتبارسنجی محدوده مجاز پرسنل
    /// </summary>
    public class GeoLocationService
    {
        // تزریق کارخانه ساخت اسکوپ دیتابیس جهت اجرای ایزوله کوئری‌ها بدون تداخل DbContext
        private readonly IServiceScopeFactory _scopeFactory;

        // تزریق سرویس ارتباط با جاوااسکریپت مرورگر جهت خواندن GPS دستگاه
        private readonly IJSRuntime _jsRuntime;

        public GeoLocationService(IServiceScopeFactory scopeFactory, IJSRuntime jsRuntime)
        {
            _scopeFactory = scopeFactory;
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
            const double R = 6371000;
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
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var activeLocations = await db.AllowedLocations
                .AsNoTracking()
                .Where(l => l.IsActive)
                .ToListAsync();

            var deviceLoc = await GetCurrentDeviceLocationAsync();

            // اگر هیچ مکان مجازی تعریف نشده باشد، تردد آزاد است و فقط مختصات ثبت می‌شود
            if (!activeLocations.Any())
            {
                return new LocationValidationResult
                {
                    IsAllowed = true,
                    MatchedLocationName = "FreeMode",
                    Coordinates = deviceLoc.FormattedCoordinates
                };
            }

            // در صورت وجود مکان مجاز فعال، دریافت موقعیت دستگاه اجباری است
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

            // محاسبه فاصله کارمند تا نزدیک‌ترین مکان مجاز و بررسی قرارگیری درون دایره شعاع
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
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pCal = new sqlite_web.Components.Models.Calendar.PCalendar { CurrentLanguage = "fa" };
                var template = pCal.Translate(messageKey);
                var formattedMsg = (args != null && args.Length > 0) ? string.Format(template, args) : template;
                var cleanMsg = System.Text.RegularExpressions.Regex.Replace(formattedMsg, "<.*?>", string.Empty);
                var now = DateTime.Now;

                db.AdminNotificationLogs.Add(new AdminNotificationLog
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

                await db.SaveChangesAsync();
            }
            catch { }
        }

        // واکشی اطلاعات مرکز و شعاع مجاز یک مکان از دیتابیس بر اساس نام ثبت‌شده در رکورد تردد
        public async Task<AllowedLocation?> GetAllowedLocationByNameAsync(string? locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName)) return null;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await db.AllowedLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Name == locationName);
        }

        // واکشی لیست کامل مکان‌های جغرافیایی مجاز همراه با اطلاعات دسته‌بندی‌های متصل به صورت ایزوله
        public async Task<List<AllowedLocation>> GetAllAllowedLocationsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await db.AllowedLocations
                .Include(l => l.AllowedMainCategory)
                .Include(l => l.AllowedSubCategory1)
                .Include(l => l.AllowedSubCategory2)
                .AsNoTracking()
                .OrderByDescending(l => l.Id)
                .ToListAsync();
        }

        // افزودن دسته‌بندی جدید در یکی از سه سطح درخت دسته‌بندی پرسنل به صورت ایزوله
        public async Task AddPersonnelCategoryAsync(string title, int level, int? parentId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.PersonelCategorys.Add(new PersonnelCategory
            {
                Title = title,
                Level = level,
                ParentId = parentId
            });
            await db.SaveChangesAsync();
        }

        // ذخیره مکان مجاز جدید یا ویرایش مشخصات مکان موجود در دیتابیس به صورت ایزوله
        public async Task<bool> SaveOrUpdateAllowedLocationAsync(
            int? editingId,
            string name,
            double latitude,
            double longitude,
            int radiusMeters,
            bool isActive,
            int? mainCatId,
            int? sub1CatId,
            int? sub2CatId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (editingId.HasValue && editingId.Value > 0)
            {
                var existing = await db.AllowedLocations.FirstOrDefaultAsync(l => l.Id == editingId.Value);
                if (existing == null) return false;

                existing.Name = name;
                existing.Latitude = latitude;
                existing.Longitude = longitude;
                existing.RadiusMeters = radiusMeters;
                existing.IsActive = isActive;
                existing.AllowedMainCategoryId = mainCatId;
                existing.AllowedSubCategory1Id = sub1CatId;
                existing.AllowedSubCategory2Id = sub2CatId;

                db.AllowedLocations.Update(existing);
            }
            else
            {
                db.AllowedLocations.Add(new AllowedLocation
                {
                    Name = name,
                    Latitude = latitude,
                    Longitude = longitude,
                    RadiusMeters = radiusMeters,
                    IsActive = isActive,
                    AllowedMainCategoryId = mainCatId,
                    AllowedSubCategory1Id = sub1CatId,
                    AllowedSubCategory2Id = sub2CatId
                });
            }

            await db.SaveChangesAsync();
            return true;
        }

        // حذف قطعی یک مکان مجاز از دیتابیس به صورت ایزوله
        public async Task<bool> DeleteAllowedLocationAsync(int locationId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var loc = await db.AllowedLocations.FirstOrDefaultAsync(l => l.Id == locationId);
            if (loc == null) return false;

            db.AllowedLocations.Remove(loc);
            await db.SaveChangesAsync();
            return true;
        }

        // حذف قطعی یک دسته‌بندی و پاکسازی ارجاع‌های آن از پرونده‌های پرسنل مرتبط به صورت ایزوله
        public async Task<bool> DeletePersonnelCategoryAsync(int categoryId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cat = await db.PersonelCategorys.FirstOrDefaultAsync(c => c.Id == categoryId);
            if (cat == null) return false;

            var relatedEmps = await db.Employees
                .Where(e => e.MainCategoryId == cat.Id || e.SubCategory1Id == cat.Id || e.SubCategory2Id == cat.Id)
                .ToListAsync();

            foreach (var e in relatedEmps)
            {
                if (e.MainCategoryId == cat.Id) e.MainCategoryId = null;
                if (e.SubCategory1Id == cat.Id) e.SubCategory1Id = null;
                if (e.SubCategory2Id == cat.Id) e.SubCategory2Id = null;
            }

            db.PersonelCategorys.Remove(cat);
            await db.SaveChangesAsync();
            return true;
        }
    }
}
