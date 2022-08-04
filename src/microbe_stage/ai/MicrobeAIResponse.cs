using Godot;

public class MicrobeAIResponse
{
    public Vector3? LookTarget;
    public Vector3? MovementDirection;
    public Vector3? ToxinShootTarget;
    public Microbe.MicrobeState State = Microbe.MicrobeState.Normal;
    public MicrobeSignalCommand? Command;
}
