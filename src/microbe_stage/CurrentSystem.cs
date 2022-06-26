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

        foreach (var chunk in chunks)
        {
            chunk.ProcessChunk(delta, clouds);

            //TODO: Make this not terrible
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

                if (ciliaCount > 0
                    && (microbe.GlobalTransform.origin - chunk.GlobalTransform.origin).LengthSquared() < 500.0f)
                {
                    chunk.ApplyCentralImpulse((microbe.GlobalTransform.origin - chunk.Translation) * 0.01f * ciliaCount);
                }
            }
        }
    }
}
