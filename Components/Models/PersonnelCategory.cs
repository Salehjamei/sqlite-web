using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sqlite_web.Components.Models
{
    /// مدل درختی سه‌سطحی دسته‌بندی پرسنل (کتگوری اصلی، فرزند اول و فرزند دوم)
    public class PersonnelCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        // سطح دسته‌بندی: 1 = کتگوری اصلی، 2 = کتگوری فرزند اول، 3 = کتگوری فرزند دوم (عنوان کاری)
        public int Level { get; set; } = 1;

        // شناسه دسته والد (برای سطح 1 مقدار null است)
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public PersonnelCategory? Parent { get; set; }

        public List<PersonnelCategory> Children { get; set; } = new();
    }
}
