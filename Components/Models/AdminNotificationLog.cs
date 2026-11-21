using System;
using System.ComponentModel.DataAnnotations;

namespace sqlite_web.Components.Models
{
    public class AdminNotificationLog
    {
        [Key]
        public int Id { get; set; }

        // بخش یا صفحه صادرکننده اعلان (مقادیر: "Kiosk", "AdminPanel", "ManageAdmins")
        public string Category { get; set; } = "General";

        // موضوع یا زیرمجموعه دقیق رویداد (مقادیر: "Attendance", "EmployeeInfo", "Settings", "AdminAccess")
        public string SubCategory { get; set; } = "General";

        // کلید ترجمه پیام در کلاس PCalendar (برای نمایش چندزبانه در پنل سوپر ادمین)
        public string MessageKey { get; set; } = string.Empty;

        // پارامترهای متغیر پیام به فرمت JSON (مثلاً: ["نام کارمند"])
        public string? MessageArgs { get; set; }

        // متن کامل پیام اعلان (به عنوان پشتیبان و سازگاری با لاگ‌های قبلی)
        public string Message { get; set; } = string.Empty;

        // نوع اعلان (Success, Error, Info)
        public string NotificationType { get; set; } = "Success";

        // زمان دقیق ایجاد اعلان (برای مرتب‌سازی سیستمی)
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // فیلدهای مجزای تاریخ و ساعت ثبت اعلان (برای کنترل، فیلتر و سفارشی‌سازی ادمین)
        public string CreatedDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public string CreatedTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");

        // فیلدهای اختیاری تاریخ و ساعت رویداد هدف (مثلاً در زمان ثبت دستی تردد توسط ادمین)
        public string? EventDate { get; set; }
        public string? EventTime { get; set; }

        // مشخصات ادمینی که عملیات را انجام داده یا اعلان برای او صادر شده است
        public int? AdminId { get; set; }
        public string AdminUsername { get; set; } = "System / Kiosk";

        // مشخصات کارمند هدف
        public int? EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = string.Empty;
        public string EmployeePersonnelCode { get; set; } = string.Empty;
        public string EmployeePhone { get; set; } = string.Empty;

        // وضعیت مشاهده شدن توسط سوپر ادمین
        public bool IsReadBySuperAdmin { get; set; } = false;
    }
}
