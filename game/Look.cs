namespace WaitingSimulator.Game;

/// <summary>How one figure differs from the rest: its size and the colors of its skin, clothes and hair.</summary>
internal readonly record struct Look(float HeightScale, Paint Skin, Paint Shirt, Paint Trousers, Paint Hair);
