namespace sqlite_web.Components.Models
{
    public class Employee
    {
        public int Id { get; set; }
        //نام اجباری نباید تکراری باشد
        public string FullName { get; set; }
        // کد ملی 10 رقمی اجباری ، نباید تکراری باشد
        public string PersonnelCode { get; set; }
        
        // اختیاری فیلدهای جدید برای شماره تماس و عکس پرسنلی
        public string Phone { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        // تاریخ رسمی شروع به کار کارمند در مجموعه (پیش‌فرض تاریخ روز ثبت پرونده)
        public DateTime HireDate { get; set; } = DateTime.Today;

        // پرچم عدم نمایش در کیوسک (در صورت مرخصی طولانی مد یا تعلیق، true می‌شود)
        public bool IsHiddenFromKiosk { get; set; } = false;
        // شناسه کتگوری اصلی پرسنل (سطح اول)
        public int? MainCategoryId { get; set; }
        public PersonnelCategory? MainCategory { get; set; }

        // شناسه کتگوری فرزند اول پرسنل (سطح دوم - دسته شغلی / واحد)
        public int? SubCategory1Id { get; set; }
        public PersonnelCategory? SubCategory1 { get; set; }

        // شناسه کتگوری فرزند دوم پرسنل (سطح سوم - عنوان کاری)
        public int? SubCategory2Id { get; set; }
        public PersonnelCategory? SubCategory2 { get; set; }

    }
}
