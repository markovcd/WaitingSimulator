using Godot;

namespace WaitingSimulator.Game;

/// <summary>
/// The cel-shaded look every surface shares (<c>Toon.gdshader</c>, with <c>Outline.gdshader</c> as its ink line), and
/// one material per color and edging, so a crowd of figures draws from a handful of materials.
/// </summary>
internal static class Toon
{
    private static readonly Shader Surface = GD.Load<Shader>("res://Toon.gdshader");
    private static readonly Shader Ink = GD.Load<Shader>("res://Outline.gdshader");

    private static readonly Dictionary<(Color, Edging), ShaderMaterial> Shared = [];

    /// <summary>The material for <paramref name="color"/> with <paramref name="edging"/>, shared by everything painted the same.</summary>
    public static ShaderMaterial Of(Color color, Edging edging = Edging.None)
    {
        if (!Shared.TryGetValue((color, edging), out var material))
        {
            material = Tintable(color);
            if (edging != Edging.None)
            {
                var ink = new ShaderMaterial { Shader = Ink };
                ink.SetShaderParameter("square", edging == Edging.Square);
                material.NextPass = ink;
            }

            Shared[(color, edging)] = material;
        }

        return material;
    }

    /// <summary>A material of its own for <paramref name="color"/>, for a surface whose color changes.</summary>
    public static ShaderMaterial Tintable(Color color)
    {
        var material = new ShaderMaterial { Shader = Surface };
        Tint(material, color);
        return material;
    }

    /// <summary>A material of its own for a floor of <paramref name="color"/> laid in tiles <paramref name="size"/> meters across.</summary>
    public static ShaderMaterial Tiled(Color color, float size)
    {
        var material = Tintable(color);
        material.SetShaderParameter("tile", size);
        return material;
    }

    /// <summary>A material for <paramref name="color"/> that gives off <paramref name="glow"/> of its color, lit or not.</summary>
    public static ShaderMaterial Glowing(Color color, float glow)
    {
        var material = Tintable(color);
        material.SetShaderParameter("glow", glow);
        return material;
    }

    /// <summary>Repaints <paramref name="material"/> <paramref name="color"/>.</summary>
    public static void Tint(ShaderMaterial material, Color color) => material.SetShaderParameter("albedo", color);
}
