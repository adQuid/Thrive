using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class MicrobeInternalCalculations
{
    public static Vector3 MaximumSpeedDirection(IEnumerable<OrganelleTemplate> organelles)
    {
        Vector3 maximumMovementDirection = Vector3.Zero;

        var movementOrganelles = organelles.Where(o => o.Definition.HasMovementComponent)
            .ToList();

        foreach (var organelle in movementOrganelles)
        {
            maximumMovementDirection += GetOrganelleDirection(organelle);
        }

        // After calculating the sum of all organelle directions we subtract the movement components which
        // are symmetric and we choose the one who would benefit the max-speed the most.
        foreach (var organelle in movementOrganelles)
        {
            maximumMovementDirection = ChooseFromSymmetricFlagella(movementOrganelles,
                organelle, maximumMovementDirection);
        }

        // If the flagella are positioned symmetrically we assume the forward position as default
        if (maximumMovementDirection == Vector3.Zero)
            return Vector3.Forward;

        return maximumMovementDirection;
    }

    public static Vector3 GetOrganelleDirection(OrganelleTemplate organelle)
    {
        return (Hex.AxialToCartesian(new Hex(0, 0)) - Hex.AxialToCartesian(organelle.Position)).Normalized();
    }

    public static float CalculateSpeed(IEnumerable<OrganelleTemplate> organelles, MembraneType membraneType,
        float membraneRigidity)
    {
        float microbeMass = Constants.MICROBE_BASE_MASS;

        float organelleMovementForce = 0;

        // For each direction we calculate the organelles contribution to the movement force
        float forwardsDirectionMovementForce = 0;
        float backwardsDirectionMovementForce = 0;
        float leftwardDirectionMovementForce = 0;
        float rightwardDirectionMovementForce = 0;

        // Force factor for each direction
        float forwardDirectionFactor;
        float backwardDirectionFactor;
        float rightDirectionFactor;
        float leftDirectionFactor;

        var organellesList = organelles.ToList();

        foreach (var organelle in organellesList)
        {
            microbeMass += organelle.Definition.Mass;

            if (organelle.Definition.HasMovementComponent)
            {
                Vector3 organelleDirection = GetOrganelleDirection(organelle);

                // We decompose the vector of the organelle orientation in 2 vectors, forward and right
                // To get the backward and left is easy because they are the opposite of those former 2
                forwardDirectionFactor = organelleDirection.Dot(Vector3.Forward);
                backwardDirectionFactor = -forwardDirectionFactor;
                rightDirectionFactor = organelleDirection.Dot(Vector3.Right);
                leftDirectionFactor = -rightDirectionFactor;

                float movementConstant = Constants.FLAGELLA_BASE_FORCE
                    * organelle.Definition.Components.Movement!.Momentum / 100.0f;

                // We get the movement force for every direction as well
                forwardsDirectionMovementForce += MovementForce(movementConstant, forwardDirectionFactor);
                backwardsDirectionMovementForce += MovementForce(movementConstant, backwardDirectionFactor);
                rightwardDirectionMovementForce += MovementForce(movementConstant, rightDirectionFactor);
                leftwardDirectionMovementForce += MovementForce(movementConstant, leftDirectionFactor);
            }
        }

        var maximumMovementDirection = MaximumSpeedDirection(organellesList);

        // Maximum-force direction is not normalized so we need to normalize it here
        maximumMovementDirection = maximumMovementDirection.Normalized();

        // Calculate the maximum total force-factors in the maximum-force direction
        forwardDirectionFactor = maximumMovementDirection.Dot(Vector3.Forward);
        backwardDirectionFactor = -forwardDirectionFactor;
        rightDirectionFactor = maximumMovementDirection.Dot(Vector3.Right);
        leftDirectionFactor = -rightDirectionFactor;

        // Add each movement force to the total movement force in the maximum-force direction.
        organelleMovementForce += MovementForce(forwardsDirectionMovementForce, forwardDirectionFactor);
        organelleMovementForce += MovementForce(backwardsDirectionMovementForce, backwardDirectionFactor);
        organelleMovementForce += MovementForce(rightwardDirectionMovementForce, rightDirectionFactor);
        organelleMovementForce += MovementForce(leftwardDirectionMovementForce, leftDirectionFactor);

        float baseMovementForce = Constants.CELL_BASE_THRUST * membraneType.MovementFactor * (1 - membraneRigidity * Constants.MEMBRANE_RIGIDITY_MOBILITY_MODIFIER);

        float finalSpeed = (baseMovementForce + organelleMovementForce) / microbeMass;

        return finalSpeed;
    }

    /// <summary>
    ///   Calculates the rotation speed for a cell
    /// </summary>
    /// <param name="organelles">The organelles the cell has with their positions for the calculations</param>
    /// <returns>The rotation slerp factor (speed)</returns>
    /// <remarks>
    ///   <para>
    ///     TODO: should this also be affected by the membrane type?
    ///   </para>
    /// </remarks>
    public static float CalculateRotationSpeed(IEnumerable<IPositionedOrganelle> organelles)
    {
        float inertia = 1;

        int ciliaCount = 0;

        // For simplicity we calculate all cilia af if they are at a uniform (max radius) distance from the center
        float radiusSquared = 1;

        // Simple moment of inertia calculation. Note that it is mass multiplied by square of the distance, so we can
        // use the cheaper distance calculations
        foreach (var organelle in organelles)
        {
            var distance = Hex.AxialToCartesian(organelle.Position).LengthSquared();

            if (organelle.Definition.HasCiliaComponent)
            {
                ++ciliaCount;

                if (radiusSquared < distance)
                    radiusSquared = distance;
            }

            // Ignore the center organelle in rotation calculations
            if (distance < MathUtils.EPSILON)
                continue;

            inertia += distance * organelle.Definition.Mass * Constants.CELL_MOMENT_OF_INERTIA_DISTANCE_MULTIPLIER;
        }

        float speed = Constants.CELL_BASE_ROTATION / inertia;

        // Add the extra speed from cilia after we took away some with the rotational inertia calculation
        if (ciliaCount > 0)
        {
            speed += ciliaCount * Mathf.Sqrt(radiusSquared) * Constants.CILIA_RADIUS_FACTOR_MULTIPLIER *
                Constants.CILIA_ROTATION_FACTOR;
        }

        return Mathf.Clamp(speed, Constants.CELL_MIN_ROTATION, Constants.CELL_MAX_ROTATION);
    }

    /// <summary>
    ///   Converts the speed from <see cref="CalculateRotationSpeed"/> to a user displayable form
    /// </summary>
    /// <param name="rawSpeed">The raw speed value</param>
    /// <returns>Converted value to be shown in the GUI</returns>
    public static float RotationSpeedToUserReadableNumber(float rawSpeed)
    {
        return rawSpeed * 500;
    }

    public static float OsmoregulationCost(IEnumerable<OrganelleDefinition> organelles, MembraneType membrane)
    {
        return Constants.ATP_COST_FOR_OSMOREGULATION * organelles.Select(x => x.OsmoregulationCost).Sum() *
           membrane.OsmoregulationFactor;
    }

    public static float MovementCost(IEnumerable<OrganelleDefinition> organelles, MembraneType membrane)
    {
        return Constants.BASE_MOVEMENT_ATP_COST * organelles.Select(x => x.HexCount).Sum() + organelles.Select(x => x.Mass).Sum();
    }

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
    ///   Calculates the maximum speed a process can run at in a biome
    ///   based on the environmental compounds.
    /// </summary>
    public static ProcessSpeedInformation CalculateProcessMaximumSpeed(TweakedProcess process,
        BiomeConditions biome)
    {
        var result = new ProcessSpeedInformation(process.Process);

        float speedFactor = 1.0f;
        float efficiency = 1.0f;

        // Environmental compound that can limit the rate

        var availableInEnvironment = EnvironmentalAvailabilityThrottleFactor(process.Process, biome, null);

        // Environmental inputs need to be processed first
        foreach (var input in process.Process.Inputs)
        {
            var availableRate = availableInEnvironment / input.Value;

            if (!input.Key.IsEnvironmental)
            {
                result.AvailableAmounts[input.Key] = availableInEnvironment;
            }
            else
            {
                result.AvailableAmounts[input.Key] = biome.Compounds[input.Key].Dissolved;
            }

            efficiency *= availableInEnvironment;

            // More than needed environment value boosts the effectiveness
            result.AvailableRates[input.Key] = availableRate;

            speedFactor *= availableRate;

            result.WritableInputs[input.Key] = input.Value;
        }

        result.Efficiency = efficiency;

        speedFactor *= process.Rate;

        // Note that we don't consider storage constraints here so we don't use spaceConstraintModifier calculations
        foreach (var entry in process.Process.Outputs)
        {
            var amount = entry.Value;

            result.WritableOutputs[entry.Key] = amount * availableInEnvironment;

            if (amount <= 0)
                result.WritableLimitingCompounds.Add(entry.Key);
        }

        result.CurrentSpeed = speedFactor;

        return result;
    }

    /// <summary>
    ///   Computes the energy balance for the given organelles in biome
    /// </summary>
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
        BiomeConditions biome, MembraneType membrane, WorldGenerationSettings? worldSettings = null,
        bool isPlayer = false)
    {
        var organellesList = organelles.ToList();

        var maximumMovementDirection = MaximumSpeedDirection(organellesList);
        return ComputeEnergyBalance(organellesList, biome, membrane, maximumMovementDirection, worldSettings, isPlayer);
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
    /// <param name="worldSettings">The wprld generation settings for this game</param>
    /// <param name="isPlayer">Whether this microbe is the player cell</param>
    public static EnergyBalanceInfo ComputeEnergyBalance(IEnumerable<OrganelleTemplate> organelles,
        BiomeConditions biome, MembraneType membrane, Vector3 onlyMovementInDirection,
        WorldGenerationSettings? worldSettings, bool isPlayer = false)
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

                if (processData.WritableInputs.TryGetValue(Compound.ByName("atp"), out var amount))
                {
                    processATPConsumption += amount;

                    result.AddConsumption(organelle.Definition.InternalName, amount);
                }

                if (processData.WritableOutputs.TryGetValue(Compound.ByName("atp"), out amount))
                {
                    processATPProduction += amount;

                    result.AddProduction(organelle.Definition.InternalName, amount);
                }
            }

            // Take special cell components that take energy into account
            if (organelle.Definition.HasComponentFactory<MovementComponentFactory>())
            {
                var amount = Constants.FLAGELLA_ENERGY_COST;

                var organelleDirection = GetOrganelleDirection(organelle);
                if (organelleDirection.Dot(onlyMovementInDirection) > 0)
                {
                    movementATPConsumption += amount;
                    result.Flagella += amount;
                    result.AddConsumption(organelle.Definition.InternalName, amount);
                }
            }

            if (organelle.Definition.HasComponentFactory<CiliaComponentFactory>())
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
        result.BaseMovement = MovementCost(organelles.Select(x => x.Definition), membrane);
        result.AddConsumption("baseMovement", result.BaseMovement);
        var totalMovementConsumption = movementATPConsumption + result.BaseMovement;

        // Add osmoregulation
        result.Osmoregulation = OsmoregulationCost(organelles.Select(x => x.Definition), membrane);

        if (isPlayer && worldSettings != null)
        {
            result.Osmoregulation *= (float)worldSettings.OsmoregulationMultiplier;
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
                result[compound] = new CompoundBalance(0.0f, 0.0f);
            }
        }

        var compoundBag = new CompoundBag(organelles.Sum(x => x.Storage()));

        compoundBag.AddCompound(Compound.ByName("glucose"), compoundBag.Capacity / 2);
        compoundBag.AddCompound(Compound.ByName("iron"), compoundBag.Capacity / 2);
        compoundBag.AddCompound(Compound.ByName("hydrogensulfide"), compoundBag.Capacity / 2);

        var tweekedProcesses = SlicedProcesses(compoundBag, organelles, biome, compoundBag.Capacity);

        foreach (var process in tweekedProcesses)
        {
            foreach (var input in process.Process.Inputs)
            {
                result[input.Key].AddConsumption("all", input.Value * process.Rate);
            }

            foreach (var output in process.Process.Outputs)
            {
                result[output.Key].AddProduction("all", output.Value * process.Rate);
            }
        }

        return result;
    }

    public static Dictionary<Compound, CompoundBalance> ComputeCompoundBalance(
        IEnumerable<OrganelleTemplate> organelles, BiomeConditions biome)
    {
        return ComputeCompoundBalance(organelles.Select(o => o.Definition), biome);
    }

    public static IEnumerable<TweakedProcess> SlicedProcesses(CompoundBag compoundBag, IEnumerable<OrganelleDefinition> organelles, BiomeConditions biome, float totalCost)
    {
        var samplingCount = 10f;

        // Pretend we have one second of osmoregulation less so we report the processes that must have happened
        var modifiedCompoundBag = new CompoundBag(compoundBag);

        var allProcesses = new List<TweakedProcess>();
        for (var iteration = 0; iteration < samplingCount; iteration++)
        {
            if (modifiedCompoundBag.Compounds.ContainsKey(Compound.ByName("atp")))
            {
                // Not using the normal method here in order to allow negative values
                modifiedCompoundBag.Compounds[Compound.ByName("atp")] -= totalCost / samplingCount;
            }

            foreach (var tweakedProcess in organelles.Select(o => o.RunnableProcesses).SelectMany(i => i))
            {
                var proc = EnvironmentModifiedProcess(1 / samplingCount, biome, tweakedProcess.Process, modifiedCompoundBag, tweakedProcess, null);

                // Consume inputs
                foreach (var entry in proc.Process.Inputs)
                {
                    if (entry.Key.IsEnvironmental)
                        continue;

                    var inputRemoved = entry.Value * proc.Rate;

                    // This should always succeed (due to the earlier check) so it is always assumed here that this succeeded
                    modifiedCompoundBag.TakeCompound(entry.Key, inputRemoved);
                }

                // Add outputs
                foreach (var entry in proc.Process.Outputs)
                {
                    if (entry.Key.IsEnvironmental)
                        continue;

                    var outputGenerated = entry.Value * proc.Rate;

                    modifiedCompoundBag.AddCompound(entry.Key, outputGenerated);
                }

                allProcesses.Add(proc);
            }
        }

        var temp = allProcesses.GroupBy(process => process.Process.Name).Select(group => group.First());

        return temp;
    }

    public static TweakedProcess EnvironmentModifiedProcess(float delta, BiomeConditions biome, BioProcess processData, CompoundBag bag, TweakedProcess origonalProcess,
        SingleProcessStatistics? currentProcessStatistics)
    {
        // Can your cell do the process
        bool canDoProcess = true;

        // Throttle based on compounds in the environment
        float environmentModifier = EnvironmentalAvailabilityThrottleFactor(processData, biome!, currentProcessStatistics);

        if (environmentModifier <= MathUtils.EPSILON)
            canDoProcess = false;

        // Throttle based on compounds in the microbe
        float availableInputsModifier = InputAvailabilityThrottleFactor(processData, biome!, currentProcessStatistics, bag, origonalProcess, environmentModifier);

        // Throttle based on available space
        float spaceConstraintModifier = OutputSpaceThrottleFactor(processData, biome!, currentProcessStatistics, bag, origonalProcess, environmentModifier, delta);

        // Only carry out this process if you have all the required ingredients and enough space for the outputs
        if (!canDoProcess)
        {
            if (currentProcessStatistics != null)
                currentProcessStatistics.CurrentSpeed = 0;
            return new TweakedProcess(origonalProcess.Process, 0.0f);
        }

        float totalModifier = origonalProcess.Rate * environmentModifier * Math.Min(availableInputsModifier, spaceConstraintModifier);

        if (currentProcessStatistics != null)
            currentProcessStatistics.CurrentSpeed = totalModifier;

        totalModifier *= delta;

        return new TweakedProcess(origonalProcess.Process, totalModifier);
    }

    public static float EnvironmentalAvailabilityThrottleFactor(BioProcess processData, BiomeConditions biome, SingleProcessStatistics? currentProcessStatistics)
    {
        float environmentModifier = 1.0f;
        foreach (var entry in processData.Inputs)
        {
            if (!entry.Key.IsEnvironmental)
                continue;

            currentProcessStatistics?.AddInputAmount(entry.Key, biome.GetDissolvedInBiome(entry.Key));

            // Multiply envornment modifier by needed compound amounts, which compounds between different compounds
            environmentModifier *= biome.GetDissolvedInBiome(entry.Key) / entry.Value;

            if (environmentModifier <= MathUtils.EPSILON)
                currentProcessStatistics?.AddLimitingFactor(entry.Key);
        }

        return environmentModifier;
    }

    public static float InputAvailabilityThrottleFactor(BioProcess processData, BiomeConditions biome, SingleProcessStatistics? currentProcessStatistics, CompoundBag bag, TweakedProcess process, float environmentModifier)
    {
        float availableInputsModifier = 1.0f;
        foreach (var entry in processData.Inputs)
        {
            if (entry.Key.IsEnvironmental)
                continue;

            var inputRemoved = entry.Value * process.Rate * environmentModifier;

            // We don't multiply by delta here because we report the per-second values anyway. In the actual process
            // output numbers (computed after testing the speed), we need to multiply by inverse delta
            currentProcessStatistics?.AddInputAmount(entry.Key, inputRemoved);

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
                        availableInputsModifier = Math.Min(neededModifier, availableInputsModifier);
                        // Due to rounding errors there can be very small disparity here between the amount available
                        // and what we will take with the modifiers. See the comment in outputs for more details
                    }
                    else
                    {
                        canRun = false;
                    }
                }
                else
                {
                    canRun = false;
                }

                if (!canRun)
                {
                    availableInputsModifier = 0.0f;
                    currentProcessStatistics?.AddLimitingFactor(entry.Key);
                }
            }
        }

        return availableInputsModifier;
    }

    public static float OutputSpaceThrottleFactor(BioProcess processData, BiomeConditions biome, SingleProcessStatistics? currentProcessStatistics, CompoundBag bag, TweakedProcess process, float environmentModifier, float delta)
    {
        float spaceConstraintModifier = 1.0f;
        foreach (var entry in processData.Outputs)
        {
            var outputAdded = entry.Value * process.Rate * environmentModifier;

            currentProcessStatistics?.AddOutputAmount(entry.Key, outputAdded);

            outputAdded = outputAdded * spaceConstraintModifier * delta;

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
                    spaceConstraintModifier = 0.0f;
                    currentProcessStatistics?.AddCapacityProblem(entry.Key);
                }
            }
        }

        return spaceConstraintModifier;
    }

    private static float MovementForce(float movementForce, float directionFactor)
    {
        if (directionFactor < 0)
            return 0;

        return movementForce * directionFactor;
    }

    /// <summary>
    ///  Symmetric flagella are a corner case for speed calculations because the sum of all
    ///  directions is kind of broken in their case, so we have to choose which one of the symmetric flagella
    ///  we must discard from the direction calculation
    ///  Here we only discard if the flagella we input is the "bad" one
    /// </summary>
    private static Vector3 ChooseFromSymmetricFlagella(IEnumerable<OrganelleTemplate> organelles,
        OrganelleTemplate testedOrganelle, Vector3 maximumMovementDirection)
    {
        foreach (var organelle in organelles)
        {
            if (organelle != testedOrganelle &&
                organelle.Position + testedOrganelle.Position == new Hex(0, 0))
            {
                var organelleLength = (maximumMovementDirection - GetOrganelleDirection(organelle)).Length();
                var testedOrganelleLength = (maximumMovementDirection -
                    GetOrganelleDirection(testedOrganelle)).Length();

                if (organelleLength > testedOrganelleLength)
                    return maximumMovementDirection;

                return maximumMovementDirection - GetOrganelleDirection(testedOrganelle);
            }
        }

        return maximumMovementDirection;
    }
}
