namespace WuGesture.App.Tests;

public sealed class WebMessageRouterTests
{
    [Fact]
    public void Route_DispatchesObjectMessageToMatchingHandler()
    {
        var received = new List<string>();
        var router = CreateRouter(new Dictionary<string, Action<string>>(StringComparer.Ordinal)
        {
            ["save-rules"] = received.Add
        });

        router.Route("""{"type":"save-rules","rules":[]}""");

        Assert.Equal("""{"type":"save-rules","rules":[]}""", Assert.Single(received));
    }

    [Fact]
    public void Route_PassesStatusRequestStringToStatusHandler()
    {
        string? received = "not-called";
        var router = CreateRouter(onStatusRequest: value => received = value);

        router.Route("\"get-status\"");

        Assert.Equal("get-status", received);
    }

    [Fact]
    public void Route_IgnoresUnknownMessageTypeWithoutFailing()
    {
        var failures = 0;
        var router = CreateRouter(onFailure: _ => failures++);

        router.Route("""{"type":"not-a-real-message"}""");

        Assert.Equal(0, failures);
    }

    [Fact]
    public void Route_IgnoresMessagesWithoutTypeOrWithNonObjectRoot()
    {
        var failures = 0;
        var router = CreateRouter(onFailure: _ => failures++);

        router.Route("""{"rules":[]}""");
        router.Route("42");
        router.Route("null");
        router.Route("""{"type":""}""");

        Assert.Equal(0, failures);
    }

    [Fact]
    public void Route_ReportsMalformedJsonThroughFailureCallback()
    {
        Exception? failure = null;
        var router = CreateRouter(onFailure: exception => failure = exception);

        router.Route("{not json");

        Assert.IsAssignableFrom<System.Text.Json.JsonException>(failure);
    }

    [Fact]
    public void Route_ContainsHandlerExceptionsAndStillRoutesLaterMessages()
    {
        var failures = new List<Exception>();
        var handled = 0;
        var router = CreateRouter(
            new Dictionary<string, Action<string>>(StringComparer.Ordinal)
            {
                ["save-rules"] = _ => throw new InvalidOperationException("boom"),
                ["reload-rules"] = _ => handled++
            },
            onFailure: failures.Add);

        router.Route("""{"type":"save-rules"}""");
        router.Route("""{"type":"reload-rules"}""");

        Assert.Single(failures);
        Assert.IsType<InvalidOperationException>(failures[0]);
        Assert.Equal(1, handled);
    }

    private static WebMessageRouter CreateRouter(
        IReadOnlyDictionary<string, Action<string>>? handlers = null,
        Action<string?>? onStatusRequest = null,
        Action<Exception>? onFailure = null)
    {
        return new WebMessageRouter(
            handlers ?? new Dictionary<string, Action<string>>(StringComparer.Ordinal),
            onStatusRequest ?? (_ => { }),
            onFailure ?? (_ => { }));
    }
}
