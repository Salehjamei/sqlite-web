using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;

namespace sqlite_web.Components.Pages
{
    // استفاده از کلمه partial برای اتصال به فایل HTML الزامی است
    public partial class Home
    {
        // تزریق وابستگی دیتابیس به سبک Code-Behind
        [Inject]
        public AppDbContext DbContext { get; set; } = default!;
        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        private bool isAnyAdminLoggedIn = false;
        private bool isCurrentUserSuperAdmin = false;
        private bool canAddPerm = false;
        private bool canEditPerm = false;
        private bool canDeletePerm = false;

        private string message = "";

        private List<Employee> employees = new List<Employee>();
        private List<Attendance> allAttendances = new List<Attendance>();


        private async Task LoadData()
        {
            try
            {
                if (DbContext != null)
                {
                    employees = await DbContext.Employees.AsNoTracking().ToListAsync() ?? new List<Employee>();
                    allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync() ?? new List<Attendance>();
                }
            }
            catch (Exception ex)
            {
                message = "خطا در بارگذاری داده‌ها: " + ex.Message;
            }
        }

        private async Task ClockIn(int employeeId)
        {
            try
            {
                var attendance = new Attendance { EmployeeId = employeeId, ClockInTime = DateTime.Now };
                DbContext.Attendances.Add(attendance);
                await DbContext.SaveChangesAsync();
                message = "🟢 ورود جدید در تاریخ امروز ثبت شد.";
                await LoadData();
            }
            catch (Exception ex)
            {
                message = "خطا در ثبت ورود: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }

        private async Task ClockOut(int attendanceId)
        {
            try
            {
                var record = await DbContext.Attendances.FindAsync(attendanceId);
                if (record != null)
                {
                    record.ClockOutTime = DateTime.Now;
                    await DbContext.SaveChangesAsync();
                    message = "🔴 خروج ثبت شد. دکمه ورود مجدد فعال گردید.";
                    await LoadData();
                }
            }
            catch (Exception ex)
            {
                message = "خطا در ثبت خروج: " + (ex.InnerException?.Message ?? ex.Message);
            }
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    // خواندن توکن ادمینی که سیستم را باز نگه داشته است
                    var savedToken = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");
                    if (!string.IsNullOrEmpty(savedToken) && DbContext != null)
                    {
                        var admin = await DbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
                        if (admin != null)
                        {
                            isAnyAdminLoggedIn = true;

                            // 👈 خواندن وضعیت مجوزهای ادمین جاری برای مخفی‌سازی دکمه مدیریت
                            isCurrentUserSuperAdmin = admin.IsSuperAdmin;
                            canAddPerm = admin.CanAddEmployee;
                            canEditPerm = admin.CanEditEmployee;
                            canDeletePerm = admin.CanDeleteEmployee;

                            await LoadData();
                            StateHasChanged();
                        }

                    }
                }
                catch { }
            }
        }
        // 🌟 متد خروج ادمین از صفحه اصلی و قفل شدن آنی کیوسک پرسنل
        private async Task AdminLogoutFromHome()
        {
            try
            {
                if (JSRuntime != null)
                {
                    // پاک کردن توکن سشن ادمین از لایو حافظه مرورگر
                    await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");

                    // تغییر وضعیت متغیر برای اعمال تغییرات آنی در فرانت‌اَند
                    isAnyAdminLoggedIn = false;

                    // پاک کردن پیغام‌های احتمالی قبلی
                    message = "🔒 سیستم مدیریت خارج شد و کیوسک تردد قفل گردید.";
                    isAnyAdminLoggedIn = false;
                    isCurrentUserSuperAdmin = false;
                    canAddPerm = false;
                    canEditPerm = false;
                    canDeletePerm = false;

                    // تازه سازی اجباری صفحه وب Blazor
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                message = "خطا در خروج ادمین: " + ex.Message;
            }
        }


    }
}
