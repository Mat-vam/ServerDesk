using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace ServerDesk.TUI.Components;

internal static class KeyboardNavigation
{
    public static void Configure(View window, params View[] stops)
    {
        for (int i = 0; i < stops.Length; i++)
        {
            int currentIndex = i;
            View stop = stops[i];

            stop.CanFocus = true;
            stop.TabStop = TabBehavior.TabStop;

            // Fokus musi moc przejsc przez panele nadrzedne.
            for (View? parent = stop.SuperView;
                 parent is not null && parent != window;
                 parent = parent.SuperView)
            {
                parent.CanFocus = true;
            }

            stop.KeyDown += (_, key) =>
            {
                int direction;

                if (key == Key.Tab)
                    direction = 1;
                else if (key == Key.Tab.WithShift)
                    direction = -1;
                else
                    return;

                for (int offset = 1; offset <= stops.Length; offset++)
                {
                    int nextIndex =
                        (currentIndex + direction * offset + stops.Length)
                        % stops.Length;

                    View next = stops[nextIndex];

                    if (!CanNavigateTo(next))
                        continue;

                    key.Handled = true;
                    next.SetFocus();
                    return;
                }
            };
        }
    }

    private static bool CanNavigateTo(View view)
    {
        for (View? current = view;
             current is not null;
             current = current.SuperView)
        {
            if (!current.CanFocus || !current.Visible || !current.Enabled)
                return false;
        }

        return true;
    }
}