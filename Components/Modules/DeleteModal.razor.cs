using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace sqlite_web.Components.Modules
{
    /// <summary>
    /// کلاس منطق پشت‌صحنه (Code-Behind) کامپوننت ماژولار و تفکیک شده مودال حذف قطعی
    /// </summary>
    public partial class DeleteModal : ComponentBase
    {
        // 🔘 ورودی‌های داینامیک و عمومی کامپوننت (Parameters) واکشی شده از صفحات اصلی

        [Parameter] public bool Show { get; set; } = false;
        [Parameter] public string Title { get; set; } = "تاییدیه حذف قطعی اطلاعات";
        [Parameter] public string Message { get; set; } = string.Empty;
        [Parameter] public string SubMessage { get; set; } = "توجه: این عملیات غیرقابل بازگشت است و دیتا کاملاً پاک خواهد شد.";
        // 🟢 پارامتر جدید دریافت نام پرسنل یا ادمین هدف جهت نمایش اختصاصی در کادر پاپ‌آپ
        [Parameter] public string TargetName { get; set; } = string.Empty;

        // 🟢 رویدادهای بازگشتی (EventCallbacks) جهت اعلام کلیک کلاینت به متدهای صفحه اصلی

        [Parameter] public EventCallback OnConfirm { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }

        /// <summary>
        /// متد مدیریت رویداد کلیک روی دکمه سرخ رنگ تایید حذف
        /// </summary>
        protected async Task HandleConfirmClick()
        {
            if (OnConfirm.HasDelegate)
            {
                await OnConfirm.InvokeAsync();
            }
        }

        /// <summary>
        /// متد مدیریت رویداد کلیک روی دکمه انصراف یا لایه تیره بیرون کادر
        /// </summary>
        protected async Task HandleCancelClick()
        {
            if (OnCancel.HasDelegate)
            {
                await OnCancel.InvokeAsync();
            }
        }
    }
}
