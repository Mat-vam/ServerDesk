namespace ServerDesk.TUI.Localization;

public static class ScreenStateText
{
    public static string Get(string state)
    {
        if (string.Equals(
            state,
            "Attached",
            StringComparison.OrdinalIgnoreCase))
        {
            return UiText.Get("ScreenStateAttached");
        }

        if (string.Equals(
            state,
            "Detached",
            StringComparison.OrdinalIgnoreCase))
        {
            return UiText.Get("ScreenStateDetached");
        }

        return state;
    }
}