using System;
using System.Collections.Generic;
using System.Globalization;

namespace sqlite_web.Components.Models.Calendar
{
    public class PCalendar
    {
        // پرچم اصلی کنترل نوع تقویم (True: شمسی، False: میلادی)
        public bool UsePersianCalendar { get; set; } = true;

        // رشته متنی مدیریت پیام‌های خطای اعتبارسنجی فرم‌ها
        public string AttendanceValidationMessage { get; set; } = string.Empty;

        // فیلدهای موقت بایند شده به منوهای آبشاری تاریخ ورود
        public string ManualInYear { get; set; } = "1405";
        public string ManualInMonth { get; set; } = "07";
        public string ManualInDay { get; set; } = "01";

        // فیلدهای موقت بایند شده به منوهای آبشاری تاریخ خروج
        public string ManualOutYear { get; set; } = "1405";
        public string ManualOutMonth { get; set; } = "07";
        public string ManualOutDay { get; set; } = "01";

        // فرمت‌دهی زنده تاریخ به شمسی یا میلادی بر اساس سوییچ انتخابی کاربر
        public string GetDynamicFormattedDate(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "-";
            DateTime date = dateTime.Value;

            if (UsePersianCalendar)
            {
                PersianCalendar pc = new PersianCalendar();
                return $"{pc.GetYear(date)}/{pc.GetMonth(date):D2}/{pc.GetDayOfMonth(date):D2}";
            }
            return date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        }

        // استخراج نام روز هفته به زبان انتخابی (فارسی / انگلیسی)
        public string GetDynamicDayName(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";
            DayOfWeek day = dateTime.Value.DayOfWeek;

            if (UsePersianCalendar)
            {
                return day switch
                {
                    DayOfWeek.Saturday => "شنبه",
                    DayOfWeek.Sunday => "یکشنبه",
                    DayOfWeek.Monday => "دوشنبه",
                    DayOfWeek.Tuesday => "سه‌شنبه",
                    DayOfWeek.Wednesday => "چهارشنبه",
                    DayOfWeek.Thursday => "پنج‌شنبه",
                    DayOfWeek.Friday => "جمعه",
                    _ => ""
                };
            }
            return dateTime.Value.ToString("dddd", CultureInfo.InvariantCulture);
        }

        // استخراج نام ماه و سال کارکرد بر اساس قالب متنی سیستم
        public string GetDynamicMonthText(int year, int month)
        {
            DateTime dt = new DateTime(year, month, 1);
            if (UsePersianCalendar)
            {
                PersianCalendar pc = new PersianCalendar();
                int pMonth = pc.GetMonth(dt);
                string monthName = pMonth switch
                {
                    1 => "فروردین",
                    2 => "اردیبهشت",
                    3 => "خرداد",
                    4 => "تیر",
                    5 => "مرداد",
                    6 => "شهریور",
                    7 => "مهر",
                    8 => "آبان",
                    9 => "آذر",
                    10 => "دی",
                    11 => "بهمن",
                    12 => "اسفند",
                    _ => ""
                };
                return $"{monthName} {pc.GetYear(dt)}";
            }
            return dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        }

        // تولید محدوده سال‌ها متناسب با تقویم انتخابی برای منوهای آبشاری
        public List<string> GetFormYears()
        {
            if (UsePersianCalendar) return new List<string> { "1404", "1405", "1406", "1407" };
            return new List<string> { "2025", "2026", "2027", "2028" };
        }

        // تولید لیست ۱۲ ماه سال (شمسی یا میلادی) برای منوهای آبشاری
        public List<KeyValuePair<string, string>> GetFormMonths()
        {
            var list = new List<KeyValuePair<string, string>>();
            if (UsePersianCalendar)
            {
                string[] pMonths = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
                for (int i = 1; i <= 12; i++) list.Add(new KeyValuePair<string, string>(i.ToString("D2"), pMonths[i - 1]));
            }
            else
            {
                string[] eMonths = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                for (int i = 1; i <= 12; i++) list.Add(new KeyValuePair<string, string>(i.ToString("D2"), eMonths[i - 1]));
            }
            return list;
        }

        // همگام‌سازی فیلدهای متنی آبشاری منوها هنگام باز شدن مودال یا کلیک روی دکمه ویرایش ردیف
        public void SyncFormCalendarFields(DateTime inDt, DateTime outDt)
        {
            if (UsePersianCalendar)
            {
                PersianCalendar pc = new PersianCalendar();
                ManualInYear = pc.GetYear(inDt).ToString();
                ManualInMonth = pc.GetMonth(inDt).ToString("D2");
                ManualInDay = pc.GetDayOfMonth(inDt).ToString("D2");

                ManualOutYear = pc.GetYear(outDt).ToString();
                ManualOutMonth = pc.GetMonth(outDt).ToString("D2");
                ManualOutDay = pc.GetDayOfMonth(outDt).ToString("D2");
            }
            else
            {
                ManualInYear = inDt.Year.ToString();
                ManualInMonth = inDt.Month.ToString("D2");
                ManualInDay = inDt.Day.ToString("D2");

                ManualOutYear = outDt.Year.ToString();
                ManualOutMonth = outDt.Month.ToString("D2");
                ManualOutDay = outDt.Day.ToString("D2");
            }
        }

        // تبدیل فیلدهای انتخاب شده منوهای آبشاری تفکیک شده کلاینت به شیء استاندارد DateTime سی‌شارپ
        public DateTime ParseFormFieldsToDateTime(string year, string month, string day, string timeStr)
        {
            int y = int.Parse(year);
            int m = int.Parse(month);
            int d = int.Parse(day);

            var timeParts = timeStr.Split(':');
            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);

            if (UsePersianCalendar)
            {
                PersianCalendar pc = new PersianCalendar();
                return pc.ToDateTime(y, m, d, hour, minute, 0, 0);
            }
            return new DateTime(y, m, d, hour, minute, 0);
        }
    }
}
