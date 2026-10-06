using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models; // 👈 مطمئن شوید این نیم‌پیس با پروژه شما یکی باشد

namespace sqlite_web.Services
{
    public class AdminStateService
    {
        private readonly AppDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;

        // 🌟 دقیقاً همان متغیرهای عمومی و اختصاصی مورد نیاز شما در صفحات
        public bool IsAuthorized { get; set; } = false;
        public string adminFullName { get; set; } = "";
        public string adminUsername { get; set; } = "";
        public string adminPassword { get; set; } = "";

        public bool pCanAdd { get; set; } = false;
        public bool pCanEdit { get; set; } = false;
        public bool pCanDelete { get; set; } = false;
        
        // پرچم برای تشخیص اینکه آیا اسکن توکن اولیه پایان یافته یا خیر
        public bool IsChecked { get; private set; } = false;

        // رویداد مرکزی برای مطلع کردن هدر و بقیه صفحات از تغییر وضعیت ادمین
        public event Action? OnChange;

        public AdminStateService(AppDbContext dbContext, IJSRuntime jsRuntime)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
        }

        // متد اصلی اسکن توکن و استخراج دسترسی‌ها از دیتابیس SQLite
        public async Task InitializeAsync()
        {
            try
            {
                var savedToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");
                
                if (!string.IsNullOrEmpty(savedToken) && _dbContext != null)
                {
                    // جستجوی مستقیم ادمین در دیتابیس بر اساس توکن ذخیره شده
                    var admin = await _dbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                    if (admin != null)
                    {
                        // پر کردن متغیرهای شما
                        adminUsername = admin.Username;
                        adminPassword = admin.Password;
                        adminFullName = admin.Username; 

                        pCanAdd = admin.CanAddEmployee;
                        pCanEdit = admin.CanEditEmployee;
                        pCanDelete = admin.CanDeleteEmployee;

                        // بررسی سطح دسترسی: اگر سوپر ادمین باشد یا حداقل یک دسترسی داشته باشد مجاز است
                        if (admin.IsSuperAdmin || pCanAdd || pCanEdit || pCanDelete)
                        {
                            IsAuthorized = true;
                        }
                        else
                        {
                            IsAuthorized = false; // ادمین بدون دسترسی (کیوسک)
                        }
                    }
                }
                else
                {
                    Reset();
                }
            }
            catch
            {
                // مهار خطا در فاز Prerendering سمت سرور
            }
            finally
            {
                IsChecked = true;
                NotifyStateChanged(); // رندر مجدد هدر
            }
        }

        // متد خروج مرکزی سیستم
        public async Task LogoutAsync()
        {
            Reset();
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
            }
            catch { }
            NotifyStateChanged();
        }

        private void Reset()
        {
            adminUsername = "";
            adminFullName = "";
            adminPassword = "";
            IsAuthorized = false;
            pCanAdd = false;
            pCanEdit = false;
            pCanDelete = false;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
