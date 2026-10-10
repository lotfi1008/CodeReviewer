namespace CodeReviewer.Web.Services;

public static class ServiceHttp
{
    public const string ClientName = "ConnectedServices";

    public static HttpClientHandler CreateHandler(bool allowUntrustedCertificates) => new()
    {
        // Limited to the source-control and model clients, never process-wide TLS settings.
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = allowUntrustedCertificates
            ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            : null
    };
}
