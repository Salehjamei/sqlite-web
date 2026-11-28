namespace sqlite_web.Components.Models.Admin
{
    public class AdminUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        // مسیر تصویر پروفایل ادمین ذخیره‌شده در پوشه wwwroot
        public string? ImagePath { get; set; }

        // تاریخ و زمان دقیق ایجاد حساب کاربری ادمین در سیستم
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // شناسه سوپر ادمینی که این حساب مدیریتی را ایجاد کرده است
        public int? CreatedByAdminId { get; set; }

        // 👈 فیل مدیر ارشد است یا معمولی
        public bool IsSuperAdmin { get; set; }
        public bool CanAddEmployee { get; set; } = false;
        public bool CanEditEmployee { get; set; } = false;
        public bool CanDeleteEmployee { get; set; } = false;
        // مجوز مشاهده موقعیت جغرافیایی و نقشه ورود و خروج پرسنل در کارنامه
        public bool CanViewLocationMap { get; set; } = false;
        // مجوز دسترسی به صفحه مدیریت مکان‌های مجاز و دسته‌بندی سه‌سطحی پرسنل
        public bool CanManageLocations { get; set; } = false;

        // 🔒 فیلدهای جدید امنیتی و مدیریت توکن:
        public string? CurrentToken { get; set; } // توکن فعلی و معتبر ادمین که در مرورگر ذخیره می‌شود
        public bool IsFrozen { get; set; } = false; // پرچم فریز/غیرفعال‌سازی موقت ادمین توسط سوپر ادمین
        public DateTime? TokenExpireTime { get; set; } // تاریخ و ساعت دقیق منقضی شدن خودکار توکن فعلی
        public bool IsTokenStopped { get; set; } = false; // پرچم توکن متوقف شده (برای ابطال دستی سشن بدون حذف فیزیکی ادمین)
    }
}