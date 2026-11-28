using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sqlite_web.Components.Models;

namespace sqlite_web.Services
{
    // سرویس متمرکز مدیریت داده‌های پرسنل، درخت دسته‌بندی سه‌سطحی و متدهای فیلتر مشترک بین صفحات
    public class PersonnelService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public PersonnelService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        // واکشی ایزوله لیست کامل دسته‌بندی‌های سه‌سطحی پرسنل از دیتابیس بدون تداخل DbContext
        public async Task<List<PersonnelCategory>> GetAllCategoriesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await db.PersonelCategorys
                .Include(c => c.Parent)
                .AsNoTracking()
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Title)
                .ToListAsync();
        }

        // استخراج لیست کتگوری‌های اصلی (سطح ۱) برای پر کردن اولین سلکتور
        public List<PersonnelCategory> GetMainCategories(IEnumerable<PersonnelCategory> allCategories)
        {
            return allCategories.Where(c => c.Level == 1).ToList();
        }

        // استخراج لیست کتگوری‌های فرزند اول (سطح ۲) متناسب با کتگوری اصلی انتخاب‌شده
        public List<PersonnelCategory> GetSub1Categories(IEnumerable<PersonnelCategory> allCategories, int mainCatId)
        {
            return allCategories
                .Where(c => c.Level == 2 && (mainCatId == 0 || c.ParentId == mainCatId))
                .ToList();
        }

        // استخراج لیست کتگوری‌های فرزند دوم (سطح ۳ - عنوان کاری) متناسب با فرزند اول انتخاب‌شده
        public List<PersonnelCategory> GetSub2Categories(IEnumerable<PersonnelCategory> allCategories, int sub1CatId)
        {
            return allCategories
                .Where(c => c.Level == 3 && (sub1CatId == 0 || c.ParentId == sub1CatId))
                .ToList();
        }

        // واکشی لیست پرسنل همراه با اطلاعات کامل دسته‌بندی‌های سه‌سطحی متصل به هر کارمند
        public async Task<List<Employee>> GetEmployeesAsync(bool onlyVisibleInKiosk = false)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var query = db.Employees
                .Include(e => e.MainCategory)
                .Include(e => e.SubCategory1)
                .Include(e => e.SubCategory2)
                .AsNoTracking()
                .AsQueryable();

            if (onlyVisibleInKiosk)
            {
                query = query.Where(e => !e.IsHiddenFromKiosk);
            }

            return await query.OrderBy(e => e.FullName).ToListAsync();
        }

        // واکشی ایزوله سوابق تردد پرسنل جهت نمایش وضعیت حضور در جداول کیوسک و پنل مدیریت
        public async Task<List<Attendance>> GetAllAttendancesAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await db.Attendances
                .AsNoTracking()
                .ToListAsync();
        }

        // متد مشترک فیلتر ترکیبی پرسنل بر اساس سلکتورهای دسته‌بندی سه‌سطحی و عبارت جستجو در تمام صفحات
        public IEnumerable<Employee> FilterEmployees(
            IEnumerable<Employee> sourceList,
            int mainCatId = 0,
            int sub1CatId = 0,
            int sub2CatId = 0,
            string? searchQuery = null)
        {
            var query = sourceList ?? Enumerable.Empty<Employee>();

            if (mainCatId > 0)
                query = query.Where(e => e.MainCategoryId == mainCatId);

            if (sub1CatId > 0)
                query = query.Where(e => e.SubCategory1Id == sub1CatId);

            if (sub2CatId > 0)
                query = query.Where(e => e.SubCategory2Id == sub2CatId);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim();
                query = query.Where(e =>
                    (!string.IsNullOrEmpty(e.FullName) && e.FullName.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(e.PersonnelCode) && e.PersonnelCode.Contains(q, StringComparison.OrdinalIgnoreCase))
                );
            }

            return query.OrderBy(e => e.FullName);
        }

        // تولید رشته خوانای مسیر دسته‌بندی کارمند برای نمایش در ردیف جداول (مثلاً: کارخانه / تولید / اپراتور)
        public string FormatEmployeeCategoryPath(Employee emp)
        {
            var parts = new List<string>();
            if (emp.MainCategory != null) parts.Add(emp.MainCategory.Title);
            if (emp.SubCategory1 != null) parts.Add(emp.SubCategory1.Title);
            if (emp.SubCategory2 != null) parts.Add(emp.SubCategory2.Title);

            return parts.Any() ? string.Join(" / ", parts) : "-";
        }
        // بررسی تکراری نبودن کد ملی کارمند در دیتابیس
        public async Task<bool> IsPersonnelCodeDuplicateAsync(string code, int? excludeEmployeeId = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            return await db.Employees.AnyAsync(e =>
                e.PersonnelCode == code && (!excludeEmployeeId.HasValue || e.Id != excludeEmployeeId.Value));
        }

        // ثبت کارمند جدید یا ویرایش اطلاعات کارمند موجود به صورت ایزوله و بازگرداندن شیء نهایی و وضعیت تغییر نمایش کیوسک
        public async Task<(Employee? SavedEmployee, bool VisibilityChanged)> SaveOrUpdateEmployeeAsync(
            int? editingId,
            string fullName,
            string? phone,
            string personnelCode,
            string? imagePath,
            bool isHiddenFromKiosk,
            int? mainCatId,
            int? sub1CatId,
            int? sub2CatId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (editingId.HasValue)
            {
                var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == editingId.Value);
                if (emp == null) return (null, false);

                bool visibilityChanged = emp.IsHiddenFromKiosk != isHiddenFromKiosk;

                emp.FullName = fullName;
                emp.Phone = phone;
                emp.PersonnelCode = personnelCode;
                emp.ImagePath = imagePath;
                emp.IsHiddenFromKiosk = isHiddenFromKiosk;
                emp.MainCategoryId = mainCatId;
                emp.SubCategory1Id = sub1CatId;
                emp.SubCategory2Id = sub2CatId;

                db.Employees.Update(emp);
                await db.SaveChangesAsync();
                return (emp, visibilityChanged);
            }
            else
            {
                var newEmp = new Employee
                {
                    FullName = fullName,
                    Phone = phone,
                    PersonnelCode = personnelCode,
                    ImagePath = imagePath,
                    IsHiddenFromKiosk = isHiddenFromKiosk,
                    MainCategoryId = mainCatId,
                    SubCategory1Id = sub1CatId,
                    SubCategory2Id = sub2CatId
                };

                db.Employees.Add(newEmp);
                await db.SaveChangesAsync();
                return (newEmp, false);
            }
        }

        // حذف کامل پرونده کارمند و تمام سوابق تردد او از دیتابیس به صورت ایزوله
        public async Task<Employee?> DeleteEmployeeWithAttendancesAsync(int employeeId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId);
            if (emp == null) return null;

            var attendances = db.Attendances.Where(a => a.EmployeeId == employeeId);
            db.Attendances.RemoveRange(attendances);
            db.Employees.Remove(emp);
            await db.SaveChangesAsync();

            return emp;
        }

        // ثبت ورود جدید (از کیوسک یا سریع ادمین) با بررسی عدم وجود تردد باز قبلی
        public async Task<(bool Success, Attendance? Record)> RecordClockInAsync(
            int employeeId,
            DateTime clockInTime,
            string entrySource,
            string? coordinates,
            string? locationName,
            int? adminId = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            bool hasOpen = await db.Attendances.AnyAsync(a => a.EmployeeId == employeeId && a.ClockOutTime == null);
            if (hasOpen) return (false, null);

            var att = new Attendance
            {
                EmployeeId = employeeId,
                ClockInTime = clockInTime,
                CreationDate = clockInTime,
                EntrySource = entrySource,
                ClockInLocation = coordinates,
                LocationName = locationName,
                CreatedByAdminId = adminId
            };

            db.Attendances.Add(att);
            await db.SaveChangesAsync();
            return (true, att);
        }

        // ثبت خروج روی یک رکورد تردد باز (از کیوسک یا سریع ادمین)
        public async Task<Attendance?> RecordClockOutAsync(
            int attendanceId,
            DateTime clockOutTime,
            string? coordinates,
            int? adminId = null)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var att = await db.Attendances.FirstOrDefaultAsync(a => a.Id == attendanceId);
            if (att == null) return null;

            att.ClockOutTime = clockOutTime;
            att.ClockOutLocation = coordinates;
            if (adminId.HasValue)
            {
                att.LastModifiedByAdminId = adminId;
            }

            db.Attendances.Update(att);
            await db.SaveChangesAsync();
            return att;
        }

        // ثبت دستی یا ویرایش رکورد تردد در کارنامه همراه با ثبت تاریخچه تغییرات (Audit Log)
        public async Task<bool> SaveOrUpdateManualAttendanceAsync(
            int? editingAttendanceId,
            int employeeId,
            DateTime inTime,
            DateTime outTime,
            int? adminId,
            string adminUsername,
            string auditReason)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (editingAttendanceId.HasValue)
            {
                var att = await db.Attendances.FirstOrDefaultAsync(a => a.Id == editingAttendanceId.Value);
                if (att == null) return false;

                db.AttendanceEditLogs.Add(new AttendanceEditLog
                {
                    AttendanceId = att.Id,
                    AdminId = adminId ?? 0,
                    AdminUsername = adminUsername,
                    EditDate = DateTime.Now,
                    OldClockInTime = att.ClockInTime,
                    OldClockOutTime = att.ClockOutTime,
                    NewClockInTime = inTime,
                    NewClockOutTime = outTime,
                    Reason = auditReason
                });

                att.ClockInTime = inTime;
                att.ClockOutTime = outTime;
                att.LastModifiedByAdminId = adminId;

                db.Attendances.Update(att);
            }
            else
            {
                db.Attendances.Add(new Attendance
                {
                    EmployeeId = employeeId,
                    ClockInTime = inTime,
                    ClockOutTime = outTime,
                    CreationDate = DateTime.Now,
                    EntrySource = "AdminManual",
                    CreatedByAdminId = adminId,
                    LastModifiedByAdminId = adminId
                });
            }

            await db.SaveChangesAsync();
            return true;
        }

        // حذف تکی یک رکورد تردد از کارنامه پرسنل
        public async Task<Attendance?> DeleteAttendanceRecordAsync(int attendanceId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var att = await db.Attendances.FirstOrDefaultAsync(a => a.Id == attendanceId);
            if (att == null) return null;

            db.Attendances.Remove(att);
            await db.SaveChangesAsync();
            return att;
        }

    }
}
