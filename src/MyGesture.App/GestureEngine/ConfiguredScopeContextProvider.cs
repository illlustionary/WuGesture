namespace MyGesture.App.GestureEngine;

public sealed class ConfiguredScopeContextProvider : IGestureScopeContextProvider
{
    private readonly IGestureScopeContextProvider innerProvider;
    private Dictionary<string, string> appCategories;

    public ConfiguredScopeContextProvider(
        IEnumerable<GestureApplicationConfig> applications,
        IGestureScopeContextProvider? innerProvider = null)
    {
        this.innerProvider = innerProvider ?? new ForegroundWindowScopeContextProvider();
        appCategories = BuildAppCategories(applications);
    }

    public GestureScopeContext GetCurrentContext()
    {
        var context = innerProvider.GetCurrentContext();
        if (string.IsNullOrWhiteSpace(context.AppName))
        {
            return context;
        }

        return appCategories.TryGetValue(context.AppName.Trim(), out var category)
            ? new GestureScopeContext(context.AppName, category)
            : context;
    }

    public void UpdateApplications(IEnumerable<GestureApplicationConfig> applications)
    {
        appCategories = BuildAppCategories(applications);
    }

    private static Dictionary<string, string> BuildAppCategories(IEnumerable<GestureApplicationConfig> applications)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var application in applications)
        {
            var name = NormalizeAppName(application.Name);
            var category = application.Category.Trim();
            if (name.Length == 0 || category.Length == 0)
            {
                continue;
            }

            result[name] = category;
        }

        return result;
    }

    private static string NormalizeAppName(string value)
    {
        var name = Path.GetFileNameWithoutExtension(value.Trim());
        return string.IsNullOrWhiteSpace(name) ? value.Trim() : name;
    }
}
