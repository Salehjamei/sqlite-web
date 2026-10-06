using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Admin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace sqlite_web.Components.Pages.Admin
{
    public partial class AdminPanel
    {
        [Inject]
        public AppDbContext DbContext { get; set; } = default!;

        [Inject]
        public IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        public NavigationManager MyNavigationManager { get; set; } = default!;

        // متغیرهای متنی فارسی فرانت‌اَند
        public string TxtLockTitle { get; set; } = "🔒 قفل امنیتی پنل مدیریت";
        public string TxtLockSub { get; set; } = "جهت دسترسی به تنظیمات پرسنل, اطلاعات مدیریت خود را وارد کنید.";
        public string TxtUsernameLabel { get; set; } = "نام کاربری ادمین:";
        public string TxtPasswordLabel { get; set; } = "رمز عبور:";
        public string TxtLoginBtn { get; set; } = "ورود به پنل مدیریت";
        public string TxtBackToHome { get; set; } = "← بازگشت به صفحه اصلی کیوسک";
        public string TxtPanelTitle { get; set; } = "پنل مدیریت ارشد سیستم تردد";
        public string TxtLogoutBtn { get; set; } = "🏃‍♂️ خروج از مدیریت";
        public string TxtKioskPage { get; set; } = "صفحه کیوسک پرسنل ←";
        public string TxtCreateAdminTitle { get; set; } = "👤 ثبت مدیر جدید";
        public string TxtCreateEmpTitle { get; set; } = "➕ تعریف پرسنل جدید";
        public string TxtSaveBtn { get; set; } = "ذخیره";
        public string TxtManageEmpTitle { get; set; } = "مدیریت پرسنل و تاریخچه حضور";
        public string TxtThCode { get; set; } = "کد ملی";
        public string TxtThName { get; set; } = "نام پرسنل";
        public string TxtThReport { get; set; } = "تاریخچه تفکیکی";
        public string TxtShowReportBtn { get; set; } = "🔍 نمایش کارکرد";
        public string TxtModalTitle { get; set; } = "📊 سوابق تفکیکی و جمع کارکرد";
        public string TxtNoData { get; set; } = "هیچ سابقه ترددی برای این فرد یافت نشد.";
        public string TxtWorking { get; set; } = "در حال کار...";
        public string TxtTotalSum { get; set; } = "📊 مجموع کل کارکرد پرسنل";
        public string TxtCloseBtn { get; set; } = "بستن پنجره";

        private bool isLoggedIn = false;
        private bool isCurrentUserSuperAdmin = false;
        private string loginUsername = "";
        private string loginPassword = "";
        private string loginErrorMessage = "";
        private string currentAdminName = "";

        // متغیرهای فرم پرسنل
        private string newEmpName = "";
        private string newEmpCode = "";
        private string alertMessage = "";

        // متغیرهای پاپ‌آ‍پ مودال
        private bool showModal = false;
        private Employee? selectedEmployee = null;
        private DateTime? selectedDateForEdit = null;

        private List<Employee> employees = new();
        private List<Attendance> allAttendances = new();

        // احیای متغیرهای تقویم دستی روزهای گذشته
        private DateTime manualDate = DateTime.Today;
        private string manualInTime = "08:00";
        private string manualOutTime = "17:00";
        private string manualErrorMessage = "";

        // احیای فیلدهای ویرایش و آپلود تصویر پرسنل
        private bool isEmpEditMode = false;
        private int editingEmployeeId = 0;
        private string newEmpPhone = "";
        private string previewImageUrl = "";
        private IBrowserFile? uploadedFileFile = null;
        private bool renderUploadInput = true;

        // احیای متغیرهای مدیریت خطای اعتبارسنجی فرم‌ها (حل کلیدی ارورهای تصویر)
        private bool isEmpNameInvalid = false;
        private bool isEmpCodeInvalid = false;
        private bool isEmpPhoneInvalid = false;
        private string empNameError = "";
        private string empCodeError = "";
        private string empPhoneError = "";
        //خطای عکس
        private bool isEmpImageInvalid = false;
        private string empImageError = "";


        // احیای رفرنس المنت‌ها برای فوکوس هوشمند
        private ElementReference empNameInput;
        private ElementReference empCodeInput;
        private ElementReference empPhoneInput;

        // شیء نگهداری لایو مجوزهای ادمین جاری
        public AdminUser currentAdminPerms { get; set; } = new AdminUser();

        private async Task<AdminUser?> GetCurrentAdminLiveAsync()
        {
            try
            {
                var savedToken = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "admin_session_token");
                if (string.IsNullOrEmpty(savedToken) || DbContext == null) return null;
                return await DbContext.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == savedToken);
            }
            catch
            {
                return null;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var admin = await GetCurrentAdminLiveAsync();
                if (admin != null)
                {
                    if (!admin.IsSuperAdmin && !admin.CanAddEmployee && !admin.CanEditEmployee && !admin.CanDeleteEmployee)
                    {
                        MyNavigationManager.NavigateTo("/", true);
                        return;
                    }

                    isLoggedIn = true;
                    currentAdminName = admin.FullName;
                    isCurrentUserSuperAdmin = admin.IsSuperAdmin;
                    currentAdminPerms = admin;
                    await LoadData();
                    StateHasChanged();
                }
            }
        }

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            if (DbContext != null)
            {
                employees = await DbContext.Employees.AsNoTracking().ToListAsync() ?? new();
                allAttendances = await DbContext.Attendances.AsNoTracking().ToListAsync() ?? new();
            }
        }

        private async Task HandleLogin()
        {
            loginErrorMessage = "";
            if (string.IsNullOrWhiteSpace(loginUsername) || string.IsNullOrWhiteSpace(loginPassword))
            {
                loginErrorMessage = "لطفاً نام کاربری و رمز عبور را وارد کنید.";
                return;
            }

            if (DbContext != null)
            {
                var admin = await DbContext.Admins.FirstOrDefaultAsync(a => a.Username == loginUsername && a.Password == loginPassword);
                if (admin != null)
                {
                    await JSRuntime.InvokeVoidAsync("localStorage.setItem", "admin_session_token", admin.Username);

                    if (!admin.IsSuperAdmin && !admin.CanAddEmployee && !admin.CanEditEmployee && !admin.CanDeleteEmployee)
                    {
                        loginUsername = "";
                        loginPassword = "";
                        MyNavigationManager.NavigateTo("/", true);
                        return;
                    }

                    isLoggedIn = true;
                    currentAdminName = admin.FullName;
                    isCurrentUserSuperAdmin = admin.IsSuperAdmin;
                    currentAdminPerms = admin;
                    loginUsername = "";
                    loginPassword = "";
                    await LoadData();
                }
                else
                {
                    loginErrorMessage = "نام کاربری یا رمز عبور اشتباه است!";
                }
            }
        }

        private async Task HandleLogout()
        {
            isLoggedIn = false;
            isCurrentUserSuperAdmin = false;
            currentAdminName = "";
            alertMessage = "";
            currentAdminPerms = new AdminUser();
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", "admin_session_token");
            MyNavigationManager.NavigateTo("/admin", true);
        }

        private void HandlePhoneInput(ChangeEventArgs e)
        {
            isEmpPhoneInvalid = false;
            var raw = e.Value?.ToString() ?? "";

            // حذف تمام کاراکترهای غیر عددی
            var numbersOnly = System.Text.RegularExpressions.Regex.Replace(raw, @"[^\d]", "");

            // قطع زنجیره عدد در رقم ۱۵ام
            if (numbersOnly.Length > 15)
            {
                numbersOnly = numbersOnly.Substring(0, 15);
            }
            newEmpPhone = numbersOnly;
        }

        private void HandleImageUpload(InputFileChangeEventArgs e)
        {
            uploadedFileFile = e.File;
            previewImageUrl = $"data:{e.File.ContentType};base64,";
        }

        private async Task SaveEmployeeData()
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null) { MyNavigationManager.NavigateTo("/admin", true); return; }

            // ریست کردن وضعیت خطاها
            isEmpNameInvalid = false;
            isEmpCodeInvalid = false;
            isEmpPhoneInvalid = false;
            isEmpImageInvalid = false;
            empNameError = empCodeError = empPhoneError = empImageError = "";
            alertMessage = "";

            // [بخش‌های ۱ تا ۷ اعتبارسنجی نام، کد ملی و تلفن که از قبل داشتید بدون تغییر در اینجا اجرا شوند]
            if (isEmpEditMode)
            {
                if (!liveAdmin.IsSuperAdmin && !liveAdmin.CanEditEmployee)
                {
                    alertMessage = "🚫 خطای امنیتی: شما مجوز ویرایش پرسنل را ندارید!";
                    return;
                }
            }
            else
            {
                if (!liveAdmin.IsSuperAdmin && !liveAdmin.CanAddEmployee)
                {
                    alertMessage = "🚫 خطای امنیتی: شما مجوز افزودن پرسنل جدید را ندارید!";
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(newEmpName))
            {
                empNameError = "کادر نام و نام خانوادگی نباید خالی باشد.";
                isEmpNameInvalid = true;
                await empNameInput.FocusAsync();
                return;
            }

            var isNameExists = employees.Any(e => e.FullName.Trim().Equals(newEmpName.Trim(), StringComparison.OrdinalIgnoreCase) && e.Id != editingEmployeeId);
            if (isNameExists)
            {
                empNameError = "این نام و نام خانوادگی قبلاً در سیستم ثبت شده است.";
                isEmpNameInvalid = true;
                await empNameInput.FocusAsync();
                return;
            }
            if (string.IsNullOrWhiteSpace(newEmpCode))
            {
                empCodeError = "کادر کد ملی نباید خالی باشد.";
                isEmpCodeInvalid = true;
                await empCodeInput.FocusAsync();
                return;
            }
            if (newEmpCode.Length != 10)
            {
                empCodeError = $"کد ملی معتبر نیست! شما {newEmpCode.Length} رقم وارد کرده‌اید (باید دقیقاً ۱۰ رقم باشد).";
                isEmpCodeInvalid = true;
                await empCodeInput.FocusAsync();
                return;
            }
            var isCodeExists = employees.Any(e => e.PersonnelCode == newEmpCode && e.Id != editingEmployeeId);
            if (isCodeExists)
            {
                empCodeError = "این کد ملی قبلاً برای شخص دیگری ثبت شده است.";
                isEmpCodeInvalid = true;
                await empCodeInput.FocusAsync();
                return;
            }
            // ۶. بررسی خالی بودن شماره تماس
            if (string.IsNullOrWhiteSpace(newEmpPhone))
            {
                empPhoneError = "کادر شماره تماس نباید خالی باشد.";
                isEmpPhoneInvalid = true;
                await empPhoneInput.FocusAsync();
                return;
            }

            // ۷. اعمال لایه امنیتی محدوده مجاز (کمتر از ۱۰ و بیشتر از ۱۵ رقم ممنوع است)
            if (newEmpPhone.Length < 10 || newEmpPhone.Length > 15)
            {
                empPhoneError = $"شماره تماس معتبر نیست! شما {newEmpPhone.Length} رقم وارد کرده‌اید (باید بین ۱۰ تا ۱۵ رقم باشد).";
                isEmpPhoneInvalid = true;
                await empPhoneInput.FocusAsync();
                return;
            }

            if (string.IsNullOrWhiteSpace(newEmpName)) { /* ... */ return; }
            if (newEmpCode.Length != 10) { /* ... */ return; }
            if (newEmpPhone.Length < 10 || newEmpPhone.Length > 15) { /* ... */ return; }

            try
            {
                string relativePath = "";

                // پردازش و مهار خطاهای آپلود فایل عکس
                if (uploadedFileFile != null)
                {
                    // الف) بررسی پسوند فایل به صورت امن (سازگار با حروف بزرگ و کوچک)
                    var extension = Path.GetExtension(uploadedFileFile.Name).ToLower();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        empImageError = "فرمت فایل مجاز نیست! فقط عکس‌های با پسوند jpg، jpeg یا png قابل قبول هستند.";
                        isEmpImageInvalid = true;
                        return;
                    }

                    // ب) تلاش برای ذخیره فایل روی هارد دیسک با مدیریت محدودیت حجم بلیزور
                    try
                    {
                        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(folderPath, fileName);

                        // افزایش سقف مجاز حجم آپلود فایل بلیزور تا ۵ مگابایت (5 * 1024 * 1024)
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFileFile.OpenReadStream(5 * 1024 * 1024).CopyToAsync(stream);
                        }
                        relativePath = $"/uploads/{fileName}";
                    }
                    catch (InvalidOperationException)
                    {
                        empImageError = "حجم عکس بیش از حد مجاز است! حداکثر حجم فایل باید ۵ مگابایت باشد.";
                        isEmpImageInvalid = true;
                        return;
                    }
                    catch (Exception ex)
                    {
                        empImageError = "خطای سیستم در ذخیره فایل فیزیکی: " + ex.Message;
                        isEmpImageInvalid = true;
                        return;
                    }
                }

                // درج نهایی در دیتابیس SQLite پس از موفقیت‌آمیز بودن آپلود عکس
                if (isEmpEditMode)
                {
                    var emp = await DbContext.Employees.FindAsync(editingEmployeeId);
                    if (emp != null)
                    {
                        emp.FullName = newEmpName;
                        emp.PersonnelCode = newEmpCode;
                        emp.Phone = newEmpPhone;

                        if (!string.IsNullOrEmpty(relativePath))
                        {
                            if (!string.IsNullOrEmpty(emp.ImagePath))
                            {
                                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", emp.ImagePath.TrimStart('/'));
                                if (File.Exists(oldFilePath)) File.Delete(oldFilePath);
                            }
                            emp.ImagePath = relativePath;
                        }

                        await DbContext.SaveChangesAsync();
                        alertMessage = "🎉 تغییرات با موفقیت ذخیره شد و کارنامه کارمند به‌روزرسانی گردید.";
                        await CancelEmpEdit();
                    }
                }
                else
                {
                    var newEmp = new Employee { FullName = newEmpName, PersonnelCode = newEmpCode, Phone = newEmpPhone, ImagePath = relativePath };
                    DbContext.Employees.Add(newEmp);
                    await DbContext.SaveChangesAsync();

                    alertMessage = "🎉 کارمند جدید با موفقیت ثبت شد و به کیوسک پرسنل اضافه گردید.";

                    newEmpName = ""; newEmpCode = ""; newEmpPhone = ""; previewImageUrl = ""; uploadedFileFile = null;
                    renderUploadInput = false; await Task.Delay(10); renderUploadInput = true;
                }

                await LoadData();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                alertMessage = "❌ خطا در ثبت اطلاعات دیتابیس: " + ex.Message;
            }
        }

        private async Task DeleteEmployee(int id)
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null || (!liveAdmin.IsSuperAdmin && !liveAdmin.CanDeleteEmployee))
            {
                alertMessage = "🚫 خطای امنیتی: شما مجوز حذف پرسنل را ندارید!";
                return;
            }
            var emp = await DbContext.Employees.FindAsync(id);
            if (emp != null)
            {
                DbContext.Employees.Remove(emp);
                await DbContext.SaveChangesAsync();
                alertMessage = "کارمند با موفقیت حذف شد.";
                await LoadData();
            }
        }
        private async Task ForcePresent(int empId)
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null || (!liveAdmin.IsSuperAdmin && !liveAdmin.CanEditEmployee))
            {
                alertMessage = "🚫 خطای امنیتی: شما مجوز تغییر زمان حضور پرسنل را ندارید!";
                return;
            }
            var att = new Attendance { EmployeeId = empId, ClockInTime = DateTime.Now };
            DbContext.Attendances.Add(att);
            await DbContext.SaveChangesAsync();
            alertMessage = "حضور ثبت شد";
            await LoadData();
        }
        private async Task ForceAbsent(int recordId)
        {
            var record = await DbContext.Attendances.FindAsync(recordId);
            if (record != null)
            {
                DbContext.Attendances.Remove(record);
                await DbContext.SaveChangesAsync();
                await LoadData();
            }
        }
        private async Task StopAttendanceNow(int recordId)
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null || (!liveAdmin.IsSuperAdmin && !liveAdmin.CanEditEmployee))
            {
                alertMessage = "🚫 خطای امنیتی: شما مجوز توقف شیفت پرسنل را ندارید!";
                return;
            }
            var record = await DbContext.Attendances.FindAsync(recordId);
            if (record != null)
            {
                record.ClockOutTime = DateTime.Now;
                await DbContext.SaveChangesAsync();
                alertMessage = "توقف شیفت کاری اعمال شد";
                await LoadData();
            }
        }
        // احیای متد ثبت دستی تقویم روزهای گذشته
        private async Task AddManualAttendance()
        {
            manualErrorMessage = "";

            // 🔒 بررسی زنده مجوز ویرایش ادمین جاری از روی دیتابیس قبل از ثبت ردیف جدید
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null) { MyNavigationManager.NavigateTo("/admin", true); return; }

            if (!liveAdmin.IsSuperAdmin && !liveAdmin.CanEditEmployee)
            {
                manualErrorMessage = "🚫 شما مجوز ثبت تردد دستی برای پرسنل را ندارید!";
                return;
            }

            if (selectedEmployee == null || DbContext == null) return;

            if (!TimeSpan.TryParse(manualInTime, out var inTs) || !TimeSpan.TryParse(manualOutTime, out var outTs))
            {
                manualErrorMessage = "لطفاً فرمت ساعت را به درستی وارد کنید.";
                return;
            }

            if (outTs <= inTs)
            {
                manualErrorMessage = "ساعت خروج نمی‌تواند قبل از ساعت ورود باشد.";
                return;
            }

            try
            {
                DateTime fullClockIn = manualDate.Date.Add(inTs);
                DateTime fullClockOut = manualDate.Date.Add(outTs);

                var newRecord = new Attendance
                {
                    EmployeeId = selectedEmployee.Id,
                    ClockInTime = fullClockIn,
                    ClockOutTime = fullClockOut
                };

                DbContext.Attendances.Add(newRecord);
                await DbContext.SaveChangesAsync();

                manualDate = DateTime.Today;
                manualInTime = "08:00";
                manualOutTime = "17:00";
                manualErrorMessage = "🎉 حضور دستی با موفقیت در تاریخچه ثبت شد.";
                await LoadData();
            }
            catch (Exception ex)
            {
                manualErrorMessage = "خطا: " + ex.Message;
            }
        }

        private async Task OpenReportModal(Employee emp)
        {
            // 🔒 بررسی زنده اصالت توکن قبل از باز شدن مودال
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null)
            {
                // اگر توکن نبود یا ادمین نامعتبر بود، فوراً به صفحه لاگین هدایتش کن
                isLoggedIn = false;
                MyNavigationManager.NavigateTo("/admin", true);
                return;
            }

            selectedEmployee = emp;
            selectedDateForEdit = null;
            showModal = true;
        }

        private void CloseModal()
        {
            showModal = false;
            selectedEmployee = null;
            selectedDateForEdit = null;
        }
        private void SelectDateForEdit(DateTime date)
        {
            selectedDateForEdit = date;
        }
        private void BackToDaysList()
        {
            selectedDateForEdit = null;
        }
        // 🌟 آدرس‌دهی مستقیم به جنریک لیست سیستم جهت مهار تداخل کامپایلر
        private string CalculateDuration(System.Collections.Generic.List<sqlite_web.Components.Models.Attendance> records)
        {
            if (records == null || !records.Any()) return "۰ دقیقه";
            TimeSpan totalTime = TimeSpan.Zero;

            foreach (var record in records)
            {
                if (record.ClockInTime != null && record.ClockOutTime != null)
                {
                    var duration = record.ClockOutTime.Value - record.ClockInTime.Value;
                    if (duration.TotalSeconds > 0) totalTime += duration;
                }
            }

            int hours = (int)totalTime.TotalHours;
            int minutes = totalTime.Minutes;
            return hours > 0 ? $"{hours} ساعت و {minutes} دقیقه" : $"{minutes} دقیقه";
        }

        private string CalculateTotalWork(int empId)
        {
            var records = allAttendances.Where(a => a.EmployeeId == empId).ToList();
            return CalculateDuration(records);
        }
        private async Task UpdateClockInTime(int recordId, string? timeStr)
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null || (!liveAdmin.IsSuperAdmin && !liveAdmin.CanEditEmployee)) return;
            if (string.IsNullOrEmpty(timeStr) || selectedDateForEdit == null) return;
            var record = await DbContext.Attendances.FindAsync(recordId);
            if (record != null && TimeSpan.TryParse(timeStr, out var ts))
            {
                var baseDate = record.ClockInTime?.Date ?? selectedDateForEdit.Value.Date;
                record.ClockInTime = baseDate.Add(ts);
                await DbContext.SaveChangesAsync();
                await LoadData();
            }
        }
        private async Task UpdateClockOutTime(int recordId, string? timeStr)
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null || (!liveAdmin.IsSuperAdmin && !liveAdmin.CanEditEmployee)) return;
            if (string.IsNullOrEmpty(timeStr) || selectedDateForEdit == null) return;
            var record = await DbContext.Attendances.FindAsync(recordId);
            if (record != null && TimeSpan.TryParse(timeStr, out var ts))
            {
                var baseDate = record.ClockInTime?.Date ?? selectedDateForEdit.Value.Date;
                record.ClockOutTime = baseDate.Add(ts);
                await DbContext.SaveChangesAsync();
                await LoadData();
            }
        }
        private async Task DeleteSingleAttendance(int id)
        {
            var liveAdmin = await GetCurrentAdminLiveAsync();
            if (liveAdmin == null || (!liveAdmin.IsSuperAdmin && !liveAdmin.CanDeleteEmployee)) return;
            var record = await DbContext.Attendances.FindAsync(id);
            if (record != null)
            {
                DbContext.Attendances.Remove(record);
                await DbContext.SaveChangesAsync();
                await LoadData();
            }
        }
        private void StartEmpEdit(Employee emp)
        {
            isEmpEditMode = true;
            editingEmployeeId = emp.Id;
            newEmpName = emp.FullName;
            newEmpCode = emp.PersonnelCode;
            newEmpPhone = emp.Phone;
            previewImageUrl = emp.ImagePath;
            uploadedFileFile = null;
        }
        private async Task CancelEmpEdit()
        {
            isEmpImageInvalid = false;
            empImageError = "";

            isEmpEditMode = false;
            editingEmployeeId = 0;
            newEmpName = ""; newEmpCode = ""; newEmpPhone = ""; previewImageUrl = ""; uploadedFileFile = null;
            isEmpNameInvalid = false;
            isEmpCodeInvalid = false;
            isEmpPhoneInvalid = false;
            empNameError = empCodeError = empPhoneError = "";
            await Task.CompletedTask;
        }
    }
}