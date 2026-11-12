using Terminal.Gui.Input;
using Terminal.Gui.Views;

namespace ServerDesk.TUI.Components;

public sealed class DeviceListView : ListView
{
    protected override bool OnKeyDownNotHandled(Key key)
    {
        if (HasFocus &&
            (key == Key.CursorUp ||
             key == Key.CursorDown))
        {
            // Strzałki na granicy listy pozostają w obsłudze listy.
            return true;
        }

        return base.OnKeyDownNotHandled(key);
    }
}
