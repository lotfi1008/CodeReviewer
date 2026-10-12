namespace CodeReviewer.Web.Services;

public static class ServiceHttp
{
    public const string ClientName = "ConnectedServices";
    public const string SourceControlClientName = "SourceControl";

    public static void AddClients(IServiceCollection services, IConfiguration config)
    {
        services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => CreateHandler()));
        services.AddHttpClient(ClientName).ConfigureHttpClient(client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddHttpClient(SourceControlClientName).ConfigurePrimaryHttpMessageHandler(() =>
            CreateHandler(config.GetValue("SourceControlConnections:UseProxy", true)));
    }

    public static HttpClientHandler CreateHandler(bool useProxy = true) => new()
    {
        UseProxy = useProxy,
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
}
