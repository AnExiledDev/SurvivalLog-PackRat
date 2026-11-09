namespace PackRat.Core;

/// <summary>What the player reads in toasts and tab replies, in English and Simplified Chinese.</summary>
public static class Texts
{
    public static string Source(bool zh) => zh ? "背包达人" : "Pack Rat";

    public static string Saved(bool zh) => zh ? "已保存。游戏中立即生效，否则读档时生效。" : "Saved. Applies right away in a game, or when you load a save.";

    public static string NotSaved(bool zh) => zh ? "未保存：" : "Not saved: ";

    /// <summary>The toast after a load, or null when nothing needs saying.</summary>
    public static (string Title, string? Body)? AfterLoad(bool isUninstallPrep, int toShoppingBags, int toFloor, int leftInPlace, bool zh)
    {
        var moved = Moved(toShoppingBags, toFloor, leftInPlace, zh);

        if (isUninstallPrep)
        {
            return zh
                ? ("背包达人可以安全移除了", "所有大小和重量已恢复为游戏原值。" + (moved ?? "") + "请先存档，再移除本模组。")
                : ("Pack Rat is safe to remove", "Every size and weight is back to the game's. " + (moved == null ? "" : moved + " ") + "Save the game, then remove the mod.");
        }

        if (moved == null) return null;

        var total = toShoppingBags + toFloor + leftInPlace;

        return zh ? ($"{total} 件物品放不下了", moved) : (total == 1 ? "1 item no longer fit" : $"{total} items no longer fit", moved);
    }

    private static string? Moved(int toShoppingBags, int toFloor, int leftInPlace, bool zh)
    {
        if (toShoppingBags + toFloor + leftInPlace == 0) return null;

        if (zh)
        {
            var parts = "";
            if (toShoppingBags > 0) parts += $"{toShoppingBags} 件装进了客厅的购物袋。";
            if (toFloor > 0) parts += $"{toFloor} 件放在了地上。";
            if (leftInPlace > 0) parts += $"{leftInPlace} 件留在了原来的背包里。";

            return parts;
        }

        var lines = new System.Collections.Generic.List<string>();
        if (toShoppingBags > 0) lines.Add($"{toShoppingBags} went into the shopping bags in the living room");
        if (toFloor > 0) lines.Add($"{toFloor} went on the floor");
        if (leftInPlace > 0) lines.Add($"{leftInPlace} stayed where they were");

        return string.Join(", ", lines) + ".";
    }
}
