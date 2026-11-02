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
        [Parameter] public string Title { get; set; }
        [Parameter] public string Message { get; set; }
        [Parameter] public string SubMessage { get; set; }
        // 🟢 پارامتر جدید دریافت نام پرسنل یا ادمین هدف جهت نمایش اختصاصی در کادر پاپ‌آپ
        [Parameter] public string TargetName { get; set; }
        [Parameter] public string SelectedItemLabel { get; set; }
        [Parameter] public string DeleteConfirmBtn { get; set; }
        [Parameter] public string kancelBtn { get; set; }
        // 🟢 رویدادهای بازگشتی (EventCallbacks) جهت اعلام کلیک کلاینت به متدهای صفحه اصلی

        [Parameter] public EventCallback OnConfirm { get; set; }
        [Parameter] public EventCallback OnCancel { get; set; }
        /// <summary>
        /// متد مدیریت رویداد کلیک روی دکمه سرخ رنگ تایید حذف
        /// </summary>
        protected async Task HandleConfirmClick()
        {
            if (OnConfirm.HasDelegate) { await OnConfirm.InvokeAsync(); }
        }
        /// <summary>
        /// متد مدیریت رویداد کلیک روی دکمه انصراف یا لایه تیره بیرون کادر
        /// </summary>
        protected async Task HandleCancelClick()
        {
            if (OnCancel.HasDelegate) { await OnCancel.InvokeAsync(); }
        }
    }
}
