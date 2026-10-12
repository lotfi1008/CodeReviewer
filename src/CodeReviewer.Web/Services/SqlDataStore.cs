using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;

namespace CodeReviewer.Web.Services;

public sealed record ReviewRun(Guid Id, MergeRequestItem MergeRequest, string Model, string Origin,
    string Status, DateTime StartedUtc, DateTime? FinishedUtc, int FilesReviewed, string? Note,
    IReadOnlyList<ReviewFinding> Findings, ReviewProfile? Profile = null, IReadOnlyList<FileAssessment>? Coverage = null, ChangeImpactResult? Impact = null);

public enum ReviewDeleteResult { Deleted, NotFound, Running }

public sealed class SqlDataStore
{
    private readonly string connectionString;
    private readonly IDataProtector secrets;
    public SqlDataStore(IConfiguration config, IDataProtectionProvider protection)
    {
        connectionString = SqlConnections.ConnectionString(config);
        secrets = protection.CreateProtector("CodeReviewer.SqlSecrets.v1");
    }

    private SqlConnection Open()
    {
        var connection = new SqlConnection(connectionString);
        connection.Open();
        return connection;
    }

    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID('dbo.Connections') IS NULL
            CREATE TABLE dbo.Connections (
                Id uniqueidentifier NOT NULL PRIMARY KEY, Kind varchar(10) NOT NULL,
                Name nvarchar(200) NOT NULL, Provider varchar(40) NOT NULL,
                Endpoint nvarchar(2000) NOT NULL, Model nvarchar(200) NOT NULL,
                Enabled bit NOT NULL, SupportsJsonSchema bit NOT NULL,
                ProtectedSecret nvarchar(max) NOT NULL);
            IF OBJECT_ID('dbo.CatalogDefaults') IS NULL
            BEGIN
                CREATE TABLE dbo.CatalogDefaults (Id int PRIMARY KEY CHECK (Id=1), SourceId uniqueidentifier NULL, ModelId uniqueidentifier NULL);
                INSERT dbo.CatalogDefaults (Id,SourceId,ModelId) VALUES (1,NULL,NULL);
            END;
            IF OBJECT_ID('dbo.ReviewRuns') IS NULL
            CREATE TABLE dbo.ReviewRuns (
                Id uniqueidentifier PRIMARY KEY, MergeRequest nvarchar(max) NOT NULL CHECK (ISJSON(MergeRequest)=1),
                Model nvarchar(400) NOT NULL, Origin varchar(40) NOT NULL, Status varchar(30) NOT NULL,
                StartedUtc datetime2 NOT NULL, FinishedUtc datetime2 NULL, FilesReviewed int NOT NULL,
                Note nvarchar(max) NULL);
            IF OBJECT_ID('dbo.Findings') IS NULL
            CREATE TABLE dbo.Findings (
                Id bigint IDENTITY PRIMARY KEY, ReviewId uniqueidentifier NOT NULL REFERENCES dbo.ReviewRuns(Id),
                Category nvarchar(80) NOT NULL, Severity varchar(20) NOT NULL, Confidence float NOT NULL,
                FilePath nvarchar(2000) NOT NULL, Line int NOT NULL, Title nvarchar(1000) NOT NULL,
                Description nvarchar(max) NOT NULL, Suggestion nvarchar(max) NOT NULL);
            IF COL_LENGTH('dbo.Findings','Details') IS NULL ALTER TABLE dbo.Findings ADD Details nvarchar(max) NULL;
            IF OBJECT_ID('dbo.SourceSnapshots') IS NULL
            CREATE TABLE dbo.SourceSnapshots (
                ConnectionId uniqueidentifier NOT NULL, Scope nvarchar(200) NOT NULL,
                Payload nvarchar(max) NOT NULL CHECK (ISJSON(Payload)=1), UpdatedUtc datetime2 NOT NULL,
                PRIMARY KEY (ConnectionId, Scope));
            IF OBJECT_ID('dbo.ConnectionChecks') IS NULL
            CREATE TABLE dbo.ConnectionChecks (
                Id bigint IDENTITY PRIMARY KEY, ConnectionId uniqueidentifier NOT NULL,
                CheckedUtc datetime2 NOT NULL, Success bit NOT NULL, Message nvarchar(1000) NOT NULL);
            IF COL_LENGTH('dbo.CatalogDefaults','Initialized') IS NULL
                ALTER TABLE dbo.CatalogDefaults ADD Initialized bit NOT NULL DEFAULT 0;
            IF OBJECT_ID('dbo.ReviewEntries') IS NULL
            BEGIN
                CREATE TABLE dbo.ReviewEntries (
                    Id uniqueidentifier NOT NULL PRIMARY KEY,
                    ConnectionId uniqueidentifier NOT NULL, RepositoryId nvarchar(200) NOT NULL,
                    RequestNumber int NOT NULL, HeadSha varchar(64) NOT NULL, Kind varchar(30) NOT NULL,
                    FilePath nvarchar(2000) NOT NULL, Line int NULL, Decision varchar(30) NOT NULL,
                    Body nvarchar(max) NOT NULL, Author nvarchar(300) NOT NULL, CreatedUtc datetime2 NOT NULL);
                CREATE INDEX IX_ReviewEntries_Request ON dbo.ReviewEntries (ConnectionId, RepositoryId, RequestNumber, CreatedUtc);
            END;
            IF OBJECT_ID('dbo.ReviewProfiles') IS NULL
                CREATE TABLE dbo.ReviewProfiles (Id uniqueidentifier PRIMARY KEY, Settings nvarchar(max) NOT NULL CHECK(ISJSON(Settings)=1));
            IF COL_LENGTH('dbo.ReviewRuns','Profile') IS NULL
                ALTER TABLE dbo.ReviewRuns ADD Profile nvarchar(max) NULL;
            IF OBJECT_ID('dbo.ReviewEvents') IS NULL
            BEGIN
                CREATE TABLE dbo.ReviewEvents (Id bigint IDENTITY PRIMARY KEY, ReviewId uniqueidentifier NOT NULL REFERENCES dbo.ReviewRuns(Id),
                    TimeUtc datetime2 NOT NULL, Level varchar(20) NOT NULL, Message nvarchar(2000) NOT NULL,
                    FilePath nvarchar(2000) NOT NULL, CompletedFiles int NOT NULL, TotalFiles int NOT NULL);
                CREATE INDEX IX_ReviewEvents_Run ON dbo.ReviewEvents(ReviewId, Id);
            END;
            """;
        command.ExecuteNonQuery();
        foreach(var profile in ReviewProfile.Presets())
        {
            command.Parameters.Clear();
            command.CommandText="INSERT dbo.ReviewProfiles (Id,Settings) SELECT @id,@settings WHERE NOT EXISTS(SELECT 1 FROM dbo.ReviewProfiles WHERE Id=@id)";
            Add(command,"@id",profile.Id); Add(command,"@settings",JsonSerializer.Serialize(profile)); command.ExecuteNonQuery();
        }
    }

    public ConnectionCatalog ReadCatalog()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM dbo.Connections ORDER BY Name; SELECT SourceId,ModelId FROM dbo.CatalogDefaults WHERE Id=1";
        using var reader = command.ExecuteReader();
        var result = new ConnectionCatalog();
        while (reader.Read())
        {
            var secret = reader.GetString(8);
            if (secret.Length != 0)
            {
                try { secret = secrets.Unprotect(secret); }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    // رمزگشایی با کلید Data Protection فعلی ممکن نیست (کلید اصلی از دست رفته).
                    // راز خالی می‌شود تا برنامه بالا بیاید و کاربر بتواند توکن/کلید را دوباره وارد کند.
                    Console.Error.WriteLine($"هشدار: رمزگشایی راز اتصال {reader.GetGuid(0)} ممکن نبود؛ مقدار خالی در نظر گرفته شد. توکن یا کلید API را از UI دوباره وارد کنید.");
                    secret = "";
                }
            }
            if (reader.GetString(1) == "Source")
                result.Sources.Add(new SourceConnection { Id=reader.GetGuid(0), Name=reader.GetString(2), Provider=reader.GetString(3), BaseUrl=reader.GetString(4), Enabled=reader.GetBoolean(6), Token=secret });
            else
                result.Models.Add(new AiConnection { Id=reader.GetGuid(0), Name=reader.GetString(2), Provider=reader.GetString(3), Endpoint=reader.GetString(4), Model=reader.GetString(5), Enabled=reader.GetBoolean(6), SupportsJsonSchema=reader.GetBoolean(7), ApiKey=secret });
        }
        reader.NextResult();
        if (reader.Read()) { result.DefaultSourceId = reader.IsDBNull(0) ? null : reader.GetGuid(0); result.DefaultModelId = reader.IsDBNull(1) ? null : reader.GetGuid(1); }
        return result;
    }

    public bool CatalogInitialized()
    {
        using var connection = Open(); using var command = connection.CreateCommand();
        command.CommandText = "SELECT Initialized FROM dbo.CatalogDefaults WHERE Id=1";
        return (bool)command.ExecuteScalar()!;
    }

    // All catalog mutations in this process are serialized by ConnectionCatalogStore.
    public void SaveCatalog(ConnectionCatalog catalog)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "DELETE dbo.Connections"; command.ExecuteNonQuery();
        void Insert(Guid id, string kind, string name, string provider, string endpoint, string model, bool enabled, bool schema, string secret)
        {
            command.Parameters.Clear();
            command.CommandText = "INSERT dbo.Connections VALUES (@id,@kind,@name,@provider,@endpoint,@model,@enabled,@schema,@secret)";
            Add(command,"@id",id); Add(command,"@kind",kind); Add(command,"@name",name); Add(command,"@provider",provider); Add(command,"@endpoint",endpoint); Add(command,"@model",model); Add(command,"@enabled",enabled); Add(command,"@schema",schema); Add(command,"@secret",secret.Length == 0 ? "" : secrets.Protect(secret));
            command.ExecuteNonQuery();
        }
        foreach (var s in catalog.Sources) Insert(s.Id,"Source",s.Name,s.Provider,s.BaseUrl,"",s.Enabled,false,s.Token);
        foreach (var m in catalog.Models) Insert(m.Id,"Model",m.Name,m.Provider,m.Endpoint,m.Model,m.Enabled,m.SupportsJsonSchema,m.ApiKey);
        command.Parameters.Clear();
        command.CommandText = "UPDATE dbo.CatalogDefaults SET SourceId=@source, ModelId=@model,Initialized=1 WHERE Id=1";
        Add(command,"@source",catalog.DefaultSourceId); Add(command,"@model",catalog.DefaultModelId);
        command.ExecuteNonQuery(); transaction.Commit();
    }

    public Guid StartReview(MergeRequestItem mr, string model, string origin = "ConnectedModel", ReviewProfile? profile = null)
    {
        var id = Guid.NewGuid();
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="INSERT dbo.ReviewRuns (Id,MergeRequest,Model,Origin,Status,StartedUtc,FinishedUtc,FilesReviewed,Note,Profile) VALUES (@id,@mr,@model,@origin,'Running',SYSUTCDATETIME(),NULL,0,NULL,@profile)";
        Add(command,"@profile",profile is null ? null : JsonSerializer.Serialize(profile));
        Add(command,"@id",id); Add(command,"@mr",JsonSerializer.Serialize(mr)); Add(command,"@model",model); Add(command,"@origin",origin);
        command.ExecuteNonQuery(); return id;
    }

    public void FinishReview(Guid id, string status, int files, IReadOnlyList<ReviewFinding> findings, string? note = null)
    {
        using var connection=Open(); using var transaction=connection.BeginTransaction();
        using var command=connection.CreateCommand(); command.Transaction=transaction;
        command.CommandText="UPDATE dbo.ReviewRuns SET Status=@status, FinishedUtc=CASE WHEN @status='Running' THEN NULL ELSE SYSUTCDATETIME() END, FilesReviewed=@files, Note=@note WHERE Id=@id AND Status='Running'";
        Add(command,"@id",id); Add(command,"@status",status); Add(command,"@files",files); Add(command,"@note",note);
        if (command.ExecuteNonQuery()!=1) throw new InvalidOperationException("بازبینی در حال اجرا یافت نشد.");
        command.Parameters.Clear(); command.CommandText="DELETE dbo.Findings WHERE ReviewId=@id"; Add(command,"@id",id); command.ExecuteNonQuery();
        foreach (var f in findings)
        {
            command.Parameters.Clear();
            command.CommandText="INSERT dbo.Findings (ReviewId,Category,Severity,Confidence,FilePath,Line,Title,Description,Suggestion,Details) VALUES (@id,@category,@severity,@confidence,@file,@line,@title,@description,@suggestion,@details)";
            Add(command,"@id",id); Add(command,"@category",f.Category); Add(command,"@severity",f.Severity); Add(command,"@confidence",f.Confidence); Add(command,"@file",f.File); Add(command,"@line",f.Line); Add(command,"@title",f.Title); Add(command,"@description",f.Description); Add(command,"@suggestion",f.Suggestion); Add(command,"@details",JsonSerializer.Serialize(f));
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public int ReviewCount(string status = "")
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT COUNT(*) FROM dbo.ReviewRuns WHERE (@status='' OR Status=@status)"; Add(command,"@status",status);
        return (int)command.ExecuteScalar()!;
    }

    public IReadOnlyList<ReviewRun> History(int skip = 0, int take = 100, string status = "", Guid? id = null, bool includeDetails = true)
    {
        var results=new List<ReviewRun>();
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT * FROM dbo.ReviewRuns WHERE (@status='' OR Status=@status) AND (@id IS NULL OR Id=@id) ORDER BY StartedUtc DESC,Id OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        Add(command,"@status",status); command.Parameters.Add("@id",SqlDbType.UniqueIdentifier).Value=(object?)id ?? DBNull.Value;
        Add(command,"@skip",Math.Max(0,skip)); Add(command,"@take",Math.Clamp(take,1,100));
        using (var reader=command.ExecuteReader())
            while(reader.Read()) results.Add(new(reader.GetGuid(0),JsonSerializer.Deserialize<MergeRequestItem>(reader.GetString(1))!,reader.GetString(2),reader.GetString(3),reader.GetString(4),DateTime.SpecifyKind(reader.GetDateTime(5),DateTimeKind.Utc),reader.IsDBNull(6)?null:DateTime.SpecifyKind(reader.GetDateTime(6),DateTimeKind.Utc),reader.GetInt32(7),reader.IsDBNull(8)?null:reader.GetString(8),[],reader.IsDBNull(9)?null:JsonSerializer.Deserialize<ReviewProfile>(reader.GetString(9))));
        for(var i=0;includeDetails && i<results.Count;i++)
        {
            command.Parameters.Clear(); command.CommandText="SELECT Category,Severity,Confidence,FilePath,Line,Title,Description,Suggestion,Details FROM dbo.Findings WHERE ReviewId=@id ORDER BY Id";
            Add(command,"@id",results[i].Id); using var reader=command.ExecuteReader(); var findings=new List<ReviewFinding>();
            while(reader.Read()) findings.Add(!reader.IsDBNull(8) ? JsonSerializer.Deserialize<ReviewFinding>(reader.GetString(8))! : new(reader.GetString(0),reader.GetString(1),reader.GetDouble(2),reader.GetString(3),reader.GetInt32(4),reader.GetString(5),reader.GetString(6),reader.GetString(7)));
            results[i]=results[i] with { Findings=findings, Coverage=ReadSnapshot<List<FileAssessment>>(results[i].MergeRequest.ConnectionId,$"coverage:{results[i].Id}"), Impact=ReadSnapshot<ChangeImpactResult>(results[i].MergeRequest.ConnectionId,$"impact:{results[i].Id}") };
        }
        return results;
    }

    // حذف واقعی و اتمی یک گزارش بازبینی به‌همراه همهٔ دادهٔ وابستهٔ آن.
    // خروجی Idempotent است: پیام صریح (NotFound/Running) به‌جای استثنا برگردانده می‌شود.
    public ReviewDeleteResult DeleteReview(Guid id)
    {
        using var connection=Open();
        using var transaction=connection.BeginTransaction();
        using var command=connection.CreateCommand(); command.Transaction=transaction;

        // قفل به‌روزرسانی تا بین بررسی وضعیت و حذف، Worker نتواند وضعیت را تغییر دهد.
        command.CommandText="SELECT Status,MergeRequest FROM dbo.ReviewRuns WITH (UPDLOCK) WHERE Id=@id";
        Add(command,"@id",id);
        string status; Guid connectionId;
        using (var reader=command.ExecuteReader())
        {
            if(!reader.Read()) { reader.Close(); transaction.Rollback(); return ReviewDeleteResult.NotFound; }
            status=reader.GetString(0);
            connectionId=JsonSerializer.Deserialize<MergeRequestItem>(reader.GetString(1))!.ConnectionId;
        }
        if(status=="Running") { transaction.Rollback(); return ReviewDeleteResult.Running; }

        // ترتیب حذف: وابستگی‌ها پیش از والد تا کلیدهای خارجی نقض نشوند.
        command.CommandText="DELETE dbo.Findings WHERE ReviewId=@id"; command.ExecuteNonQuery();
        command.CommandText="DELETE dbo.ReviewEvents WHERE ReviewId=@id"; command.ExecuteNonQuery();
        command.CommandText="DELETE dbo.SourceSnapshots WHERE ConnectionId=@connection AND Scope LIKE '%:' + CONVERT(nvarchar(36),@id)";
        command.Parameters.Clear(); Add(command,"@connection",connectionId); Add(command,"@id",id); command.ExecuteNonQuery();
        command.Parameters.Clear();
        command.CommandText="DELETE dbo.ReviewRuns WHERE Id=@id"; Add(command,"@id",id);
        if(command.ExecuteNonQuery()!=1) { transaction.Rollback(); return ReviewDeleteResult.NotFound; }

        transaction.Commit();
        return ReviewDeleteResult.Deleted;
    }

    public void SaveSnapshot<T>(Guid connectionId, string scope, T value)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="MERGE dbo.SourceSnapshots WITH (HOLDLOCK) AS t USING (SELECT @id AS ConnectionId,@scope AS Scope) AS s ON t.ConnectionId=s.ConnectionId AND t.Scope=s.Scope WHEN MATCHED THEN UPDATE SET Payload=@payload,UpdatedUtc=SYSUTCDATETIME() WHEN NOT MATCHED THEN INSERT VALUES (@id,@scope,@payload,SYSUTCDATETIME());";
        Add(command,"@id",connectionId); Add(command,"@scope",scope); Add(command,"@payload",JsonSerializer.Serialize(value)); command.ExecuteNonQuery();
    }

    public T? ReadSnapshot<T>(Guid connectionId, string scope)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT Payload FROM dbo.SourceSnapshots WHERE ConnectionId=@id AND Scope=@scope";
        Add(command,"@id",connectionId); Add(command,"@scope",scope);
        return command.ExecuteScalar() is string json ? JsonSerializer.Deserialize<T>(json) : default;
    }

    public void RecordCheck(Guid id, bool success, string message)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="INSERT dbo.ConnectionChecks VALUES (@id,SYSUTCDATETIME(),@success,@message)";
        Add(command,"@id",id); Add(command,"@success",success); Add(command,"@message",message[..Math.Min(message.Length,1000)]); command.ExecuteNonQuery();
    }

    public IReadOnlyList<MergeRequestItem> KnownRequests()
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT Payload FROM dbo.SourceSnapshots WHERE Scope LIKE 'requests:%' ORDER BY UpdatedUtc DESC";
        using var reader=command.ExecuteReader(); var results=new List<MergeRequestItem>();
        while(reader.Read()) results.AddRange(JsonSerializer.Deserialize<List<MergeRequestItem>>(reader.GetString(0)) ?? []);
        return results;
    }

    public void RecoverInterruptedReviews()
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="UPDATE dbo.ReviewRuns SET Status='Interrupted',FinishedUtc=SYSUTCDATETIME(),Note=N'اجرای قبلی با توقف برنامه قطع شد؛ برای بررسی دوباره اجرا کنید.' WHERE Status='Running'"; command.ExecuteNonQuery();
    }

    public void SaveReviewEntry(ReviewEntry entry)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="INSERT dbo.ReviewEntries (Id,ConnectionId,RepositoryId,RequestNumber,HeadSha,Kind,FilePath,Line,Decision,Body,Author,CreatedUtc) VALUES (@id,@source,@repo,@number,@sha,@kind,@file,@line,@decision,@body,@author,@time)";
        Add(command,"@id",entry.Id); Add(command,"@source",entry.ConnectionId); Add(command,"@repo",entry.RepositoryId); Add(command,"@number",entry.Number); Add(command,"@sha",entry.Sha); Add(command,"@kind",entry.Kind); Add(command,"@file",entry.File); Add(command,"@line",entry.Line); Add(command,"@decision",entry.Decision); Add(command,"@body",entry.Text); Add(command,"@author",entry.Author); Add(command,"@time",entry.CreatedUtc);
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<ReviewEntry> ReviewEntries(MergeRequestItem request)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT Id,HeadSha,Kind,FilePath,Line,Decision,Body,Author,CreatedUtc FROM dbo.ReviewEntries WHERE ConnectionId=@source AND RepositoryId=@repo AND RequestNumber=@number ORDER BY CreatedUtc DESC";
        Add(command,"@source",request.ConnectionId); Add(command,"@repo",request.RepositoryId); Add(command,"@number",request.Number);
        using var reader=command.ExecuteReader(); var result=new List<ReviewEntry>();
        while(reader.Read()) result.Add(new(reader.GetGuid(0),request.ConnectionId,request.RepositoryId,request.Number,reader.GetString(1),reader.GetString(2),reader.GetString(3),reader.IsDBNull(4)?null:reader.GetInt32(4),reader.GetString(5),reader.GetString(6),reader.GetString(7),DateTime.SpecifyKind(reader.GetDateTime(8),DateTimeKind.Utc)));
        return result;
    }

    private static void Add(SqlCommand command,string name,object? value) => command.Parameters.AddWithValue(name,value ?? DBNull.Value);

    public IReadOnlyList<ReviewProfile> Profiles()
    {
        using var connection=Open(); using var command=connection.CreateCommand(); command.CommandText="SELECT Settings FROM dbo.ReviewProfiles";
        using var reader=command.ExecuteReader(); var result=new List<ReviewProfile>();
        while(reader.Read()) result.Add(JsonSerializer.Deserialize<ReviewProfile>(reader.GetString(0))!);
        return result.OrderBy(x=>x.MaxFiles).ThenBy(x=>x.Name).ToArray();
    }
    public void UpgradeOperationalProfiles()
    {
        using var c=Open(); using var q=c.CreateCommand();
        q.CommandText="""
            IF OBJECT_ID('dbo.AppMigrations') IS NULL CREATE TABLE dbo.AppMigrations(Id nvarchar(100) PRIMARY KEY,AppliedUtc datetime2 NOT NULL);
            IF NOT EXISTS(SELECT 1 FROM dbo.AppMigrations WHERE Id='operational-profiles-v1')
            BEGIN
                BEGIN TRANSACTION;
                DELETE dbo.ReviewProfiles WHERE JSON_VALUE(Settings,'$.Name')=N'عیب‌یابی یک فایل';
                UPDATE dbo.ReviewProfiles SET Settings=JSON_MODIFY(JSON_MODIFY(JSON_MODIFY(Settings,'$.MaxFiles',0),'$.TimeoutSeconds',7200),'$.ModelTimeoutSeconds',120);
                INSERT dbo.AppMigrations VALUES('operational-profiles-v1',SYSUTCDATETIME());
                COMMIT;
            END
            """;
        q.ExecuteNonQuery();
    }

    public void SaveProfile(ReviewProfile profile)
    {
        profile.Validate();
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="MERGE dbo.ReviewProfiles WITH(HOLDLOCK) AS t USING (SELECT @id Id) s ON t.Id=s.Id WHEN MATCHED THEN UPDATE SET Settings=@settings WHEN NOT MATCHED THEN INSERT VALUES (@id,@settings);";
        Add(command,"@id",profile.Id); Add(command,"@settings",JsonSerializer.Serialize(profile)); command.ExecuteNonQuery();
    }

    public void AppendReviewEvent(Guid id,string message,string level="Info",string file="",int completed=0,int total=0)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="INSERT dbo.ReviewEvents (ReviewId,TimeUtc,Level,Message,FilePath,CompletedFiles,TotalFiles) VALUES (@id,SYSUTCDATETIME(),@level,@message,@file,@completed,@total)";
        Add(command,"@id",id); Add(command,"@level",level); Add(command,"@message",message); Add(command,"@file",file); Add(command,"@completed",completed); Add(command,"@total",total); command.ExecuteNonQuery();
    }

    public ReviewLog? ReadReviewLog(Guid id)
    {
        using var connection=Open(); using var command=connection.CreateCommand();
        command.CommandText="SELECT Status,StartedUtc,FinishedUtc FROM dbo.ReviewRuns WHERE Id=@id; SELECT TOP(200) Id,TimeUtc,Level,Message,FilePath,CompletedFiles,TotalFiles FROM dbo.ReviewEvents WHERE ReviewId=@id ORDER BY Id DESC";
        Add(command,"@id",id); using var reader=command.ExecuteReader();
        if(!reader.Read()) return null;
        var status=reader.GetString(0); var started=DateTime.SpecifyKind(reader.GetDateTime(1),DateTimeKind.Utc);
        DateTime? finished=reader.IsDBNull(2)?null:DateTime.SpecifyKind(reader.GetDateTime(2),DateTimeKind.Utc);
        reader.NextResult(); var events=new List<ReviewEvent>();
        while(reader.Read()) events.Add(new(reader.GetInt64(0),DateTime.SpecifyKind(reader.GetDateTime(1),DateTimeKind.Utc),reader.GetString(2),reader.GetString(3),reader.GetString(4),reader.GetInt32(5),reader.GetInt32(6)));
        return new(status,started,finished,events.OrderBy(x=>x.Id).ToArray());
    }
}

