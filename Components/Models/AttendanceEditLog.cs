using System;

namespace sqlite_web.Components.Models
{
    public class AttendanceEditLog
    {
        public int Id { get; set; }
        public int AttendanceId { get; set; } // اتصال به رکورد تردد اصلی
        public int AdminId { get; set; } // شناسه ادمین ویرایش کننده
        public string AdminUsername { get; set; } = string.Empty; // نام کاربری ادمین جهت نمایش سریع در گزارش ها
        public DateTime EditDate { get; set; } = DateTime.Now; // زمان دقیق اعمال ویرایش

        // ذخیره مقادیر قدیمی جهت مقایسه (قبل از ویرایش)
        public DateTime? OldClockInTime { get; set; }
        public DateTime? OldClockOutTime { get; set; }

        // ذخیره مقادیر جدید (بعد از ویرایش)
        public DateTime? NewClockInTime { get; set; }
        public DateTime? NewClockOutTime { get; set; }

        // دلیل ویرایش رکورد
        public string Reason { get; set; } = "ویرایش توسط ادمین";

        // رابطه ناوبری دیتابیس
        public Attendance Attendance { get; set; }
    }
}
