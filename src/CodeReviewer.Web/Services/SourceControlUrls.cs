namespace CodeReviewer.Web.Services;

public static class SourceControlUrls
{
    // GitLab's external_url may identify an internal host behind a reverse proxy.
    public static string GitLabWebUrl(SourceConnection connection, string apiUrl)
    {
        var configured = new Uri(connection.BaseUrl);
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var upstream) || upstream.Scheme is not ("http" or "https"))
            return connection.BaseUrl;
        return configured.GetLeftPart(UriPartial.Authority) + upstream.AbsolutePath + upstream.Query + upstream.Fragment;
    }
}
