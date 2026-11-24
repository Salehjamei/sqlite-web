using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqlite_web.Components.Models
{
    /// <summary>
    /// مدل مکان‌های جغرافیایی مجاز (کارخانه، فروشگاه، نمایشگاه و...) همراه با شعاع دستی و محدودیت دسته‌بندی
    /// </summary>
    public class AllowedLocation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        // مختصات جغرافیایی نقطه مرکزی مکان
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // شعاع مجاز تردد اطراف نقطه مرکزی بر حسب متر (تعیین دستی توسط سوپر ادمین)
        public int RadiusMeters { get; set; } = 100;

        public bool IsActive { get; set; } = true;

        // محدودیت دسترسی مکانی بر اساس درخت دسته‌بندی پرسنل (مقدار null یعنی مجاز برای همه زیرمجموعه‌ها)
        public int? AllowedMainCategoryId { get; set; }
        [ForeignKey("AllowedMainCategoryId")]
        public PersonnelCategory? AllowedMainCategory { get; set; }

        public int? AllowedSubCategory1Id { get; set; }
        [ForeignKey("AllowedSubCategory1Id")]
        public PersonnelCategory? AllowedSubCategory1 { get; set; }

        public int? AllowedSubCategory2Id { get; set; }
        [ForeignKey("AllowedSubCategory2Id")]
        public PersonnelCategory? AllowedSubCategory2 { get; set; }
    }
}
