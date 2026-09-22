
using JwtAppToApp.Client.Abstractions;
using JwtAppToApp.Client.Services;


var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------
// MVC
// --------------------------------------------------

builder.Services
    .AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Insert(0, "/Views/{0}.cshtml");
    });

// --------------------------------------------------
// خواندن و اعتبارسنجی BaseUrl
// --------------------------------------------------

var configuredBaseUrl = builder.Configuration["WEB_API_CREDENTIALS:AIRIC_API:BaseUrl"];

if (string.IsNullOrWhiteSpace(configuredBaseUrl))
{
    throw new InvalidOperationException("تنظیم WEB_API_CREDENTIALS:AIRIC_API:BaseUrl در appsettings.json پیدا نشد.");
}

configuredBaseUrl = configuredBaseUrl.Trim();

if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var parsedBaseAddress) || (parsedBaseAddress.Scheme != Uri.UriSchemeHttp && parsedBaseAddress.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException("تنظیم WEB_API_CREDENTIALS:AIRIC_API:BaseUrl باید یک آدرس معتبر HTTP یا HTTPS باشد.");
}

// اطمینان از وجود / در انتهای آدرس پایه
var apiBaseAddress = new Uri(
    parsedBaseAddress.AbsoluteUri.TrimEnd('/') + "/");

// --------------------------------------------------
// ثبت سرویس‌های برنامه
// --------------------------------------------------

builder.Services.AddScoped<IApiClient, ApiClient>();


// --------------------------------------------------
// ثبت AuthorizedHandler
// --------------------------------------------------

builder.Services.AddTransient<AuthorizedHandler>();

// --------------------------------------------------
// تلاش مجدد برای لاگین 
// --------------------------------------------------

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, delay, retryAttempt, context) =>
        {
            var statusCode = outcome.Result?.StatusCode.ToString()
                             ?? outcome.Exception?.GetType().Name
                             ?? "Unknown";
        });

builder.Services
    .AddHttpClient("AUTH_API", client => client.BaseAddress = apiBaseAddress)
    .AddPolicyHandler(retryPolicy);
//AUTH_API
//دارای BaseAddress
//دارای Retry
//فاقد AuthorizedHandler
//برای گرفتن توکن به توکن قبلی نیاز ندارد

builder.Services
    .AddHttpClient("AIRIC_API", client => client.BaseAddress = apiBaseAddress)
    .AddHttpMessageHandler<AuthorizedHandler>()
    .AddPolicyHandler(retryPolicy);
//AIRIC_API
//دارای AuthorizedHandler
//توکن را به هدر Authorization اضافه می‌کند
//برای APIهای محافظت‌شده استفاده می‌شود
//در ApiClient استفاده می‌شود

// --------------------------------------------------
// ثبت TokenProvider
// به صورت Singleton برای حفظ کش توکن
// --------------------------------------------------

builder.Services.AddSingleton<ITokenProvider>(serviceProvider =>
{
    var httpClientFactory =
        serviceProvider.GetRequiredService<IHttpClientFactory>();

    var configuration =
        serviceProvider.GetRequiredService<IConfiguration>();

    var logger =
        serviceProvider.GetRequiredService<ILogger<TokenProvider>>();

    var authHttpClient =
        httpClientFactory.CreateClient("AUTH_API");

    return new TokenProvider(
        authHttpClient,
        configuration,
        logger);
});

// --------------------------------------------------
// ساخت برنامه
// --------------------------------------------------

var app = builder.Build();

// --------------------------------------------------
// Middleware
// --------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


// --------------------------------------------------
// Route پیش‌فرض
// --------------------------------------------------

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Index}/{action=Index}/{id?}");

app.Run();

