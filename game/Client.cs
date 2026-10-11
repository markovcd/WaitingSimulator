using Godot;
using WaitingSim;

namespace WaitingSimulator.Game;

/// <summary>
/// The client's root node. Started with <c>-- --smoke</c>, it loads the sim and quits as soon as it is ready,
/// which is how the gate checks the game starts under Godot.
/// </summary>
public sealed partial class Client : Node
{
    /// <inheritdoc/>
    public override void _Ready()
    {
        if (OS.GetCmdlineUserArgs().Contains("--smoke"))
        {
            // Touching a sim type loads the sim's assembly, so a sim that cannot load fails the smoke run.
            GD.Print($"Client ready; sim {typeof(Sim).Assembly.GetName().Version}.");
            GetTree().Quit();
        }
    }
}
