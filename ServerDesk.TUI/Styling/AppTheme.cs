using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using GuiAttribute = Terminal.Gui.Drawing.Attribute;

namespace ServerDesk.TUI.Styling;

public static class AppTheme
{
    public static Scheme CreateBase()
    {
        return new Scheme
        {
            Normal = new GuiAttribute(
                ColorName16.Gray, ColorName16.Black),

            HotNormal = new GuiAttribute(
                ColorName16.BrightCyan, ColorName16.Black),

            Focus = new GuiAttribute(
                ColorName16.Black, ColorName16.BrightCyan),

            HotFocus = new GuiAttribute(
                ColorName16.Black, ColorName16.BrightCyan),

            Active = new GuiAttribute(
                ColorName16.BrightCyan, ColorName16.Black),

            HotActive = new GuiAttribute(
                ColorName16.BrightCyan, ColorName16.Black),

            Highlight = new GuiAttribute(
                ColorName16.Black, ColorName16.BrightCyan),

            Editable = new GuiAttribute(
                ColorName16.Gray, ColorName16.Black),

            ReadOnly = new GuiAttribute(
                ColorName16.Gray, ColorName16.Black),

            Disabled = new GuiAttribute(
                ColorName16.DarkGray, ColorName16.Black)
        };
    }

    public static Scheme CreateAccent()
    {
        return new Scheme(CreateBase())
        {
            Normal = new GuiAttribute(
                ColorName16.BrightCyan,
                ColorName16.Black)
        };
    }

    public static Scheme CreateWarning()
    {
        var normal = new GuiAttribute(
            ColorName16.BrightYellow,
            ColorName16.Black);

        var focused = new GuiAttribute(
            ColorName16.Black,
            ColorName16.BrightYellow);

        return new Scheme(CreateBase())
        {
            Normal = normal,
            HotNormal = normal,
            Active = normal,
            HotActive = normal,
            Focus = focused,
            HotFocus = focused,
            Highlight = focused
        };
    }

    public static void Apply(View root)
    {
        Scheme scheme;

        if (root is TextView)
        {
            // Neutralne tło konsoli także przy aktywnym fokusie.
            var neutral = new GuiAttribute(
                ColorName16.Gray,
                ColorName16.Black);

            scheme = new Scheme(CreateBase())
            {
                Focus = neutral,
                HotFocus = neutral,
                Active = neutral,
                HotActive = neutral
            };
        }
        else if (root is TextField)
        {
            var normal = new GuiAttribute(
                ColorName16.White,
                ColorName16.DarkGray);

            var focused = new GuiAttribute(
                ColorName16.Black,
                ColorName16.BrightCyan);

            scheme = new Scheme(CreateBase())
            {
                Normal = normal,
                HotNormal = normal,
                Editable = normal,

                Focus = focused,
                HotFocus = focused,
                Active = focused,
                HotActive = focused,

                Disabled = new GuiAttribute(
                    ColorName16.Gray,
                    ColorName16.DarkGray)
            };
        }
        else
        {
            scheme = root is Button
                ? CreateAccent()
                : CreateBase();
        }

        root.SetScheme(scheme);

        foreach (View child in root.SubViews)
            Apply(child);
    }
}