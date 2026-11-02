namespace TrayAot;

/// <summary>What the tray menu can do. The tray calls these on its own UI thread.</summary>
internal sealed record TrayActions(Action Open, Action Restart, Action Quit, Func<string> StatusLine);
