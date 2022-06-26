using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Handles floating chunks emitting compounds and dissolving. This is centralized to be able to apply the max chunks
///   cap.
/// </summary>
public class CurrentSystem
{
    private readonly Node worldRoot;

    private readonly CompoundCloudSystem clouds;

    public CurrentSystem(Node worldRoot, CompoundCloudSystem cloudSystem)
    {
        this.worldRoot = worldRoot;
        clouds = cloudSystem;
    }

    public void Process(float delta)
    {
        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var chunks = worldRoot.GetChildrenToProcess<FloatingChunk>(Constants.AI_TAG_CHUNK).ToList();
        var microbes = worldRoot.GetChildrenToProcess<Microbe>(Constants.AI_TAG_MICROBE).ToList();

        // Currents that pull objects towards a center point
        var sinkholes = new List<Sinkhole>();
        List<RigidBody> movables = chunks.ToList<RigidBody>();
        movables.AddRange(microbes.ToList<RigidBody>());


        foreach (var microbe in microbes.Where(m => m.State == Microbe.MicrobeState.Engulf))
        {
            var ciliaCount = 0;

            foreach (var organelle in microbe.organelles)
            {
                if (organelle.Definition.HasComponentFactory<CiliaComponentFactory>())
                {
                    ciliaCount++;
                }
            }

            if (ciliaCount > 0)
            {
                sinkholes.Add(new Sinkhole(microbe.Translation, ciliaCount * 0.01f));
            }
        }

        foreach (var sinkhole in sinkholes)
        {
            foreach (var element in movables)
            {
                if ((sinkhole.Location - element.GlobalTransform.origin).LengthSquared() < 500.0f)
                {
                    element.ApplyCentralImpulse((sinkhole.Location - element.GlobalTransform.origin) * sinkhole.Force);
                }
            }
        }
    }

    private class Sinkhole
    {
        public Vector3 Location;
        public float Force;

        public Sinkhole(Vector3 Location, float Force)
        {
            this.Location = Location;
            this.Force = Force;
        }
    }
}
