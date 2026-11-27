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

        // رسم نقاط ورود (سبز) و خروج (قرمز) کارمند درون مودال نقشه
        private async Task RenderAttendanceViewMapAsync()
        {
            if (AttendanceRecord == null) return;

            try
            {
                var inCoords = ParseCoordPair(AttendanceRecord.ClockInLocation);
                var outCoords = ParseCoordPair(AttendanceRecord.ClockOutLocation);

                string inLabel = $"🟢 {CalendarManager.Translate("PCal_AdminPanel_LabelClockIn")} {AttendanceRecord.ClockInTime?.ToString("HH:mm")}";
                string outLabel = $"🔴 {CalendarManager.Translate("PCal_AdminPanel_LabelClockOut")} {AttendanceRecord.ClockOutTime?.ToString("HH:mm")}";

                await JSRuntime.InvokeVoidAsync("spnMapManager.initAttendanceViewMap",
                    MapElementId, inCoords.lat, inCoords.lng, outCoords.lat, outCoords.lng, inLabel, outLabel);
            }
            catch (JSDisconnectedException) { }
            catch (Exception ex)
            {
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
