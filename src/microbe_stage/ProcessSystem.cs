using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Runs processes in parallel on entities
/// </summary>
public class ProcessSystem
{
    private readonly List<Task> tasks = new();

    private readonly Node worldRoot;
    private BiomeConditions? biome;

    public ProcessSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        // Guard against Godot delta problems. https://github.com/Revolutionary-Games/Thrive/issues/1976
        if (delta <= 0)
            return;

        if (biome == null)
        {
            GD.PrintErr("ProcessSystem has no biome set");
            return;
        }

        var nodes = worldRoot.GetTree().GetNodesInGroup(Constants.PROCESS_GROUP);
        var nodeCount = nodes.Count;

        // Used to go from the calculated compound values to per second values for reporting statistics
        float inverseDelta = 1.0f / delta;

        // The objects are processed here in order to take advantage of threading
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < nodeCount; i += Constants.PROCESS_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                for (int a = start;
                     a < start + Constants.PROCESS_OBJECTS_PER_TASK && a < nodeCount; ++a)
                {
                    ProcessNode(nodes[a] as IProcessable, delta, inverseDelta);
                }
            });

            tasks.Add(task);
        }

        // Start and wait for tasks to finish
        executor.RunTasks(tasks);
        tasks.Clear();
    }

    /// <summary>
    ///   Sets the biome whose environmental values affect processes
    /// </summary>
    public void SetBiome(BiomeConditions newBiome)
    {
        biome = newBiome;
    }

    /// <summary>
    ///   Get the amount of environmental compound
    /// </summary>
    public float GetDissolved(Compound compound)
    {
        if (biome == null)
            throw new InvalidOperationException("Biome needs to be set before getting dissolved compounds");

        return biome.GetDissolvedInBiome(compound);
    }

    private void ProcessNode(IProcessable? processor, float delta, float inverseDelta)
    {
        if (processor == null)
        {
            GD.PrintErr("A node has been put in the process group but it isn't derived from IProcessable");
            return;
        }

        var bag = processor.ProcessCompoundStorage;

        // Set all compounds to not be useful, when some compound is
        // used it will be marked useful
        bag.ClearUseful();

        var processStatistics = processor.ProcessStatistics;

        processStatistics?.MarkAllUnused();

        foreach (TweakedProcess process in processor.ActiveProcesses)
        {
            // If rate is 0 dont do it
            // The rate specifies how fast fraction of the specified process
            // numbers this cell can do
            // TODO: would be nice still to report these to process statistics
            if (process.Rate <= 0.0f)
                continue;

            var processData = process.Process;

            var currentProcessStatistics = processStatistics?.GetAndMarkUsed(process);
            currentProcessStatistics?.BeginFrame(delta);

            RunProcess(delta, processData, bag, process, currentProcessStatistics, inverseDelta);
        }

        bag.ClampNegativeCompoundAmounts();
        bag.FixNaNCompounds();

        processStatistics?.RemoveUnused();
    }

    private void RunProcess(float delta, BioProcess processData, CompoundBag bag, TweakedProcess process,
        SingleProcessStatistics? currentProcessStatistics, float inverseDelta)
    {
        // Can your cell do the process
        bool canDoProcess = true;

        float environmentModifier = 1.0f;

        // This modifies the process overall speed to allow really fast processes to run, for example if there are
        // a ton of one organelle it might consume 100 glucose per go, which might be unlikely for the cell to have
        // so if there is *some* but not enough space for results (and also inputs) this can run the process as
        // fraction of the speed to allow the cell to still function well
        float spaceConstraintModifier = 1.0f;

        // Throttle based on compounds in the environment
        foreach (var entry in processData.Inputs)
        {
            // Set used compounds to be useful, we dont want to purge those
            bag.SetUseful(entry.Key);

            if (!entry.Key.IsEnvironmental)
                continue;

            var dissolved = GetDissolved(entry.Key);

            currentProcessStatistics?.AddInputAmount(entry.Key, dissolved);

            // Multiply envornment modifier by needed compound amounts, which compounds between different compounds
            environmentModifier *= dissolved / entry.Value;

            if (environmentModifier <= MathUtils.EPSILON)
                currentProcessStatistics?.AddLimitingFactor(entry.Key);
        }

        if (environmentModifier <= MathUtils.EPSILON)
            canDoProcess = false;

        // Throttle based on compounds in the microbe
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputRemoved = entry.Value * process.Rate * environmentModifier;

            // We don't multiply by delta here because we report the per-second values anyway. In the actual process
            // output numbers (computed after testing the speed), we need to multiply by inverse delta
            currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved);

            inputRemoved = inputRemoved * delta * spaceConstraintModifier;

            // If not enough we can't run the process unless we can lower spaceConstraintModifier enough
            var availableAmount = bag.GetCompoundAmount(entry.Key);
            if (availableAmount < inputRemoved)
            {
                bool canRun = true;

                if (availableAmount > MathUtils.EPSILON)
                {
                    var neededModifier = availableAmount / inputRemoved;

                    if (neededModifier > Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    {
                        spaceConstraintModifier = neededModifier;
                        // Due to rounding errors there can be very small disparity here between the amount available
                        // and what we will take with the modifiers. See the comment in outputs for more details
                    }
                    else
                    {
                        canRun = false;
                    }
                }

                if (!canRun)
                {
                    canDoProcess = false;
                    currentProcessStatistics?.AddLimitingFactor(entry.Key);
                }
            }
        }

        // Throttle based on available space
        foreach (var entry in processData.Outputs)
        {
            // For now lets assume compounds we produce are also useful
            bag.SetUseful(entry.Key);

            var outputAdded = entry.Value * process.Rate * environmentModifier;

            currentProcessStatistics?.AddOutputAmount(entry.Key, outputAdded);

            outputAdded = outputAdded * delta * spaceConstraintModifier;

            // if environmental right now this isn't released anywhere
            if (entry.Key.IsEnvironmental)
                continue;

            // If no space we can't do the process, if we can't adjust the space constraint modifier enough
            var remainingSpace = bag.Capacity - bag.GetCompoundAmount(entry.Key);
            if (outputAdded > remainingSpace)
            {
                bool canRun = false;

                if (remainingSpace > MathUtils.EPSILON)
                {
                    var neededModifier = remainingSpace / outputAdded;

                    if (neededModifier > Constants.MINIMUM_RUNNABLE_PROCESS_FRACTION)
                    {
                        spaceConstraintModifier = neededModifier;
                        canRun = true;
                    }

                    // With all of the modifiers we can lose a tiny bit of compound that won't fit due to rounding
                    // errors, but we ignore that here
                }

                if (!canRun)
                {
                    canDoProcess = false;
                    currentProcessStatistics?.AddCapacityProblem(entry.Key);
                }
            }
        }

        // Only carry out this process if you have all the required ingredients and enough space for the outputs
        if (!canDoProcess)
        {
            if (currentProcessStatistics != null)
                currentProcessStatistics.CurrentSpeed = 0;
            return;
        }

        float totalModifier = process.Rate * delta * environmentModifier * spaceConstraintModifier;

        if (currentProcessStatistics != null)
            currentProcessStatistics.CurrentSpeed = process.Rate * environmentModifier * spaceConstraintModifier;

        // Consume inputs
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputRemoved = entry.Value * totalModifier;

            currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved * inverseDelta);

            // This should always succeed (due to the earlier check) so it is always assumed here that this succeeded
            bag.TakeCompound(entry.Key, inputRemoved);
        }

        // Add outputs
        foreach (var entry in processData.Outputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var outputGenerated = entry.Value * totalModifier;

            currentProcessStatistics?.AddOutputAmount(entry.Key, outputGenerated * inverseDelta);

            bag.AddCompound(entry.Key, outputGenerated);
        }
    }
}
