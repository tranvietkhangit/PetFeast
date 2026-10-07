using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PetFeast.Data;
using PetFeast.Models;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Services;
using PetFeast.Models.Identity;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// HttpContext
builder.Services.AddHttpContextAccessor();

// Database
builder.Services.AddDbContext<PetFeastDBContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "PetFeastDBContextConnection"));
});

// Email
builder.Services.AddScoped<EmailService>();
// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Đăng nhập
    options.SignIn.RequireConfirmedEmail = true;
    options.SignIn.RequireConfirmedAccount = false;

    // Email không được trùng
    options.User.RequireUniqueEmail = true;

    // LOCKOUT

    // Cho phép khóa tài khoản
    options.Lockout.AllowedForNewUsers = true;

    // Sai tối đa 5 lần
    options.Lockout.MaxFailedAccessAttempts = 5;

    // Khóa 5 phút
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<PetFeastDBContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";

    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect("/Error/403");
        return Task.CompletedTask;
    };
});

// Repositories
builder.Services.AddScoped<CategoryIRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<PointsRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
// Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<PetFeastDBContext>();

    db.Database.Migrate();
}
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseExceptionHandler("/Error");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Tạo Admin mặc định
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("Admin"));
    }

    string email = "admin@gmail.com";
    string password = "Admin@123";

    var admin =
        await userManager.FindByEmailAsync(email);

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result =
            await userManager.CreateAsync(admin, password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}

app.Run();