using Godot;
using System.Collections.Generic;

public class MicrobeAIResponse
{
    public Vector3? LookTarget;
    public Vector3? MovementDirection;
    public Vector3? ToxinShootTarget;
    public Microbe.MicrobeState State = Microbe.MicrobeState.Normal;
    public MicrobeSignalCommand? Command;
    public List<DroneAIResponse> DroneResponses = new List<DroneAIResponse>();
}
