using System;

namespace sqlite_web.Components.Models.Admin
{
    /// <summary>
    /// مدل دیتابیس ذخیره پویا و متمرکز قوانین کاری و پارامترهای محاسباتی سیستم
    /// </summary>
    public class WorkSettings
    {
        public int Id { get; set; }

        // ساعت شروع و پایان موظفی (مثال: 07:00 و 17:30)
        public string RegularStartTime { get; set; } = "07:00";
        public string RegularEndTime { get; set; } = "17:30";

        // روزهای تعطیل هفته که به صورت کاملاً اضافه کاری محاسبه می‌شوند (با کاما تفکیک می‌شوند: "Saturday,Sunday")
        public string OffDaysOfWeek { get; set; } = "Saturday,Sunday";
        // 🟢 فیلد جدید: آستانه دقیقه جهت انتقال اضافه کاری به موظفی (پیش‌فرض ۱۵ دقیقه)
        public int DeficitThresholdMinutes { get; set; } = 15;
    }
}