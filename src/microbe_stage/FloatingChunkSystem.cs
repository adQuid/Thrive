using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Handles floating chunks emitting compounds and dissolving. This is centralized to be able to apply the max chunks
///   cap.
/// </summary>
public class FloatingChunkSystem
{
    private readonly Node worldRoot;

    private readonly CompoundCloudSystem clouds;

    private Vector3 latestPlayerPosition = Vector3.Zero;

    public FloatingChunkSystem(Node worldRoot, CompoundCloudSystem cloudSystem)
    {
        this.worldRoot = worldRoot;
        clouds = cloudSystem;
    }

    public void Process(float delta, Random random, Vector3? playerPosition)
    {
        if (playerPosition != null)
            latestPlayerPosition = playerPosition.Value;

        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var chunks = worldRoot.GetChildrenToProcess<FloatingChunk>(Constants.AI_TAG_CHUNK).ToList();
        foreach (var chunk in chunks)
        {
            chunk.ProcessChunk(delta, clouds);

            if (random.NextDouble() < 0.25 * delta)
            {
                chunk.PopImmediately(clouds);
            }
        }
    }
}
