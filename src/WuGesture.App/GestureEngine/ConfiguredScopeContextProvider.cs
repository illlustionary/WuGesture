namespace WuGesture.App.GestureEngine;

public sealed class ConfiguredScopeContextProvider : IGestureScopeContextProvider
{
    private readonly IGestureScopeContextProvider innerProvider;
    private Dictionary<string, IReadOnlyList<string>> appCategories;

    public ConfiguredScopeContextProvider(
        IEnumerable<GestureApplicationConfig> applications,
        IGestureScopeContextProvider? innerProvider = null)
    {
        this.innerProvider = innerProvider ?? new ForegroundWindowScopeContextProvider();
        appCategories = BuildAppCategories(applications);
    }

    public GestureScopeContext GetCurrentContext()
    {
        return ApplyCategories(innerProvider.GetCurrentContext());
    }

    public GestureScopeContext GetContextForWindow(IntPtr window)
    {
        return ApplyCategories(innerProvider.GetContextForWindow(window));
    }

    private GestureScopeContext ApplyCategories(GestureScopeContext context)
    {
        if (string.IsNullOrWhiteSpace(context.AppName))
        {
            return context;
        }

        return appCategories.TryGetValue(context.AppName.Trim(), out var categories)
            ? new GestureScopeContext(context.AppName, categories)
            : context;
    }

    public void UpdateApplications(IEnumerable<GestureApplicationConfig> applications)
    {
        appCategories = BuildAppCategories(applications);
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildAppCategories(IEnumerable<GestureApplicationConfig> applications)
    {
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var application in applications)
        {
            var name = ApplicationIdentityNormalizer.NormalizeProcessName(application.Name);
            var categories = application.Categories
                .Select(category => category.Trim())
                .Where(category => category.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (name.Length == 0 || categories.Length == 0)
            {
                continue;
            }

            result[name] = categories;
        }

        return result;
    }

}
