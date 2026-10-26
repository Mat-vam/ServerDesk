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
            // ListView nie obsluzyl strzalki,
            // np. na koncu listy.
            // Nie przekazujemy jej do globalnej nawigacji.
            return true;
        }

        return base.OnKeyDownNotHandled(key);
    }
}
