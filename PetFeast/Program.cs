using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models;
using PetFeast.Models.Interfaces;
using PetFeast.Models.Services;
using PetFeast.Models.Identity;
using Microsoft.AspNetCore.Identity;var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<EmailService>();
builder.Services.AddDbContext<PetFeastDBContext>(
options => {options.UseSqlServer(builder.Configuration.GetConnectionString("PetFeastDBContextConnection"));});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<PetFeastDBContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<CategoryIRepository, CategoryRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IShoppingCartRepository, ShoppingCartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<PointsRepository>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();

    // T?o role Admin n?u ch?a t?n t?i
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
            Email = email
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
