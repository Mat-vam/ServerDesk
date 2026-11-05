using System.Globalization;
using System.Resources;

namespace ServerDesk.TUI.Localization;

public static class UiText
{
    private static readonly ResourceManager Resources = new(
        "ServerDesk.TUI.Localization.Strings",
        typeof(UiText).Assembly);

    public static string Get(string key)
    {
        return Resources.GetString(
            key,
            CultureInfo.CurrentUICulture)
            ?? throw new InvalidOperationException(
                $"Missing translation resource: {key}");
    }

    public static string Format(
        string key,
        params object?[] arguments)
    {
        return string.Format(
            CultureInfo.CurrentUICulture,
            Get(key),
            arguments);
    }

    public static void SetLanguage(string language)
    {
        if (language is not ("en" or "pl"))
        {
            throw new ArgumentException(
                "Supported languages: en, pl.",
                nameof(language));
        }

        CultureInfo culture =
            CultureInfo.GetCultureInfo(language);

        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}