using System;
using System.ComponentModel.DataAnnotations;

namespace sqlite_web.Components.Models
{
    public class AdminNotificationLog
    {
        [Key]
        public int Id { get; set; }

        // بخش یا ماژول صادرکننده اعلان (مقادیر: "Kiosk", "AdminPanel", "ManageAdmins", "Leaves", ...)
        public string Category { get; set; } = "General";

        // متن کامل پیام اعلان
        public string Message { get; set; } = string.Empty;

        // نوع اعلان (Success, Error, Info)
        public string NotificationType { get; set; } = "Success";

        // زمان دقیق ایجاد اعلان
        public DateTime CreatedAt { get; set; } = DateTime.Now;

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
