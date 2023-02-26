using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;
using Newtonsoft.Json;

class DroneAI
{
    [JsonProperty]
    private Microbe microbe;

    public DroneAI(Microbe microbe)
    {
        this.microbe = microbe;
    }

    public DroneAIResponse Think(float delta, Random random, MicrobeAICommonData data)
    {
        var retval = new DroneAIResponse();
        retval.Drone = microbe;

        if (microbe != null)
        {
            var thingIMightWantToEat = NearestEngulfableThing(data);

            if (thingIMightWantToEat != null && DistanceFromMe((Vector3)thingIMightWantToEat) < 200.0f)
            {
                retval.State = Microbe.MicrobeState.Engulf;
            }
        }

        return retval;
    }

    private Vector3? NearestEngulfableThing(MicrobeAICommonData data)
    {
        float lowestDistance = float.MaxValue;
        Vector3? target = null;

        foreach (var chunk in data.AllChunks)
        {
            if (DistanceFromMe(chunk.GlobalTransform.origin) < lowestDistance)
            {
                target = chunk.GlobalTransform.origin;
                lowestDistance = DistanceFromMe(chunk.GlobalTransform.origin);
            }
        }

        foreach (var possiblePrey in data.AllMicrobes)
        {
            if (MicrobeAIFunctions.CanTryToEatMicrobe(microbe, possiblePrey) && DistanceFromMe(possiblePrey.GlobalTransform.origin) < lowestDistance)
            {
                target = possiblePrey.GlobalTransform.origin;
                lowestDistance = DistanceFromMe(possiblePrey.GlobalTransform.origin);
            }
        }

        return target;
    }

    private float DistanceFromMe(Vector3 target)
    {
        return (target - microbe.GlobalTransform.origin).LengthSquared();
    }

    private void DebugFlash()
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }

}
