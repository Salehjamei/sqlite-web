using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin;

namespace sqlite_web
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        // جدول کارمندان
        public DbSet<Employee> Employees { get; set; } = default!;
        //کلاس ورود و خروج
        public DbSet<Attendance> Attendances { get; set; } = default!;
        //ادمین ها
        public DbSet<AdminUser> Admins { get; set; } = default!;
        // تنظیمات ساعات و روزهای کاری
        public DbSet<WorkSettings> WorkSettings { get; set; } = default!;
        // جدول تاریخچه آرشیو فیزیکی تغییرات ادمین ها (Audit Log)
        public DbSet<AttendanceEditLog> AttendanceEditLogs { get; set; } = default!;
    }
}
