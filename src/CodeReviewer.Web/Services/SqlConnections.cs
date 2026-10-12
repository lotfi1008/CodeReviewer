using Microsoft.Data.SqlClient;

namespace CodeReviewer.Web.Services;

public static class SqlConnections
{
    public static string ConnectionString(IConfiguration config)
    {
        var configured = config.GetConnectionString("CodeReviewer")
            ?? throw new InvalidOperationException("ConnectionStrings:CodeReviewer تنظیم نشده است.");
        var builder = new SqlConnectionStringBuilder(configured) { TrustServerCertificate = true };
        // Strict ignores TrustServerCertificate; Mandatory keeps encryption and accepts internal certificates.
        if (builder.Encrypt == SqlConnectionEncryptOption.Strict) builder.Encrypt = SqlConnectionEncryptOption.Mandatory;
        return builder.ConnectionString;
    }
}
