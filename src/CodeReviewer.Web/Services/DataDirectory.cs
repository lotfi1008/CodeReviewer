namespace CodeReviewer.Web.Services;

public static class DataDirectory
{
    public static string Resolve(IConfiguration config)
    {
        var path = config["Storage:DataDirectory"];
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path))
            throw new InvalidOperationException("Storage:DataDirectory must specify an absolute, writable data directory in configuration.");
        return Path.GetFullPath(path);
    }
}
