namespace sqlite_web.Components.Models.Admin
{
    public class AdminUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        // 👈 فیلد جدید: مشخص می‌کند فرد مدیر ارشد است یا معمولی
        public bool IsSuperAdmin { get; set; }
        public bool CanAddEmployee { get; set; } = false;
        public bool CanEditEmployee { get; set; } = false;
        public bool CanDeleteEmployee { get; set; } = false;
    }
}