namespace WaitingSimulator.Game;

/// <summary>How one figure differs from the rest: its size, the colors of its skin, clothes, hair and gear, and what it wears.</summary>
internal readonly record struct Look(float HeightScale, Paint Skin, Paint Shirt, Paint Trousers, Paint Hair, Paint Gear, Wear Wears);
