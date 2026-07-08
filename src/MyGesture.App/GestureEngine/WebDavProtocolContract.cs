using System.Net.Http.Headers;

namespace MyGesture.App.GestureEngine;

internal static class WebDavProtocolContract
{
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);
    public static readonly HttpMethod PropFindMethod = new("PROPFIND");
    public static readonly HttpMethod MkColMethod = new("MKCOL");
    public static readonly MediaTypeHeaderValue JsonMediaType = new("application/json");
    public const string DepthHeaderName = "Depth";
    public const string ZeroDepth = "0";
    public const string BasicAuthenticationScheme = "Basic";
    public const string HttpScheme = "http";
    public const string HttpsScheme = "https";
}
