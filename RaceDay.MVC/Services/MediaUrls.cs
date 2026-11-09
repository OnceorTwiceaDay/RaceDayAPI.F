namespace RaceDay.MVC.Services;

public static class MediaUrls
{
    public static string? ForMvc(string? apiPath)
    {
        if (string.IsNullOrWhiteSpace(apiPath)) return null;
        // The browser loads images from MVC; MVC proxies through the API, never to Azure.
        if (apiPath.StartsWith("/api/media/", StringComparison.Ordinal))
            return "/media/" + apiPath["/api/media/".Length..];
        return null;
    }
}
