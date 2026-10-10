using Microsoft.EntityFrameworkCore;
using sqlite_web;
using sqlite_web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("SqliteConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// 🟢 بهینه‌سازی کانال ارتباطی تعاملی و افزایش سقف پیام‌ها برای جابه‌جایی بدون کرش عکس پرسنل
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // افزایش سقف به ۱۰ مگابایت
    });


// ثبت سرویس مدیریت وضعیت ادمین به صورت Scoped (مخصوص هر سشن کاربر)
builder.Services.AddScoped<sqlite_web.Services.AdminStateService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// 🌟 توضیحات کد: ارجاع متمرکز فرآیند ساخت و مقداردهی اولیه سوپر ادمین ارشد به هسته سرویس ادمین
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // فراخوانی مستقیم کلاس وضعیت ادمین از لایه خدمات تزریق شده سیستم
        var adminState = services.GetRequiredService<sqlite_web.Services.AdminStateService>();

        // اصلاح الگو: اجرای امن و مستقیم متد آسنکرون بدون خطر مسدودسازی ریسمان‌ها (Thread Blocking)
        await adminState.InitializeDefaultSuperAdminAsync();
    }
    catch (Exception ex)
    {
        // رفع وارنینگ CS0168 با چاپ واقعی خطا در محیط کنسول سرور
        Console.WriteLine($"❌ خطا در ساخت دیتابیس یا مقداردهی اولیه ادمین: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

app.Run();
