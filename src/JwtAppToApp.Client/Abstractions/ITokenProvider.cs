namespace JwtAppToApp.Client.Abstractions;

public interface ITokenProvider
{
    // دریافت Token معتبر
    //در حالت عادی، GetTokenAsync باید ابتدا Refresh Token را امتحان کند و فقط در صورت شکست، Login مجدد انجام دهد.
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
    //حذف Token و Login مجدد
    Task<string> ForceRefreshTokenAsync(CancellationToken cancellationToken = default);
    //پاک‌کردن Token از Cache
    void Invalidate();
}