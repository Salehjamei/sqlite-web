using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin;

namespace sqlite_web
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // به عنوان نمونه یک جدول برای کاربران تعریف می‌کنیم
        public DbSet<Employee> Employees { get; set; }// جدول جدید کارمندان
        public DbSet<Attendance> Attendances { get; set; } //کلاس ورودو خروج
        public DbSet<AdminUser> Admins { get; set; }
    }    
}
