using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using sqlite_web.Components.Models;
using sqlite_web.Services;

namespace sqlite_web.Components.Pages
{
    public partial class Home
    {
        [Inject] public AppDbContext DbContext { get; set; } = default!;
        [Inject] public AdminStateService AdminState { get; set; } = default!; // 👈 تزریق سرویس متمرکز

        private string message = "";
        private List<Employee> employees = new();
        private List<Attendance> allAttendances = new();

        // 🔘 پروپرتی‌های دسترسی که مستقیماً و بدون متغیر کمکی از سرویس خوانده می‌شوند
        private bool isAnyAdminLoggedIn => AdminState.IsAuthorized;
        private bool isCurrentUserSuperAdmin => AdminState.isCurrentUserSuperAdmin;
        private bool canAddPerm => AdminState.CurrentOnlineAdmin?.CanAddEmployee ?? false;
        private bool canEditPerm => AdminState.CurrentOnlineAdmin?.CanEditEmployee ?? false;
        private bool canDeletePerm => AdminState.CurrentOnlineAdmin?.CanDeleteEmployee ?? false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // پایش و اعتبارسنجی توکن از طریق هسته سرویس
                await AdminState.CheckAccessAndRedirectAsync();

                if (AdminState.IsAuthorized)
                {
                    await LoadData();
                }
                StateHasChanged();
            }
        }

        private async Task LoadData()
        {
            try
            {
                if (DbContext != null)
                {
                    employees = await DbContext.Employees.AsNoTracking().ToListAsync();
                    allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync();
                }
            }
            catch (Exception ex)
            {
                message = $"خطا در بارگذاری داده‌ها: {ex.Message}";
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
                message = $"خطا در ثبت ورود: {ex.InnerException?.Message ?? ex.Message}";
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
                message = $"خطا در ثبت خروج: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private async Task AdminLogoutFromHome()
        {
            await AdminState.LogoutAsync(); // ابطال کامل سشن در دیتابیس و مرورگر
            message = "🔒 سیستم مدیریت خارج شد و کیوسک تردد قفل گردید.";
            StateHasChanged();
        }
    }
}
