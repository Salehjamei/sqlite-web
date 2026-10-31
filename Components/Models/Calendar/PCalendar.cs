using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace sqlite_web.Components.Models.Calendar
{
    public class PCalendar
    {
        public bool UsePersianCalendar { get; set; } = true;
        public string AttendanceValidationMessage { get; set; } = string.Empty;

        public string ManualInYear { get; set; } = "1405";
        public string ManualInMonth { get; set; } = "07";
        public string ManualInDay { get; set; } = "01";

        public string ManualOutYear { get; set; } = "1405";
        public string ManualOutMonth { get; set; } = "07";
        public string ManualOutDay { get; set; } = "01";

        // 🟢 زبان پیش‌فرض (قابل ذخیره در LocalStorage)
        public string CurrentLanguage { get; set; } = "fa";

        // 🟢 موتور مترجم سراسری برای تمام کلمات کلیدی جدول اصلی و مودال‌ها
        public string Translate(string key)
        {
            var translations = new Dictionary<string, Dictionary<string, string>>
    {
        // عناوین ریشه صفحه و جدول اصلی پرسنل
        { "AppTitle", new Dictionary<string, string> { { "fa", "مدیریت پرسنل و کارکردها" }, { "en", "Personnel Management" }, { "tr", "Personel Yönetimi" } } },
        { "ThPhoto", new Dictionary<string, string> { { "fa", "عکس" }, { "en", "Photo" }, { "tr", "Fotoğraf" } } },
        { "ThName", new Dictionary<string, string> { { "fa", "نام و نام خانوادگی" }, { "en", "Full Name" }, { "tr", "Adı Soyadı" } } },
        { "ThCode", new Dictionary<string, string> { { "fa", "کد ملی" }, { "en", "National Code" }, { "tr", "T.C. Kimlik No" } } },
        { "ThPhone", new Dictionary<string, string> { { "fa", "شماره تماس" }, { "en", "Phone Number" }, { "tr", "Telefon No" } } },
        { "ThReport", new Dictionary<string, string> { { "fa", "کارنامه تفکیکی" }, { "en", "Detailed Report" }, { "tr", "Detaylı Karne" } } },
        { "ThQuickAtt", new Dictionary<string, string> { { "fa", "وضعیت / تردد سریع" }, { "en", "Status / Quick Tracking" }, { "tr", "Durum / Hızlı Geçiş" } } },
        { "ThActions", new Dictionary<string, string> { { "fa", "عملیات پرونده" }, { "en", "Actions" }, { "tr", "İşlemler" } } },

        // مودال تنظیمات محاسباتی سرور
        { "SettingsTitle", new Dictionary<string, string> { { "fa", "⚙️ تنظیمات قوانین کاری و محاسبات سرور" }, { "en", "⚙️ Work Settings & Calculations" }, { "tr", "⚙️ Çalışma Ayarları & Hesaplamalar" } } },
        { "StartTime", new Dictionary<string, string> { { "fa", "⏱️ شروع ساعت موظفی:" }, { "en", "⏱️ Regular Start Time:" }, { "tr", "⏱️ Mesai Başlangıç Saati:" } } },
        { "EndTime", new Dictionary<string, string> { { "fa", "⏱️ پایان ساعت موظفی:" }, { "en", "⏱️ Regular End Time:" }, { "tr", "⏱️ Mesai Bitiش Saati:" } } },
        { "CalendarLang", new Dictionary<string, string> { { "fa", "🌐 نوع تقویم و زبان پیش‌فرض سیستم (ماندگار):" }, { "en", "🌐 Calendar & Language:" }, { "tr", "🌐 Takvim & Dil Seçeneği:" } } },
        { "ThresholdLabel", new Dictionary<string, string> { { "fa", "⚠️ آستانه تهاتر کسری موظفی با اضافه کاری (دقیقه):" }, { "en", "⚠️ Deficit Threshold (Minutes):" }, { "tr", "⚠️ Eksik Mesai Mahsup Eşiği (Dakika):" } } },
        { "ThresholdHelp", new Dictionary<string, string> { { "fa", "* اگر مجموع کسری کار عادی پرسنل در ماه مساوی یا بیشتر از این مقدار باشد، کسری از اضافه کاری کسر خواهد شد." }, { "en", "* If total deficit minutes exceed this value, it will be deducted from overtime." }, { "tr", "* Aylık toplam eksik mesai bu değeri aşarsa, fazla mesaiden düşülecektir." } } },
        { "OffDaysTitle", new Dictionary<string, string> { { "fa", "📅 تعیین روزهای تعطیل هفته (۱۰۰٪ اضافه کاری):" }, { "en", "📅 Define Weekly Off-Days (100% Overtime):" }, { "tr", "📅 Haftalık Tatil Günlerini Belirle (%100 Fazla Mesai):" } } },
        { "SaveBtn", new Dictionary<string, string> { { "fa", "💾 ذخیره تنظیمات در سرور" }, { "en", "💾 Save Settings on Server" }, { "tr", "💾 Ayarları Sunucuya Kaydet" } } },
        { "CancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" } } },

        // روزهای هفته درون مودال تنظیمات
        { "Saturday", new Dictionary<string, string> { { "fa", "شنبه" }, { "en", "Saturday" }, { "tr", "Cumartesi" } } },
        { "Sunday", new Dictionary<string, string> { { "fa", "یکشنبه" }, { "en", "Sunday" }, { "tr", "Pazar" } } },
        { "Monday", new Dictionary<string, string> { { "fa", "دوشنبه" }, { "en", "Monday" }, { "tr", "Pazartesi" } } },
        { "Tuesday", new Dictionary<string, string> { { "fa", "سه‌شنبه" }, { "en", "Tuesday" }, { "tr", "Salı" } } },
        { "Wednesday", new Dictionary<string, string> { { "fa", "چهارشنبه" }, { "en", "Wednesday" }, { "tr", "Çarşamba" } } },
        { "Thursday", new Dictionary<string, string> { { "fa", "پنج‌شنبه" }, { "en", "Thursday" }, { "tr", "Perşembe" } } },
        { "Friday", new Dictionary<string, string> { { "fa", "جمعه" }, { "en", "Friday" }, { "tr", "Cuma" } } },

        // مودال حذف پرسنل
        { "DeleteTitle", new Dictionary<string, string> { { "fa", "⚠️ تاییدیه حذف قطعی پرونده پرسنلی" }, { "en", "⚠️ Confirm Permanent File Deletion" }, { "tr", "⚠️ Kalıcı Dosya Silme Onayı" } } },
        { "DeleteMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل پرونده پرسنلی کارمند مطمئن هستید؟" }, { "en", "Are you sure you want to permanently delete this employee's file?" }, { "tr", "Bu personelin dosyasını kalıcı olarak silmek istediğinizden emin misiniz?" } } },
        { "DeleteSubMsg", new Dictionary<string, string> { { "fa", "توجه: کلیه سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد." }, { "en", "Note: All attendance history will be permanently deleted and cannot be recovered." }, { "tr", "Not: Tüm giriş-çıkış geçmişi kalıcı olarak silinecek ve geri alınamayacaktır." } } },
        { "DeleteConfirmBtn", new Dictionary<string, string> { { "fa", "بله، حذف شود" }, { "en", "Yes, Delete" }, { "tr", "Evet, Silinsin" } } },
        { "kancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" } } },
        // دکمه‌های عملیاتی تردد سریع جدول ریشه صفحه
{ "BtnClockIn", new Dictionary<string, string> { { "fa", "ثبت ورود" }, { "en", "Clock In" }, { "tr", "Giriş" } } },
{ "BtnClockOut", new Dictionary<string, string> { { "fa", "ثبت خروج" }, { "en", "Clock Out" }, { "tr", "Çıkış" } } },

