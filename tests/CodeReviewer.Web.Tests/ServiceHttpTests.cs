using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CodeReviewer.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeReviewer.Web.Tests;

public sealed class ServiceHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProxyPolicyIsExplicitAndScopedToHandler(bool useProxy)
    {
        using var handler=ServiceHttp.CreateHandler(useProxy);
        Assert.Equal(useProxy,handler.UseProxy);
        using var modelHandler=ServiceHttp.CreateHandler();
        Assert.True(modelHandler.UseProxy);
    }

    [Theory]
    [InlineData(HttpRequestError.SecureConnectionError,"TLS")]
    [InlineData(HttpRequestError.ProxyTunnelError,"پراکسی")]
    [InlineData(HttpRequestError.NameResolutionError,"DNS")]
    public void SourceErrorsExplainCauseWithoutExposingRawMessage(HttpRequestError cause,string expected)
    {
        var message=SourceControlErrors.Describe(new HttpRequestException(cause,"private-token"));
        Assert.Contains(expected,message);
        Assert.DoesNotContain("private-token",message);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("", true)]
    [InlineData(ServiceHttp.ClientName, false)]
    [InlineData(ServiceHttp.ClientName, true)]
    [InlineData(ServiceHttp.SourceControlClientName, false)]
    [InlineData(ServiceHttp.SourceControlClientName, true)]
    public async Task AllRegisteredClientsAcceptUntrustedMismatchedAndExpiredCertificates(string clientName, bool expired)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=internal-test",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2),DateTimeOffset.UtcNow.AddDays(expired ? -1 : 1));
        using var certificate = X509CertificateLoader.LoadPkcs12(generated.Export(X509ContentType.Pfx),null);
        using var listener = new TcpListener(IPAddress.Loopback,0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var server = Task.Run(async () =>
        {
            using var socket = await listener.AcceptTcpClientAsync(timeout.Token);
            using var tls = new SslStream(socket.GetStream(),false);
            try
            {
                await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate=certificate },timeout.Token);
                using var reader = new StreamReader(tls,Encoding.ASCII,leaveOpen:true);
                while(!string.IsNullOrEmpty(await reader.ReadLineAsync(timeout.Token))) { }
                await tls.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 2\r\nConnection: close\r\n\r\nOK"),timeout.Token);
            }
            catch(Exception ex) when(ex is AuthenticationException or IOException) { throw; }
        });
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["SourceControlConnections:UseProxy"]="false",
            ["ServiceConnections:AllowUntrustedCertificates"]="false"
        }).Build();
        var services=new ServiceCollection();
        ServiceHttp.AddClients(services,config);
        using var provider=services.BuildServiceProvider();
        using var client=provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName);
        Assert.Equal("OK",await client.GetStringAsync($"https://127.0.0.1:{port}",timeout.Token));
        await server;
    }
}
