namespace PackRat.Core;

/// <summary>The ScrollBars setting as bags.js takes it.</summary>
internal static class BagScroll
{
    /// <summary>On, a grid too big for the window scrolls; off, every screen is left to the game, which shrinks it to fit.</summary>
    public static string Script(bool on) => "window.__slprBagsScroll&&window.__slprBagsScroll(" + (on ? "true" : "false") + ");";
}
