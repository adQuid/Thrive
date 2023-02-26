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

        if (microbe.CellTypeProperties.MembraneType.CellWall)
        {
            var thingIMightWantToEat = NearestEngulfableThing(data);

            if (thingIMightWantToEat != null && DistanceFromMe((Vector3)thingIMightWantToEat) < 15.0f)
            {
                retval.State = Microbe.MicrobeState.Engulf;
            }
        }

        return retval;
    }

    private Vector3? NearestEngulfableThing(MicrobeAICommonData data)
    {
        float lowestDistance = 9999.0f;
        Vector3? target = null;

        foreach (var chunk in data.AllChunks)
        {
            if (DistanceFromMe(chunk.GlobalTransform.origin) < lowestDistance)
            {
                target = chunk.GlobalTransform.origin;
                lowestDistance = DistanceFromMe(chunk.GlobalTransform.origin);
            }
        }

        foreach (var microbe in data.AllMicrobes)
        {
            if (DistanceFromMe(microbe.GlobalTransform.origin) < lowestDistance)
            {
                target = microbe.GlobalTransform.origin;
                lowestDistance = DistanceFromMe(microbe.GlobalTransform.origin);
            }
        }

        return target;
    }

    private float DistanceFromMe(Vector3 target)
    {
        return (target - microbe.Translation).LengthSquared();
    }

    private void DebugFlash()
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }

}
