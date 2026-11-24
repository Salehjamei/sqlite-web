using System;
using System.Collections.Generic;

namespace sqlite_web.Components.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; } // اتصال به دیتابیس کارمندان
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }

        /// <summary>
        /// 📅 زمان دقیق فیزیکی اولین ثبت رکورد در دیتابیس (جهت بررسی زمان واقعی)
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public string EntrySource { get; set; } = "Kiosk";

        /// <summary>
        /// 👤 شناسه ادمینی که برای اولین بار این رکورد را ثبت کرد (اگر توسط خود پرسنل باشد، نال می‌ماند)
        /// </summary>
        public int? CreatedByAdminId { get; set; }

        /// <summary>
        /// 👤 شناسه آخرین ادمینی که این رکورد را دستکاری و ویرایش کرد
        /// </summary>
        public int? LastModifiedByAdminId { get; set; }

        // روابط ناوبری دیتابیس (Navigation Properties)
        public Employee Employee { get; set; }

        /// <summary>
        /// 📜 لیست تمام ویرایش‌ها و تغییراتی که ادمین‌ها روی این تایم تردد انجام داده‌اند
        /// </summary>
        public List<AttendanceEditLog> EditLogs { get; set; } = new();
        // مختصات جغرافیایی ثبت‌شده در لحظه ورود (عرض و طول جغرافیایی)
        public string? ClockInLocation { get; set; }

        // مختصات جغرافیایی ثبت‌شده در لحظه خروج (عرض و طول جغرافیایی)
        public string? ClockOutLocation { get; set; }

        // نام مکان مجاز شناسایی‌شده در لحظه ثبت تردد
        public string? LocationName { get; set; }

    }
}
