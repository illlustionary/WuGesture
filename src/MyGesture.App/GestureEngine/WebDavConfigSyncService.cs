using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace MyGesture.App.GestureEngine;

public sealed class WebDavConfigSyncService
{
    private const string ConfigFileName = "gestures.json";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public async Task UploadAsync(string configPath, WebDavUiSettings settings, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException("本地配置文件不存在。", configPath);
        }

        var targetUri = BuildTargetUri(settings);
        using var client = CreateHttpClient(settings);
        await EnsureRemoteCollectionsAsync(client, targetUri, cancellationToken);

        await using var stream = File.OpenRead(configPath);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Put, targetUri)
        {
            Content = content
        };
        using var response = await client.SendAsync(request, cancellationToken);
        EnsureSuccess(response, "保存到 WebDAV 失败");
    }

    public async Task<string> DownloadAsync(WebDavUiSettings settings, CancellationToken cancellationToken = default)
    {
        var targetUri = BuildTargetUri(settings);
        using var client = CreateHttpClient(settings);
        using var response = await client.GetAsync(targetUri, cancellationToken);
        EnsureSuccess(response, "从 WebDAV 恢复失败");
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task TestConnectionAsync(WebDavUiSettings settings, CancellationToken cancellationToken = default)
    {
        var targetUri = BuildTargetUri(settings);
        using var client = CreateHttpClient(settings);
        await EnsureRemoteCollectionsAsync(client, targetUri, cancellationToken);

        using var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), GetCollectionUri(targetUri));
        request.Headers.Add("Depth", "0");
        using var response = await client.SendAsync(request, cancellationToken);
        EnsureSuccess(response, "WebDAV 连接测试失败");
    }

    private static HttpClient CreateHttpClient(WebDavUiSettings settings)
    {
        var handler = new HttpClientHandler
        {
            PreAuthenticate = true
        };

        var client = new HttpClient(handler)
        {
            Timeout = RequestTimeout
        };

        if (!string.IsNullOrWhiteSpace(settings.UserName) || !string.IsNullOrWhiteSpace(settings.Password))
        {
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{settings.UserName}:{settings.Password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        return client;
    }

    private static Uri BuildTargetUri(WebDavUiSettings settings)
    {
        if (!Uri.TryCreate(settings.Address, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("WebDAV 地址必须是有效的 http 或 https 地址。");
        }

        var remotePath = (settings.RemotePath ?? "").Trim().Replace('\\', '/').TrimStart('/');
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            remotePath = ConfigFileName;
        }
        else if (remotePath.EndsWith('/'))
        {
            remotePath += ConfigFileName;
        }
        else if (!Path.HasExtension(remotePath))
        {
            remotePath += "/" + ConfigFileName;
        }

        var baseText = baseUri.ToString();
        if (!baseText.EndsWith('/'))
        {
            baseText += "/";
        }

        return new Uri(new Uri(baseText), remotePath);
    }

    private static async Task EnsureRemoteCollectionsAsync(HttpClient client, Uri targetUri, CancellationToken cancellationToken)
    {
        var segments = targetUri.Segments
            .Select(segment => Uri.UnescapeDataString(segment).Trim('/'))
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();

        if (segments.Length <= 1)
        {
            return;
        }

        var builder = new UriBuilder(targetUri)
        {
            Query = "",
            Fragment = ""
        };

        var path = "";
        for (var index = 0; index < segments.Length - 1; index++)
        {
            path += "/" + Uri.EscapeDataString(segments[index]);
            builder.Path = path + "/";

            using var request = new HttpRequestMessage(new HttpMethod("MKCOL"), builder.Uri);
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode ||
                response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.Conflict)
            {
                continue;
            }

            EnsureSuccess(response, "创建 WebDAV 远程目录失败");
        }
    }

    private static Uri GetCollectionUri(Uri targetUri)
    {
        var builder = new UriBuilder(targetUri)
        {
            Query = "",
            Fragment = ""
        };

        var path = builder.Path;
        var slashIndex = path.LastIndexOf('/');
        builder.Path = slashIndex < 0 ? "/" : path[..(slashIndex + 1)];
        return builder.Uri;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string prefix)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        throw new InvalidOperationException($"{prefix}：{(int)response.StatusCode} {response.ReasonPhrase}");
    }
}
