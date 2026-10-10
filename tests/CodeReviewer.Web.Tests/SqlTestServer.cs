using Microsoft.Data.SqlClient;

namespace CodeReviewer.Web.Tests;

// زیرساخت مشترک آزمون‌های وابسته به SQL Server.
// سرور آزمون از متغیر محیطی CODEREVIEWER_TEST_SQL خوانده می‌شود و در نبود آن به localhost برمی‌گردد.
// اگر سرور در دسترس نباشد یا لاگین مجوز CREATE DATABASE نداشته باشد، آزمون‌ها به‌جای شکست به‌صورت پویا Skip می‌شوند.
internal static class SqlTestServer
{
    public const string SkipReason = "SQL Server آزمون در دسترس نیست یا مجوز CREATE DATABASE ندارد؛ برای اجرای این آزمون متغیر محیطی CODEREVIEWER_TEST_SQL را تنظیم کنید.";

    public static string BaseConnection => Environment.GetEnvironmentVariable("CODEREVIEWER_TEST_SQL") is { Length: > 0 } value
        ? value
        : "Server=localhost;Integrated Security=True;Encrypt=True;TrustServerCertificate=True";

    private static readonly Lazy<bool> available = new(Probe);

    public static bool Available => available.Value;

    public static string WithDatabase(string database)
        => new SqlConnectionStringBuilder(BaseConnection) { InitialCatalog = database }.ConnectionString;

    private static bool Probe()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(BaseConnection) { InitialCatalog = "master", ConnectTimeout = 5 };
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            // بررسی اینکه لاگین می‌تواند پایگاه‌داده آزمون یک‌بارمصرف بسازد (بدون ساختن واقعی آن).
            command.CommandText = "SELECT CASE WHEN IS_SRVROLEMEMBER('sysadmin')=1 OR IS_SRVROLEMEMBER('dbcreator')=1 OR HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE')=1 THEN 1 ELSE 0 END";
            return Convert.ToInt32(command.ExecuteScalar()) == 1;
        }
        catch
        {
            return false;
        }
    }
}

// مانند [Fact] عمل می‌کند اما در نبود SQL Server قابل‌استفاده، آزمون را Skip می‌کند.
internal sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlTestServer.Available) Skip = SqlTestServer.SkipReason;
    }
}
