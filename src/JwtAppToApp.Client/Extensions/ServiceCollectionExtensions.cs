using System;
using System.Linq;

using JwtAppToApp.Client.Abstractions;
using JwtAppToApp.Client.Authentication;
using JwtAppToApp.Client.Options;
using JwtAppToApp.Client.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Extensions.Http;

namespace JwtAppToApp.Client.Extensions;

public static class ServiceCollectionExtensions
{
    private const string CredentialsRoot = "WEB_API_CREDENTIALS";

    public static IServiceCollection AddJwtAppToAppClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string? applicationName = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var rootSection = configuration.GetSection(CredentialsRoot);

        var sections = rootSection
            .GetChildren()
            .ToArray();

        IConfigurationSection selectedSection;

        /*
         * اگر نام Application ارسال شده باشد،
         * همان بخش از WEB_API_CREDENTIALS انتخاب می‌شود.
         */
        if (!string.IsNullOrWhiteSpace(applicationName))
        {
            selectedSection =
                sections.FirstOrDefault(section =>
                    string.Equals(
                        section.Key,
                        applicationName,
                        StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Credential section '{CredentialsRoot}:{applicationName}' was not found.");
        }
        /*
         * اگر فقط یک بخش وجود داشته باشد،
         * همان بخش به‌صورت خودکار انتخاب می‌شود.
         */
        else if (sections.Length == 1)
        {
            selectedSection = sections[0];
        }
        /*
         * اگر هیچ Credentialای وجود نداشته باشد،
         * برنامه با خطای واضح متوقف می‌شود.
         */
        else if (sections.Length == 0)
        {
            throw new InvalidOperationException(
                $"No credential section was found under '{CredentialsRoot}'.");
        }
        /*
         * اگر چند Credential وجود داشته باشد ولی applicationName
         * ارسال نشده باشد، انتخاب مبهم است.
         */
        else
        {
            var names = string.Join(
                ", ",
                sections.Select(section => section.Key));

            throw new InvalidOperationException(
                $"Multiple credential sections were found under " +
                $"'{CredentialsRoot}': {names}. " +
                "Specify the application name.");
        }

        /*
         * ثبت و اعتبارسنجی تنظیمات JWT
         */
        services
            .AddOptions<JwtAppToAppOptions>()
            .Bind(selectedSection)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.UserName),
                "UserName is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                "Password is required.")
            .Validate(
                options => IsValidHttpUrl(options.BaseUrl),
                "BaseUrl must be a valid HTTP or HTTPS URL.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.LoginUrl),
                "LoginUrl is required.")
            .ValidateOnStart();

        /*
         * ذخیره نام Credential انتخاب‌شده
         */
        services.PostConfigure<JwtAppToAppOptions>(options =>
        {
            options.CredentialSectionName = selectedSection.Key;
        });

        /*
         * HttpClient مربوط به TokenProvider
         *
         * این HttpClient برای Login و Refresh Token استفاده می‌شود.
         * Retry Policy روی درخواست‌های آن اعمال می‌شود.
         */
        services
            .AddHttpClient<ITokenProvider, TokenProvider>(
                (serviceProvider, client) =>
                {
                    var options =
                        serviceProvider
                            .GetRequiredService<
                                IOptions<JwtAppToAppOptions>>()
                            .Value;

                    client.BaseAddress =
                        CreateBaseAddress(options.BaseUrl);
                })
            .AddPolicyHandler(GetRetryPolicy());

        /*
         * Handler مسئول افزودن Bearer Token
         * به درخواست‌های API است.
         */
        services.AddTransient<AuthorizedHandler>();

        /*
         * HttpClient مربوط به ApiClient
         */
        services
            .AddHttpClient<IApiClient, ApiClient>(
                (serviceProvider, client) =>
                {
                    var options =
                        serviceProvider
                            .GetRequiredService<
                                IOptions<JwtAppToAppOptions>>()
                            .Value;

                    client.BaseAddress =
                        CreateBaseAddress(options.BaseUrl);
                })
            .AddHttpMessageHandler<AuthorizedHandler>();

        return services;
    }

    /*
     * Policy مربوط به Retry
     *
     * شرایط Retry:
     * - خطاهای شبکه‌ای HttpRequestException
     * - پاسخ‌های 5xx
     * - پاسخ 408 Request Timeout
     *
     * تعداد تلاش مجدد: 3 بار
     *
     * زمان انتظار:
     * - تلاش اول: 2 ثانیه
     * - تلاش دوم: 4 ثانیه
     * - تلاش سوم: 8 ثانیه
     */
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(
                        Math.Pow(2, retryAttempt)));
    }

    /*
     * اعتبارسنجی BaseUrl
     */
    private static bool IsValidHttpUrl(string? value)
    {
        return Uri.TryCreate(
                   value,
                   UriKind.Absolute,
                   out var uri)
               &&
               (
                   uri.Scheme == Uri.UriSchemeHttp
                   ||
                   uri.Scheme == Uri.UriSchemeHttps
               );
    }

    /*
     * ساخت BaseAddress استاندارد
     *
     * برای جلوگیری از خطا در ترکیب آدرس‌ها،
     * انتهای URL حتماً با / تمام می‌شود.
     */
    private static Uri CreateBaseAddress(string baseUrl)
    {
        if (!Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var uri)
            ||
            (
                uri.Scheme != Uri.UriSchemeHttp
                &&
                uri.Scheme != Uri.UriSchemeHttps
            ))
        {
            throw new InvalidOperationException(
                $"Invalid BaseUrl: '{baseUrl}'.");
        }

        var normalizedUrl =
            uri.AbsoluteUri.EndsWith(
                "/",
                StringComparison.Ordinal)
                ? uri.AbsoluteUri
                : uri.AbsoluteUri + "/";

        return new Uri(normalizedUrl);
    }
}
