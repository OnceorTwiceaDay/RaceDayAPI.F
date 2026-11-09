using RaceDay.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "RaceDay.Mvc.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services.AddHttpClient<RaceDayApiClient>(client =>
{
    var url = builder.Configuration["RaceDayApi:BaseUrl"] ?? "https://localhost:59670/";
    client.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Never use a shared CookieContainer: sessions belong to individual MVC visitors.
    UseCookies = false,
    AllowAutoRedirect = false
});

var app = builder.Build();
app.Logger.LogInformation("RaceDay.MVC API base URL: {BaseUrl}", builder.Configuration["RaceDayApi:BaseUrl"] ?? "https://localhost:59670/");
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
public partial class Program { }
