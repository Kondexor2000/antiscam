using AntiScam.Blog.Api.Gateway;

namespace AntiScam.Blog.Api.Services;

public static class GatewayApiInputValidator
{
    public static Dictionary<string, string[]> Validate(GatewayApiInput input)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 100)
        {
            errors["name"] = ["Nazwa jest wymagana i nie może przekraczać 100 znaków."];
        }

        if (string.IsNullOrWhiteSpace(input.Version) || input.Version.Trim().Length > 40)
        {
            errors["version"] = ["Wersja jest wymagana i nie może przekraczać 40 znaków."];
        }

        if (!Uri.TryCreate(input.UpstreamUrl, UriKind.Absolute, out var upstream) ||
            (upstream.Scheme != Uri.UriSchemeHttp && upstream.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(upstream.Host) ||
            !string.IsNullOrEmpty(upstream.UserInfo) ||
            !string.IsNullOrEmpty(upstream.Query) ||
            !string.IsNullOrEmpty(upstream.Fragment) ||
            input.UpstreamUrl.Length > 2048)
        {
            errors["upstreamUrl"] = ["Podaj poprawny adres HTTP/HTTPS upstream bez danych logowania, query ani fragmentu."];
        }

        return errors;
    }
}
