using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CodeReviewer.Web.Services;

namespace CodeReviewer.Web.Tests;

public sealed class ServiceHttpTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelfSignedCertificateIsAcceptedOnlyWhenConfigured(bool allow)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=internal-test",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1);
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),DateTimeOffset.UtcNow.AddDays(1));
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
            catch(Exception ex) when(!allow && ex is AuthenticationException or IOException) { }
        });
        using var client = new HttpClient(ServiceHttp.CreateHandler(allow));
        if(allow) Assert.Equal("OK",await client.GetStringAsync($"https://127.0.0.1:{port}",timeout.Token));
        else await Assert.ThrowsAsync<HttpRequestException>(()=>client.GetStringAsync($"https://127.0.0.1:{port}",timeout.Token));
        await server;
    }
}
