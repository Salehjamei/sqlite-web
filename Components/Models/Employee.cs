namespace sqlite_web.Components.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string PersonnelCode { get; set; } // اختیاری: برای بزرگ‌تر شدن نرم‌افزار در آینده
        
        // 👈 فیلدهای جدید برای شماره تماس و عکس پرسنلی
        public string Phone { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }
}
