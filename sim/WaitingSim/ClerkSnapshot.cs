using System.Numerics;

namespace WaitingSim;

/// <summary>A crowd's clerk as the sim last left it.</summary>
/// <param name="Glance">Where on the counter the clerk's glance is.</param>
/// <param name="Picked">Whom the glance landed on, while the clerk asks them "Yes?" before serving them.</param>
/// <param name="Serving">Whom the clerk is serving.</param>
public sealed record ClerkSnapshot(Vector2 Glance, AgentId? Picked, AgentId? Serving);
