// دریافت موقعیت جغرافیایی لحظه‌ای دستگاه از مرورگر با مدیریت خطاهای دسترسی
window.getDeviceLocation = () => {
    return new Promise((resolve) => {
        if (!navigator.geolocation) {
            resolve({ success: false, errorCode: "NOT_SUPPORTED" });
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    success: true,
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude,
                    accuracy: position.coords.accuracy
                });
            },
            (error) => {
                let code = "UNKNOWN";
                if (error.code === 1) code = "PERMISSION_DENIED";
                else if (error.code === 2) code = "POSITION_UNAVAILABLE";
                else if (error.code === 3) code = "TIMEOUT";

                resolve({ success: false, errorCode: code });
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    });
};
