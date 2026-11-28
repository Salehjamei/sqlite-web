using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using sqlite_web.Components.Models;
using sqlite_web.Components.Models.Calendar;
using sqlite_web.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace sqlite_web.Components.Modules
{
    // ماژول مستقل نقشه تعاملی برای انتخاب محدوده جغرافیایی و نمایش مودال موقعیت ورود/خروج پرسنل
    public partial class InteractiveMap : ComponentBase, IDisposable
    {
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public SystemErrorLoggerService ErrorLogger { get; set; } = default!;
        [Inject] public AdminStateService AdminState { get; set; } = default!;
        // تزریق سرویس مرکزی مدیریت موقعیت جغرافیایی و مکان‌های مجاز
        [Inject] public GeoLocationService GeoService { get; set; } = default!;

        [Parameter] public string MapElementId { get; set; } = $"spn_map_{Guid.NewGuid():N}";
        [Parameter] public string Height { get; set; } = "230px";
        [Parameter] public bool IsPickerMode { get; set; } = true;

        // پارامترهای حالت انتخاب مکان (Picker Mode)
        [Parameter] public double Latitude { get; set; } = 40.765400;
        [Parameter] public double Longitude { get; set; } = 29.940800;
        [Parameter] public int RadiusMeters { get; set; } = 100;
        [Parameter] public EventCallback<(double Lat, double Lng)> OnLocationPicked { get; set; }

        // پارامترهای حالت مودال نمایش تردد کارمند در کارنامه (Modal View Mode)
        [Parameter] public bool IsModal { get; set; } = false;
        [Parameter] public bool ShowModal { get; set; } = false;
        [Parameter] public EventCallback<bool> ShowModalChanged { get; set; }
        [Parameter] public Attendance? AttendanceRecord { get; set; }
        [Parameter] public string EmployeeName { get; set; } = string.Empty;
        [Parameter] public string CurrentLanguage { get; set; } = "fa";
        [Parameter] public PCalendar CalendarManager { get; set; } = new();

        private DotNetObjectReference<InteractiveMap>? _dotNetRef;
        private bool _isMapInitialized = false;
        private double _lastLat;
        private double _lastLng;
        private int _lastRadius;
        private int _lastRenderedAttId = 0;

        // راه‌اندازی نقشه در حالت تعبیه داخل فرم (IsModal = false)
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !IsModal)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await RenderPickerMapAsync();
                _isMapInitialized = true;
            }
            else if (IsModal && ShowModal && AttendanceRecord != null && _lastRenderedAttId != AttendanceRecord.Id)
            {
                // راه‌اندازی نقشه کارنامه به محض باز شدن پنجره مودال
                _lastRenderedAttId = AttendanceRecord.Id;
                await RenderAttendanceViewMapAsync();
            }
        }

        // به‌روزرسانی دایره شعاع و مرکز نقشه هنگام تغییر پارامترهای فرم مکان
        protected override async Task OnParametersSetAsync()
        {
            if (!ShowModal)
            {
                _lastRenderedAttId = 0;
            }

            if (!_isMapInitialized || !IsPickerMode || IsModal) return;

            if (Math.Abs(_lastLat - Latitude) > 0.000001 ||
                Math.Abs(_lastLng - Longitude) > 0.000001 ||
                _lastRadius != RadiusMeters)
            {
                _lastLat = Latitude;
                _lastLng = Longitude;
                _lastRadius = RadiusMeters;

                try
                {
                    await JSRuntime.InvokeVoidAsync("spnMapManager.updatePickerMap", Latitude, Longitude, RadiusMeters);
                }
                catch (JSDisconnectedException) { }
                catch (Exception ex)
                {
                    await ErrorLogger.LogBackendErrorAsync("InteractiveMap.UpdatePickerMap", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername);
                }
            }
        }

        // رسم اولیه نقشه انتخاب محدوده و شعاع مجاز
        private async Task RenderPickerMapAsync()
        {
            try
            {
                _lastLat = Latitude;
                _lastLng = Longitude;
                _lastRadius = RadiusMeters;
                await JSRuntime.InvokeVoidAsync("spnMapManager.initPickerMap", MapElementId, Latitude, Longitude, RadiusMeters, _dotNetRef);
            }
            catch (JSDisconnectedException) { }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("InteractiveMap.RenderPickerMap", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername);
            }
        }

        // استخراج نام انجام‌دهنده و نوع عملیات برای ثبت ورود جهت نمایش در مودال و پاپ‌آپ نقشه
        protected string GetClockInActorText()
        {
            // اگر رکورد تردد انتخاب نشده باشد، رشته خالی برگردانده می‌شود
            if (AttendanceRecord == null) return string.Empty;

            // نگهداری نام کاربری ادمین ثبت‌کننده ورود در صورت وجود
            string adminName = string.Empty;

            // بررسی اینکه آیا ثبت ورود توسط یک ادمین انجام شده است یا خیر
            if (AttendanceRecord.CreatedByAdminId.HasValue)
            {
                // جستجوی مشخصات ادمین ثبت‌کننده در لیست مدیران بارگذاری‌شده سیستم
                var admin = AdminState.AdminList?.FirstOrDefault(a => a.Id == AttendanceRecord.CreatedByAdminId.Value);

                // استخراج نام کاربری ادمین از لیست، یا از نشست آنلاین فعلی و یا نمایش شناسه عددی او در صورت نبود نام
                adminName = admin?.Username ?? (AdminState.CurrentOnlineAdmin?.Id == AttendanceRecord.CreatedByAdminId.Value
                    ? AdminState.CurrentOnlineAdmin.Username
                    : $"Admin #{AttendanceRecord.CreatedByAdminId}");
            }

            // بازگرداندن برچسب «ثبت سریع» همراه با نام ادمین در صورت ثبت سریع از پنل مدیریت
            if (AttendanceRecord.EntrySource == "AdminFast")
                return $"{CalendarManager.Translate("PCal_MainHome_SourceFast")} ({adminName})";

            // بازگرداندن برچسب «ثبت دستی» همراه با نام ادمین در صورت ورود دستی از کارنامه
            if (AttendanceRecord.EntrySource == "AdminManual")
                return $"{CalendarManager.Translate("PCal_MainHome_SourceManual")} ({adminName})";

            // استخراج نام مکان مجاز در صورت ثبت شدن تردد در محدوده یک مکان تعریف‌شده
            string loc = !string.IsNullOrWhiteSpace(AttendanceRecord.LocationName) ? $" - {AttendanceRecord.LocationName}" : "";

            // بازگرداندن برچسب «دستگاه کیوسک» همراه با نام مکان مجاز به عنوان حالت پیش‌فرض
            return $"{CalendarManager.Translate("PCal_MainHome_SourceKiosk")}{loc}";
        }

        // استخراج نام انجام‌دهنده و نوع عملیات برای ثبت خروج جهت نمایش در مودال و پاپ‌آپ نقشه
        protected string GetClockOutActorText()
        {
            // اگر رکورد تردد وجود نداشته باشد یا هنوز خروج برای آن ثبت نشده باشد، رشته خالی برمی‌گردد
            if (AttendanceRecord == null || !AttendanceRecord.ClockOutTime.HasValue) return string.Empty;

            // تشخیص بسته شدن خودکار تردد توسط سرویس پس‌زمینه سیستم در ساعت 23:59
            if (AttendanceRecord.EntrySource == "AutoClose" || AttendanceRecord.ClockOutTime.Value.ToString("HH:mm") == "23:59")
                return CalendarManager.Translate("PCal_MainHome_SourceAuto");

            // دریافت شناسه آخرین ادمین ویرایش‌کننده یا ادمین ثبت‌کننده برای خروج‌های مدیریتی
            int? actorId = AttendanceRecord.LastModifiedByAdminId ?? AttendanceRecord.CreatedByAdminId;
            if (actorId.HasValue)
            {
                // جستجوی مشخصات ادمین انجام‌دهنده در لیست مدیران سیستم
                var admin = AdminState.AdminList?.FirstOrDefault(a => a.Id == actorId.Value);

                // استخراج نام کاربری ادمین از لیست، یا نشست آنلاین فعلی و یا نمایش شناسه عددی او
                string adminName = admin?.Username ?? (AdminState.CurrentOnlineAdmin?.Id == actorId.Value
                    ? AdminState.CurrentOnlineAdmin.Username
                    : $"Admin #{actorId}");

                // تعیین عنوان نوع عملیات خروج (ثبت دستی یا ثبت سریع ادمین)
                string sourceLabel = AttendanceRecord.EntrySource == "AdminManual"
                    ? CalendarManager.Translate("PCal_MainHome_SourceManual")
                    : CalendarManager.Translate("PCal_MainHome_SourceFast");

                // بازگرداندن نوع عملیات خروج به همراه نام کاربری ادمین انجام‌دهنده
                return $"{sourceLabel} ({adminName})";
            }

            // استخراج نام مکان مجاز ثبت‌شده در صورت خروج عادی از طریق دستگاه کیوسک
            string loc = !string.IsNullOrWhiteSpace(AttendanceRecord.LocationName) ? $" - {AttendanceRecord.LocationName}" : "";

            // بازگرداندن برچسب «دستگاه کیوسک» به همراه نام مکان مجاز
            return $"{CalendarManager.Translate("PCal_MainHome_SourceKiosk")}{loc}";
        }

        // رسم نقاط ورود (پین سبز)، خروج (پین قرمز) و دایره شعاع مجاز مکان درون مودال نقشه
        private async Task RenderAttendanceViewMapAsync()
        {
            // توقف اجرا در صورت تهی بودن رکورد تردد انتخاب‌شده
            if (AttendanceRecord == null) return;

            try
            {
                // تجزیه رشته‌های مختصات جغرافیایی ورود و خروج به مقادیر عددی عرض و طول جغرافیایی
                var inCoords = ParseCoordPair(AttendanceRecord.ClockInLocation);
                var outCoords = ParseCoordPair(AttendanceRecord.ClockOutLocation);

                // واکشی اطلاعات مرکز و شعاع مجاز مکان ثبت‌شده از طریق سرویس GeoLocationService
                double? locLat = null;
                double? locLng = null;
                int locRadius = 0;

                if (!string.IsNullOrWhiteSpace(AttendanceRecord.LocationName))
                {
                    var matchedLoc = await GeoService.GetAllowedLocationByNameAsync(AttendanceRecord.LocationName);
                    if (matchedLoc != null)
                    {
                        locLat = matchedLoc.Latitude;
                        locLng = matchedLoc.Longitude;
                        locRadius = matchedLoc.RadiusMeters;
                    }
                }

                // ساخت محتوای HTML پاپ‌آپ پین سبز ورود شامل ساعت و مشخصات انجام‌دهنده
                string inLabel = $"<div style='text-align:center; font-family:Tahoma; font-size:11px;'><b>🟢 {CalendarManager.Translate("PCal_AdminPanel_LabelClockIn")} {AttendanceRecord.ClockInTime?.ToString("HH:mm")}</b><br/><span style='color:#555;'>{GetClockInActorText()}</span></div>";

                // ساخت محتوای HTML پاپ‌آپ پین قرمز خروج شامل ساعت و مشخصات انجام‌دهنده
                string outLabel = $"<div style='text-align:center; font-family:Tahoma; font-size:11px;'><b>🔴 {CalendarManager.Translate("PCal_AdminPanel_LabelClockOut")} {AttendanceRecord.ClockOutTime?.ToString("HH:mm")}</b><br/><span style='color:#555;'>{GetClockOutActorText()}</span></div>";

                // فراخوانی تابع جاوااسکریپت جهت ترسیم نقشه، دایره شعاع مجاز مکان و پین‌های ورود و خروج
                await JSRuntime.InvokeVoidAsync("spnMapManager.initAttendanceViewMap",
                    MapElementId,
                    inCoords.lat, inCoords.lng,
                    outCoords.lat, outCoords.lng,
                    inLabel, outLabel,
                    locLat, locLng, locRadius, AttendanceRecord.LocationName);
            }
            catch (JSDisconnectedException)
            {
                // نادیده گرفتن خطای قطع ارتباط طبیعی مرورگر هنگام بستن یا رفرش صفحه
            }
            catch (Exception ex)
            {
                // ثبت جزئیات خطای فنی رسم نقشه تردد در تاریخچه اعلانات سوپر ادمین
                await ErrorLogger.LogBackendErrorAsync("InteractiveMap.RenderAttendanceViewMap", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername);
            }
        }

        // تجزیه رشته مختصات جغرافیایی "lat,lng" به دو عدد اعشاری استاندارد
        protected (double? lat, double? lng) ParseCoordPair(string? coord)
        {
            if (string.IsNullOrWhiteSpace(coord)) return (null, null);
            var parts = coord.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(parts[1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double lng))
            {
                return (lat, lng);
            }
            return (null, null);
        }

        // بستن مودال نقشه و اطلاع‌رسانی به کامپوننت والد
        protected async Task CloseModalAsync()
        {
            ShowModal = false;
            _lastRenderedAttId = 0;
            await ShowModalChanged.InvokeAsync(false);
        }

        [JSInvokable]
        public async Task OnMapPointSelected(double lat, double lng)
        {
            try
            {
                _lastLat = lat;
                _lastLng = lng;
                if (OnLocationPicked.HasDelegate)
                {
                    await OnLocationPicked.InvokeAsync((lat, lng));
                }
            }
            catch (Exception ex)
            {
                await ErrorLogger.LogBackendErrorAsync("InteractiveMap.OnMapPointSelected", ex, AdminState.CurrentOnlineAdmin?.Id, AdminState.adminUsername);
            }
        }

        public void Dispose()
        {
            _dotNetRef?.Dispose();
        }
    }
}
