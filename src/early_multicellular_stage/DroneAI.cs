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

    [JsonProperty]
    private Microbe? microbeIMightShoot;

    [JsonProperty]
    private Vector3? thingImightEngulf;

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
            PopulateTargets(data);

            if (thingImightEngulf != null && DistanceFromMe((Vector3)thingImightEngulf) < 200.0f)
            {
                retval.State = Microbe.MicrobeState.Engulf;
            }

            if (microbeIMightShoot != null && DistanceFromMe((Vector3)microbeIMightShoot.GlobalTransform.origin) < 400.0f)
            {
                retval.ToxinShootTarget = microbeIMightShoot.GlobalTransform.origin;
            }
        }

        return retval;
    }

    private void PopulateTargets(MicrobeAICommonData data)
    {
        float lowestEngulfDistance = float.MaxValue;
        Vector3? target = null;

        foreach (var chunk in data.AllChunks)
        {
            if (DistanceFromMe(chunk.GlobalTransform.origin) < lowestEngulfDistance)
            {
                thingImightEngulf = chunk.GlobalTransform.origin;
                lowestEngulfDistance = DistanceFromMe(chunk.GlobalTransform.origin);
            }
        }

        foreach (var possiblePrey in data.AllMicrobes)
        {
            if (MicrobeAIFunctions.CanTryToEatMicrobe(microbe, possiblePrey) && DistanceFromMe(possiblePrey.GlobalTransform.origin) < lowestEngulfDistance)
            {
                if (MicrobeAIFunctions.CouldEngulf(microbe.EngulfSize, possiblePrey.EngulfSize))
                {
                    thingImightEngulf = possiblePrey.GlobalTransform.origin;
                    lowestEngulfDistance = DistanceFromMe(possiblePrey.GlobalTransform.origin);
                }
                if (MicrobeAIFunctions.WouldTryToToxinHuntBiggerPrey(microbe.Species.Behaviour.Opportunism) && MicrobeAIFunctions.CanShootToxin(microbe))
                {
                    microbeIMightShoot = possiblePrey;
                }

            }
        }
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
