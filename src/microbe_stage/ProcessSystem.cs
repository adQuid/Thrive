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

<<<<<<< HEAD
=======
    /// <summary>
    ///   Computes the process efficiency numbers for given organelles
    ///   given the active biome data.
    /// </summary>
    public static Dictionary<string, OrganelleEfficiency> ComputeOrganelleProcessEfficiencies(
        IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome)
    {
        var result = new Dictionary<string, OrganelleEfficiency>();

        foreach (var organelle in organelles)
        {
            var info = new OrganelleEfficiency(organelle);

            foreach (var process in organelle.RunnableProcesses)
            {
                info.Processes.Add(CalculateProcessMaximumSpeed(process, biome));
            }

            result[organelle.InternalName] = info;
        }

        return result;
    }

    /// <summary>
    ///   Computes the energy balance for the given organelles in biome
    /// </summary>
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
        BiomeConditions biome, MembraneType membrane, bool isPlayerSpecies,
        WorldGenerationSettings worldSettings)
    {
        var organellesList = organelles.ToList();

        var maximumMovementDirection = MicrobeInternalCalculations.MaximumSpeedDirection(organellesList);
        return ComputeEnergyBalance(organellesList, biome, membrane, maximumMovementDirection, isPlayerSpecies,
            worldSettings);
    }

    /// <summary>
    ///   Computes the energy balance for the given organelles in biome
    /// </summary>
    /// <param name="organelles">The organelles to compute the balance with</param>
    /// <param name="biome">The conditions the organelles are simulated in</param>
    /// <param name="membrane">The membrane type to adjust the energy balance with</param>
    /// <param name="onlyMovementInDirection">
    ///   Only movement organelles that can move in this (cell origin relative) direction are calculated. Other
    ///   movement organelles are assumed to be inactive in the balance calculation.
    /// </param>
    /// <param name="isPlayerSpecies">Whether this microbe is a member of the player's species</param>
    /// <param name="worldSettings">The world generation settings for this game</param>
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
        BiomeConditions biome, MembraneType membrane, Vector3 onlyMovementInDirection,
        bool isPlayerSpecies, WorldGenerationSettings worldSettings)
    {
        var result = new EnergyBalanceInfo();

        float processATPProduction = 0.0f;
        float processATPConsumption = 0.0f;
        float movementATPConsumption = 0.0f;

        int hexCount = 0;

        foreach (var organelle in organelles)
        {
            foreach (var process in organelle.Definition.RunnableProcesses)
            {
                var processData = CalculateProcessMaximumSpeed(process, biome);

                if (processData.WritableInputs.TryGetValue(ATP, out var amount))
                {
                    processATPConsumption += amount;

                    result.AddConsumption(organelle.Definition.InternalName, amount);
                }

                if (processData.WritableOutputs.TryGetValue(ATP, out amount))
                {
                    processATPProduction += amount;

                    result.AddProduction(organelle.Definition.InternalName, amount);
                }
            }

            // Take special cell components that take energy into account
            if (organelle.Definition.HasMovementComponent)
            {
                var amount = Constants.FLAGELLA_ENERGY_COST;

                var organelleDirection = MicrobeInternalCalculations.GetOrganelleDirection(organelle);
                if (organelleDirection.Dot(onlyMovementInDirection) > 0)
                {
                    movementATPConsumption += amount;
                    result.Flagella += amount;
                    result.AddConsumption(organelle.Definition.InternalName, amount);
                }
            }

            if (organelle.Definition.HasCiliaComponent)
            {
                var amount = Constants.CILIA_ENERGY_COST;

                movementATPConsumption += amount;
                result.Cilia += amount;
                result.AddConsumption(organelle.Definition.InternalName, amount);
            }

            // Store hex count
            hexCount += organelle.Definition.HexCount;
        }

        // Add movement consumption together
        result.BaseMovement = Constants.BASE_MOVEMENT_ATP_COST * hexCount;
        result.AddConsumption("baseMovement", result.BaseMovement);
        var totalMovementConsumption = movementATPConsumption + result.BaseMovement;

        // Add osmoregulation
        result.Osmoregulation = Constants.ATP_COST_FOR_OSMOREGULATION * hexCount *
            membrane.OsmoregulationFactor;

        if (isPlayerSpecies)
        {
            result.Osmoregulation *= worldSettings.OsmoregulationMultiplier;
        }

        result.AddConsumption("osmoregulation", result.Osmoregulation);

        // Compute totals
        result.TotalProduction = processATPProduction;
        result.TotalConsumptionStationary = processATPConsumption + result.Osmoregulation;
        result.TotalConsumption = result.TotalConsumptionStationary + totalMovementConsumption;

        result.FinalBalance = result.TotalProduction - result.TotalConsumption;
        result.FinalBalanceStationary = result.TotalProduction - result.TotalConsumptionStationary;

        return result;
    }

    /// <summary>
    ///   Computes the compound balances for given organelle list in a patch
    /// </summary>
    public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalance(
        IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome)
    {
        var result = new Dictionary<Compound, CompoundBalance>();

        void MakeSureResultExists(Compound compound)
        {
            if (!result.ContainsKey(compound))
            {
                result[compound] = new CompoundBalance();
            }
        }

        foreach (var organelle in organelles)
        {
            foreach (var process in organelle.RunnableProcesses)
            {
                var speedAdjusted = CalculateProcessMaximumSpeed(process, biome);

                foreach (var input in speedAdjusted.Inputs)
                {
                    MakeSureResultExists(input.Key);
                    result[input.Key].AddConsumption(organelle.InternalName, input.Value);
                }

                foreach (var output in speedAdjusted.Outputs)
                {
                    MakeSureResultExists(output.Key);
                    result[output.Key].AddProduction(organelle.InternalName, output.Value);
                }
            }
        }

        return result;
    }

    public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalance(
        IEnumerable<OrganelleTemplate> organelles, BiomeConditions biome)
    {
        return ComputeCompoundBalance(organelles.Select(o => o.Definition), biome);
    }

    /// <summary>
    ///   Calculates the maximum speed a process can run at in a biome
    ///   based on the environmental compounds.
    /// </summary>
    public static ProcessSpeedInformation CalculateProcessMaximumSpeed(TweakedProcess process,
        BiomeConditions biome)
    {
        var result = new ProcessSpeedInformation(process.Process);

        float speedFactor = 1.0f;
        float efficiency = 1.0f;

        // Environmental inputs need to be processed first
        foreach (var input in process.Process.Inputs)
        {
            if (!input.Key.IsEnvironmental)
                continue;

            // Environmental compound that can limit the rate

            var availableInEnvironment = GetAmbientInBiome(input.Key, biome);

            var availableRate = input.Key == Temperature ?
                CalculateTemperatureEffect(availableInEnvironment) :
                availableInEnvironment / input.Value;

            result.AvailableAmounts[input.Key] = availableInEnvironment;

            efficiency *= availableInEnvironment;

            // More than needed environment value boosts the effectiveness
            result.AvailableRates[input.Key] = availableRate;

            speedFactor *= availableRate;

            result.WritableInputs[input.Key] = input.Value;
        }

        result.Efficiency = efficiency;

        speedFactor *= process.Rate;

        // Note that we don't consider storage constraints here so we don't use spaceConstraintModifier calculations

        // So that the speed factor is available here
        foreach (var entry in process.Process.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            // Normal, cloud input

            result.WritableInputs.Add(entry.Key, entry.Value * speedFactor);
        }

        foreach (var entry in process.Process.Outputs)
        {
            var amount = entry.Value * speedFactor;

            result.WritableOutputs[entry.Key] = amount;

            if (amount <= 0)
                result.WritableLimitingCompounds.Add(entry.Key);
        }

        result.CurrentSpeed = speedFactor;

        return result;
    }

>>>>>>> c3ba4cb2e (Increased auto-evo performance by 60% with more caching (#3573))
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
        // Mark usefull compounds
        foreach (var entry in processData.Inputs)
        {
            bag.SetUseful(entry.Key);
        }

        foreach (var entry in processData.Outputs)
        {
            bag.SetUseful(entry.Key);
        }

        TweakedProcess newProcess = MicrobeInternalCalculations.EnvironmentModifiedProcess(delta, biome!,  processData,  bag,  process,
         currentProcessStatistics);

        // Consume inputs
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputRemoved = entry.Value * newProcess.Rate;

            currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved * inverseDelta);

            // This should always succeed (due to the earlier check) so it is always assumed here that this succeeded
            bag.TakeCompound(entry.Key, inputRemoved);
        }

        // Add outputs
        foreach (var entry in processData.Outputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var outputGenerated = entry.Value * newProcess.Rate;

            currentProcessStatistics?.AddOutputAmount(entry.Key, outputGenerated * inverseDelta);

            bag.AddCompound(entry.Key, outputGenerated);
        }
    }
}
