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

// 🌟 کدهای ایجاد خودکار ادمین ارشد اولیه در صورت خالی بودن دیتابیس
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        
        // مطمئن می‌شویم دیتابیس و جدول‌ها ساخته شده‌اند
        context.Database.EnsureCreated();

        // اگر هیچ مدیری در جدول نبود، ادمین ارشد را بساز
        if (!context.Admins.Any())
        {
            context.Admins.Add(new sqlite_web.Components.Models.Admin.AdminUser
            {
                FullName = "مدیر ارشد سیستم",
                Username = "AdminTop",
                Password = "AdminTop",
                IsSuperAdmin = true ,// 👈 این اکانت برای همیشه تنها مدیر ارشد دیتابیس خواهد بود
                CanAddEmployee = true,
                CanEditEmployee = true,
                CanDeleteEmployee = true
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // در صورت بروز خطا در لود اولیه، برنامه متوقف نشود
        Console.WriteLine("خطا در ایجاد ادمین اولیه: " + ex.Message);
    }
}

// این خط از قبل در انتهای فایل شما وجود دارد:
app.Run();