{ "BtnReportView", new Dictionary<string, string> { { "fa", "کارنامه" }, { "en", "Record" }, { "tr", "İş geçmişi" } } },
{ "TitleTAble", new Dictionary<string, string> { { "fa", "مدیریت پرسنل" }, { "en", "Personnel Management" }, { "tr", "Personel yönetimi" } } },
{ "SearchBar", new Dictionary<string, string> { { "fa", "نام یا کد ملی..." }, { "en", "Name or National ID..." }, { "tr", "İsim veya Kimlik No..." } } },
{ "Search", new Dictionary<string, string> { { "fa", "جستجو" }, { "en", "Search" }, { "tr", "Aramak" } } },
{ "session", new Dictionary<string, string> { { "fa", "نام یا کد ملی..." }, { "en", "Your session has expired or you do not have access. Redirecting to the login page..." }, { "tr", "Oturumunuzun süresi doldu veya erişiminiz yok. Giriş sayfasına yönlendiriliyorsunuz..." } } },

// متون پاپ‌آپ مودال تعریف و ویرایش کارمندان
{ "EmpFormAddTitle", new Dictionary<string, string> { { "fa", "کارمند جدید" }, { "en", "New Employee" }, { "tr", "Yeni Personel" } } },
{ "EmpFormEditTitle", new Dictionary<string, string> { { "fa", "✏️ ویرایش اطلاعات کارمند" }, { "en", "✏️ Edit Employee Info" }, { "tr", "✏️ Personel Bilgilerini Düzenle" } } },
{ "EmpLabelName", new Dictionary<string, string> { { "fa", "نام و نام خانوادگی:" }, { "en", "Full Name:" }, { "tr", "Adı Soyadı:" } } },
{ "EmpLabelCode", new Dictionary<string, string> { { "fa", "کد ملی (۱۰ رقم):" }, { "en", "National Code (10 Digits):" }, { "tr", "T.C. Kimlik No (10 Haneli):" } } },
{ "EmpLabelPhone", new Dictionary<string, string> { { "fa", "شماره تماس:" }, { "en", "Phone Number:" }, { "tr", "Telefon Numarası:" } } },
{ "EmpLabelUpload", new Dictionary<string, string> { { "fa", "آپلود عکس:" }, { "en", "Upload Photo:" }, { "tr", "Fotoğraf Yükle:" } } },
{ "EmpBtnSave", new Dictionary<string, string> { { "fa", "💾 ذخیره اطلاعات" }, { "en", "💾 Save Information" }, { "tr", "💾 Bilgileri Kaydet" } } },
{ "EmpBtnSaveChanges", new Dictionary<string, string> { { "fa", "💾 ثبت تغییرات" }, { "en", "Work Settings" }, { "tr", "💾 Değişiklikleri Kaydet" } } },

