using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using sqlite_web.Components.Models.Calendar;
using System;
using System.IO;
using System.Threading.Tasks;

namespace sqlite_web.Services
{
    // سرویس مرکزی مدیریت، فشرده‌سازی، ذخیره‌سازی و پاکسازی تصاویر پرسنل و مدیران سیستم
    public class ImageStorageService
    {
        // تزریق محیط میزبانی وب جهت دسترسی به مسیر فیزیکی پوشه wwwroot
        private readonly IWebHostEnvironment _webHostEnvironment;

        // تزریق سرویس ثبت خطاهای سیستمی در سرور جهت ثبت خطاهای احتمالی کار با فایل‌ها
        private readonly SystemErrorLoggerService _errorLogger;

        // سقف مجاز حجم فایل ورودی معادل ۲۰۰ مگابایت
        private const long MaxAllowedFileSizeBytes = 200L * 1024 * 1024;

        // سازنده سرویس و دریافت وابستگی‌های مورد نیاز از DI
        public ImageStorageService(IWebHostEnvironment webHostEnvironment, SystemErrorLoggerService errorLogger)
        {
            _webHostEnvironment = webHostEnvironment;
            _errorLogger = errorLogger;
        }

        // کاهش ابعاد تصویر به حداکثر ۵۰۰ در ۵۰۰ پیکسل، فشرده‌سازی JPEG و ذخیره در زیرپوشه مشخص‌شده درون wwwroot
        public async Task<(bool Success, string ImageUrl, string ErrorMessage)> SaveCompressedImageAsync(
            IBrowserFile? file,
            string subFolder,
            string filePrefix,
            string? previousImageUrl = null,
            string currentLanguage = "fa",
            int? actorAdminId = null,
            string? actorAdminUsername = null)
        {
            // در صورت انتخاب نشدن فایل، عملیات بدون خطا متوقف می‌شود
            if (file == null) return (false, string.Empty, string.Empty);

            try
            {
                // بررسی عدم عبور حجم فایل انتخاب‌شده از سقف مجاز ۲۰۰ مگابایت همراه با ترجمه خطا
                if (file.Size > MaxAllowedFileSizeBytes)
                {
                    var pCal = new PCalendar { CurrentLanguage = currentLanguage };
                    return (false, string.Empty, pCal.Translate("PCal_AdminModal_ImageSizeError"));
                }

                // ساخت مسیر فیزیکی پوشه مقصد درون wwwroot/uploads/{subFolder} در صورت عدم وجود
                string targetDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", subFolder);
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // حذف فایل تصویر قدیمی از روی دیسک در صورت وجود مسیر نسبی قبلی جهت جلوگیری از انباشت فایل‌ها
                DeleteImageFile(previousImageUrl);

                // کاهش ابعاد تصویر به حداکثر ۵۰۰ در ۵۰۰ پیکسل و تبدیل به فرمت بهینه JPEG
                var compressedFile = await file.RequestImageFileAsync("image/jpeg", 500, 500);

                // تولید نام یکتا برای فایل جدید با پسوند استاندارد jpg
                string uniqueFileName = $"{filePrefix}_{Guid.NewGuid():N}.jpg";
                string fullFilePath = Path.Combine(targetDirectory, uniqueFileName);

                // ذخیره جریانی فایل فشرده‌شده روی دیسک سرور با سقف جریان ۲۰۰ مگابایت
                await using var fileStream = new FileStream(fullFilePath, FileMode.Create);
                await compressedFile.OpenReadStream(maxAllowedSize: MaxAllowedFileSizeBytes).CopyToAsync(fileStream);

                // ساخت آدرس نسبی قابل نمایش در مرورگر و بازگرداندن نتیجه موفق
                string relativeUrl = $"/uploads/{subFolder}/{uniqueFileName}";
                return (true, relativeUrl, string.Empty);
            }
            catch (Exception ex)
            {
                // ثبت جزئیات خطای فنی در جدول اعلانات سوپر ادمین و بازگرداندن متن خطا
                await _errorLogger.LogBackendErrorAsync($"ImageStorageService.SaveCompressedImageAsync({subFolder})", ex, actorAdminId, actorAdminUsername, null, currentLanguage);
                return (false, string.Empty, ex.Message);
            }
        }

        // حذف ایمن فایل تصویر از روی دیسک سرور بر اساس مسیر نسبی ذخیره‌شده در دیتابیس
        public void DeleteImageFile(string? imageUrl)
        {
            try
            {
                // بررسی اینکه مسیر تصویر یک فایل ذخیره‌شده در پوشه uploads باشد (و نه رشته Base64 قدیمی)
                if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                    return;

                // تبدیل مسیر نسبی وب به مسیر فیزیکی سیستم عامل و حذف فایل در صورت وجود
                string relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // نادیده گرفتن خطای احتمالی در دسترس نبودن فایل قدیمی
            }
        }
    }
}
