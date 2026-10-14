using System;
using System.Collections.Generic;
using System.Linq;
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
        [Inject] public AdminStateService AdminState { get; set; } = default!;

        private string message = "";
        private List<Employee> employees = new();
        private List<Attendance> allAttendances = new();

        // Search
        private string searchQuery = "";

        // Toast
        private bool showToast = false;
        private string toastMessage = "";

        private bool isAnyAdminLoggedIn => AdminState.IsAuthorized;
        private bool isCurrentUserSuperAdmin => AdminState.isCurrentUserSuperAdmin;
        private bool canAddPerm => AdminState.CurrentOnlineAdmin?.CanAddEmployee ?? false;
        private bool canEditPerm => AdminState.CurrentOnlineAdmin?.CanEditEmployee ?? false;
        private bool canDeletePerm => AdminState.CurrentOnlineAdmin?.CanDeleteEmployee ?? false;

        private IEnumerable<Employee> DisplayEmployees
        {
            get
            {
                if (string.IsNullOrWhiteSpace(searchQuery))
                    return employees.OrderBy(e => e.FullName);
                var q = searchQuery.Trim();
                return employees.Where(e =>
                    (!string.IsNullOrEmpty(e.FullName) && e.FullName.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                    || (!string.IsNullOrEmpty(e.PersonnelCode) && e.PersonnelCode.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                ).OrderBy(e => e.FullName);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
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

                var empName = employees.FirstOrDefault(e => e.Id == employeeId)?.FullName;
                if (string.IsNullOrEmpty(empName))
                {
                    var emp = await DbContext.Employees.FindAsync(employeeId);
                    empName = emp?.FullName ?? "کاربر";
                }

                await LoadData();
                await ShowToastAsync($"{empName} ورود ثبت شد.");
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

                    var empName = employees.FirstOrDefault(e => e.Id == record.EmployeeId)?.FullName;
                    if (string.IsNullOrEmpty(empName))
                    {
                        var emp = await DbContext.Employees.FindAsync(record.EmployeeId);
                        empName = emp?.FullName ?? "کاربر";
                    }

                    await LoadData();
                    await ShowToastAsync($"{empName} خروج ثبت شد.");
                }
            }
            catch (Exception ex)
            {
                message = $"خطا در ثبت خروج: {ex.InnerException?.Message ?? ex.Message}";
            }
        }

        private async Task AdminLogoutFromHome()
        {
            await AdminState.LogoutAsync();
            message = "🔒 سیستم مدیریت خارج شد و کیوسک تردد قفل گردید.";
            StateHasChanged();
        }

        private void ClearSearch()
        {
            searchQuery = "";
        }

        private async Task ShowToastAsync(string text, int milliseconds = 3500)
        {
            toastMessage = text;
            showToast = true;
            StateHasChanged();
            await Task.Delay(milliseconds);
            showToast = false;
            StateHasChanged();
        }
    }
}