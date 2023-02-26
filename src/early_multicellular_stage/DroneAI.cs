using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

class DroneAI
{
    private Microbe microbe;

    public DroneAI(Microbe microbe)
    {
        this.microbe = microbe;
    }

    public DroneAIResponse Think(float delta, Random random, MicrobeAICommonData data)
    {
        var retval = new DroneAIResponse();
        retval.Drone = microbe;

        retval.State = Microbe.MicrobeState.Engulf;
        //DebugFlash(microbe);

        return retval;
    }

    private void DebugFlash()
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }

}
