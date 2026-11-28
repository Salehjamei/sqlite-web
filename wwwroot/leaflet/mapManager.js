window.spnMapManager = {
    pickerMap: null,
    pickerMarker: null,
    pickerCircle: null,
    viewMap: null,

    // راه‌اندازی نقشه تعاملی انتخاب مکان مجاز همراه با رسم دایره شعاع و رویداد کلیک روی نقشه
    initPickerMap: function (elementId, lat, lng, radiusMeters, dotNetRef) {
        setTimeout(function () {
            var container = document.getElementById(elementId);
            if (!container || typeof L === 'undefined') return;

            if (window.spnMapManager.pickerMap) {
                window.spnMapManager.pickerMap.remove();
                window.spnMapManager.pickerMap = null;
            }

            var map = L.map(elementId).setView([lat, lng], 16);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '© OpenStreetMap'
            }).addTo(map);

            var marker = L.marker([lat, lng], { draggable: true }).addTo(map);
            var circle = L.circle([lat, lng], {
                color: '#0d6efd',
                fillColor: '#0d6efd',
                fillOpacity: 0.2,
                radius: radiusMeters
            }).addTo(map);

            // ارسال مختصات به C# هنگام کلیک روی هر نقطه از نقشه
            map.on('click', function (e) {
                marker.setLatLng(e.latlng);
                circle.setLatLng(e.latlng);
                dotNetRef.invokeMethodAsync('OnMapPointSelected', e.latlng.lat, e.latlng.lng);
            });

            // ارسال مختصات به C# هنگام کشیدن و رها کردن نشانگر روی نقشه
            marker.on('dragend', function (e) {
                var pos = marker.getLatLng();
                circle.setLatLng(pos);
                dotNetRef.invokeMethodAsync('OnMapPointSelected', pos.lat, pos.lng);
            });

            window.spnMapManager.pickerMap = map;
            window.spnMapManager.pickerMarker = marker;
            window.spnMapManager.pickerCircle = circle;

            setTimeout(function () { map.invalidateSize(); }, 200);
        }, 150);
    },

    // تغییر زنده مرکز و شعاع دایره روی نقشه هنگام تغییر عدد شعاع یا دریافت GPS
    updatePickerMap: function (lat, lng, radiusMeters) {
        if (!window.spnMapManager.pickerMap) return;
        var latlng = [lat, lng];
        window.spnMapManager.pickerMap.setView(latlng, 16);
        if (window.spnMapManager.pickerMarker) window.spnMapManager.pickerMarker.setLatLng(latlng);
        if (window.spnMapManager.pickerCircle) {
            window.spnMapManager.pickerCircle.setLatLng(latlng);
            window.spnMapManager.pickerCircle.setRadius(radiusMeters);
        }
    },

    // نمایش نقشه موقعیت ثبت ورود (سبز)، خروج (قرمز) و دایره محدوده شعاع مجاز مکان در مودال کارنامه ادمین
    initAttendanceViewMap: function (elementId, inLat, inLng, outLat, outLng, inLabel, outLabel, locLat, locLng, locRadius, locName) {
        setTimeout(function () {
            var container = document.getElementById(elementId);
            if (!container || typeof L === 'undefined') return;

            if (window.spnMapManager.viewMap) {
                window.spnMapManager.viewMap.remove();
                window.spnMapManager.viewMap = null;
            }

            var centerLat = inLat || outLat || locLat;
            var centerLng = inLng || outLng || locLng;
            if (!centerLat || !centerLng) return;

            var map = L.map(elementId).setView([centerLat, centerLng], 16);

            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '© OpenStreetMap'
            }).addTo(map);

            var bounds = [];

            // ترسیم دایره آبی شعاع مجاز مکان در صورت وجود اطلاعات مکان تطبیق‌یافته
            if (locLat && locLng && locRadius > 0) {
                var fenceCircle = L.circle([locLat, locLng], {
                    color: '#0d6efd',
                    fillColor: '#0d6efd',
                    fillOpacity: 0.15,
                    weight: 2,
                    radius: locRadius
                }).addTo(map);

                if (locName) {
                    fenceCircle.bindTooltip(locName, { permanent: false, direction: 'top' });
                }
                bounds.push([locLat, locLng]);
            }

            // ترسیم نقطه سبز ثبت ورود کارمند
            if (inLat && inLng) {
                var inCircle = L.circleMarker([inLat, inLng], {
                    radius: 9, color: '#198754', fillColor: '#28a745', fillOpacity: 0.95
                }).addTo(map);
                inCircle.bindPopup(inLabel).openPopup();
                bounds.push([inLat, inLng]);
            }

            // ترسیم نقطه قرمز ثبت خروج کارمند
            if (outLat && outLng) {
                var outCircle = L.circleMarker([outLat, outLng], {
                    radius: 9, color: '#dc3545', fillColor: '#dc3545', fillOpacity: 0.95
                }).addTo(map);
                outCircle.bindPopup(outLabel);
                bounds.push([outLat, outLng]);
            }

            if (bounds.length > 1) {
                map.fitBounds(bounds, { padding: [45, 45] });
            }

            window.spnMapManager.viewMap = map;
            setTimeout(function () { map.invalidateSize(); }, 200);
        }, 150);
    }
};
