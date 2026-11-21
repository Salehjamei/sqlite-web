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
        //لایوت هدر سایت
        { "KioskBtn", new Dictionary<string, string> { { "fa", "کیوسک تردد" }, { "en", "Attendance Kiosk" }, { "tr", "Geçiş Kiosku" } } },
        { "PersonnelManagementBtn", new Dictionary<string, string> { { "fa", "مدیریت پرسنل" }, { "en", "Personnel Management" }, { "tr", "Personel Yönetimi" } } },
        { "LogoutBtn", new Dictionary<string, string> { { "fa", "خروج از پنل" }, { "en", "Logout Panel" }, { "tr", "Panelden Çıkış" } } },
        { "AdminDashboard", new Dictionary<string, string> { { "fa", "داشبورد مدیریت" }, { "en", "Admin Dashboard" }, { "tr", "Yönetim Paneli" } } },
        { "UserPermissionsBtn", new Dictionary<string, string> { { "fa", "سطوح دسترسی" }, { "en", "User Permissions" }, { "tr", "Kullanıcı Yetkileri" } } },


        // عناوین ریشه صفحه و جدول اصلی پرسنل
        { "AppTitle", new Dictionary<string, string> { { "fa", "مدیریت پرسنل و کارکردها" }, { "en", "Personnel Management" }, { "tr", "Personel Yönetimi" } } },
        { "ThPhoto", new Dictionary<string, string> { { "fa", "عکس" }, { "en", "Photo" }, { "tr", "Fotoğraf" } } },
        { "ThName", new Dictionary<string, string> { { "fa", "نام و نام خانوادگی" }, { "en", "Full Name" }, { "tr", "Adı Soyadı" } } },
        { "ThCode", new Dictionary<string, string> { { "fa", "کد ملی" }, { "en", "National Code" }, { "tr", "T.C. Kimlik No" } } },
        { "ThPhone", new Dictionary<string, string> { { "fa", "شماره تماس" }, { "en", "Phone Number" }, { "tr", "Telefon No" } } },
        { "ThReport", new Dictionary<string, string> { { "fa", "کارنامه تفکیکی" }, { "en", "Detailed Report" }, { "tr", "Detaylı Karne" } } },
        { "ThQuickAtt", new Dictionary<string, string> { { "fa", "وضعیت / تردد سریع" }, { "en", "Status / Quick Tracking" }, { "tr", "Durum / Hızlı Geçiş" } } },
        { "ThActions", new Dictionary<string, string> { { "fa", "عملیات پرونده" }, { "en", "Actions" }, { "tr", "İşlemler" } } },
        //اعلان های پنل پرسنل، افزودن ویرایش ورود و خروج
        { "PCal_AdminPanel_EmpEditSuccess", new Dictionary<string, string> { { "fa", "مشخصات کارمند {0} با موفقیت ویرایش شد." }, { "en", "Employee {0} details updated successfully." }, { "tr", "{0} adlı personelin bilgileri başarıyla güncellendi." } } },
        { "PCal_AdminPanel_EmpAddSuccess", new Dictionary<string, string> { { "fa", "کارمند جدید با نام {0} با موفقیت در سیستم ثبت شد." }, { "en", "New employee {0} added successfully." }, { "tr", "{0} adlı yeni personel sisteme başarıyla eklendi." } } },
        { "PCal_AdminPanel_EmpDeletedMsg", new Dictionary<string, string> { { "fa", "پرونده پرسنلی کارمند {0} با موفقیت حذف شد." }, { "en", "Employee {0} record deleted successfully." }, { "tr", "{0} adlı personelin dosyası silindi." } } },
        { "PCal_AdminPanel_FastInSuccess", new Dictionary<string, string> { { "fa", "ورود سریع برای کارمند {0} با موفقیت ثبت شد." }, { "en", "Quick clock-in recorded for {0}." }, { "tr", "{0} için hızlı giriş kaydı oluşturuldu." } } },
        { "PCal_AdminPanel_FastOutSuccess", new Dictionary<string, string> { { "fa", "خروج سریع برای کارمند {0} با موفقیت ثبت شد." }, { "en", "Quick clock-out recorded for {0}." }, { "tr", "{0} için hızlı çıkış kaydı oluşturuldu." } } },
        //اعلان بخش کارنامه
        { "PCal_AdminPanel_AttDeletedMsg", new Dictionary<string, string> { { "fa", "رکورد تردد مربوط به کارمند {0} با موفقیت ابطال شد." }, { "en", "Attendance record for {0} successfully deleted." }, { "tr", "{0} adlı personelin geçiş kaydı başarıyla iptal edildi." } } },
        { "PCal_AdminPanel_ManualAddSuccess", new Dictionary<string, string> { { "fa", "تردد دستی جدید برای کارمند {0} با موفقیت ثبت شد." }, { "en", "Manual attendance record added for {0}." }, { "tr", "{0} için yeni manuel geçiş kaydı eklendi." } } },
        { "PCal_AdminPanel_ManualEditSuccess", new Dictionary<string, string> { { "fa", "ساعت کاری کارمند {0} با موفقیت ویرایش شد." }, { "en", "Working hours updated for {0}." }, { "tr", "{0} adlı personelin çalışma saatleri güncellendi." } } },
        
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
        { "DeleteMsgAdmin", new Dictionary<string, string> { { "fa", "آیا از حذف ادمین مطمئن هستید؟" }, { "en", "Are you sure about removing the admin?" }, { "tr", "Yöneticiyi silmek istediğinizden emin misiniz?" } } },
        { "DeleteSubMsg", new Dictionary<string, string> { { "fa", "توجه: کلیه سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد." }, { "en", "Note: All attendance history will be permanently deleted and cannot be recovered." }, { "tr", "Not: Tüm giriş-çıkış geçmişi kalıcı olarak silinecek ve geri alınamayacaktır." } } },
        { "DeleteConfirmBtn", new Dictionary<string, string> { { "fa", "بله، حذف شود" }, { "en", "Yes, Delete" }, { "tr", "Evet, Silinsin" } } },
        { "kancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" } } },
         // متون کمکی پاپ‌آپ حذف پرسنل
        { "DeleteSelectedItem", new Dictionary<string, string> { { "fa", "آیتم انتخابی:" }, { "en", "Selected Item:" }, { "tr", "Seçilen Öğe:" } } },
        // ==========================================
        // 🗑️ PCalendar - Admin Deletion Confirmation Modal
        // --- Modal Layout & Structural Text ---
        { "PCal_AdminDelete_Title", new Dictionary<string, string> { { "fa", "تاییدیه حذف قطعی اطلاعات مدیر سیستم" }, { "en", "Confirmation of Permanent System Admin Deletion" }, { "tr", "Sistem Yöneticisi Bilgilerinin Kalıcı Olarak Silinmesi Onayı" } } },
        { "PCal_AdminDelete_DefaultTargetName", new Dictionary<string, string> { { "fa", "مدیر انتخاب شده" }, { "en", "Selected Admin" }, { "tr", "Seçilen Yönetici" } } },
        { "PCal_AdminDelete_SubMessage", new Dictionary<string, string> { { "fa", "توجه: با تایید نهایی حذف، کلیه دسترسی‌ها، مجوزها و سشن‌های فعال این ادمین فوراً ابطال و پاک خواهد شد." }, { "en", "Note: Upon final deletion approval, all access rights, permissions, and active sessions of this admin will be immediately revoked and wiped." }, { "tr", "Not: Nihai silme onayı ile birlikte, bu yöneticinin tüm erişim hakları, izinleri ve aktif oturumları derhal iptal edilecek ve silinecektir." } } },

        // دکمه‌های عملیاتی تردد سریع جدول ریشه صفحه
        { "BtnClockIn", new Dictionary<string, string> { { "fa", "ثبت ورود" }, { "en", "Clock In" }, { "tr", "Giriş" } } },
        { "BtnClockOut", new Dictionary<string, string> { { "fa", "ثبت خروج" }, { "en", "Clock Out" }, { "tr", "Çıkış" } } },

        { "BtnReportView", new Dictionary<string, string> { { "fa", "کارنامه" }, { "en", "Record" }, { "tr", "İş geçmişi" } } },
        { "TitleTAble", new Dictionary<string, string> { { "fa", "مدیریت پرسنل" }, { "en", "Personnel Management" }, { "tr", "Personel yönetimi" } } },
        { "SearchBar", new Dictionary<string, string> { { "fa", "نام یا کد ملی..." }, { "en", "Name or National ID..." }, { "tr", "İsim veya Kimlik No..." } } },
        { "Search", new Dictionary<string, string> { { "fa", "جستجو" }, { "en", "Search" }, { "tr", "Aramak" } } },
        { "session", new Dictionary<string, string> { { "fa", "نام یا کد ملی..." }, { "en", "Your session has expired or you do not have access. Redirecting to the login page..." }, { "tr", "Oturumunuzun süresi doldu veya erişiminiz yok. Giriş sayfasına yönlendiriliyorsunuz..." } } },
        
       
        //صفحه ی مدیریت مدیران
        // اعلان‌های صفحه مدیریت مدیران و سطوح دسترسی (ManageAdmins)
        { "PCal_ManageAdmins_AddSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر جدید با نام {0} با موفقیت در سرور تعریف و ثبت گردید." }, { "en", "New system admin account {0} has been successfully registered." }, { "tr", "{0} adlı yeni sistem yöneticisi hesabı başarıyla oluşturuldu." } } },
        { "PCal_ManageAdmins_EditSuccess", new Dictionary<string, string> { { "fa", "مشخصات و مجوزهای حساب کاربری مدیر با نام {0} با موفقیت اصلاح شد." }, { "en", "Permissions and details for admin {0} have been updated." }, { "tr", "{0} adlı yöneticinin bilgileri ve yetkileri güncellendi." } } },
        { "PCal_ManageAdmins_FreezeSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر با نام {0} مسدود (فریز) شد و سشن‌های او ابطال گردید." }, { "en", "Admin account {0} has been frozen and active sessions revoked." }, { "tr", "{0} adlı yönetici hesabı donduruldu ve aktif oturumları iptal edildi." } } },
        { "PCal_ManageAdmins_UnfreezeSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر با نام {0} با موفقیت فعال و رفع مسدودیت شد." }, { "en", "Admin account {0} has been unfrozen and reactivated." }, { "tr", "{0} adlı yönetici hesabının engeli kaldırıldı ve aktifleştirildi." } } },
        { "PCal_ManageAdmins_RevokeTokenSuccess", new Dictionary<string, string> { { "fa", "توکن زنده عملیاتی مدیر با نام {0} ابطال شد و کاربر فوراً از برنامه خارج گردید." }, { "en", "Active operational token for admin {0} was revoked." }, { "tr", "{0} adlı yöneticinin aktif oturum anahtarı iptal edildi." } } },
        { "PCal_ManagAdmins_DeleteSuccess", new Dictionary<string, string> { { "fa", "حساب کاربری مدیر با نام {0} به همراه کلیه سوابق و مجوزهای او با موفقیت حذف گردید." }, { "en", "Admin account {0} and all associated permissions were permanently deleted." }, { "tr", "{0} adlı yönetici hesabı ve tüm yetkileri kalıcı olarak silindi." } } },
        { "PCal_ManageAdmins_LastSuperAdminError", new Dictionary<string, string> { { "fa", "خطا: سیستم باید حداقل دارای یک سوپر ادمین باشد. امکان حذف رتبه تنها سوپر ادمین ({0}) وجود ندارد!" }, { "en", "Error: The system must have at least one Super Admin. Cannot demote the only Super Admin ({0})!" }, { "tr", "Hata: Sistemde en az bir Süper Yönetici bulunmalıdır. Tek Süper Yöneticinin ({0}) yetkisi kaldırılamaz!" } } },
        // --- Empty State Messages ---
        { "PCal_AdminModal_NoRecordsFound", new Dictionary<string, string> { { "fa", "هیچ رکوردی در جدول مدیران سرور وجود ندارد." }, { "en", "No records found in the server admins table." }, { "tr", "Sunucu yöneticileri tablosunda hiçbir kayıt bulunamadı." } } },
        // --- Error & Access Restriction Messages ---
        { "PCal_AdminModal_AccessDeniedError", new Dictionary<string, string> { { "fa", "🛑 خطای دسترسی: حساب کاربری شما فاقد مجوزهای ارشد جهت مدیریت سطوح دسترسی مدیران است!" }, { "en", "🛑 Access Denied: Your account lacks senior permissions to manage admin access levels!" }, { "tr", "🛑 Erişim Reddedildi: Hesabınız, yönetici erişim seviyelerini yönetmek için kıdemli izinlere sahip değil!" } } },
                // اعلان خروج خودکار سیستم در پایان روز
        { "PCal_System_AutoLogoutSuccess", new Dictionary<string, string> { { "fa", "خروج خودکار سیستم برای کارمند {0} در ساعت {1} ثبت شد." }, { "en", "System automatic clock-out recorded for {0} at {1}." }, { "tr", "{0} adlı personel için saat {1} itibarıyla sistem otomatik çıkışı kaydedildi." } } },

        // 🟢 واژگان اختصاصی صفحه مدیریت سطوح دسترسی و مدیران
        { "AccessPageTitle", new Dictionary<string, string> { { "fa", "مدیریت سطوح دسترسی و مدیران" }, { "en", "Access Levels & Admins Management" }, { "tr", "Yetkilendirme Seviyeleri & Yönetici Yönetimi" } } },
        { "NewAdminBtn", new Dictionary<string, string> { { "fa", "➕ مدیر جدید" }, { "en", "➕ New Admin" }, { "tr", "➕ Yeni Yönetici" } } },
        { "SearchAdminPlaceholder", new Dictionary<string, string> { { "fa", "جستجوی نام کاربری..." }, { "en", "Search username..." }, { "tr", "Kullanıcı adı ara..." } } },
        // عناوین ستون‌های جدول سطوح دسترسی
        { "ThAdminUsername", new Dictionary<string, string> { { "fa", "نام کاربری مدیر" }, { "en", "Admin Username" }, { "tr", "Yönetici Kullanıcı Adı" } } },
        { "ThAccessType", new Dictionary<string, string> { { "fa", "نوع دسترسی" }, { "en", "Access Type" }, { "tr", "Yetki Türü" } } },
        { "ThAccountStatus", new Dictionary<string, string> { { "fa", "وضعیت حساب" }, { "en", "Account Status" }, { "tr", "Hesap Durumu" } } },
        { "ThActiveToken", new Dictionary<string, string> { { "fa", "توکن زنده عملیاتی" }, { "en", "Active Operational Token" }, { "tr", "Aktif Operasyonel Token" } } },
        { "ThTokenExpiration", new Dictionary<string, string> { { "fa", "انقضای توکن" }, { "en", "Token Expiration" }, { "tr", "Token Süresi" } } },
        { "ThSessionStatus", new Dictionary<string, string> { { "fa", "وضعیت سشن" }, { "en", "Session Status" }, { "tr", "Oturum Durumu" } } },
        { "ThAdminActions", new Dictionary<string, string> { { "fa", "عملیات پرونده" }, { "en", "Actions" }, { "tr", "İşlemler" } } },
        { "MenuBBtn", new Dictionary<string, string> { { "fa", "عملیات" }, { "en", "Operations" }, { "tr", "Operasyon" } } },

        // مقادیر و دکمه‌های داینامیک درون ردیف‌های جدول
        { "RoleSuperAdmin", new Dictionary<string, string> { { "fa", "سوپر ادمین" }, { "en", "Super Admin" }, { "tr", "Süper Yönetici" } } },
        { "RoleLimitedAdmin", new Dictionary<string, string> { { "fa", "مدیر محدود" }, { "en", "Limited Admin" }, { "tr", "Kısıtlı Yönetici" } } },
        { "StatusActive", new Dictionary<string, string> { { "fa", "فعال" }, { "en", "Active" }, { "tr", "Aktif" } } },
        { "StatusInactive", new Dictionary<string, string> { { "fa", "غیرفعال" }, { "en", "Inactive" }, { "tr", "Pasif" } } },
        { "BtnLogoutRevoke", new Dictionary<string, string> { { "fa", "خروج و ابطال" }, { "en", "Logout & Revoke" }, { "tr", "Çıkış Yap & İptal Et" } } },
        { "TokenRevoked", new Dictionary<string, string> { { "fa", "ابطال شده" }, { "en", "Revoked" }, { "tr", "İptal Edildi" } } },
        { "NoSession", new Dictionary<string, string> { { "fa", "فاقد سشن" }, { "en", "No Session" }, { "tr", "Oturum Yok" } } },
        { "SessionOnline", new Dictionary<string, string> { { "fa", "آنلاین" }, { "en", "Online" }, { "tr", "Çevrimiçi" } } },
        { "SearchBBtn", new Dictionary<string, string> { { "fa", "جستجو" }, { "en", "Search" }, { "tr", "Aramak" } } },

        { "EditBBtn", new Dictionary<string, string> { { "fa", "ویرایش" }, { "en", "Edit" }, { "tr", "düzenle" } } },
        { "DeleteBBtn", new Dictionary<string, string> { { "fa", "حذف" }, { "en", "Delete" }, { "tr", "Silmek" } } },
        //مودال افزودن و ویرایش
        // مودال افزودن و ویرایش - پیشوند اختصاصی PCal_AdminModal
        { "PCal_AdminModal_EditTitle", new Dictionary<string, string> { { "fa", "✏️ ویرایش کامل مشخصات و مجوزهای مدیر" }, { "en", "✏️ Edit Admin Permissions & Details" }, { "tr", "✏️ Yönetici İzinlerini ve Bilgilerini Düzenle" } } },
        { "PCal_AdminModal_AddTitle", new Dictionary<string, string> { { "fa", "➕ ثبت و تعریف مدیر سیستم جدید" }, { "en", "➕ Register New System Admin" }, { "tr", "➕ Yeni Sistem Yöneticisi Tanımla" } } },

        { "PCal_AdminModal_UsernameLabel", new Dictionary<string, string> { { "fa", "نام کاربری ادمین:" }, { "en", "Admin Username:" }, { "tr", "Yönetici Kullanıcı Adı:" } } },
        { "PCal_AdminModal_PasswordLabel", new Dictionary<string, string> { { "fa", "رمز عبور ادمین" }, { "en", "Admin Password" }, { "tr", "Yönetici Şifresi" } } },
        { "PCal_AdminModal_PasswordHint", new Dictionary<string, string> { { "fa", "(جهت حفظ قبلی خالی بگذارید):" }, { "en", "(leave blank to keep current):" }, { "tr", "(mevcut olanı korumak için boş bırakın):" } } },

        { "PCal_AdminModal_SuperAdminLabel", new Dictionary<string, string> { { "fa", "👑 اعطای دسترسی سوپر ادمین (مدیریت مدیران)" }, { "en", "👑 Grant Super Admin Privileges (Manage Admins)" }, { "tr", "👑 Süper Yönetici Yetkileri Ver (Yöneticileri Yönet)" } } },

        { "PCal_AdminModal_PermissionsSectionTitle", new Dictionary<string, string> { { "fa", "📌 تعیین سطوح دسترسی پرسنلی:" }, { "en", "📌 Determine Personnel Access Levels:" }, { "tr", "📌 Personel Erişim Seviyelerini Belirle:" } } },
        { "PCal_AdminModal_CanAddLabel", new Dictionary<string, string> { { "fa", "توانایی افزودن پرسنل جدید" }, { "en", "Ability to add new personnel" }, { "tr", "Yeni personel ekleme yetkisi" } } },
        { "PCal_AdminModal_CanEditLabel", new Dictionary<string, string> { { "fa", "توانایی ویرایش مشخصات و تردد پرسنل" }, { "en", "Ability to edit personnel details & traffic" }, { "tr", "Personel bilgilerini ve trafiğini düzenleme yetkisi" } } },
        { "PCal_AdminModal_CanDeleteLabel", new Dictionary<string, string> { { "fa", "توانایی حذف کامل پرسنل و رکوردها" }, { "en", "Ability to completely delete personnel & records" }, { "tr", "Personel ve kayıtları tamamen silme yetkisi" } } },

        { "PCal_AdminModal_SaveBtn", new Dictionary<string, string> { { "fa", "💾 ذخیره اطلاعات مدیر" }, { "en", "💾 Save Admin Information" }, { "tr", "💾 Yönetici Bilgilerini Kaydet" } } },
        { "PCal_AdminModal_CancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" } } },
        // ==========================================
        // 🔒 PCalendar - مودال‌های امنیتی و سیستم
        // ==========================================
        
        // --- ۱. مودال خروج (Logout Modal) ---
        { "PCal_Security_LogoutTitle", new Dictionary<string, string> { { "fa", "🚪 تاییدیه خروج از سیستم" }, { "en", "🚪 Logout Confirmation" }, { "tr", "🚪 Sistemden Çıkış Onayı" } } },
        { "PCal_Security_LogoutMsg", new Dictionary<string, string> { { "fa", "آیا برای خروج از پنل مدیریت سیستم مطمئن هستید؟" }, { "en", "Are you sure you want to log out of the system administration panel?" }, { "tr", "Sistem yönetim panelinden çıkış yapmak istediğinizden emin misiniz?" } } },
        { "PCal_Security_LogoutSubMsg", new Dictionary<string, string> { { "fa", "با خروج شما، سشن فعلی باطل شده و نیاز به ورود مجدد خواهید داشت." }, { "en", "Upon logging out, your current session will be invalidated, and you will need to log in again." }, { "tr", "Çıkış yaptığınızda mevcut oturumunuz sonlandırılacak ve tekrar giriş yapmanız gerekecektir." } } },
        { "PCal_Security_LogoutConfirmBtn", new Dictionary<string, string> { { "fa", "بله، خارج می‌شوم" }, { "en", "Yes, Log Out" }, { "tr", "Evet, Çıkış Yapıyorum" } } },
        { "PCal_Security_CancelBtn", new Dictionary<string, string> { { "fa", "انصراف" }, { "en", "Cancel" }, { "tr", "İptal" } } },
        
        // --- ۲. مودال عدم دسترسی (Access Denied Modal) ---
        { "PCal_Security_AccessTitle", new Dictionary<string, string> { { "fa", "⚠️ خطای دسترسی سیستم" }, { "en", "⚠️ System Access Error" }, { "tr", "⚠️ Sistem Erişim Hatası" } } },
        { "PCal_Security_AccessMsg", new Dictionary<string, string> { { "fa", "حساب کاربری شما فاقد مجوز ورود است!" }, { "en", "Your account lacks login permissions!" }, { "tr", "Hesabınızın giriş yetkisi bulunmuyor!" } } },
        { "PCal_Security_AccessSubMsg", new Dictionary<string, string> { { "fa", "شما اجازه ورود به پنل مدیریت پرسنل را ندارید. جهت اعطای دسترسی‌های لازم، لطفاً با مدیر ارشد سیستم تماس حاصل فرمایید." }, { "en", "You are not allowed to enter the personnel management panel. Please contact the senior system administrator to grant the necessary permissions." }, { "tr", "Personel yönetim paneline giriş izniniz yoktur. Gerekli yetkilerin verilmesi için lütfen kıdemli sistem yöneticisi ile iletişime geçin." } } },
        { "PCal_Security_IUnderstandBtn", new Dictionary<string, string> { { "fa", "متوجه شدم" }, { "en", "I Understand" }, { "tr", "Anladım" } } },
        
        // --- ۳. مودال تغییر دستی آدرس (URL Hijacking Modal) ---
        { "PCal_Security_HijackTitle", new Dictionary<string, string> { { "fa", "⚠️ هشدار عدم سطح دسترسی" }, { "en", "⚠️ Access Level Warning" }, { "tr", "⚠️ Erişim Seviyesi Uyarısı" } } },
        { "PCal_Security_HijackMsg", new Dictionary<string, string> { { "fa", "دسترسی غیرمجاز از طریق تغییر لینک!" }, { "en", "Unauthorized access via URL modification!" }, { "tr", "Bağlantı değişikliği yoluyla yetkisiz erişim!" } } },
        { "PCal_Security_HijackSubMsg", new Dictionary<string, string> { { "fa", "کاربر محترم، حساب شما مجوز ورود مستقیم به صفحه درخواستی را ندارد. سیستم به صورت خودکار شما را به مسیر مجاز بازگرداند." }, { "en", "Dear user, your account does not have permission to directly access the requested page. The system has automatically redirected you to an authorized path." }, { "tr", "Sayın kullanıcı, hesabınız talep edilen sayfaya doğrudan erişim iznine sahip değildir. Sistem sizi otomatik olarak yetkilendirilmiş yola yönlendirdi." } } },
        // --- متون دکمه‌های ناوبری موبایل ---
        { "PCal_Security_CloseMenuTitle", new Dictionary<string, string> { { "fa", "بستن منو" }, { "en", "Close Menu" }, { "tr", "Menüyü Kapat" } } },
        // ==========================================

        // 🏠 PCalendar - کدهای صفحه اصلی سیستم تردد
        // ==========================================
        { "PCal_MainHome_PageTitle", new Dictionary<string, string> { { "fa", "لیست پرسنل و ثبت تردد" }, { "en", "Personnel List & Attendance Log" }, { "tr", "Personel Listesi ve Giriş-Çıkış Kaydı" } } },
        { "PCal_MainHome_SearchPlaceholder", new Dictionary<string, string> { { "fa", "جستجوی نام یا کد..." }, { "en", "Search name or code..." }, { "tr", "İsim veya kod ara..." } } },
        { "PCal_MainHome_SearchBtnText", new Dictionary<string, string> { { "fa", "جستجو 🔍" }, { "en", "Search 🔍" }, { "tr", "Ara 🔍" } } },
        { "PCal_MainHome_ClearBtnTitle", new Dictionary<string, string> { { "fa", "پاک کردن" }, { "en", "Clear" }, { "tr", "Temizle" } } },
        
        // وضعیت‌ها و پیغام‌های قفل سیستم
        { "PCal_MainHome_LockedTitle", new Dictionary<string, string> { { "fa", "🔒 سیستم ثبت تردد غیرفعال است" }, { "en", "🔒 Attendance System is Disabled" }, { "tr", "🔒 Giriş-Çıkış Sistemi Devre Dışı" } } },
        { "PCal_MainHome_LockedSub", new Dictionary<string, string> { { "fa", "این دستگاه مجاز به ثبت تردد نیست. لطفا از ادمین مجموعه بخواهید ابتدا سیستم را فعال کند." }, { "en", "This device is not authorized for attendance tracking. Please ask the administrator to activate the system." }, { "tr", "Bu cihaz giriş-çıkış kaydı için yetkilendirilmemiştir. Lütfen yöneticiden sistemi aktif etmesini isteyin." } } },
        { "PCal_MainHome_AdminLoginBtn", new Dictionary<string, string> { { "fa", "🔑 ورود ادمین" }, { "en", "🔑 Admin Login" }, { "tr", "🔑 Yönetici Girişi" } } },
        
        // عناوین جدول
        { "PCal_MainHome_ThAvatar", new Dictionary<string, string> { { "fa", "عکس" }, { "en", "Photo" }, { "tr", "Fotoğraf" } } },
        { "PCal_MainHome_ThName", new Dictionary<string, string> { { "fa", "نام کارمند" }, { "en", "Employee Name" }, { "tr", "Personel Adı" } } },
        { "PCal_MainHome_ThStatus", new Dictionary<string, string> { { "fa", "وضعیت فعلی" }, { "en", "Current Status" }, { "tr", "Mevcut Durum" } } },
        { "PCal_MainHome_ThActions", new Dictionary<string, string> { { "fa", "عملیات تردد سریع" }, { "en", "Quick Action" }, { "tr", "Hızlı İşlem" } } },
        { "PCal_MainHome_NoPersonnel", new Dictionary<string, string> { { "fa", "هیچ پرسنلی مطابق جستجو یافت نشد." }, { "en", "No personnel found matching the search." }, { "tr", "Aramaya uygun personel bulunamadı." } } },
        
        // نشان‌ها و دکمه‌های تردد
        { "PCal_MainHome_StatusAbsent", new Dictionary<string, string> { { "fa", "خارج / غایب" }, { "en", "Absent / Out" }, { "tr", "Dışarıda / Yok" } } },
        { "PCal_MainHome_StatusPresent", new Dictionary<string, string> { { "fa", "حاضر" }, { "en", "Present" }, { "tr", "İçeride / Mevcut" } } },
        { "PCal_MainHome_BtnClockIn", new Dictionary<string, string> { { "fa", "ورود" }, { "en", "Clock In" }, { "tr", "Giriş" } } },
        { "PCal_MainHome_BtnClockOut", new Dictionary<string, string> { { "fa", "خروج" }, { "en", "Clock Out" }, { "tr", "Çıkış" } } },
        
        // بخش اعلان‌ها (Toast)
        { "PCal_MainHome_Notification", new Dictionary<string, string> { { "fa", "اطلاع‌رسانی" }, { "en", "Notification" }, { "tr", "Bildirim" } } },
        { "PCal_MainHome_Now", new Dictionary<string, string> { { "fa", "اکنون" }, { "en", "Now" }, { "tr", "Şimdi" } } },
        { "PCal_MainHome_CloseBtn", new Dictionary<string, string> { { "fa", "بستن" }, { "en", "Close" }, { "tr", "Kapat" } } },
        
        // متون پویای بک‌اِند
        { "PCal_MainHome_DefaultUser", new Dictionary<string, string> { { "fa", "کاربر" }, { "en", "User" }, { "tr", "Kullanıcı" } } },
        { "PCal_MainHome_ClockInSuccess", new Dictionary<string, string> { { "fa", "{0} ورود ثبت شد." }, { "en", "{0} checked in successfully." }, { "tr", "{0} giriş kaydı yapıldı." } } },
        { "PCal_MainHome_ClockOutSuccess", new Dictionary<string, string> { { "fa", "{0} خروج ثبت شد." }, { "en", "{0} checked out successfully." }, { "tr", "{0} çıkış kaydı yapıldı." } } },
        { "PCal_MainHome_LoadError", new Dictionary<string, string> { { "fa", "خطا در بارگذاری داده‌ها: {0}" }, { "en", "Error loading data: {0}" }, { "tr", "Veri yükleme hatası: {0}" } } },
        { "PCal_MainHome_ClockInError", new Dictionary<string, string> { { "fa", "خطا در ثبت ورود: {0}" }, { "en", "Error during clock-in: {0}" }, { "tr", "Giriş kaydı hatası: {0}" } } },
        { "PCal_MainHome_ClockOutError", new Dictionary<string, string> { { "fa", "خطا در ثبت خروج: {0}" }, { "en", "Error during clock-out: {0}" }, { "tr", "Çıkış kaydı hatası: {0}" } } },
        { "PCal_MainHome_KioskLocked", new Dictionary<string, string> { { "fa", "🔒 سیستم مدیریت خارج شد و کیوسک تردد قفل گردید." }, { "en", "🔒 Management system logged out and kiosk locked." }, { "tr", "🔒 Yönetim sisteminden çıkış yapıldı ve kiosk kilitlendi." } } },
        // اضافه کردن کلمات کلیدی برای تفکیک منابع ثبت تردد کارمندان
        { "PCal_MainHome_SourceManual", new Dictionary<string, string> { { "fa", "ثبت دستی ادمین" }, { "en", "Manual Admin" }, { "tr", "Manuel Yönetici" } } },
        { "PCal_MainHome_SourceFast", new Dictionary<string, string> { { "fa", "سریع ادمین" }, { "en", "Fast Admin" }, { "tr", "Hızlı Yönetici" } } },
        { "PCal_MainHome_SourceKiosk", new Dictionary<string, string> { { "fa", "دستگاه کیوسک" }, { "en", "Kiosk Device" }, { "tr", "Kiosk Cihazı" } } },
        { "PCal_MainHome_SourceAuto", new Dictionary<string, string> { { "fa", "خروج خودکار" }, { "en", "Auto Logout" }, { "tr", "Otomatik Çıkış" } } },
        { "PCal_MainHome_AutoLabel", new Dictionary<string, string> { { "fa", "سیستم" }, { "en", "System" }, { "tr", "Sistem" } } },
        { "PCal_AdminPanel_ReportModalTitle", new Dictionary<string, string> { { "fa", "کارنامه تفکیکی و مدیریت ساعت کاری:" }, { "en", "Detailed Report & Work Hours Management:" }, { "tr", "Detaylı Karne ve Çalışma Saatleri Yönetimi:" } } },

        // کلمات کلیدی مربوط به چندزبانگی صفحه ورود ادمین سیستم
        { "PCal_Login_CardHeader", new Dictionary<string, string> { { "fa", "ورود به پنل مدیریت" }, { "en", "Admin Panel Login" }, { "tr", "Yönetici Paneli Girişi" } } },
        { "PCal_Login_UsernameLabel", new Dictionary<string, string> { { "fa", "نام کاربری ادمین:" }, { "en", "Admin Username:" }, { "tr", "Yönetici Kullanıcı Adı:" } } },
        { "PCal_Login_PasswordLabel", new Dictionary<string, string> { { "fa", "رمز عبور:" }, { "en", "Password:" }, { "tr", "Şifre:" } } },
        { "PCal_Login_SubmitBtn", new Dictionary<string, string> { { "fa", "ورود به سیستم" }, { "en", "Login to System" }, { "tr", "Sisteme Giriş Yap" } } },
        { "PCal_Login_RequiredFields", new Dictionary<string, string> { { "fa", "لطفا نام کاربری و رمز عبور را وارد نمایید." }, { "en", "Please enter your username and password." }, { "tr", "Lütfen kullanıcı adınızı ve şifrenizi giriniz." } } },
        // home
        { "PCal_MainHome_AlreadyHasOpenRecord", new Dictionary<string, string> { { "fa", "شما یک تردد باز فعال در سیستم دارید. ابتدا باید خروج ثبت کنید." }, { "en", "You have an active open attendance record. You must clock out first." }, { "tr", "Sistemde aktif açık bir geçiş kaydınız var. Önce çıkış yapmalısınız." } } },
        //AdminPanel
        // متون خطاها و پیغام های صفحه مدیریت پرسنل (AdminPanel.cs)
        { "PCal_AdminPanel_TotalHoursUnit", new Dictionary<string, string> { { "fa", "00:00 ساعت" }, { "en", "00:00 Hrs" }, { "tr", "00:00 Saat" } } },
        { "PCal_AdminPanel_ImageUploadError", new Dictionary<string, string> { { "fa", "خطا در بارگذاری فایل تصویر: {0}" }, { "en", "Error uploading image file: {0}" }, { "tr", "Resim dosyası yüklenirken hata oluştu: {0}" } } },
        { "PCal_AdminPanel_EmpNameRequired", new Dictionary<string, string> { { "fa", "خطا: وارد کردن نام و نام خانوادگی کارمند الزامی است!" }, { "en", "Error: Employee full name is required!" }, { "tr", "Hata: Personel adı ve soyadı zorunludur!" } } },
        { "PCal_AdminPanel_EmpCodeRequired", new Dictionary<string, string> { { "fa", "خطا: وارد کردن کد ملی / شناسایی کارمند الزامی است!" }, { "en", "Error: Employee national ID code is required!" }, { "tr", "Hata: Personel kimlik numarası zorunludur!" } } },
        { "PCal_AdminPanel_ErrNameLength", new Dictionary<string, string> { { "fa", "خطا: طول نام و نام خانوادگی نمی‌تواند بیشتر از ۴۰ کاراکتر باشد." }, { "en", "Error: Full name cannot exceed 40 characters." }, { "tr", "Hata: Ad ve soyad 40 karakteri geçemez." } } },
        { "PCal_AdminPanel_ErrCodeLength", new Dictionary<string, string> { { "fa", "خطا: کد ملی باید حداقل ۱۰ رقم و حداکثر ۲۰ رقم باشد." }, { "en", "Error: National ID must be between 10 and 20 digits." }, { "tr", "Hata: Kimlik numarası en az 10, en fazla 20 haneli olmalıdır." } } },
        { "PCal_AdminPanel_ErrPhoneLength", new Dictionary<string, string> { { "fa", "خطا: شماره تماس نمی‌تواند بیشتر از ۱۵ رقم باشد." }, { "en", "Error: Phone number cannot exceed 15 digits." }, { "tr", "Hata: Telefon numarası 15 haneyi geçemez." } } },
        { "PCal_AdminPanel_DuplicateCodeEdit", new Dictionary<string, string> { { "fa", "خطا: این کد ملی قبلاً برای کارمند دیگری در سیستم ثبت شده است! کد ملی باید منحصربه‌فرد باشد." }, { "en", "Error: This ID is already assigned to another employee! It must be unique." }, { "tr", "Hata: Bu kimlik numarası başka bir personele kayıtlı! Benzersiz olmalıdır." } } },
        { "PCal_AdminPanel_DuplicateCodeAdd", new Dictionary<string, string> { { "fa", "خطا: کارمندی با این کد ملی قبلاً در سیستم تعریف شده است! ورود کد ملی تکراری مجاز نیست." }, { "en", "Error: An employee with this ID already exists! Duplicate ID is not allowed." }, { "tr", "Hata: Bu kimlik numarasına sahip bir personel zaten mevcut!" } } },
        { "PCal_AdminPanel_DelEmpTitle", new Dictionary<string, string> { { "fa", "تاییدیه حذف قطعی پرونده پرسنلی" }, { "en", "Confirm Permanent Employee Deletion" }, { "tr", "Personel Dosyası Kalıcı Silme Onayı" } } },
        { "PCal_AdminPanel_DelEmpMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل پرونده پرسنلی کارمند با نام «{0}» مطمئن هستید؟" }, { "en", "Are you sure you want to completely delete employee '{0}'?" }, { "tr", "'{0}' adlı personelin dosyasını tamamen silmek istediğinizden emin misiniz?" } } },
        { "PCal_AdminPanel_DelEmpSubMsg", new Dictionary<string, string> { { "fa", "توجه: کلیه سوابق تردد این کارمند فوراً و به صورت غیرقابل بازگشت پاک خواهد شد." }, { "en", "Note: All attendance records for this employee will be permanently wiped." }, { "tr", "Not: Bu personelin tüm geçiş kayıtları kalıcı olarak silinecektir." } } },
        { "PCal_AdminPanel_FastInError", new Dictionary<string, string> { { "fa", "خطا در ثبت ورود سریع: {0}" }, { "en", "Error during quick clock-in: {0}" }, { "tr", "Hızlı giriş kaydı hatası: {0}" } } },
        { "PCal_AdminPanel_FastOutError", new Dictionary<string, string> { { "fa", "خطا در ثبت خروج سریع: {0}" }, { "en", "Error during quick clock-out: {0}" }, { "tr", "Hızlı çıkış kaydı hatası: {0}" } } },
        { "PCal_AdminPanel_ManualDateMismatch", new Dictionary<string, string> { { "fa", "خطا: تاریخ خروج باید دقیقاً همان روز تاریخ ورود باشد!" }, { "en", "Error: Clock-out date must match the clock-in date!" }, { "tr", "Hata: Çıkış tarihi giriş tarihi ile aynı gün olmalıdır!" } } },
        { "PCal_AdminPanel_ManualTimeOrderError", new Dictionary<string, string> { { "fa", "خطا: ساعت خروج نمی‌تواند قبل‌تر از ساعت ورود تنظیم شود!" }, { "en", "Error: Clock-out time cannot be earlier than clock-in time!" }, { "tr", "Hata: Çıkış saati giriş saatinden önce olamaz!" } } },
        { "PCal_AdminPanel_AuditEditReason", new Dictionary<string, string> { { "fa", "اصلاح ساعت کارکرد توسط ادمین در پنل کارنامه پرسنل" }, { "en", "Working hours adjusted by admin in report panel" }, { "tr", "Yönetici tarafından karne panelinde çalışma saatleri düzenlendi" } } },
        { "PCal_AdminPanel_InvalidTimeFormat", new Dictionary<string, string> { { "fa", "فرمت زمان وارد شده اشتباه است (مثال معتبر: 07:00 یا 17:30)" }, { "en", "Invalid time format (valid example: 07:00 or 17:30)" }, { "tr", "Geçersiz saat formatı (örnek: 07:00 veya 17:30)" } } },
        { "PCal_AdminPanel_ActiveAttendance", new Dictionary<string, string> { { "fa", "در حال تردد" }, { "en", "Active" }, { "tr", "Aktif Geçiş" } } },
        { "PCal_AdminPanel_AllMonths", new Dictionary<string, string> { { "fa", "همه ماه‌های کارکرد" }, { "en", "All Working Months" }, { "tr", "Tüm Çalışma Ayları" } } },
        { "PCal_AdminPanel_DelAttTitle", new Dictionary<string, string> { { "fa", "تاییدیه حذف قطعی رکورد تردد پرسنل" }, { "en", "Confirm Attendance Record Deletion" }, { "tr", "Geçiş Kaydı Silme Onayı" } } },
        { "PCal_AdminPanel_DelAttTargetName", new Dictionary<string, string> { { "fa", "تردد مورخ {0} ساعت {1}" }, { "en", "Attendance on {0} at {1}" }, { "tr", "{0} tarihli saat {1} geçişi" } } },
        { "PCal_AdminPanel_DelAttMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل این رکورد تردد کاری پرسنل در تاریخ {0} مطمئن هستید؟" }, { "en", "Are you sure you want to delete this attendance record on {0}?" }, { "tr", "{0} tarihindeki bu çalışma geçiş kaydını silmek istediğinizden emin misiniz?" } } },
        { "PCal_AdminPanel_DelAttSubMsg", new Dictionary<string, string> { { "fa", "توجه: با تایید نهایی، این ساعت ورود و خروج خاص به طور کامل پاک شده و در محاسبات این ماه لحاظ نخواهد شد." }, { "en", "Note: Once confirmed, this clock-in/out record will be deleted and excluded from monthly calculations." }, { "tr", "Not: Onaylandığında bu giriş-çıkış kaydı silinecek ve aylık hesaplamalara dahil edilmeyecektir." } } },
        { "PCal_AdminPanel_DeleteError", new Dictionary<string, string> { { "fa", "خطا در حذف: {0}" }, { "en", "Error during deletion: {0}" }, { "tr", "Silme işlemi sırasında hata: {0}" } } },
         // متون باقی مانده فرانت اند صفحه مدیریت پرسنل و مودال کارنامه (AdminPanel.razor)
        { "PCal_AdminPanel_NoPermissionTitle", new Dictionary<string, string> { { "fa", "شما مجوز ندارید" }, { "en", "Permission denied" }, { "tr", "Yetkiniz yok" } } },
        { "PCal_AdminPanel_SessionExpired", new Dictionary<string, string> { { "fa", "سشن شما منقضی شده یا دسترسی ندارید. در حال انتقال به صفحه ورود..." }, { "en", "Your session has expired or access is denied. Redirecting to login..." }, { "tr", "Oturumunuz sona erdi veya erişiminiz yok. Giriş sayfasına yönlendiriliyorsunuz..." } } },
        { "PCal_AdminPanel_NoEmployeesFound", new Dictionary<string, string> { { "fa", "هیچ رکوردی در جدول کارمندان وجود ندارد." }, { "en", "No records found in the employees table." }, { "tr", "Personel tablosunda kayıt bulunamadı." } } },
        { "PCal_AdminPanel_SelectMonthLabel", new Dictionary<string, string> { { "fa", "انتخاب ماه کارکرد:" }, { "en", "Select Working Month:" }, { "tr", "Çalışma Ayını Seçin:" } } },
        { "PCal_AdminPanel_RegularLabel", new Dictionary<string, string> { { "fa", "موظف:" }, { "en", "Regular:" }, { "tr", "Normal Mesai:" } } },
        { "PCal_AdminPanel_OvertimeLabel", new Dictionary<string, string> { { "fa", "اضافه کار:" }, { "en", "Overtime:" }, { "tr", "Fazla Mesai:" } } },
        { "PCal_AdminPanel_AddManualBtn", new Dictionary<string, string> { { "fa", "ثبت تردد دستی جدید" }, { "en", "Add Manual Attendance" }, { "tr", "Yeni Manuel Geçiş Ekle" } } },
        { "PCal_AdminPanel_ThDate", new Dictionary<string, string> { { "fa", "تاریخ روز" }, { "en", "Date" }, { "tr", "Tarih" } } },
        { "PCal_AdminPanel_ThTimeRange", new Dictionary<string, string> { { "fa", "ساعت تردد (ورود / خروج)" }, { "en", "Time (In / Out)" }, { "tr", "Geçiş Saati (Giriş / Çıkış)" } } },
        { "PCal_AdminPanel_ThDuration", new Dictionary<string, string> { { "fa", "کارکرد" }, { "en", "Duration" }, { "tr", "Süre" } } },
        { "PCal_AdminPanel_ThTimeActions", new Dictionary<string, string> { { "fa", "عملیات ساعت کاری" }, { "en", "Time Actions" }, { "tr", "Saat İşlemleri" } } },
        { "PCal_AdminPanel_NoRecordsInRange", new Dictionary<string, string> { { "fa", "هیچ سابقه کارکردی برای بازه انتخابی وجود ندارد." }, { "en", "No attendance history found for the selected range." }, { "tr", "Seçilen aralık için çalışma geçmişi bulunamadı." } } },
        { "PCal_AdminPanel_LabelClockIn", new Dictionary<string, string> { { "fa", "ورود:" }, { "en", "In:" }, { "tr", "Giriş:" } } },
        { "PCal_AdminPanel_LabelClockOut", new Dictionary<string, string> { { "fa", "خروج:" }, { "en", "Out:" }, { "tr", "Çıkış:" } } },
        { "PCal_AdminPanel_ActiveStatusDot", new Dictionary<string, string> { { "fa", "در حال تردد" }, { "en", "Active" }, { "tr", "Aktif" } } },
        { "PCal_AdminPanel_BtnStopFast", new Dictionary<string, string> { { "fa", "توقف تردد سریع" }, { "en", "Stop Tracking" }, { "tr", "Geçişi Durdur" } } },
        { "PCal_AdminPanel_BtnEditTime", new Dictionary<string, string> { { "fa", "ویرایش ساعت" }, { "en", "Edit Time" }, { "tr", "Saati Düzenle" } } },
        { "PCal_AdminPanel_BtnDeleteTime", new Dictionary<string, string> { { "fa", "حذف" }, { "en", "Delete" }, { "tr", "Sil" } } },
        { "PCal_AdminPanel_SelectEmpPrompt", new Dictionary<string, string> { { "fa", "جهت مشاهده سوابق کارکرد، کارمند مورد نظر را انتخاب کنید." }, { "en", "Select an employee to view attendance history." }, { "tr", "Çalışma geçmişini görüntülemek için bir personel seçin." } } },
        { "PCal_AdminPanel_ManualModalEditTitle", new Dictionary<string, string> { { "fa", "ویرایش ردیف ساعت کاری" }, { "en", "Edit Working Hours" }, { "tr", "Çalışma Saatini Düzenle" } } },
        { "PCal_AdminPanel_ManualModalAddTitle", new Dictionary<string, string> { { "fa", "ثبت دستی ساعت تردد جدید" }, { "en", "Add Manual Attendance Record" }, { "tr", "Yeni Manuel Geçiş Kaydı Ekle" } } },
        { "PCal_AdminPanel_ManualInDateLabel", new Dictionary<string, string> { { "fa", "تاریخ روز ورود:" }, { "en", "Clock-In Date:" }, { "tr", "Giriş Tarihi:" } } },
        { "PCal_AdminPanel_ManualInTimeLabel", new Dictionary<string, string> { { "fa", "ساعت ورود:" }, { "en", "Clock-In Time:" }, { "tr", "Giriş Saati:" } } },
        { "PCal_AdminPanel_ManualOutDateLabel", new Dictionary<string, string> { { "fa", "تاریخ روز خروج (قفل همگام با ورود):" }, { "en", "Clock-Out Date (Synced with Clock-In):" }, { "tr", "Çıkış Tarihi (Giriş ile Senkronize):" } } },
        { "PCal_AdminPanel_ManualOutTimeLabel", new Dictionary<string, string> { { "fa", "ساعت خروج:" }, { "en", "Clock-Out Time:" }, { "tr", "Çıkış Saati:" } } },
        { "PCal_AdminPanel_ManualApplyChangesBtn", new Dictionary<string, string> { { "fa", "اعمال تغییرات" }, { "en", "Apply Changes" }, { "tr", "Değişiklikleri Uygula" } } },
        { "PCal_AdminPanel_ManualSubmitBtn", new Dictionary<string, string> { { "fa", "ثبت ساعت تردد" }, { "en", "Save Attendance" }, { "tr", "Geçiş Saatini Kaydet" } } },
        // متون پاپ‌آپ مودال تعریف و ویرایش کارمندان
        { "EmpFormAddTitle", new Dictionary<string, string> { { "fa", "کارمند جدید" }, { "en", "New Employee" }, { "tr", "Yeni Personel" } } },
        { "EmpLabelName", new Dictionary<string, string> { { "fa", "نام و نام خانوادگی:" }, { "en", "Full Name:" }, { "tr", "Adı Soyadı:" } } },
        { "EmpLabelCode", new Dictionary<string, string> { { "fa", "کد ملی / شناسایی (حداقل ۱۰ رقم):" }, { "en", "National / ID Code (Min 10 Digits):" }, { "tr", "Kimlik / Personel No (En az 10 Haneli):" } } },
        { "EmpLabelPhone", new Dictionary<string, string> { { "fa", "شماره تماس:" }, { "en", "Phone Number:" }, { "tr", "Telefon Numarası:" } } },
        { "EmpLabelUpload", new Dictionary<string, string> { { "fa", "آپلود عکس:" }, { "en", "Upload Photo:" }, { "tr", "Fotoğraf Yükle:" } } },
        { "EmpBtnSave", new Dictionary<string, string> { { "fa", "💾 ذخیره اطلاعات" }, { "en", "💾 Save Information" }, { "tr", "💾 Bilgileri Kaydet" } } },
        { "EmpBtnSaveChanges", new Dictionary<string, string> { { "fa", "💾 ثبت تغییرات" }, { "en", "Work Settings" }, { "tr", "💾 Değişiklikleri Kaydet" } } },
        { "WorkSet", new Dictionary<string, string> { { "fa", "تنظیمات کاری" }, { "en", "Work Setting" }, { "tr", "Çalışma ayarları" } } },
        { "PCal_AdminPanel_OvertimeAchievedTitle", new Dictionary<string, string> { { "fa", "دارای اضافه کاری محقق شده" }, { "en", "Achieved overtime" }, { "tr", "Gerçekleşen fazla mesai mevcut" } } },
        { "PCal_AdminPanel_ActiveOvertimeTitle", new Dictionary<string, string> { { "fa", "حضور زنده در محدوده اضافه کاری" }, { "en", "Active presence in overtime zone" }, { "tr", "Fazla mesai aralığında aktif bulunma" } } },
        ///IsHiddenFromKiosk
        { "PCal_Emp_HideFromKioskLabel", new Dictionary<string, string> { { "fa", "عدم نمایش این پرسنل در صفحه کیوسک (مرخصی طولانی / غیبت)" }, { "en", "Hide this employee from Kiosk screen (Long leave / Absent)" }, { "tr", "Bu personeli Kiosk ekranında gizle (Uzun izin / Devamsızlık)" } } },
        { "PCal_Emp_BadgeHiddenKiosk", new Dictionary<string, string> { { "fa", "مخفی در کیوسک" }, { "en", "Hidden in Kiosk" }, { "tr", "Kiosk'ta Gizli" } } },
        // notification Log
        { "PCal_Toast_MinutesAgo", new Dictionary<string, string> { { "fa", "{0} دقیقه پیش" }, { "en", "{0} mins ago" }, { "tr", "{0} dakika önce" } } },
        { "PCal_Toast_HoursAgo", new Dictionary<string, string> { { "fa", "{0} ساعت پیش" }, { "en", "{0} hrs ago" }, { "tr", "{0} saat önce" } } },
        { "PCal_AdminNotif_BtnTitle", new Dictionary<string, string> { { "fa", "اعلانات جاری سیستم" }, { "en", "System Live Notifications" }, { "tr", "Sistem Canlı Bildirimleri" } } },
        { "PCal_AdminNotif_ModalTitle", new Dictionary<string, string> { { "fa", "تاریخچه و اعلانات جاری عملیات مدیران و کیوسک" }, { "en", "Admins & Kiosk Live Operations Log" }, { "tr", "Yönetici ve Kiosk Canlı İşlem Bildirimleri" } } },
        { "PCal_AdminNotif_ThAdmin", new Dictionary<string, string> { { "fa", "مدیر عامل / منبع" }, { "en", "Admin / Source" }, { "tr", "Yönetici / Kaynak" } } },
        { "PCal_AdminNotif_ThEmployee", new Dictionary<string, string> { { "fa", "مشخصات پرسنل" }, { "en", "Employee Details" }, { "tr", "Personel Bilgileri" } } },
        { "PCal_AdminNotif_ThMessage", new Dictionary<string, string> { { "fa", "شرح رویداد" }, { "en", "Event Description" }, { "tr", "Olay Açıklaması" } } },
        { "PCal_AdminNotif_ThTime", new Dictionary<string, string> { { "fa", "زمان ثبت" }, { "en", "Timestamp" }, { "tr", "Kayıt Zamanı" } } },
        { "PCal_AdminNotif_NoLogs", new Dictionary<string, string> { { "fa", "هیچ اعلانی در سیستم ثبت نشده است." }, { "en", "No notifications recorded in the system." }, { "tr", "Sistemde kayıtlı bildirim bulunamadı." } } },
        { "PCal_AdminNotif_ClearAllBtn", new Dictionary<string, string> { { "fa", "پاکسازی تاریخچه اعلانات" }, { "en", "Clear Notification Logs" }, { "tr", "Bildirim Geçmişini Temizle" } } },
        { "PCal_NotifCat_All", new Dictionary<string, string> { { "fa", "همه بخش‌ها" }, { "en", "All Sections" }, { "tr", "Tüm Bölümler" } } },
        { "PCal_NotifCat_Kiosk", new Dictionary<string, string> { { "fa", "صفحه کیوسک" }, { "en", "Kiosk Screen" }, { "tr", "Kiosk Ekranı" } } },
        { "PCal_NotifCat_AdminPanel", new Dictionary<string, string> { { "fa", "پنل پرسنل" }, { "en", "Personnel Panel" }, { "tr", "Personel Paneli" } } },
        { "PCal_NotifCat_ManageAdmins", new Dictionary<string, string> { { "fa", "مدیریت مدیران" }, { "en", "Manage Admins" }, { "tr", "Yönetici Yönetimi" } } },
        ///NotificationLogs page
        // واژگان صفحه اختصاصی مدیریت و فیلتر اعلانات
        { "PCal_Notif_RefreshBtn", new Dictionary<string, string> { { "fa", "بروزرسانی" }, { "en", "Refresh" }, { "tr", "Yenile" } } },
        { "PCal_Notif_ResetFiltersBtn", new Dictionary<string, string> { { "fa", "پاکسازی فیلترها" }, { "en", "Reset Filters" }, { "tr", "Filtreleri Temizle" } } },
        { "PCal_Notif_SearchPlaceholder", new Dictionary<string, string> { { "fa", "نام کارمند، ادمین یا متن پیام..." }, { "en", "Employee, admin or message..." }, { "tr", "Personel, yönetici veya mesaj..." } } },
        { "PCal_Notif_FilterCategory", new Dictionary<string, string> { { "fa", "بخش (Category)" }, { "en", "Category" }, { "tr", "Bölüm (Category)" } } },
        { "PCal_Notif_FilterSubCategory", new Dictionary<string, string> { { "fa", "موضوع (SubCategory)" }, { "en", "SubCategory" }, { "tr", "Alt Konu" } } },
        { "PCal_Notif_FilterType", new Dictionary<string, string> { { "fa", "وضعیت" }, { "en", "Type" }, { "tr", "Durum" } } },
        { "PCal_Notif_FilterDate", new Dictionary<string, string> { { "fa", "تاریخ ثبت" }, { "en", "Created Date" }, { "tr", "Kayıt Tarihi" } } },
        { "PCal_Notif_FilterTimeRange", new Dictionary<string, string> { { "fa", "بازه ساعتی" }, { "en", "Time Range" }, { "tr", "Saat Aralığı" } } },
        { "PCal_Notif_ClearConfirmMsg", new Dictionary<string, string> { { "fa", "آیا از حذف کامل تمام سوابق و تاریخچه اعلانات سیستم مطمئن هستید؟" }, { "en", "Are you sure you want to permanently delete all notification logs?" }, { "tr", "Tüm sistem bildirim geçmişini kalıcı olarak silmek istediğinizden emin misiniz?" } } },
        { "PCal_Notif_ClearConfirmSub", new Dictionary<string, string> { { "fa", "توجه: پس از پاکسازی، امکان بازیابی گزارش‌های ثبت‌شده وجود نخواهد داشت." }, { "en", "Note: Once cleared, recorded logs cannot be recovered." }, { "tr", "Not: Temizlendikten sonra kayıtlı günlükler geri getirilemez." } } },

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

            if (CurrentLanguage == "fa" || CurrentLanguage == "ar")
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