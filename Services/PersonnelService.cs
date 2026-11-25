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
    }
}
