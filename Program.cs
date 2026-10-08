using Microsoft.EntityFrameworkCore;
using sqlite_web;
using sqlite_web.Components;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("SqliteConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
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

        // اجرای متد ساخت متمرکز دیتابیس ادمین اولیه به صورت ناهمزمان
        Task.Run(async () => await adminState.InitializeDefaultSuperAdminAsync()).Wait();
    }
    catch (Exception ex)
    {
        // مهار خطاهای احتمالی فاز لود اولیه روی هارد سرور
    }
}

app.Run();

