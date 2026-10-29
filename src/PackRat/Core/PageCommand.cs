using System;

namespace PackRat.Core;

/// <summary>
/// What the Inventories and Items tabs ask for. They send "slmod|packrat|" followed by a verb; anything else parses to
/// Unknown so it can be logged rather than acted on.
/// </summary>
public abstract record PageCommand
{
    public const string Prefix = "slmod|packrat|";

    private const string SaveGridsVerb = "grids|save|";
    private const string SaveItemsVerb = "items|save|";

    private PageCommand()
    {
    }

    /// <summary>
    /// A tab opened and wants the game's tables, the settings and both files. <paramref name="WantsData"/> is true when the
    /// page does not have the item and inventory names and icons yet (they are large, so they are sent once per page).
    /// </summary>
    public sealed record Load(bool WantsData) : PageCommand;

    /// <summary>The page has no script, usually because the web view reloaded.</summary>
    public sealed record NoScript : PageCommand;

    /// <summary>The Inventories tab saved: <paramref name="Json"/> is the whole new file.</summary>
    public sealed record SaveGrids(string Json) : PageCommand;

    /// <summary>The Items tab saved: <paramref name="Json"/> is the whole new file.</summary>
    public sealed record SaveItems(string Json) : PageCommand;

    public sealed record Unknown(string Text) : PageCommand;

    public static bool IsOurs(string? message) => message is not null && message.StartsWith(Prefix, StringComparison.Ordinal);

    /// <summary>Parses a whole message, prefix included.</summary>
    public static PageCommand Parse(string message)
    {
        if (!IsOurs(message)) return new Unknown(message);

        var verb = message.Substring(Prefix.Length);

        if (verb.StartsWith(SaveGridsVerb, StringComparison.Ordinal)) return new SaveGrids(verb.Substring(SaveGridsVerb.Length));
        if (verb.StartsWith(SaveItemsVerb, StringComparison.Ordinal)) return new SaveItems(verb.Substring(SaveItemsVerb.Length));

        return verb switch
        {
            "load" => new Load(false),
            "load|data" => new Load(true),
            "noscript" => new NoScript(),
            _ => new Unknown(message),
        };
    }
}
