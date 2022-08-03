using Godot;

public class MicrobeAIResponse
{
    public Vector3? LookTarget;
    public Vector3? MovementTarget;
    public Vector3? ToxinShootTarget;
    public Microbe.MicrobeState State = Microbe.MicrobeState.Normal;
    public MicrobeSignalCommand? Command;
}
