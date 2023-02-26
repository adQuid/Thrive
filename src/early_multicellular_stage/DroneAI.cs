using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

class DroneAI
{
    public static void Think(Microbe microbe)
    {
        DebugFlash(microbe);
    }

    private static void DebugFlash(Microbe microbe)
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }

}
