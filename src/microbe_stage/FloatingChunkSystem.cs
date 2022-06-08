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

    public void Process(float delta, Vector3? playerPosition, List<Microbe> allMicrobes)
    {
        if (playerPosition != null)
            latestPlayerPosition = playerPosition.Value;

        // https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        var chunks = worldRoot.GetChildrenToProcess<FloatingChunk>(Constants.AI_TAG_CHUNK).ToList();

        var findTooManyChunksTask = new Task<IEnumerable<FloatingChunk>>(() =>
        {
            int tooManyChunks =
                Math.Min(Constants.MAX_DESPAWNS_PER_FRAME, chunks.Count - Constants.FLOATING_CHUNK_MAX_COUNT);

            if (tooManyChunks < 1)
                return Array.Empty<FloatingChunk>();

            var comparePosition = latestPlayerPosition;

            return chunks.OrderByDescending(c => c.Translation.DistanceSquaredTo(comparePosition))
                .Take(tooManyChunks);
        });

        TaskExecutor.Instance.AddTask(findTooManyChunksTask);

        foreach (var chunk in chunks)
        {
            chunk.ProcessChunk(delta, clouds);

            //TODO: Make this not terrible
            foreach (var microbe in allMicrobes.Where(m => m.State == Microbe.MicrobeState.Engulf))
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
                    chunk.ApplyCentralImpulse((microbe.GlobalTransform.origin - chunk.Translation) * 0.15f * ciliaCount);
                }
            }
        }

        findTooManyChunksTask.Wait();
        foreach (var toDespawn in findTooManyChunksTask.Result)
        {
            toDespawn.PopImmediately(clouds);
        }
    }
}
