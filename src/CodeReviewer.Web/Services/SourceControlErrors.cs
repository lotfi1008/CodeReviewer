using System.Net.Sockets;

namespace CodeReviewer.Web.Services;

public static class SourceControlErrors
{
    public static string Describe(Exception error) => error switch
    {
        HttpRequestException http => http.HttpRequestError switch
        {
            HttpRequestError.SecureConnectionError => "اتصال TLS به سورس کنترل برقرار نشد؛ سرویس یا پراکسی ممکن است ارتباط را قطع کرده باشد. تنظیمات پراکسی و گواهی را بررسی کنید. (SecureConnectionError)",
            HttpRequestError.NameResolutionError => "نام میزبان سورس کنترل پیدا نشد؛ نشانی و DNS را بررسی کنید. (NameResolutionError)",
            HttpRequestError.ProxyTunnelError => "پراکسی نتوانست تونل اتصال به سورس کنترل را برقرار کند. تنظیم SourceControlConnections:UseProxy را بررسی کنید. (ProxyTunnelError)",
            _ => $"ارتباط شبکه با سورس کنترل برقرار نشد یا قطع شد. نشانی، پورت، شبکه و پراکسی را بررسی کنید. ({http.HttpRequestError}{SocketCode(http)})"
        },
        OperationCanceledException => "مهلت اتصال به سورس کنترل تمام شد یا عملیات لغو شد.",
        InvalidOperationException invalid => invalid.Message,
        _ => $"عملیات سورس کنترل ناموفق بود. ({error.GetType().Name})"
    };

    private static string SocketCode(Exception error)
    {
        for (Exception? current=error; current is not null; current=current.InnerException)
            if (current is SocketException socket) return $" / {socket.SocketErrorCode}";
        return "";
    }
}
