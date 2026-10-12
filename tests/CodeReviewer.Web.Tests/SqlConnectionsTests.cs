using CodeReviewer.Web.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CodeReviewer.Web.Tests;

public class SqlConnectionsTests
{
    [Theory]
    [InlineData("Encrypt=True;TrustServerCertificate=False", "Mandatory")]
    [InlineData("Encrypt=Strict;TrustServerCertificate=False", "Mandatory")]
    [InlineData("Encrypt=False", "Optional")]
    [InlineData("", "Mandatory")]
    public void DatabaseAndQueueAcceptInternalCertificatesWithoutRemovingEncryption(string options, string expectedEncrypt)
    {
        var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["ConnectionStrings:CodeReviewer"]=$"Server=internal;Database=reviews;Integrated Security=True;{options}"
        }).Build();
        var parsed=new SqlConnectionStringBuilder(SqlConnections.ConnectionString(config));
        Assert.True(parsed.TrustServerCertificate);
        Assert.Equal(expectedEncrypt == "Mandatory" ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional,parsed.Encrypt);
        Assert.Equal("internal",parsed.DataSource);
        Assert.Equal("reviews",parsed.InitialCatalog);
        Assert.True(parsed.IntegratedSecurity);
    }
}
