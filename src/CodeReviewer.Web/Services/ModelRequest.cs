using System.ClientModel;

namespace CodeReviewer.Web.Services;

public static class ModelRequest
{
    public static string Describe(Exception error)
    {
        var status = error is ClientResultException client ? client.Status
            : error is HttpRequestException http && http.StatusCode.HasValue ? (int)http.StatusCode : 0;
        var explanation = status switch
        {
            401 => "کلید اتصال پذیرفته نشد.",
            403 => "دسترسی به این مدل مجاز نیست.",
            404 => "نشانی سرویس، مدل یا Deployment پیدا نشد.",
            408 or 504 => "مهلت پاسخ در سرویس یا درگاه آن تمام شد.",
            429 => "سرویس محدودیت نرخ درخواست یا سهمیه اعمال کرده است.",
            >= 500 => "سرویس مدل یا درگاه آن خطای داخلی داده است.",
            >= 400 => "سرویس درخواست را نپذیرفت؛ سازگاری مدل با پاسخ ساختاریافته و تنظیمات درخواست بررسی شود.",
            _ => error switch
            {
                TimeoutException => "مدل در مهلت تعیین‌شده پاسخ کامل نداد.",
                HttpRequestException => "ارتباط شبکه با سرویس مدل برقرار نشد یا قطع شد.",
                System.Text.Json.JsonException => "پاسخ مدل JSON معتبر و سازگار با ساختار مورد انتظار نیست.",
                _ => "پاسخ قابل استفاده دریافت نشد؛ اتصال و ساختار پاسخ بررسی شود."
            }
        };
        return $"{(status > 0 ? $"HTTP {status}" : error.GetType().Name)} — {explanation}";
    }

    private static bool Transient(Exception error) => error switch
    {
        ClientResultException c => c.Status is 408 or 429 or 500 or 502 or 503 or 504,
        HttpRequestException h => h.StatusCode is null || (int)h.StatusCode is 408 or 429 or 500 or 502 or 503 or 504,
        _ => false
    };

    // The caller's deadline covers all attempts and backoff, so retries cannot extend it.
    public static async Task<T> RunAsync<T>(Func<CancellationToken,Task<T>> operation, Action<string> log,
        CancellationToken ct, Func<TimeSpan,CancellationToken,Task>? delay = null)
    {
        delay ??= Task.Delay;
        for (var attempt = 1; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try { return await operation(ct); }
            catch (Exception ex) when (Transient(ex) && attempt < 3 && !ct.IsCancellationRequested)
            {
                var seconds = attempt * 5;
                log($"{Describe(ex)} تلاش مجدد {attempt + 1}/3 پس از {seconds} ثانیه، در مهلت همین درخواست.");
                await delay(TimeSpan.FromSeconds(seconds), ct);
            }
        }
    }
}
