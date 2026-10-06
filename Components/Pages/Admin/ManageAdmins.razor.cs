using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models.Admin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

    namespace sqlite_web.Components.Pages.Admin
{
    public partial class ManageAdmins : ComponentBase
    {
        [Inject]
        public AppDbContext DbContext { get; set; } = default!;

        // متغیرهای عمومی که فرانت‌آند به آن‌ها نیاز دارد
        public bool IsAuthorized { get; set; } = true;
        public string adminFullName { get; set; } = "";
        public string adminUsername { get; set; } = "";
        public string adminPassword { get; set; } = "";

        public bool pCanAdd { get; set; } = false;
        public bool pCanEdit { get; set; } = false;
        public bool pCanDelete { get; set; } = false;

        public bool isEditMode { get; set; } = false;
        public int editingAdminId { get; set; } = 0;
        public string feedbackMessage { get; set; } = "";

        public List<AdminUser> adminList { get; set; } = new List<AdminUser>();

        protected override async Task OnInitializedAsync()
        {
            await LoadAdmins();
        }

        public async Task LoadAdmins()
        {
            if (DbContext != null)
            {
                adminList = await DbContext.Admins.AsNoTracking().ToListAsync() ?? new List<AdminUser>();
            }
        }

        public async Task SaveAdmin()
        {
            if (string.IsNullOrWhiteSpace(adminFullName) || string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
            {
                feedbackMessage = "⚠️ لطفاً تمام کادرها را پر کنید.";
                return;
            }

            if (DbContext == null) return;

            if (isEditMode)
            {
                // اجازه ویرایش نام کاربری و رمز عبور برای همه (حتی مدیر ارشد)
                var admin = await DbContext.Admins.FindAsync(editingAdminId);
                if (admin != null)
                {
                    // بررسی تکراری نبودن نام کاربری جدید با دیتابیس
                    var usernameExists = await DbContext.Admins.AnyAsync(a => a.Username.ToLower() == adminUsername.ToLower() && a.Id != editingAdminId);
                    if (usernameExists)
                    {
                        feedbackMessage = "⚠️ این نام کاربری قبلاً توسط مدیر دیگری رزرو شده است.";
                        return;
                    }

                    admin.FullName = adminFullName;
                    admin.Username = adminUsername; // فعال شدن قابلیت تغییر نام کاربری
                    admin.Password = adminPassword; // فعال شدن قابلیت تغییر رمز عبور
                    admin.CanAddEmployee = pCanAdd;
                    admin.CanEditEmployee = pCanEdit;
                    admin.CanDeleteEmployee = pCanDelete;

                    await DbContext.SaveChangesAsync();
                    feedbackMessage = "✏️ مشخصات، نام کاربری، رمز عبور و مجوزها با موفقیت ویرایش شدند.";
                    await CancelEdit();
                }
            }
            else
            {
                var isExists = await DbContext.Admins.AnyAsync(a => a.Username.ToLower() == adminUsername.ToLower());
                if (isExists)
                {
                    feedbackMessage = "⚠️ این نام کاربری قبلاً ثبت شده است.";
                    return;
                }

                var newAdmin = new AdminUser
                {
                    FullName = adminFullName,
                    Username = adminUsername,
                    Password = adminPassword,
                    IsSuperAdmin = false,
                    CanAddEmployee = pCanAdd,
                    CanEditEmployee = pCanEdit,
                    CanDeleteEmployee = pCanDelete
                };

                DbContext.Admins.Add(newAdmin);
                await DbContext.SaveChangesAsync();
                feedbackMessage = "🎉 مدیر جدید با دسترسی‌های تفکیکی ساخته شد.";

                adminFullName = adminUsername = adminPassword = "";
                pCanAdd = pCanEdit = pCanDelete = false;
            }

            await LoadAdmins();
        }


        public void StartEdit(AdminUser ad)
        {
            isEditMode = true;
            editingAdminId = ad.Id;
            adminFullName = ad.FullName;
            adminUsername = ad.Username;
            adminPassword = ad.Password;
            pCanAdd = ad.CanAddEmployee;
            pCanEdit = ad.CanEditEmployee;
            pCanDelete = ad.CanDeleteEmployee;
        }

        public async Task DeleteAdmin(int id)
        {
            if (DbContext == null) return;
            var admin = await DbContext.Admins.FindAsync(id);
            if (admin != null && !admin.IsSuperAdmin)
            {
                DbContext.Admins.Remove(admin);
                await DbContext.SaveChangesAsync();
                feedbackMessage = "🗑️ مدیر مورد نظر از سیستم حذف شد.";
                await LoadAdmins();
            }
        }

        public async Task CancelEdit()
        {
            isEditMode = false;
            editingAdminId = 0;
            adminFullName = adminUsername = adminPassword = "";
            pCanAdd = pCanEdit = pCanDelete = false;
            await Task.CompletedTask;
        }
    }
}
