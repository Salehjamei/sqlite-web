using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace sqlite_web.Components.Models.Calendar
{
    public class PCalendar
    {
        // نوع تقویم فعال سیستم: "Persian" (شمسی)، "Gregorian" (میلادی)، "Hijri" (هجری قمری)
        public string CalendarType { get; set; } = "Persian";

        // پراپرتی سازگار با بخش‌های قبلی جهت تشخیص تقویم شمسی
        public bool UsePersianCalendar
        {
            get => CalendarType == "Persian";
            set => CalendarType = value ? "Persian" : "Gregorian";
        }
        public string AttendanceValidationMessage { get; set; } = string.Empty;

        public string ManualInYear { get; set; } = "1405";
        public string ManualInMonth { get; set; } = "07";
        public string ManualInDay { get; set; } = "01";

        public string ManualOutYear { get; set; } = "1405";
        public string ManualOutMonth { get; set; } = "07";
        public string ManualOutDay { get; set; } = "01";

        // زبان پیش‌فرض سیستم (fa, en, tr, ar)
        public string CurrentLanguage { get; set; } = "fa";

        // موتور مترجم سراسری برای تمام کلمات کلیدی جدول اصلی، اعلانات و مودال‌ها
        public string Translate(string key)
        {
            var translations = new Dictionary<string, Dictionary<string, string>>
            {
                // لایوت هدر سایت
                { "KioskBtn", new Dictionary<string, string> { { "fa", "کیوسک تردد" }, { "en", "Attendance Kiosk" }, { "tr", "Geçiş Kiosku" }, { "ar", "جهاز الحضور" } } },
                { "PersonnelManagementBtn", new Dictionary<string, string> { { "fa", "مدیریت پرسنل" }, { "en", "Personnel Management" }, { "tr", "Personel Yönetimi" }, { "ar", "إدارة الموظفين" } } },
                { "LogoutBtn", new Dictionary<string, string> { { "fa", "خروج از پنل" }, { "en", "Logout Panel" }, { "tr", "Panelden Çıkış" }, { "ar", "تسجيل الخروج" } } },
                { "AdminDashboard", new Dictionary<string, string> { { "fa", "داشبورد مدیریت" }, { "en", "Admin Dashboard" }, { "tr", "Yönetim Paneli" }, { "ar", "لوحة التحكم" } } },
                { "UserPermissionsBtn", new Dictionary<string, string> { { "fa", "سطوح دسترسی" }, { "en", "User Permissions" }, { "tr", "Kullanıcı Yetkileri" }, { "ar", "صلاحيات الوصول" } } },

                // عناوین ریشه صفحه و جدول اصلی پرسنل
                { "AppTitle", new Dictionary<string, string> { { "fa", "مدیریت پرسنل و کارکردها" }, { "en", "Personnel Management" }, { "tr", "Personel Yönetimi" }, { "ar", "إدارة الموظفين والدوام" } } },
                { "ThPhoto", new Dictionary<string, string> { { "fa", "عکس" }, { "en", "Photo" }, { "tr", "Fotoğraf" }, { "ar", "الصورة" } } },
                { "ThName", new Dictionary<string, string> { { "fa", "نام و نام خانوادگی" }, { "en", "Full Name" }, { "tr", "Adı Soyadı" }, { "ar", "الاسم الكامل" } } },
                { "ThCode", new Dictionary<string, string> { { "fa", "کد ملی" }, { "en", "National Code" }, { "tr", "T.C. Kimlik No" }, { "ar", "الرقم الوطني" } } },
                { "ThPhone", new Dictionary<string, string> { { "fa", "شماره تماس" }, { "en", "Phone Number" }, { "tr", "Telefon No" }, { "ar", "رقم الهاتف" } } },
                { "ThReport", new Dictionary<string, string> { { "fa", "کارنامه تفکیکی" }, { "en", "Detailed Report" }, { "tr", "Detaylı Karne" }, { "ar", "سجل الدوام" } } },
                { "ThQuickAtt", new Dictionary<string, string> { { "fa", "وضعیت / تردد سریع" }, { "en", "Status / Quick Tracking" }, { "tr", "Durum / Hızlı Geçiş" }, { "ar", "الحالة / تسجيل سريع" } } },
                { "ThActions", new Dictionary<string, string> { { "fa", "عملیات پرونده" }, { "en", "Actions" }, { "tr", "İşlemler" }, { "ar", "الإجراءات" } } },

                // اعلان‌های پنل پرسنل، افزودن، ویرایش، ورود و خروج
                { "PCal_AdminPanel_EmpEditSuccess", new Dictionary<string, string> { { "fa", "مشخصات کارمند {0} با موفقیت ویرایش شد." }, { "en", "Employee {0} details updated successfully." }, { "tr", "{0} adlı personelin bilgileri başarıyla güncellendi." }, { "ar", "تم تعديل بيانات الموظف {0} بنجاح." } } },
                { "PCal_AdminPanel_EmpAddSuccess", new Dictionary<string, string> { { "fa", "کارمند جدید با نام {0} با موفقیت در سیستم ثبت شد." }, { "en", "New employee {0} added successfully." }, { "tr", "{0} adlı yeni personel sisteme başarıyla eklendi." }, { "ar", "تم تسجيل الموظف الجديد {0} في النظام بنجاح." } } },
                { "PCal_AdminPanel_EmpDeletedMsg", new Dictionary<string, string> { { "fa", "پرونده پرسنلی کارمند {0} با موفقیت حذف شد." }, { "en", "Employee {0} record deleted successfully." }, { "tr", "{0} adlı personelin dosyası silindi." }, { "ar", "تم حذف ملف الموظف {0} بنجاح." } } },
                { "PCal_AdminPanel_FastInSuccess", new Dictionary<string, string> { { "fa", "ورود سریع برای کارمند {0} با موفقیت ثبت شد." }, { "en", "Quick clock-in recorded for {0}." }, { "tr", "{0} için hızlı giriş kaydı oluşturuldu." }, { "ar", "تم تسجيل الدخول السريع للموظف {0} بنجاح." } } },
                { "PCal_AdminPanel_FastOutSuccess", new Dictionary<string, string> { { "fa", "خروج سریع برای کارمند {0} با موفقیت ثبت شد." }, { "en", "Quick clock-out recorded for {0}." }, { "tr", "{0} için hızlı çıkış kaydı oluşturuldu." }, { "ar", "تم تسجيل الخروج السريع للموظف {0} بنجاح." } } },

                // اعلان بخش کارنامه
                { "PCal_AdminPanel_AttDeletedMsg", new Dictionary<string, string> { { "fa", "رکورد تردد مربوط به کارمند {0} با موفقیت ابطال شد." }, { "en", "Attendance record for {0} successfully deleted." }, { "tr", "{0} adlı personelin geçiş kaydı başarıyla iptal edildi." }, { "ar", "تم إلغاء سجل حضور الموظف {0} بنجاح." } } },
                { "PCal_AdminPanel_ManualAddSuccess", new Dictionary<string, string> { { "fa", "تردد دستی جدید برای کارمند {0} با موفقیت ثبت شد." }, { "en", "Manual attendance record added for {0}." }, { "tr", "{0} için yeni manuel geçiş kaydı eklendi." }, { "ar", "تم إضافة سجل حضور يدوي جديد للموظف {0} بنجاح." } } },
                { "PCal_AdminPanel_ManualEditSuccess", new Dictionary<string, string> { { "fa", "ساعت کاری کارمند {0} با موفقیت ویرایش شد." }, { "en", "Working hours updated for {0}." }, { "tr", "{0} adlı personelin çalışma saatleri güncellendi." }, { "ar", "تم تعديل ساعات عمل الموظف {0} بنجاح." } } },
                                // مودال تنظیمات محاسباتی سرور
                { "SettingsTitle", new Dictionary<string, string> { { "fa", "⚙️ تنظیمات قوانین کاری و محاسبات سرور" }, { "en", "⚙️ Work Settings & Calculations" }, { "tr", "⚙️ Çalışma Ayarları & Hesaplamalar" }, { "ar", "⚙️ إعدادات قوانين العمل وحسابات الخادم" } } },
                { "StartTime", new Dictionary<string, string> { { "fa", "⏱️ شروع ساعت موظفی:" }, { "en", "⏱️ Regular Start Time:" }, { "tr", "⏱️ Mesai Başlangıç Saati:" }, { "ar", "⏱️ بداية الدوام الرسمي:" } } },
                { "EndTime", new Dictionary<string, string> { { "fa", "⏱️ پایان ساعت موظفی:" }, { "en", "⏱️ Regular End Time:" }, { "tr", "⏱️ Mesai Bitiş Saati:" }, { "ar", "⏱️ نهاية الدوام الرسمي:" } } },
                { "CalendarLang", new Dictionary<string, string> { { "fa", "🌐 نوع تقویم و زبان پیش‌فرض سیستم (ماندگار):" }, { "en", "🌐 Calendar & Language:" }, { "tr", "🌐 Takvim & Dil Seçeneği:" }, { "ar", "🌐 نوع التقويم واللغة الافتراضية:" } } },
                { "ThresholdLabel", new Dictionary<string, string> { { "fa", "⚠️ آستانه تهاتر کسری موظفی با اضافه کاری (دقیقه):" }, { "en", "⚠️ Deficit Threshold (Minutes):" }, { "tr", "⚠️ Eksik Mesai Mahsup Eşiği (Dakika):" }, { "ar", "⚠️ حد خصم النقص من العمل الإضافي (بالدقائق):" } } },
                { "ThresholdHelp", new Dictionary<string, string> { { "fa", "* اگر مجموع کسری کار عادی پرسنل در ماه مساوی یا بیشتر از این مقدار باشد، کسری از اضافه کاری کسر خواهد شد." }, { "en", "* If total deficit minutes exceed this value, it will be deducted from overtime." }, { "tr", "* Aylık toplam eksik mesai bu değeri aşarsa, fazla mesaiden düşülecektir." }, { "ar", "* إذا كان إجمالي نقص الدوام الشهري يساوي أو يزيد عن هذا الحد، فسيتم خصمه من العمل الإضافي." } } },
                { "OffDaysTitle", new Dictionary<string, string> { { "fa", "📅 تعیین روزهای تعطیل هفته (۱۰۰٪ اضافه کاری):" }, { "en", "📅 Define Weekly Off-Days (100% Overtime):" }, { "tr", "📅 Haftalık Tatil Günlerini Belirle (%100 Fazla Mesai):" }, { "ar", "📅 تحديد أيام العطلة الأسبوعية (١٠٠٪ عمل إضافي):" } } },
                { "SaveBtn", new Dictionary<string, string> { { "fa", "💾 ذخیره تنظیمات در سرور" }, { "en", "💾 Save Settings on Server" }, { "tr", "💾 Ayarları Sunucuya Kaydet" }, { "ar", "💾 حفظ الإعدادات في الخادم" } } },
                { "CancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" }, { "ar", "إلغاء" } } },

                // روزهای هفته درون مودال تنظیمات
                { "Saturday", new Dictionary<string, string> { { "fa", "شنبه" }, { "en", "Saturday" }, { "tr", "Cumartesi" }, { "ar", "السبت" } } },
                { "Sunday", new Dictionary<string, string> { { "fa", "یکشنبه" }, { "en", "Sunday" }, { "tr", "Pazar" }, { "ar", "الأحد" } } },
                { "Monday", new Dictionary<string, string> { { "fa", "دوشنبه" }, { "en", "Monday" }, { "tr", "Pazartesi" }, { "ar", "الاثنين" } } },
                { "Tuesday", new Dictionary<string, string> { { "fa", "سه‌شنبه" }, { "en", "Tuesday" }, { "tr", "Salı" }, { "ar", "الثلاثاء" } } },
                { "Wednesday", new Dictionary<string, string> { { "fa", "چهارشنبه" }, { "en", "Wednesday" }, { "tr", "Çarşamba" }, { "ar", "الأربعاء" } } },
                { "Thursday", new Dictionary<string, string> { { "fa", "پنج‌شنبه" }, { "en", "Thursday" }, { "tr", "Perşembe" }, { "ar", "الخميس" } } },
                { "Friday", new Dictionary<string, string> { { "fa", "جمعه" }, { "en", "Friday" }, { "tr", "Cuma" }, { "ar", "الجمعة" } } },
                // مودال حذف پرسنل و مدیران
                { "DeleteTitle", new Dictionary<string, string> { { "fa", "⚠️ تاییدیه حذف قطعی پرونده پرسنلی" }, { "en", "⚠️ Confirm Permanent File Deletion" }, { "tr", "⚠️ Kalıcı Dosya Silme Onayı" }, { "ar", "⚠️ تأكيد الحذف النهائي لملف الموظف" } } },
                { "DeleteMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل پرونده پرسنلی کارمند مطمئن هستید؟" }, { "en", "Are you sure you want to permanently delete this employee's file?" }, { "tr", "Bu personelin dosyasını kalıcı olarak silmek istediğinizden emin misiniz?" }, { "ar", "هل أنت متأكد من الحذف الكامل لملف هذا الموظف؟" } } },
                { "DeleteMsgAdmin", new Dictionary<string, string> { { "fa", "آیا از حذف ادمین مطمئن هستید؟" }, { "en", "Are you sure about removing the admin?" }, { "tr", "Yöneticiyi silmek istediğinizden emin misiniz?" }, { "ar", "هل أنت متأكد من حذف هذا المدير؟" } } },
                { "DeleteSubMsg", new Dictionary<string, string> { { "fa", "توجه: کلیه سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد." }, { "en", "Note: All attendance history will be permanently deleted and cannot be recovered." }, { "tr", "Not: Tüm giriş-çıkış geçmişi kalıcı olarak silinecek ve geri alınamayacaktır." }, { "ar", "تنبيه: سيتم مسح جميع سجلات حضور هذا الموظف فوراً وبشكل نهائي." } } },
                { "DeleteConfirmBtn", new Dictionary<string, string> { { "fa", "بله، حذف شود" }, { "en", "Yes, Delete" }, { "tr", "Evet, Silinsin" }, { "ar", "نعم، احذف" } } },
                { "kancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" }, { "ar", "إلغاء" } } },
                { "DeleteSelectedItem", new Dictionary<string, string> { { "fa", "آیتم انتخابی:" }, { "en", "Selected Item:" }, { "tr", "Seçilen Öğe:" }, { "ar", "العنصر المحدد:" } } },
                { "PCal_AdminDelete_Title", new Dictionary<string, string> { { "fa", "تاییدیه حذف قطعی اطلاعات مدیر سیستم" }, { "en", "Confirmation of Permanent System Admin Deletion" }, { "tr", "Sistem Yöneticisi Bilgilerinin Kalıcı Olarak Silinmesi Onayı" }, { "ar", "تأكيد الحذف النهائي لبيانات مدير النظام" } } },
                { "PCal_AdminDelete_DefaultTargetName", new Dictionary<string, string> { { "fa", "مدیر انتخاب شده" }, { "en", "Selected Admin" }, { "tr", "Seçilen Yönetici" }, { "ar", "المدير المحدد" } } },
                { "PCal_AdminDelete_SubMessage", new Dictionary<string, string> { { "fa", "توجه: با تایید نهایی حذف، کلیه دسترسی‌ها، مجوزها و سشن‌های فعال این ادمین فوراً ابطال و پاک خواهد شد." }, { "en", "Note: Upon final deletion approval, all access rights, permissions, and active sessions of this admin will be immediately revoked and wiped." }, { "tr", "Not: Nihai silme onayı ile birlikte, bu yöneticinin tüm erişim hakları, izinleri ve aktif oturumları derhal iptal edilecek ve silinecektir." }, { "ar", "تنبيه: بتأكيد الحذف النهائي، سيتم إلغاء جميع الصلاحيات والجلسات النشطة لهذا المدير فوراً." } } },
                                // دکمه‌های عملیاتی تردد سریع جدول ریشه صفحه
                { "BtnClockIn", new Dictionary<string, string> { { "fa", "ثبت ورود" }, { "en", "Clock In" }, { "tr", "Giriş" }, { "ar", "تسجيل دخول" } } },
                { "BtnClockOut", new Dictionary<string, string> { { "fa", "ثبت خروج" }, { "en", "Clock Out" }, { "tr", "Çıkış" }, { "ar", "تسجيل خروج" } } },
                { "BtnReportView", new Dictionary<string, string> { { "fa", "کارنامه" }, { "en", "Record" }, { "tr", "İş geçmişi" }, { "ar", "السجل" } } },
                { "TitleTAble", new Dictionary<string, string> { { "fa", "مدیریت پرسنل" }, { "en", "Personnel Management" }, { "tr", "Personel yönetimi" }, { "ar", "إدارة الموظفين" } } },
                { "SearchBar", new Dictionary<string, string> { { "fa", "نام یا کد ملی..." }, { "en", "Name or National ID..." }, { "tr", "İsim veya Kimlik No..." }, { "ar", "الاسم أو الرقم الوطني..." } } },
                { "Search", new Dictionary<string, string> { { "fa", "جستجو" }, { "en", "Search" }, { "tr", "Aramak" }, { "ar", "بحث" } } },
                { "session", new Dictionary<string, string> { { "fa", "سشن منقضی شده است..." }, { "en", "Your session has expired or you do not have access. Redirecting to the login page..." }, { "tr", "Oturumunuzun süresi doldu veya erişiminiz yok. Giriş sayfasına yönlendiriliyorsunuz..." }, { "ar", "انتهت صلاحية الجلسة أو ليس لديك صلاحية الوصول..." } } },

                // اعلان‌های صفحه مدیریت مدیران و سطوح دسترسی (ManageAdmins)
                { "PCal_ManageAdmins_AddSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر جدید با نام {0} با موفقیت در سرور تعریف و ثبت گردید." }, { "en", "New system admin account {0} has been successfully registered." }, { "tr", "{0} adlı yeni sistem yöneticisi hesabı başarıyla oluşturuldu." }, { "ar", "تم إنشاء وتسجيل حساب المدير الجديد {0} في الخادم بنجاح." } } },
                { "PCal_ManageAdmins_EditSuccess", new Dictionary<string, string> { { "fa", "مشخصات و مجوزهای حساب کاربری مدیر با نام {0} با موفقیت اصلاح شد." }, { "en", "Permissions and details for admin {0} have been updated." }, { "tr", "{0} adlı yöneticinin bilgileri ve yetkileri güncellendi." }, { "ar", "تم تعديل بيانات وصلاحيات المدير {0} بنجاح." } } },
                { "PCal_ManageAdmins_FreezeSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر با نام {0} مسدود (فریز) شد و سشن‌های او ابطال گردید." }, { "en", "Admin account {0} has been frozen and active sessions revoked." }, { "tr", "{0} adlı yönetici hesabı donduruldu ve aktif oturumları iptal edildi." }, { "ar", "تم تجميد حساب المدير {0} وإلغاء جلساته النشطة." } } },
                { "PCal_ManageAdmins_UnfreezeSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر با نام {0} با موفقیت فعال و رفع مسدودیت شد." }, { "en", "Admin account {0} has been unfrozen and reactivated." }, { "tr", "{0} adlı yönetici hesabının engeli kaldırıldı ve aktifleştirildi." }, { "ar", "تم تفعيل حساب المدير {0} وإلغاء التجميد بنجاح." } } },
                { "PCal_ManageAdmins_RevokeTokenSuccess", new Dictionary<string, string> { { "fa", "توکن زنده عملیاتی مدیر با نام {0} ابطال شد و کاربر فوراً از برنامه خارج گردید." }, { "en", "Active operational token for admin {0} was revoked." }, { "tr", "{0} adlı yöneticinin aktif oturum anahtarı iptal edildi." }, { "ar", "تم إلغاء رمز الجلسة النشط للمدير {0} وتسجيل خروجه فوراً." } } },
                { "PCal_ManageAdmins_DeleteSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر با نام {0} به همراه کلیه سوابق و مجوزهای او با موفقیت حذف گردید." }, { "en", "Admin account {0} and all associated permissions were permanently deleted." }, { "tr", "{0} adlı yönetici hesabı ve tüm yetkileri kalıcı olarak silindi." }, { "ar", "تم حذف حساب المدير {0} وجميع صلاحياته نهائياً." } } },
                { "PCal_ManageAdmins_LastSuperAdminError", new Dictionary<string, string> { { "fa", "خطا: سیستم باید حداقل دارای یک سوپر ادمین باشد. امکان حذف رتبه تنها سوپر ادمین ({0}) وجود ندارد!" }, { "en", "Error: The system must have at least one Super Admin. Cannot demote the only Super Admin ({0})!" }, { "tr", "Hata: Sistemde en az bir Süper Yönetici bulunmalıdır. Tek Süper Yöneticinin ({0}) yetkisi kaldırılamaz!" }, { "ar", "خطأ: يجب أن يحتوي النظام على مدير عام واحد على الأقل. لا يمكن إلغاء رتبة المدير العام الوحيد ({0})!" } } },
                { "PCal_AdminModal_NoRecordsFound", new Dictionary<string, string> { { "fa", "هیچ رکوردی در جدول مدیران سرور وجود ندارد." }, { "en", "No records found in the server admins table." }, { "tr", "Sunucu yöneticileri tablosunda hiçbir kayıt bulunamadı." }, { "ar", "لا توجد أي سجلات في جدول مدراء الخادم." } } },
                { "PCal_AdminModal_AccessDeniedError", new Dictionary<string, string> { { "fa", "🛑 خطای دسترسی: حساب کاربری شما فاقد مجوزهای ارشد جهت مدیریت سطوح دسترسی مدیران است!" }, { "en", "🛑 Access Denied: Your account lacks senior permissions to manage admin access levels!" }, { "tr", "🛑 Erişim Reddedildi: Hesabınız, yönetici erişim seviyelerini yönetmek için kıdemli izinlere sahip değil!" }, { "ar", "🛑 خطأ في الوصول: حسابك يفتقر إلى الصلاحيات العليا لإدارة مستويات وصول المدراء!" } } },
                { "PCal_System_AutoLogoutSuccess", new Dictionary<string, string> { { "fa", "خروج خودکار سیستم برای کارمند {0} در ساعت {1} ثبت شد." }, { "en", "System automatic clock-out recorded for {0} at {1}." }, { "tr", "{0} adlı personel için saat {1} itibarıyla sistem otomatik çıkışı kaydedildi." }, { "ar", "تم تسجيل الخروج التلقائي من النظام للموظف {0} في الساعة {1}." } } },
                                // واژگان صفحه مدیریت سطوح دسترسی و مدیران
                { "AccessPageTitle", new Dictionary<string, string> { { "fa", "مدیریت سطوح دسترسی و مدیران" }, { "en", "Access Levels & Admins Management" }, { "tr", "Yetkilendirme Seviyeleri & Yönetici Yönetimi" }, { "ar", "إدارة مستويات الوصول والمدراء" } } },
                { "NewAdminBtn", new Dictionary<string, string> { { "fa", "➕ مدیر جدید" }, { "en", "➕ New Admin" }, { "tr", "➕ Yeni Yönetici" }, { "ar", "➕ مدير جديد" } } },
                { "SearchAdminPlaceholder", new Dictionary<string, string> { { "fa", "جستجوی نام کاربری..." }, { "en", "Search username..." }, { "tr", "Kullanıcı adı ara..." }, { "ar", "البحث عن اسم المستخدم..." } } },
                { "ThAdminUsername", new Dictionary<string, string> { { "fa", "نام کاربری مدیر" }, { "en", "Admin Username" }, { "tr", "Yönetici Kullanıcı Adı" }, { "ar", "اسم مستخدم المدير" } } },
                { "ThAccessType", new Dictionary<string, string> { { "fa", "نوع دسترسی" }, { "en", "Access Type" }, { "tr", "Yetki Türü" }, { "ar", "نوع الصلاحية" } } },
                { "ThAccountStatus", new Dictionary<string, string> { { "fa", "وضعیت حساب" }, { "en", "Account Status" }, { "tr", "Hesap Durumu" }, { "ar", "حالة الحساب" } } },
                { "ThActiveToken", new Dictionary<string, string> { { "fa", "توکن زنده عملیاتی" }, { "en", "Active Operational Token" }, { "tr", "Aktif Operasyonel Token" }, { "ar", "رمز الجلسة النشط" } } },
                { "ThTokenExpiration", new Dictionary<string, string> { { "fa", "انقضای توکن" }, { "en", "Token Expiration" }, { "tr", "Token Süresi" }, { "ar", "انتهاء الصلاحية" } } },
                { "ThSessionStatus", new Dictionary<string, string> { { "fa", "وضعیت سشن" }, { "en", "Session Status" }, { "tr", "Oturum Durumu" }, { "ar", "حالة الجلسة" } } },
                { "ThAdminActions", new Dictionary<string, string> { { "fa", "عملیات پرونده" }, { "en", "Actions" }, { "tr", "İşlemler" }, { "ar", "الإجراءات" } } },
                { "MenuBBtn", new Dictionary<string, string> { { "fa", "عملیات" }, { "en", "Operations" }, { "tr", "Operasyon" }, { "ar", "العمليات" } } },
                { "RoleSuperAdmin", new Dictionary<string, string> { { "fa", "سوپر ادمین" }, { "en", "Super Admin" }, { "tr", "Süper Yönetici" }, { "ar", "مدير عام" } } },
                { "RoleLimitedAdmin", new Dictionary<string, string> { { "fa", "مدیر محدود" }, { "en", "Limited Admin" }, { "tr", "Kısıtlı Yönetici" }, { "ar", "مدير محدود" } } },
                { "StatusActive", new Dictionary<string, string> { { "fa", "فعال" }, { "en", "Active" }, { "tr", "Aktif" }, { "ar", "نشط" } } },
                { "StatusInactive", new Dictionary<string, string> { { "fa", "غیرفعال" }, { "en", "Inactive" }, { "tr", "Pasif" }, { "ar", "غير نشط" } } },
                { "BtnLogoutRevoke", new Dictionary<string, string> { { "fa", "خروج و ابطال" }, { "en", "Logout & Revoke" }, { "tr", "Çıkış Yap & İptal Et" }, { "ar", "خروج وإلغاء" } } },
                { "TokenRevoked", new Dictionary<string, string> { { "fa", "ابطال شده" }, { "en", "Revoked" }, { "tr", "İptal Edildi" }, { "ar", "ملغى" } } },
                { "NoSession", new Dictionary<string, string> { { "fa", "فاقد سشن" }, { "en", "No Session" }, { "tr", "Oturum Yok" }, { "ar", "بدون جلسة" } } },
                { "SessionOnline", new Dictionary<string, string> { { "fa", "آنلاین" }, { "en", "Online" }, { "tr", "Çevrimiçi" }, { "ar", "متصل" } } },
                { "SearchBBtn", new Dictionary<string, string> { { "fa", "جستجو" }, { "en", "Search" }, { "tr", "Aramak" }, { "ar", "بحث" } } },
                { "EditBBtn", new Dictionary<string, string> { { "fa", "ویرایش" }, { "en", "Edit" }, { "tr", "düzenle" }, { "ar", "تعديل" } } },
                { "DeleteBBtn", new Dictionary<string, string> { { "fa", "حذف" }, { "en", "Delete" }, { "tr", "Silmek" }, { "ar", "حذف" } } },

                // مودال افزودن و ویرایش مدیران
                { "PCal_AdminModal_EditTitle", new Dictionary<string, string> { { "fa", "✏️ ویرایش کامل مشخصات و مجوزهای مدیر" }, { "en", "✏️ Edit Admin Permissions & Details" }, { "tr", "✏️ Yönetici İzinlerini ve Bilgilerini Düzenle" }, { "ar", "✏️ تعديل بيانات وصلاحيات المدير" } } },
                { "PCal_AdminModal_AddTitle", new Dictionary<string, string> { { "fa", "➕ ثبت و تعریف مدیر سیستم جدید" }, { "en", "➕ Register New System Admin" }, { "tr", "➕ Yeni Sistem Yöneticisi Tanımla" }, { "ar", "➕ تسجيل مدير نظام جديد" } } },
                { "PCal_AdminModal_UsernameLabel", new Dictionary<string, string> { { "fa", "نام کاربری ادمین:" }, { "en", "Admin Username:" }, { "tr", "Yönetici Kullanıcı Adı:" }, { "ar", "اسم مستخدم المدير:" } } },
                { "PCal_AdminModal_PasswordLabel", new Dictionary<string, string> { { "fa", "رمز عبور ادمین" }, { "en", "Admin Password" }, { "tr", "Yönetici Şifresi" }, { "ar", "كلمة مرور المدير" } } },
                { "PCal_AdminModal_PasswordHint", new Dictionary<string, string> { { "fa", "(جهت حفظ قبلی خالی بگذارید):" }, { "en", "(leave blank to keep current):" }, { "tr", "(mevcut olanı korumak için boş bırakın):" }, { "ar", "(اتركه فارغاً للإبقاء على الحالية):" } } },
                { "PCal_AdminModal_SuperAdminLabel", new Dictionary<string, string> { { "fa", "👑 اعطای دسترسی سوپر ادمین (مدیریت مدیران)" }, { "en", "👑 Grant Super Admin Privileges (Manage Admins)" }, { "tr", "👑 Süper Yönetici Yetkileri Ver (Yöneticileri Yönet)" }, { "ar", "👑 منح صلاحيات المدير العام (إدارة المدراء)" } } },
                { "PCal_AdminModal_PermissionsSectionTitle", new Dictionary<string, string> { { "fa", "📌 تعیین سطوح دسترسی پرسنلی:" }, { "en", "📌 Determine Personnel Access Levels:" }, { "tr", "📌 Personel Erişim Seviyelerini Belirle:" }, { "ar", "📌 تحديد صلاحيات إدارة الموظفين:" } } },
                { "PCal_AdminModal_CanAddLabel", new Dictionary<string, string> { { "fa", "توانایی افزودن پرسنل جدید" }, { "en", "Ability to add new personnel" }, { "tr", "Yeni personel ekleme yetkisi" }, { "ar", "إمكانية إضافة موظفين جدد" } } },
                { "PCal_AdminModal_CanEditLabel", new Dictionary<string, string> { { "fa", "توانایی ویرایش مشخصات و تردد پرسنل" }, { "en", "Ability to edit personnel details & traffic" }, { "tr", "Personel bilgilerini ve trafiğini düzenleme yetkisi" }, { "ar", "إمكانية تعديل بيانات وحضور الموظفين" } } },
                { "PCal_AdminModal_CanDeleteLabel", new Dictionary<string, string> { { "fa", "توانایی حذف کامل پرسنل و رکوردها" }, { "en", "Ability to completely delete personnel & records" }, { "tr", "Personel ve kayıtları tamamen silme yetkisi" }, { "ar", "إمكانية الحذف الكامل للموظفين والسجلات" } } },
                { "PCal_AdminModal_SaveBtn", new Dictionary<string, string> { { "fa", "💾 ذخیره اطلاعات مدیر" }, { "en", "💾 Save Admin Information" }, { "tr", "💾 Yönetici Bilgilerini Kaydet" }, { "ar", "💾 حفظ بيانات المدير" } } },
                { "PCal_AdminModal_CancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" }, { "ar", "إلغاء" } } },
                                // مودال‌های امنیتی و سیستم
                { "PCal_Security_LogoutTitle", new Dictionary<string, string> { { "fa", "🚪 تاییدیه خروج از سیستم" }, { "en", "🚪 Logout Confirmation" }, { "tr", "🚪 Sistemden Çıkış Onayı" }, { "ar", "🚪 تأكيد تسجيل الخروج" } } },
                { "PCal_Security_LogoutMsg", new Dictionary<string, string> { { "fa", "آیا برای خروج از پنل مدیریت سیستم مطمئن هستید؟" }, { "en", "Are you sure you want to log out of the system administration panel?" }, { "tr", "Sistem yönetim panelinden çıkış yapmak istediğinizden emin misiniz?" }, { "ar", "هل أنت متأكد من رغبتك في تسجيل الخروج من لوحة الإدارة؟" } } },
                { "PCal_Security_LogoutSubMsg", new Dictionary<string, string> { { "fa", "با خروج شما، سشن فعلی باطل شده و نیاز به ورود مجدد خواهید داشت." }, { "en", "Upon logging out, your current session will be invalidated, and you will need to log in again." }, { "tr", "Çıkış yaptığınızda mevcut oturumunuz sonlandırılacak ve tekrar giriş yapmanız gerekecektir." }, { "ar", "بتسجيل خروجك، سيتم إلغاء الجلسة الحالية وستحتاج إلى تسجيل الدخول مرة أخرى." } } },
                { "PCal_Security_LogoutConfirmBtn", new Dictionary<string, string> { { "fa", "بله، خارج می‌شوم" }, { "en", "Yes, Log Out" }, { "tr", "Evet, Çıkış Yapıyorum" }, { "ar", "نعم، تسجيل الخروج" } } },
                { "PCal_Security_CancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" }, { "ar", "إلغاء" } } },
                { "PCal_Security_AccessTitle", new Dictionary<string, string> { { "fa", "⚠️ خطای دسترسی سیستم" }, { "en", "⚠️ System Access Error" }, { "tr", "⚠️ Sistem Erişim Hatası" }, { "ar", "⚠️ خطأ في الوصول إلى النظام" } } },
                { "PCal_Security_AccessMsg", new Dictionary<string, string> { { "fa", "حساب کاربری شما فاقد مجوز ورود است!" }, { "en", "Your account lacks login permissions!" }, { "tr", "Hesabınızın giriş yetkisi bulunmuyor!" }, { "ar", "حسابك يفتقر إلى صلاحية الدخول!" } } },
                { "PCal_Security_AccessSubMsg", new Dictionary<string, string> { { "fa", "شما اجازه ورود به پنل مدیریت پرسنل را ندارید. جهت اعطای دسترسی‌های لازم، لطفاً با مدیر ارشد سیستم تماس حاصل فرمایید." }, { "en", "You are not allowed to enter the personnel management panel. Please contact the senior system administrator to grant the necessary permissions." }, { "tr", "Personel yönetim paneline giriş izniniz yoktur. Gerekli yetkilerin verilmesi için lütfen kıdemli sistem yöneticisi ile iletişime geçin." }, { "ar", "غير مسموح لك بالدخول إلى لوحة إدارة الموظفين. يرجى التواصل مع مدير النظام لمنحك الصلاحيات اللازمة." } } },
                { "PCal_Security_IUnderstandBtn", new Dictionary<string, string> { { "fa", "متوجه شدم" }, { "en", "I Understand" }, { "tr", "Anladım" }, { "ar", "فهمت" } } },
                { "PCal_Security_HijackTitle", new Dictionary<string, string> { { "fa", "⚠️ هشدار عدم سطح دسترسی" }, { "en", "⚠️ Access Level Warning" }, { "tr", "⚠️ Erişim Seviyesi Uyarısı" }, { "ar", "⚠️ تحذير مستوى الوصول" } } },
                { "PCal_Security_HijackMsg", new Dictionary<string, string> { { "fa", "دسترسی غیرمجاز از طریق تغییر لینک!" }, { "en", "Unauthorized access via URL modification!" }, { "tr", "Bağlantı değişikliği yoluyla yetkisiz erişim!" }, { "ar", "وصول غير مصرح به عبر تعديل الرابط!" } } },
                { "PCal_Security_HijackSubMsg", new Dictionary<string, string> { { "fa", "کاربر محترم، حساب شما مجوز ورود مستقیم به صفحه درخواستی را ندارد. سیستم به صورت خودکار شما را به مسیر مجاز بازگرداند." }, { "en", "Dear user, your account does not have permission to directly access the requested page. The system has automatically redirected you to an authorized path." }, { "tr", "Sayın kullanıcı, hesabınız talep edilen sayfaya doğrudan erişim iznine sahip değildir. Sistem sizi otomatik olarak yetkilendirilmiş yola yönlendirdi." }, { "ar", "عزيزي المستخدم، حسابك لا يملك صلاحية الوصول المباشر إلى الصفحة المطلوبة. قام النظام بإعادتك تلقائياً إلى المسار المصرح به." } } },
                { "PCal_Security_CloseMenuTitle", new Dictionary<string, string> { { "fa", "بستن منو" }, { "en", "Close Menu" }, { "tr", "Menüyü Kapat" }, { "ar", "إغلاق القائمة" } } },

                // صفحه اصلی کیوسک تردد
                { "PCal_MainHome_PageTitle", new Dictionary<string, string> { { "fa", "لیست پرسنل و ثبت تردد" }, { "en", "Personnel List & Attendance Log" }, { "tr", "Personel Listesi ve Giriş-Çıkış Kaydı" }, { "ar", "قائمة الموظفين وتسجيل الحضور" } } },
                { "PCal_MainHome_SearchPlaceholder", new Dictionary<string, string> { { "fa", "جستجوی نام یا کد..." }, { "en", "Search name or code..." }, { "tr", "İsim veya kod ara..." }, { "ar", "البحث بالاسم أو الرمز..." } } },
                { "PCal_MainHome_SearchBtnText", new Dictionary<string, string> { { "fa", "جستجو 🔍" }, { "en", "Search 🔍" }, { "tr", "Ara 🔍" }, { "ar", "بحث 🔍" } } },
                { "PCal_MainHome_ClearBtnTitle", new Dictionary<string, string> { { "fa", "پاک کردن" }, { "en", "Clear" }, { "tr", "Temizle" }, { "ar", "مسح" } } },
                { "PCal_MainHome_LockedTitle", new Dictionary<string, string> { { "fa", "🔒 سیستم ثبت تردد غیرفعال است" }, { "en", "🔒 Attendance System is Disabled" }, { "tr", "🔒 Giriş-Çıkış Sistemi Devre Dışı" }, { "ar", "🔒 نظام تسجيل الحضور معطل" } } },
                { "PCal_MainHome_LockedSub", new Dictionary<string, string> { { "fa", "این دستگاه مجاز به ثبت تردد نیست. لطفا از ادمین مجموعه بخواهید ابتدا سیستم را فعال کند." }, { "en", "This device is not authorized for attendance tracking. Please ask the administrator to activate the system." }, { "tr", "Bu cihaz giriş-çıkış kaydı için yetkilendirilmemiştir. Lütfen yöneticiden sistemi aktif etmesini isteyin." }, { "ar", "هذا الجهاز غير مصرح له بتسجيل الحضور. يرجى الطلب من المدير تفعيل النظام أولاً." } } },
                { "PCal_MainHome_AdminLoginBtn", new Dictionary<string, string> { { "fa", "🔑 ورود ادمین" }, { "en", "🔑 Admin Login" }, { "tr", "🔑 Yönetici Girişi" }, { "ar", "🔑 دخول المدير" } } },
                { "PCal_MainHome_ThAvatar", new Dictionary<string, string> { { "fa", "عکس" }, { "en", "Photo" }, { "tr", "Fotoğraf" }, { "ar", "الصورة" } } },
                { "PCal_MainHome_ThName", new Dictionary<string, string> { { "fa", "نام کارمند" }, { "en", "Employee Name" }, { "tr", "Personel Adı" }, { "ar", "اسم الموظف" } } },
                { "PCal_MainHome_ThStatus", new Dictionary<string, string> { { "fa", "وضعیت فعلی" }, { "en", "Current Status" }, { "tr", "Mevcut Durum" }, { "ar", "الحالة الحالية" } } },
                { "PCal_MainHome_ThActions", new Dictionary<string, string> { { "fa", "عملیات تردد سریع" }, { "en", "Quick Action" }, { "tr", "Hızlı İşlem" }, { "ar", "تسجيل سريع" } } },
                { "PCal_MainHome_NoPersonnel", new Dictionary<string, string> { { "fa", "هیچ پرسنلی مطابق جستجو یافت نشد." }, { "en", "No personnel found matching the search." }, { "tr", "Aramaya uygun personel bulunamadı." }, { "ar", "لم يتم العثور على موظفين مطابقين للبحث." } } },
                { "PCal_MainHome_StatusAbsent", new Dictionary<string, string> { { "fa", "خارج / غایب" }, { "en", "Absent / Out" }, { "tr", "Dışarıda / Yok" }, { "ar", "خارج / غائب" } } },
                { "PCal_MainHome_StatusPresent", new Dictionary<string, string> { { "fa", "حاضر" }, { "en", "Present" }, { "tr", "İçeride / Mevcut" }, { "ar", "حاضر" } } },
                { "PCal_MainHome_BtnClockIn", new Dictionary<string, string> { { "fa", "ورود" }, { "en", "Clock In" }, { "tr", "Giriş" }, { "ar", "دخول" } } },
                { "PCal_MainHome_BtnClockOut", new Dictionary<string, string> { { "fa", "خروج" }, { "en", "Clock Out" }, { "tr", "Çıkış" }, { "ar", "خروج" } } },
                { "PCal_MainHome_Notification", new Dictionary<string, string> { { "fa", "اطلاع‌رسانی" }, { "en", "Notification" }, { "tr", "Bildirim" }, { "ar", "إشعار" } } },
                { "PCal_MainHome_Now", new Dictionary<string, string> { { "fa", "اکنون" }, { "en", "Now" }, { "tr", "Şimdi" }, { "ar", "الآن" } } },
                { "PCal_MainHome_CloseBtn", new Dictionary<string, string> { { "fa", "بستن" }, { "en", "Close" }, { "tr", "Kapat" }, { "ar", "إغلاق" } } },
                { "PCal_MainHome_DefaultUser", new Dictionary<string, string> { { "fa", "کاربر" }, { "en", "User" }, { "tr", "Kullanıcı" }, { "ar", "مستخدم" } } },
                { "PCal_MainHome_ClockInSuccess", new Dictionary<string, string> { { "fa", "{0} ورود ثبت شد." }, { "en", "{0} checked in successfully." }, { "tr", "{0} giriş kaydı yapıldı." }, { "ar", "تم تسجيل دخول {0} بنجاح." } } },
                { "PCal_MainHome_ClockOutSuccess", new Dictionary<string, string> { { "fa", "{0} خروج ثبت شد." }, { "en", "{0} checked out successfully." }, { "tr", "{0} çıkış kaydı yapıldı." }, { "ar", "تم تسجيل خروج {0} بنجاح." } } },
                { "PCal_MainHome_LoadError", new Dictionary<string, string> { { "fa", "خطا در بارگذاری داده‌ها: {0}" }, { "en", "Error loading data: {0}" }, { "tr", "Veri yükleme hatası: {0}" }, { "ar", "خطأ في تحميل البيانات: {0}" } } },
                { "PCal_MainHome_ClockInError", new Dictionary<string, string> { { "fa", "خطا در ثبت ورود: {0}" }, { "en", "Error during clock-in: {0}" }, { "tr", "Giriş kaydı hatası: {0}" }, { "ar", "خطأ في تسجيل الدخول: {0}" } } },
                { "PCal_MainHome_ClockOutError", new Dictionary<string, string> { { "fa", "خطا در ثبت خروج: {0}" }, { "en", "Error during clock-out: {0}" }, { "tr", "Çıkış kaydı hatası: {0}" }, { "ar", "خطأ في تسجيل الخروج: {0}" } } },
                { "PCal_MainHome_KioskLocked", new Dictionary<string, string> { { "fa", "🔒 سیستم مدیریت خارج شد و کیوسک تردد قفل گردید." }, { "en", "🔒 Management system logged out and kiosk locked." }, { "tr", "🔒 Yönetim sisteminden çıkış yapıldı ve kiosk kilitlendi." }, { "ar", "🔒 تم تسجيل الخروج من الإدارة وقفل جهاز الحضور." } } },
                { "PCal_MainHome_SourceManual", new Dictionary<string, string> { { "fa", "ثبت دستی ادمین" }, { "en", "Manual Admin" }, { "tr", "Manuel Yönetici" }, { "ar", "تسجيل يدوي" } } },
                { "PCal_MainHome_SourceFast", new Dictionary<string, string> { { "fa", "سریع ادمین" }, { "en", "Fast Admin" }, { "tr", "Hızlı Yönetici" }, { "ar", "سريع من المدير" } } },
                { "PCal_MainHome_SourceKiosk", new Dictionary<string, string> { { "fa", "دستگاه کیوسک" }, { "en", "Kiosk Device" }, { "tr", "Kiosk Cihazı" }, { "ar", "جهاز البصمة" } } },
                { "PCal_MainHome_SourceAuto", new Dictionary<string, string> { { "fa", "خروج خودکار" }, { "en", "Auto Logout" }, { "tr", "Otomatik Çıkış" }, { "ar", "خروج تلقائي" } } },
                { "PCal_MainHome_AutoLabel", new Dictionary<string, string> { { "fa", "سیستم" }, { "en", "System" }, { "tr", "Sistem" }, { "ar", "النظام" } } },
                { "PCal_MainHome_AlreadyHasOpenRecord", new Dictionary<string, string> { { "fa", "شما یک تردد باز فعال در سیستم دارید. ابتدا باید خروج ثبت کنید." }, { "en", "You have an active open attendance record. You must clock out first." }, { "tr", "Sistemde aktif açık bir geçiş kaydınız var. Önce çıkış yapmalısınız." }, { "ar", "لديك سجل دخول مفتوح حالياً في النظام. يجب تسجيل الخروج أولاً." } } },

                // صفحه ورود ادمین سیستم
                { "PCal_Login_CardHeader", new Dictionary<string, string> { { "fa", "ورود به پنل مدیریت" }, { "en", "Admin Panel Login" }, { "tr", "Yönetici Paneli Girişi" }, { "ar", "تسجيل الدخول إلى لوحة الإدارة" } } },
                { "PCal_Login_UsernameLabel", new Dictionary<string, string> { { "fa", "نام کاربری ادمین:" }, { "en", "Admin Username:" }, { "tr", "Yönetici Kullanıcı Adı:" }, { "ar", "اسم مستخدم المدير:" } } },
                { "PCal_Login_PasswordLabel", new Dictionary<string, string> { { "fa", "رمز عبور:" }, { "en", "Password:" }, { "tr", "Şifre:" }, { "ar", "كلمة المرور:" } } },
                { "PCal_Login_SubmitBtn", new Dictionary<string, string> { { "fa", "ورود به سیستم" }, { "en", "Login to System" }, { "tr", "Sisteme Giriş Yap" }, { "ar", "دخول إلى النظام" } } },
                { "PCal_Login_RequiredFields", new Dictionary<string, string> { { "fa", "لطفا نام کاربری و رمز عبور را وارد نمایید." }, { "en", "Please enter your username and password." }, { "tr", "Lütfen kullanıcı adınızı ve şifrenizi giriniz." }, { "ar", "يرجى إدخال اسم المستخدم وكلمة المرور." } } },
                // پنل مدیریت پرسنل و کارنامه
                { "PCal_AdminPanel_ReportModalTitle", new Dictionary<string, string> { { "fa", "کارنامه تفکیکی و مدیریت ساعت کاری:" }, { "en", "Detailed Report & Work Hours Management:" }, { "tr", "Detaylı Karne ve Çalışma Saatleri Yönetimi:" }, { "ar", "سجل الدوام التفصيلي وإدارة ساعات العمل:" } } },
                { "PCal_AdminPanel_TotalHoursUnit", new Dictionary<string, string> { { "fa", "00:00 ساعت" }, { "en", "00:00 Hrs" }, { "tr", "00:00 Saat" }, { "ar", "00:00 ساعة" } } },
                { "PCal_AdminPanel_ImageUploadError", new Dictionary<string, string> { { "fa", "خطا در بارگذاری فایل تصویر: {0}" }, { "en", "Error uploading image file: {0}" }, { "tr", "Resim dosyası yüklenirken hata oluştu: {0}" }, { "ar", "خطأ في تحميل ملف الصورة: {0}" } } },
                { "PCal_AdminPanel_EmpNameRequired", new Dictionary<string, string> { { "fa", "خطا: وارد کردن نام و نام خانوادگی کارمند الزامی است!" }, { "en", "Error: Employee full name is required!" }, { "tr", "Hata: Personel adı ve soyadı zorunludur!" }, { "ar", "خطأ: إدخال الاسم الكامل للموظف إلزامي!" } } },
                { "PCal_AdminPanel_EmpCodeRequired", new Dictionary<string, string> { { "fa", "خطا: وارد کردن کد ملی / شناسایی کارمند الزامی است!" }, { "en", "Error: Employee national ID code is required!" }, { "tr", "Hata: Personel kimlik numarası zorunludur!" }, { "ar", "خطأ: إدخال الرقم الوطني / التعريفي للموظف إلزامي!" } } },
                { "PCal_AdminPanel_ErrNameLength", new Dictionary<string, string> { { "fa", "خطا: طول نام و نام خانوادگی نمی‌تواند بیشتر از ۴۰ کاراکتر باشد." }, { "en", "Error: Full name cannot exceed 40 characters." }, { "tr", "Hata: Ad ve soyad 40 karakteri geçemez." }, { "ar", "خطأ: لا يمكن أن يتجاوز الاسم الكامل ٤٠ حرفاً." } } },
                { "PCal_AdminPanel_ErrCodeLength", new Dictionary<string, string> { { "fa", "خطا: کد ملی باید حداقل ۱۰ رقم و حداکثر ۲۰ رقم باشد." }, { "en", "Error: National ID must be between 10 and 20 digits." }, { "tr", "Hata: Kimlik numarası en az 10, en fazla 20 haneli olmalıdır." }, { "ar", "خطأ: يجب أن يتكون الرقم الوطني من ١٠ إلى ٢٠ رقماً." } } },
                { "PCal_AdminPanel_ErrPhoneLength", new Dictionary<string, string> { { "fa", "خطا: شماره تماس نمی‌تواند بیشتر از ۱۵ رقم باشد." }, { "en", "Error: Phone number cannot exceed 15 digits." }, { "tr", "Hata: Telefon numarası 15 haneyi geçemez." }, { "ar", "خطأ: لا يمكن أن يتجاوز رقم الهاتف ١٥ رقماً." } } },
                { "PCal_AdminPanel_DuplicateCodeEdit", new Dictionary<string, string> { { "fa", "خطا: این کد ملی قبلاً برای کارمند دیگری در سیستم ثبت شده است! کد ملی باید منحصربه‌فرد باشد." }, { "en", "Error: This ID is already assigned to another employee! It must be unique." }, { "tr", "Hata: Bu kimlik numarası başka bir personele kayıtlı! Benzersiz olmalıdır." }, { "ar", "خطأ: هذا الرقم الوطني مسجل لموظف آخر في النظام! يجب أن يكون فريداً." } } },
                { "PCal_AdminPanel_DuplicateCodeAdd", new Dictionary<string, string> { { "fa", "خطا: کارمندی با این کد ملی قبلاً در سیستم تعریف شده است! ورود کد ملی تکراری مجاز نیست." }, { "en", "Error: An employee with this ID already exists! Duplicate ID is not allowed." }, { "tr", "Hata: Bu kimlik numarasına sahip bir personel zaten mevcut!" }, { "ar", "خطأ: يوجد موظف مسجل بهذا الرقم الوطني مسبقاً! لا يسمح بالتكرار." } } },
                { "PCal_AdminPanel_DelEmpTitle", new Dictionary<string, string> { { "fa", "تاییدیه حذف قطعی پرونده پرسنلی" }, { "en", "Confirm Permanent Employee Deletion" }, { "tr", "Personel Dosyası Kalıcı Silme Onayı" }, { "ar", "تأكيد الحذف النهائي لملف الموظف" } } },
                { "PCal_AdminPanel_DelEmpMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل پرونده پرسنلی کارمند با نام «{0}» مطمئن هستید؟" }, { "en", "Are you sure you want to completely delete employee '{0}'?" }, { "tr", "'{0}' adlı personelin dosyasını tamamen silmek istediğinizden emin misiniz?" }, { "ar", "هل أنت متأكد من الحذف الكامل لملف الموظف «{0}»؟" } } },
                { "PCal_AdminPanel_DelEmpSubMsg", new Dictionary<string, string> { { "fa", "توجه: کلیه سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد." }, { "en", "Note: All attendance records for this employee will be permanently wiped." }, { "tr", "Not: Bu personelin tüm geçiş kayıtları kalıcı olarak silinecektir." }, { "ar", "تنبيه: سيتم مسح جميع سجلات حضور هذا الموظف فوراً وبشكل لا يمكن التراجع عنه." } } },
                { "PCal_AdminPanel_FastInError", new Dictionary<string, string> { { "fa", "خطا در ثبت ورود سریع: {0}" }, { "en", "Error during quick clock-in: {0}" }, { "tr", "Hızlı giriş kaydı hatası: {0}" }, { "ar", "خطأ في تسجيل الدخول السريع: {0}" } } },
                { "PCal_AdminPanel_FastOutError", new Dictionary<string, string> { { "fa", "خطا در ثبت خروج سریع: {0}" }, { "en", "Error during quick clock-out: {0}" }, { "tr", "Hızlı çıkış kaydı hatası: {0}" }, { "ar", "خطأ في تسجيل الخروج السريع: {0}" } } },
                { "PCal_AdminPanel_ManualDateMismatch", new Dictionary<string, string> { { "fa", "خطا: تاریخ خروج باید دقیقاً همان روز تاریخ ورود باشد!" }, { "en", "Error: Clock-out date must match the clock-in date!" }, { "tr", "Hata: Çıkış tarihi giriş tarihi ile aynı gün olmalıdır!" }, { "ar", "خطأ: يجب أن يطابق تاريخ الخروج نفس يوم تاريخ الدخول!" } } },
                { "PCal_AdminPanel_ManualTimeOrderError", new Dictionary<string, string> { { "fa", "خطا: ساعت خروج نمی‌تواند قبل‌تر از ساعت ورود تنظیم شود!" }, { "en", "Error: Clock-out time cannot be earlier than clock-in time!" }, { "tr", "Hata: Çıkış saati giriş saatinden önce olamaz!" }, { "ar", "خطأ: لا يمكن أن يكون وقت الخروج قبل وقت الدخول!" } } },
                { "PCal_AdminPanel_AuditEditReason", new Dictionary<string, string> { { "fa", "اصلاح ساعت کارکرد توسط ادمین در پنل کارنامه پرسنل" }, { "en", "Working hours adjusted by admin in report panel" }, { "tr", "Yönetici tarafından karne panelinde çalışma saatleri düzenlendi" }, { "ar", "تعديل ساعات العمل من قبل المدير في لوحة السجلات" } } },
                { "PCal_AdminPanel_InvalidTimeFormat", new Dictionary<string, string> { { "fa", "فرمت زمان وارد شده اشتباه است (مثال معتبر: 07:00 یا 17:30)" }, { "en", "Invalid time format (valid example: 07:00 or 17:30)" }, { "tr", "Geçersiz saat formatı (örnek: 07:00 veya 17:30)" }, { "ar", "صيغة الوقت المدخلة غير صحيحة (مثال صحيح: 07:00 أو 17:30)" } } },
                { "PCal_AdminPanel_ActiveAttendance", new Dictionary<string, string> { { "fa", "در حال تردد" }, { "en", "Active" }, { "tr", "Aktif Geçiş" }, { "ar", "حاضر حالياً" } } },
                { "PCal_AdminPanel_AllMonths", new Dictionary<string, string> { { "fa", "همه ماه‌های کارکرد" }, { "en", "All Working Months" }, { "tr", "Tüm Çalışma Ayları" }, { "ar", "جميع أشهر العمل" } } },
                { "PCal_AdminPanel_DelAttTitle", new Dictionary<string, string> { { "fa", "تاییدیه حذف قطعی رکورد تردد پرسنل" }, { "en", "Confirm Attendance Record Deletion" }, { "tr", "Geçiş Kaydı Silme Onayı" }, { "ar", "تأكيد الحذف النهائي لسجل الحضور" } } },
                { "PCal_AdminPanel_DelAttTargetName", new Dictionary<string, string> { { "fa", "تردد مورخ {0} ساعت {1}" }, { "en", "Attendance on {0} at {1}" }, { "tr", "{0} tarihli saat {1} geçişi" }, { "ar", "سجل بتاريخ {0} الساعة {1}" } } },
                { "PCal_AdminPanel_DelAttMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل این رکورد تردد کاری پرسنل در تاریخ {0} مطمئن هستید؟" }, { "en", "Are you sure you want to delete this attendance record on {0}?" }, { "tr", "{0} tarihindeki bu çalışma geçiş kaydını silmek istediğinizden emin misiniz?" }, { "ar", "هل أنت متأكد من حذف سجل حضور الموظف بتاريخ {0}؟" } } },
                { "PCal_AdminPanel_DelAttSubMsg", new Dictionary<string, string> { { "fa", "توجه: با تایید نهایی، این ساعت ورود و خروج خاص به طور کامل پاک شده و در محاسبات این ماه لحاظ نخواهد شد." }, { "en", "Note: Once confirmed, this clock-in/out record will be deleted and excluded from monthly calculations." }, { "tr", "Not: Onaylandığında bu giriş-çıkış kaydı silinecek ve aylık hesaplamalara dahil edilmeyecektir." }, { "ar", "تنبيه: بتأكيد الحذف، سيتم مسح هذا السجل واستبعاده من حسابات هذا الشهر." } } },
                { "PCal_AdminPanel_DeleteError", new Dictionary<string, string> { { "fa", "خطا در حذف: {0}" }, { "en", "Error during deletion: {0}" }, { "tr", "Silme işlemi sırasında hata: {0}" }, { "ar", "خطأ أثناء الحذف: {0}" } } },
                                { "PCal_AdminPanel_NoPermissionTitle", new Dictionary<string, string> { { "fa", "شما مجوز ندارید" }, { "en", "Permission denied" }, { "tr", "Yetkiniz yok" }, { "ar", "ليس لديك صلاحية" } } },
                { "PCal_AdminPanel_SessionExpired", new Dictionary<string, string> { { "fa", "سشن شما منقضی شده یا دسترسی ندارید. در حال انتقال به صفحه ورود..." }, { "en", "Your session has expired or access is denied. Redirecting to login..." }, { "tr", "Oturumunuz sona erdi veya erişiminiz yok. Giriş sayfasına yönlendiriliyorsunuz..." }, { "ar", "انتهت صلاحية الجلسة أو لا تملك صلاحية الوصول. جاري التحويل لصفحة الدخول..." } } },
                { "PCal_AdminPanel_NoEmployeesFound", new Dictionary<string, string> { { "fa", "هیچ رکوردی در جدول کارمندان وجود ندارد." }, { "en", "No records found in the employees table." }, { "tr", "Personel tablosunda kayıt bulunamadı." }, { "ar", "لا توجد أي سجلات في جدول الموظفين." } } },
                { "PCal_AdminPanel_SelectMonthLabel", new Dictionary<string, string> { { "fa", "انتخاب ماه کارکرد:" }, { "en", "Select Working Month:" }, { "tr", "Çalışma Ayını Seçin:" }, { "ar", "اختر شهر العمل:" } } },
                { "PCal_AdminPanel_RegularLabel", new Dictionary<string, string> { { "fa", "موظف:" }, { "en", "Regular:" }, { "tr", "Normal Mesai:" }, { "ar", "الدوام الرسمي:" } } },
                { "PCal_AdminPanel_OvertimeLabel", new Dictionary<string, string> { { "fa", "اضافه کار:" }, { "en", "Overtime:" }, { "tr", "Fazla Mesai:" }, { "ar", "العمل الإضافي:" } } },
                { "PCal_AdminPanel_AddManualBtn", new Dictionary<string, string> { { "fa", "ثبت تردد دستی جدید" }, { "en", "Add Manual Attendance" }, { "tr", "Yeni Manuel Geçiş Ekle" }, { "ar", "إضافة سجل حضور يدوي" } } },
                { "PCal_AdminPanel_ThDate", new Dictionary<string, string> { { "fa", "تاریخ روز" }, { "en", "Date" }, { "tr", "Tarih" }, { "ar", "التاريخ" } } },
                { "PCal_AdminPanel_ThTimeRange", new Dictionary<string, string> { { "fa", "ساعت تردد (ورود / خروج)" }, { "en", "Time (In / Out)" }, { "tr", "Geçiş Saati (Giriş / Çıkış)" }, { "ar", "وقت الحضور (دخول / خروج)" } } },
                { "PCal_AdminPanel_ThDuration", new Dictionary<string, string> { { "fa", "کارکرد" }, { "en", "Duration" }, { "tr", "Süre" }, { "ar", "المدة" } } },
                { "PCal_AdminPanel_ThTimeActions", new Dictionary<string, string> { { "fa", "عملیات ساعت کاری" }, { "en", "Time Actions" }, { "tr", "Saat İşlemleri" }, { "ar", "إجراءات الوقت" } } },
                { "PCal_AdminPanel_NoRecordsInRange", new Dictionary<string, string> { { "fa", "هیچ سابقه کارکردی برای بازه انتخابی وجود ندارد." }, { "en", "No attendance history found for the selected range." }, { "tr", "Seçilen aralık için çalışma geçmişi bulunamadı." }, { "ar", "لا يوجد سجل دوام للفترة المحددة." } } },
                { "PCal_AdminPanel_LabelClockIn", new Dictionary<string, string> { { "fa", "ورود:" }, { "en", "In:" }, { "tr", "Giriş:" }, { "ar", "دخول:" } } },
                { "PCal_AdminPanel_LabelClockOut", new Dictionary<string, string> { { "fa", "خروج:" }, { "en", "Out:" }, { "tr", "Çıkış:" }, { "ar", "خروج:" } } },
                { "PCal_AdminPanel_ActiveStatusDot", new Dictionary<string, string> { { "fa", "در حال تردد" }, { "en", "Active" }, { "tr", "Aktif" }, { "ar", "نشط" } } },
                { "PCal_AdminPanel_BtnStopFast", new Dictionary<string, string> { { "fa", "توقف تردد سریع" }, { "en", "Stop Tracking" }, { "tr", "Geçişi Durdur" }, { "ar", "إيقاف التتبع" } } },
                { "PCal_AdminPanel_BtnEditTime", new Dictionary<string, string> { { "fa", "ویرایش ساعت" }, { "en", "Edit Time" }, { "tr", "Saati Düzenle" }, { "ar", "تعديل الوقت" } } },
                { "PCal_AdminPanel_BtnDeleteTime", new Dictionary<string, string> { { "fa", "حذف" }, { "en", "Delete" }, { "tr", "Sil" }, { "ar", "حذف" } } },
                { "PCal_AdminPanel_SelectEmpPrompt", new Dictionary<string, string> { { "fa", "جهت مشاهده سوابق کارکرد، کارمند مورد نظر را انتخاب کنید." }, { "en", "Select an employee to view attendance history." }, { "tr", "Çalışma geçmişini görüntülemek için bir personel seçin." }, { "ar", "اختر موظفاً لعرض سجل الدوام." } } },
                { "PCal_AdminPanel_ManualModalEditTitle", new Dictionary<string, string> { { "fa", "ویرایش ردیف ساعت کاری" }, { "en", "Edit Working Hours" }, { "tr", "Çalışma Saatini Düzenle" }, { "ar", "تعديل سجل ساعات العمل" } } },
                { "PCal_AdminPanel_ManualModalAddTitle", new Dictionary<string, string> { { "fa", "ثبت دستی ساعت تردد جدید" }, { "en", "Add Manual Attendance Record" }, { "tr", "Yeni Manuel Geçiş Kaydı Ekle" }, { "ar", "تسجيل حضور يدوي جديد" } } },
                { "PCal_AdminPanel_ManualInDateLabel", new Dictionary<string, string> { { "fa", "تاریخ روز ورود:" }, { "en", "Clock-In Date:" }, { "tr", "Giriş Tarihi:" }, { "ar", "تاريخ الدخول:" } } },
                { "PCal_AdminPanel_ManualInTimeLabel", new Dictionary<string, string> { { "fa", "ساعت ورود:" }, { "en", "Clock-In Time:" }, { "tr", "Giriş Saati:" }, { "ar", "وقت الدخول:" } } },
                { "PCal_AdminPanel_ManualOutDateLabel", new Dictionary<string, string> { { "fa", "تاریخ روز خروج (قفل همگام با ورود):" }, { "en", "Clock-Out Date (Synced with Clock-In):" }, { "tr", "Çıkış Tarihi (Giriş ile Senkronize):" }, { "ar", "تاريخ الخروج (متزامن مع الدخول):" } } },
                { "PCal_AdminPanel_ManualOutTimeLabel", new Dictionary<string, string> { { "fa", "ساعت خروج:" }, { "en", "Clock-Out Time:" }, { "tr", "Çıkış Saati:" }, { "ar", "وقت الخروج:" } } },
                { "PCal_AdminPanel_ManualApplyChangesBtn", new Dictionary<string, string> { { "fa", "اعمال تغییرات" }, { "en", "Apply Changes" }, { "tr", "Değişiklikleri Uygula" }, { "ar", "تطبيق التغييرات" } } },
                { "PCal_AdminPanel_ManualSubmitBtn", new Dictionary<string, string> { { "fa", "ثبت ساعت تردد" }, { "en", "Save Attendance" }, { "tr", "Geçiş Saatini Kaydet" }, { "ar", "حفظ وقت الحضور" } } },
                { "EmpFormAddTitle", new Dictionary<string, string> { { "fa", "کارمند جدید" }, { "en", "New Employee" }, { "tr", "Yeni Personel" }, { "ar", "موظف جديد" } } },
                { "EmpLabelName", new Dictionary<string, string> { { "fa", "نام و نام خانوادگی:" }, { "en", "Full Name:" }, { "tr", "Adı Soyadı:" }, { "ar", "الاسم الكامل:" } } },
                { "EmpLabelCode", new Dictionary<string, string> { { "fa", "کد ملی / شناسایی (حداقل ۱۰ رقم):" }, { "en", "National / ID Code (Min 10 Digits):" }, { "tr", "Kimlik / Personel No (En az 10 Haneli):" }, { "ar", "الرقم الوطني / التعريفي (١٠ أرقام على الأقل):" } } },
                { "EmpLabelPhone", new Dictionary<string, string> { { "fa", "شماره تماس:" }, { "en", "Phone Number:" }, { "tr", "Telefon Numarası:" }, { "ar", "رقم الهاتف:" } } },
                { "EmpLabelUpload", new Dictionary<string, string> { { "fa", "آپلود عکس:" }, { "en", "Upload Photo:" }, { "tr", "Fotoğraf Yükle:" }, { "ar", "رفع الصورة:" } } },
                { "EmpBtnSave", new Dictionary<string, string> { { "fa", "💾 ذخیره اطلاعات" }, { "en", "💾 Save Information" }, { "tr", "💾 Bilgileri Kaydet" }, { "ar", "💾 حفظ البيانات" } } },
                { "EmpBtnSaveChanges", new Dictionary<string, string> { { "fa", "💾 ثبت تغییرات" }, { "en", "Save Changes" }, { "tr", "💾 Değişiklikleri Kaydet" }, { "ar", "💾 حفظ التغييرات" } } },
                { "WorkSet", new Dictionary<string, string> { { "fa", "تنظیمات کاری" }, { "en", "Work Setting" }, { "tr", "Çalışma ayarları" }, { "ar", "إعدادات العمل" } } },
                { "PCal_AdminPanel_OvertimeAchievedTitle", new Dictionary<string, string> { { "fa", "دارای اضافه کاری محقق شده" }, { "en", "Achieved overtime" }, { "tr", "Gerçekleşen fazla mesai mevcut" }, { "ar", "يوجد عمل إضافي محقق" } } },
                { "PCal_AdminPanel_ActiveOvertimeTitle", new Dictionary<string, string> { { "fa", "حضور زنده در محدوده اضافه کاری" }, { "en", "Active presence in overtime zone" }, { "tr", "Fazla mesai aralığında aktif bulunma" }, { "ar", "تواجد نشط ضمن نطاق العمل الإضافي" } } },
                { "PCal_Emp_HideFromKioskLabel", new Dictionary<string, string> { { "fa", "عدم نمایش این پرسنل در صفحه کیوسک (مرخصی طولانی / غیبت)" }, { "en", "Hide this employee from Kiosk screen (Long leave / Absent)" }, { "tr", "Bu personeli Kiosk ekranında gizle (Uzun izin / Devamsızlık)" }, { "ar", "إخفاء هذا الموظف من شاشة الحضور (إجازة طويلة / غياب)" } } },
                { "PCal_Emp_BadgeHiddenKiosk", new Dictionary<string, string> { { "fa", "مخفی در کیوسک" }, { "en", "Hidden in Kiosk" }, { "tr", "Kiosk'ta Gizli" }, { "ar", "مخفي في الجهاز" } } },
                // تاریخچه و صفحه مدیریت اعلانات
                { "PCal_Toast_MinutesAgo", new Dictionary<string, string> { { "fa", "{0} دقیقه پیش" }, { "en", "{0} mins ago" }, { "tr", "{0} dakika önce" }, { "ar", "منذ {0} دقيقة" } } },
                { "PCal_Toast_HoursAgo", new Dictionary<string, string> { { "fa", "{0} ساعت پیش" }, { "en", "{0} hrs ago" }, { "tr", "{0} saat önce" }, { "ar", "منذ {0} ساعة" } } },
                { "PCal_AdminNotif_BtnTitle", new Dictionary<string, string> { { "fa", "اعلانات جاری سیستم" }, { "en", "System Live Notifications" }, { "tr", "Sistem Canlı Bildirimleri" }, { "ar", "إشعارات النظام المباشرة" } } },
                { "PCal_AdminNotif_ModalTitle", new Dictionary<string, string> { { "fa", "تاریخچه و اعلانات جاری عملیات مدیران و کیوسک" }, { "en", "Admins & Kiosk Live Operations Log" }, { "tr", "Yönetici ve Kiosk Canlı İşlem Bildirimleri" }, { "ar", "سجل وإشعارات عمليات المدراء وجهاز الحضور" } } },
                { "PCal_AdminNotif_ThAdmin", new Dictionary<string, string> { { "fa", "مدیر عامل / منبع" }, { "en", "Admin / Source" }, { "tr", "Yönetici / Kaynak" }, { "ar", "المدير / المصدر" } } },
                { "PCal_AdminNotif_ThEmployee", new Dictionary<string, string> { { "fa", "مشخصات پرسنل" }, { "en", "Employee Details" }, { "tr", "Personel Bilgileri" }, { "ar", "بيانات الموظف" } } },
                { "PCal_AdminNotif_ThMessage", new Dictionary<string, string> { { "fa", "شرح رویداد" }, { "en", "Event Description" }, { "tr", "Olay Açıklaması" }, { "ar", "وصف الحدث" } } },
                { "PCal_AdminNotif_ThTime", new Dictionary<string, string> { { "fa", "زمان ثبت" }, { "en", "Timestamp" }, { "tr", "Kayıt Zamanı" }, { "ar", "وقت التسجيل" } } },
                { "PCal_AdminNotif_NoLogs", new Dictionary<string, string> { { "fa", "هیچ اعلانی در سیستم ثبت نشده است." }, { "en", "No notifications recorded in the system." }, { "tr", "Sistemde kayıtlı bildirim bulunamadı." }, { "ar", "لا توجد أي إشعارات مسجلة في النظام." } } },
                { "PCal_AdminNotif_ClearAllBtn", new Dictionary<string, string> { { "fa", "پاکسازی تاریخچه اعلانات" }, { "en", "Clear Notification Logs" }, { "tr", "Bildirim Geçmişini Temizle" }, { "ar", "مسح سجل الإشعارات" } } },
                { "PCal_NotifCat_All", new Dictionary<string, string> { { "fa", "همه بخش‌ها" }, { "en", "All Sections" }, { "tr", "Tüm Bölümler" }, { "ar", "جميع الأقسام" } } },
                { "PCal_NotifCat_Kiosk", new Dictionary<string, string> { { "fa", "صفحه کیوسک" }, { "en", "Kiosk Screen" }, { "tr", "Kiosk Ekranı" }, { "ar", "شاشة الحضور" } } },
                { "PCal_NotifCat_AdminPanel", new Dictionary<string, string> { { "fa", "پنل پرسنل" }, { "en", "Personnel Panel" }, { "tr", "Personel Paneli" }, { "ar", "لوحة الموظفين" } } },
                { "PCal_NotifCat_ManageAdmins", new Dictionary<string, string> { { "fa", "مدیریت مدیران" }, { "en", "Manage Admins" }, { "tr", "Yönetici Yönetimi" }, { "ar", "إدارة المدراء" } } },
                { "PCal_Notif_RefreshBtn", new Dictionary<string, string> { { "fa", "بروزرسانی" }, { "en", "Refresh" }, { "tr", "Yenile" }, { "ar", "تحديث" } } },
                { "PCal_Notif_ResetFiltersBtn", new Dictionary<string, string> { { "fa", "پاکسازی فیلترها" }, { "en", "Reset Filters" }, { "tr", "Filtreleri Temizle" }, { "ar", "مسح الفلاتر" } } },
                { "PCal_Notif_SearchPlaceholder", new Dictionary<string, string> { { "fa", "نام کارمند، ادمین یا متن پیام..." }, { "en", "Employee, admin or message..." }, { "tr", "Personel, yönetici veya mesaj..." }, { "ar", "اسم الموظف، المدير أو نص الرسالة..." } } },
                { "PCal_Notif_FilterCategory", new Dictionary<string, string> { { "fa", "بخش (Category)" }, { "en", "Category" }, { "tr", "Bölüm (Category)" }, { "ar", "القسم (Category)" } } },
                { "PCal_Notif_FilterSubCategory", new Dictionary<string, string> { { "fa", "موضوع (SubCategory)" }, { "en", "SubCategory" }, { "tr", "Alt Konu" }, { "ar", "الموضوع (SubCategory)" } } },
                { "PCal_Notif_FilterType", new Dictionary<string, string> { { "fa", "وضعیت" }, { "en", "Type" }, { "tr", "Durum" }, { "ar", "الحالة" } } },
                { "PCal_Notif_FilterDate", new Dictionary<string, string> { { "fa", "تاریخ ثبت" }, { "en", "Created Date" }, { "tr", "Kayıt Tarihi" }, { "ar", "تاريخ التسجيل" } } },
                { "PCal_Notif_FilterTimeRange", new Dictionary<string, string> { { "fa", "بازه ساعتی" }, { "en", "Time Range" }, { "tr", "Saat Aralığı" }, { "ar", "النطاق الزمني" } } },
                { "PCal_Notif_ClearConfirmMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل تمام سوابق و تاریخچه اعلانات سیستم مطمئن هستید؟" }, { "en", "Are you sure you want to permanently delete all notification logs?" }, { "tr", "Tüm sistem bildirim geçmişini kalıcı olarak silmek istediğinizden emin misiniz?" }, { "ar", "هل أنت متأكد من الحذف النهائي لجميع سجلات الإشعارات؟" } } },
                { "PCal_Notif_ClearConfirmSub", new Dictionary<string, string> { { "fa", "توجه: پس از پاکسازی، امکان بازیابی گزارش‌های ثبت‌شده وجود نخواهد داشت." }, { "en", "Note: Once cleared, recorded logs cannot be recovered." }, { "tr", "Not: Temizlendikten sonra kayıtlı günlükler geri getirilemez." }, { "ar", "تنبيه: بعد المسح، لا يمكن استعادة التقارير المسجلة." } } },
                // اعلان‌های امنیتی نشست، توکن و کیوسک
                { "PCal_Kiosk_AdminOnlineLog", new Dictionary<string, string> { { "fa", "صفحه کیوسک تردد پرسنل با حساب مدیر {0} فعال و آنلاین شد." }, { "en", "Personnel attendance kiosk activated online under admin {0}." }, { "tr", "Personel geçiş kiosku {0} yöneticisi hesabıyla aktif edildi." }, { "ar", "تم تفعيل شاشة حضور الموظفين بحساب المدير {0}." } } },
                // اعلان‌های تغییر وضعیت نمایش در کیوسک و تنظیمات
                { "PCal_AdminPanel_EmpHiddenFromKioskLog", new Dictionary<string, string> { { "fa", "نمایش کارمند {0} در صفحه کیوسک توسط مدیر غیرفعال (مخفی) شد." }, { "en", "Employee {0} was hidden from the Kiosk screen by admin." }, { "tr", "{0} adlı personelin Kiosk ekranında görünürlüğü yönetici tarafından kapatıldı." }, { "ar", "تم إخفاء الموظف {0} من شاشة الحضور بواسطة المدير." } } },
                { "PCal_AdminPanel_EmpShownInKioskLog", new Dictionary<string, string> { { "fa", "نمایش کارمند {0} در صفحه کیوسک مجدداً توسط مدیر فعال شد." }, { "en", "Employee {0} visibility on the Kiosk screen was restored by admin." }, { "tr", "{0} adlı personelin Kiosk ekranında görünürlüğü yeniden açıldı." }, { "ar", "تمت إعادة إظهار الموظف {0} في شاشة الحضور بواسطة المدير." } } },
                { "PCal_AdminPanel_SettingsSavedSuccess", new Dictionary<string, string> { { "fa", "تنظیمات قوانین کاری و محاسبات سرور با موفقیت به‌روزرسانی و ذخیره شد." }, { "en", "Work rules and server calculation settings were updated and saved." }, { "tr", "Çalışma kuralları ve sunucu hesaplama ayarları güncellendi ve kaydedildi." }, { "ar", "تم تحديث وحفظ إعدادات قوانين العمل وحسابات الخادم بنجاح." } } },

                // اعلان‌های امنیتی نشست، ورود، خروج، انقضا و محدودسازی توکن
                { "PCal_Security_Log_AdminOnline", new Dictionary<string, string> { { "fa", "مدیر {0} با نشست فعال وارد برنامه شد و آنلاین گردید." }, { "en", "Admin {0} connected with an active session and is now online." }, { "tr", "Yönetici {0} aktif oturum ile bağlandı ve çevrimiçi oldu." }, { "ar", "اتصل المدير {0} بجلسة نشطة وأصبح متصلاً الآن." } } },
                { "PCal_Security_Log_LoginSuccess", new Dictionary<string, string> { { "fa", "مدیر {0} با موفقیت وارد پنل سیستم شد." }, { "en", "Admin {0} logged into the system panel." }, { "tr", "Yönetici {0} sistem paneline giriş yaptı." }, { "ar", "قام المدير {0} بتسجيل الدخول إلى لوحة النظام." } } },
                { "PCal_Security_Log_LoginFailed", new Dictionary<string, string> { { "fa", "تلاش ناموفق برای ورود به سیستم با نام کاربری {0} ثبت شد." }, { "en", "Failed login attempt recorded for username {0}." }, { "tr", "{0} kullanıcı adı ile başarısız giriş denemesi kaydedildi." }, { "ar", "تم تسجيل محاولة دخول فاشلة باسم المستخدم {0}." } } },
                { "PCal_Security_Log_LoginFrozenAttempt", new Dictionary<string, string> { { "fa", "تلاش ورود حساب مسدودشده {0} توسط سیستم مسدود گردید." }, { "en", "Login attempt by frozen account {0} was blocked." }, { "tr", "Dondurulmuş {0} hesabının giriş denemesi engellendi." }, { "ar", "تم حظر محاولة الدخول للحساب المجمد {0}." } } },
                { "PCal_Security_Log_LogoutSuccess", new Dictionary<string, string> { { "fa", "مدیر {0} از پنل مدیریت خارج شد و نشست او پایان یافت." }, { "en", "Admin {0} logged out and ended the session." }, { "tr", "Yönetici {0} panelden çıkış yaptı ve oturumu sonlandırdı." }, { "ar", "قام المدير {0} بتسجيل الخروج وإنهاء الجلسة." } } },
                { "PCal_Security_Log_TokenExpired", new Dictionary<string, string> { { "fa", "اعتبار زمانی توکن نشست مدیر {0} منقضی شد و دسترسی قطع گردید." }, { "en", "Session token for admin {0} expired and access was terminated." }, { "tr", "Yönetici {0} oturum anahtarının süresi doldu ve erişim kesildi." }, { "ar", "انتهت صلاحية رمز جلسة المدير {0} وتم قطع الوصول." } } },
                { "PCal_Security_Log_TokenRestricted", new Dictionary<string, string> { { "fa", "توکن نشست مدیر {0} به دلیل مسدودسازی یا ابطال محدود شد." }, { "en", "Session token for admin {0} was restricted due to freeze or revocation." }, { "tr", "Yönetici {0} oturum anahtarı dondurma veya iptal nedeniyle kısıtlandı." }, { "ar", "تم تقييد رمز جلسة المدير {0} بسبب التجميد أو الإلغاء." } } },

                // اعلان‌های جابه‌جایی و بازدید صفحات توسط مدیران
                { "PCal_Navigation_PageVisitLog", new Dictionary<string, string> { { "fa", "مدیر {0} وارد صفحه {1} شد." }, { "en", "Admin {0} navigated to {1}." }, { "tr", "Yönetici {0}, {1} sayfasına geçiş yaptı." }, { "ar", "انتقل المدير {0} إلى صفحة {1}." } } },
                { "PCal_System_AutoLogoutRetryScheduled", new Dictionary<string, string> { { "fa", "⚠️ خطا در خروج خودکار سیستم ({0}). عملیات تا ۵ دقیقه دیگر مجدداً تکرار خواهد شد." }, { "en", "⚠️ System auto-logout failed ({0}). The operation will retry once in 5 minutes." }, { "tr", "⚠️ Sistem otomatik çıkışı başarısız oldu ({0}). İşlem 5 dakika içinde bir kez daha tekrarlanacak." }, { "ar", "⚠️ فشل الخروج التلقائي للنظام ({0}). ستتم إعادة المحاولة مرة واحدة بعد ٥ دقائق." } } },
                { "PCal_System_AutoLogoutRetryFailed", new Dictionary<string, string> { { "fa", "❌ تلاش مجدد خروج خودکار سیستم نیز ناموفق بود ({0}). لطفاً وضعیت دیتابیس را بررسی نمایید." }, { "en", "❌ System auto-logout retry also failed ({0}). Please check the database status." }, { "tr", "❌ Sistem otomatik çıkış yeniden denemesi de başarısız oldu ({0}). Lütfen veritabanını kontrol edin." }, { "ar", "❌ فشلت إعادة محاولة الخروج التلقائي للنظام أيضاً ({0}). يرجى التحقق من قاعدة البيانات." } } },
                // اعلان خطاهای فنی و سیستمی بک‌اند
                { "PCal_System_BackendErrorLog", new Dictionary<string, string> { { "fa", "🚨 خطای سیستمی در بخش {0}: {1}" }, { "en", "🚨 System backend error in {0}: {1}" }, { "tr", "🚨 {0} bölümünde sistem sunucu hatası: {1}" }, { "ar", "🚨 خطأ تقني في النظام ضمن القسم {0}: {1}" } } },
                // پیام‌های بازخورد صفحه ورود ادمین
                { "PCal_Login_InvalidCredentials", new Dictionary<string, string> { { "fa", "❌ نام کاربری یا رمز عبور اشتباه است!" }, { "en", "❌ Invalid username or password!" }, { "tr", "❌ Kullanıcı adı veya şifre hatalı!" }, { "ar", "❌ اسم المستخدم أو كلمة المرور غير صحيحة!" } } },
                { "PCal_Login_AccountFrozen", new Dictionary<string, string> { { "fa", "❄️ حساب کاربری شما فریز شده است و اجازه ورود ندارید!" }, { "en", "❄️ Your account is frozen and login is denied!" }, { "tr", "❄️ Hesabınız dondurulmuştur ve giriş izniniz yoktur!" }, { "ar", "❄️ حسابك مجمد ولا يمكنك تسجيل الدخول!" } } },
                { "PCal_Login_SuccessFeedback", new Dictionary<string, string> { { "fa", "✅ ورود با موفقیت انجام شد." }, { "en", "✅ Login successful." }, { "tr", "✅ Giriş başarıyla tamamlandı." }, { "ar", "✅ تم تسجيل الدخول بنجاح." } } },
                { "PCal_Login_SystemError", new Dictionary<string, string> { { "fa", "❌ خطایی در فرآیند ورود رخ داد!" }, { "en", "❌ An error occurred during login!" }, { "tr", "❌ Giriş işlemi sırasında bir hata oluştu!" }, { "ar", "❌ حدث خطأ أثناء عملية تسجيل الدخول!" } } },


            };

            if (translations.ContainsKey(key) && translations[key].ContainsKey(CurrentLanguage))
                return translations[key][CurrentLanguage];

            return key;
        }
        // تبدیل تاریخ میلادی دیتابیس به رشته تاریخ شمسی، قمری یا میلادی بر اساس تنظیمات فعال
        public string GetDynamicFormattedDate(DateTime? dateTime)
        {
            if (!dateTime.HasValue) return "-";
            DateTime date = dateTime.Value;

            if (CalendarType == "Persian")
            {
                PersianCalendar pc = new PersianCalendar();
                return $"{pc.GetYear(date)}/{pc.GetMonth(date):D2}/{pc.GetDayOfMonth(date):D2}";
            }
            if (CalendarType == "Hijri")
            {
                HijriCalendar hc = new HijriCalendar();
                return $"{hc.GetYear(date)}/{hc.GetMonth(date):D2}/{hc.GetDayOfMonth(date):D2}";
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
            if (CurrentLanguage == "ar")
            {
                return day switch
                {
                    DayOfWeek.Saturday => "السبت",
                    DayOfWeek.Sunday => "الأحد",
                    DayOfWeek.Monday => "الاثنين",
                    DayOfWeek.Tuesday => "الثلاثاء",
                    DayOfWeek.Wednesday => "الأربعاء",
                    DayOfWeek.Thursday => "الخميس",
                    DayOfWeek.Friday => "الجمعة",
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

        // تولید عنوان ماه و سال برای فیلتر کارنامه در هر سه تقویم شمسی، قمری و میلادی
        public string GetDynamicMonthText(int year, int month)
        {
            if (CalendarType == "Persian")
            {
                string[] pMonths = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
                return $"{pMonths[month - 1]} {year}";
            }
            if (CalendarType == "Hijri")
            {
                string[] hMonthsArFa = { "محرم", "صفر", "ربيع الأول", "ربيع الثاني", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة" };
                string[] hMonthsEn = { "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani", "Jumada al-Ula", "Jumada al-Akhirah", "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah" };
                string[] hMonthsTr = { "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir", "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban", "Ramazan", "Şevval", "Zilkade", "Zilhicce" };

                if (CurrentLanguage == "en") return $"{hMonthsEn[month - 1]} {year}";
                if (CurrentLanguage == "tr") return $"{hMonthsTr[month - 1]} {year}";
                return $"{hMonthsArFa[month - 1]} {year}";
            }

            DateTime dt = new DateTime(year, month, 1);
            if (CurrentLanguage == "tr")
            {
                string[] trMonths = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
                return $"{trMonths[month - 1]} {year}";
            }
            if (CurrentLanguage == "ar")
            {
                string[] arMonths = { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
                return $"{arMonths[month - 1]} {year}";
            }
            return dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        }

        // تولید لیست سال‌های قابل انتخاب در فرم ثبت دستی بر اساس نوع تقویم
        public List<string> GetFormYears()
        {
            if (CalendarType == "Persian")
                return new List<string> { "1404", "1405", "1406", "1407" };

            if (CalendarType == "Hijri")
                return new List<string> { "1447", "1448", "1449", "1450" };

            return new List<string> { "2025", "2026", "2027", "2028" };
        }

        // تولید لیست ۱۲ ماه سال برای دراپ‌داون فرم ثبت دستی بر اساس تقویم و زبان فعال
        public List<KeyValuePair<string, string>> GetFormMonths()
        {
            var list = new List<KeyValuePair<string, string>>();

            if (CalendarType == "Persian")
            {
                string[] pMonths = { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور", "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };
                for (int i = 1; i <= 12; i++)
                    list.Add(new KeyValuePair<string, string>(i.ToString("D2"), pMonths[i - 1]));
            }
            else if (CalendarType == "Hijri")
            {
                string[] hMonthsArFa = { "محرم", "صفر", "ربيع الأول", "ربيع الثاني", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة" };
                string[] hMonthsEn = { "Muharram", "Safar", "Rabi' al-Awwal", "Rabi' al-Thani", "Jumada al-Ula", "Jumada al-Akhirah", "Rajab", "Sha'ban", "Ramadan", "Shawwal", "Dhu al-Qi'dah", "Dhu al-Hijjah" };
                string[] hMonthsTr = { "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir", "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban", "Ramazan", "Şevval", "Zilkade", "Zilhicce" };

                string[] targetArr = CurrentLanguage == "en" ? hMonthsEn : (CurrentLanguage == "tr" ? hMonthsTr : hMonthsArFa);
                for (int i = 1; i <= 12; i++)
                    list.Add(new KeyValuePair<string, string>(i.ToString("D2"), targetArr[i - 1]));
            }
            else
            {
                if (CurrentLanguage == "tr")
                {
                    string[] trMonths = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
                    for (int i = 1; i <= 12; i++)
                        list.Add(new KeyValuePair<string, string>(i.ToString("D2"), trMonths[i - 1]));
                }
                else if (CurrentLanguage == "ar")
                {
                    string[] arMonths = { "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };
                    for (int i = 1; i <= 12; i++)
                        list.Add(new KeyValuePair<string, string>(i.ToString("D2"), arMonths[i - 1]));
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

        // همگام‌سازی فیلدهای سال، ماه و روز فرم ثبت دستی با تاریخ انتخاب‌شده در هر سه تقویم
        public void SyncFormCalendarFields(DateTime inDt, DateTime outDt)
        {
            if (CalendarType == "Persian")
            {
                PersianCalendar pc = new PersianCalendar();
                ManualInYear = pc.GetYear(inDt).ToString();
                ManualInMonth = pc.GetMonth(inDt).ToString("D2");
                ManualInDay = pc.GetDayOfMonth(inDt).ToString("D2");
                ManualOutYear = pc.GetYear(outDt).ToString();
                ManualOutMonth = pc.GetMonth(outDt).ToString("D2");
                ManualOutDay = pc.GetDayOfMonth(outDt).ToString("D2");
            }
            else if (CalendarType == "Hijri")
            {
                HijriCalendar hc = new HijriCalendar();
                ManualInYear = hc.GetYear(inDt).ToString();
                ManualInMonth = hc.GetMonth(inDt).ToString("D2");
                ManualInDay = hc.GetDayOfMonth(inDt).ToString("D2");
                ManualOutYear = hc.GetYear(outDt).ToString();
                ManualOutMonth = hc.GetMonth(outDt).ToString("D2");
                ManualOutDay = hc.GetDayOfMonth(outDt).ToString("D2");
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

        // تبدیل مقادیر سال، ماه و روز انتخاب‌شده در فرم به شیء استاندارد DateTime میلادی جهت ذخیره در دیتابیس
        public DateTime ParseFormFieldsToDateTime(string year, string month, string day, string timeStr)
        {
            int y = int.Parse(year);
            int m = int.Parse(month);
            int d = int.Parse(day);
            var timeParts = timeStr.Split(':');
            int hour = int.Parse(timeParts[0]);
            int minute = int.Parse(timeParts[1]);

            if (CalendarType == "Persian")
            {
                PersianCalendar pc = new PersianCalendar();
                return pc.ToDateTime(y, m, d, hour, minute, 0, 0);
            }
            if (CalendarType == "Hijri")
            {
                HijriCalendar hc = new HijriCalendar();
                return hc.ToDateTime(y, m, d, hour, minute, 0, 0);
            }
            return new DateTime(y, m, d, hour, minute, 0);
        }
    }
}
