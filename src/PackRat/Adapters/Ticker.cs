using System;
using System.Collections.Generic;
using ExiledToolkit.Web;
using PackRat.Core;
using UnityEngine;

namespace PackRat.Adapters;

/// <summary>
/// The Unity object that gives Pack Rat its frames: it keeps the tab script in the web UI, handles the tabs' messages
/// on the game's thread, places the items a load held back, and applies a change made in the middle of a game. Each distinct failure is logged once and never
/// rethrown into the game.
/// </summary>
public sealed class Ticker : MonoBehaviour
{
    private const int MaxReportedFailures = 20;

    internal static WebPage? Page;
    internal static PageMailbox? Inbox;
    internal static Tabs? Tabs;
    internal static Action<string> Log = _ => { };
    private static readonly HashSet<string> ReportedFailures = new();

    public Ticker(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        try
        {
            var now = Time.unscaledTime;

            Page?.KeepInstalled(now);

            while (Inbox != null && Inbox.TryTake(out var message)) Tabs?.Handle(PageCommand.Parse(message));

            Overflow.Tick(now);
            LiveApply.Tick(now);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private static void ReportFailure(Exception ex)
    {
        if (ReportedFailures.Count >= MaxReportedFailures || !ReportedFailures.Add(ex.GetType().FullName + ": " + ex.Message)) return;

        Log("update failed (each distinct failure reported once): " + ex);

        if (ReportedFailures.Count == MaxReportedFailures) Log($"{MaxReportedFailures} distinct update failures reported; further ones are not logged");
    }
}