{ "WorkSet", new Dictionary<string, string> { { "fa", "تنظیمات کاری" }, { "en", "Work Setting" }, { "tr", "Çalışma ayarları" } } },

// متون کمکی پاپ‌آپ حذف پرسنل
{ "DeleteSelectedItem", new Dictionary<string, string> { { "fa", "آیتم انتخابی:" }, { "en", "Selected Item:" }, { "tr", "Seçilen Öğe:" } } }


    };

            if (translations.ContainsKey(key) && translations[key].ContainsKey(CurrentLanguage))
                return translations[key][CurrentLanguage];

            return key;
        }
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

        public string GetDynamicDayName(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "";
            DayOfWeek day = dateTime.Value.DayOfWeek;

            if (CurrentLanguage == "fa")
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
            if (CurrentLanguage == "tr")
            {
                return day switch
                {
                    DayOfWeek.Saturday => "Cumartesi",
                    DayOfWeek.Sunday => "Pazar",
                    DayOfWeek.Monday => "Pazartesi",
                    DayOfWeek.Tuesday => "Salı",
                    DayOfWeek.Wednesday => "Çarşamba",
                    DayOfWeek.Thursday => "Perşembe",
                    DayOfWeek.Friday => "Cuma",
                    _ => ""
                };
            }
            return dateTime.Value.ToString("dddd", CultureInfo.InvariantCulture);
        }

        public string GetDynamicMonthText(int year, int month)
        {
            if (UsePersianCalendar)
            {
                string monthName = month switch
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
                return $"{monthName} {year}";
            }
            DateTime dt = new DateTime(year, month, 1);
            if (CurrentLanguage == "tr")
            {
                string[] trMonths = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
                return $"{trMonths[month - 1]} {year}";
            }
            return dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        }
        // 🟢 اصلاح امضای متد: افزودن <string> برای برطرف شدن خطای CS0305
        public List<string> GetFormYears()
        {
            if (UsePersianCalendar)
                return new List<string> { "1404", "1405", "1406", "1407" };

            return new List<string> { "2025", "2026", "2027", "2028" };
        }

        // 🟢 اصلاح کامل و ساختارمند متد خروجی لیست ماه‌ها متناسب با ساختار فرانت برنامه شما
        public List<KeyValuePair<string, string>> GetFormMonths()
        {
            var list = new List<KeyValuePair<string, string>>();

            if (UsePersianCalendar)
            {
                string[] pMonths = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
                for (int i = 1; i <= 12; i++)
                    list.Add(new KeyValuePair<string, string>(i.ToString("D2"), pMonths[i - 1]));
            }
            else
            {
                if (CurrentLanguage == "tr")
                {
                    string[] trMonths = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
                    for (int i = 1; i <= 12; i++)
                        list.Add(new KeyValuePair<string, string>(i.ToString("D2"), trMonths[i - 1]));
                }
                else
                {
                    string[] eMonths = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                    for (int i = 1; i <= 12; i++)
                        list.Add(new KeyValuePair<string, string>(i.ToString("D2"), eMonths[i - 1]));
                }
            }
            return list;
        }
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