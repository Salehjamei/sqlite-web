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
    }
}
