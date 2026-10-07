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

// 🌟 ایجاد خودکار اولین ادمین سیستم با تمام دسترسی‌ها در اولین لود برنامه
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // 🛠️ اصلاح مسیر: لود دیتابیس از فضای نام مستقل پروژه‌تان (پوشه Data)
        var context = services.GetRequiredService<sqlite_web.AppDbContext>();

        // اطمینان از اینکه دیتابیس و جداول SQLite حتماً ساخته شده‌اند
        context.Database.EnsureCreated();

        // اگر جدول ادمین‌ها کاملاً خالی بود، اولین مدیر ارشد سیستم را بساز
        if (!context.Admins.Any())
        {
            // 🛠️ اصلاح مسیر: لود دقیق کلاس AdminUser از پوشه اختصاصی مدل‌های ادمین شما
            var initialSuperAdmin = new sqlite_web.Components.Models.Admin.AdminUser
            {
                Username = "AdminTop",
                Password = "AdminTop", // رمز عبور ادمین اولیه
                IsSuperAdmin = true,   // 👑 دارای دسترسی ارشد مدیریت مدیران
                CanAddEmployee = true, // ➕ دارای دسترسی ثبت کارمندان
                CanEditEmployee = true,// ✏️ دارای دسترسی ویرایش کارمندان
                CanDeleteEmployee = true // ❌ دارای دسترسی حذف کارمندان
            };

            context.Admins.Add(initialSuperAdmin);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // مهار خطاهای احتمالی در لود اولیه دیتابیس لوکال
    }
}
app.Run();

