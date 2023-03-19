using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

public class DroneAIResponse
{
    //TODO: do I want to do it this way?
    public Microbe Drone;
    public Vector3? MovementDirection;
    public Vector3? ToxinShootTarget;
    public Microbe.MicrobeState State = Microbe.MicrobeState.Normal;
}