using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   AI for a single Microbe. This is a separate class to contain all the AI status variables as well as make the
///   Microbe.cs file cleaner as this AI has a lot of code.
/// </summary>
/// <remarks>
///   <para>
///     This is run in a background thread so no state changing or scene spawning methods on Microbe may be called.
///   </para>
///   <para>
///     TODO: this should be updated to have special handling for cell colonies
///   </para>
/// </remarks>
public class MicrobeAI
{
    private readonly Compound atp;
    private readonly Compound oxytoxy;
    private readonly Compound glycotoxy;
    private readonly Compound ammonia;
    private readonly Compound phosphates;

    [JsonProperty]
    private Microbe microbe;

    [JsonProperty]
    private float previousAngle;

    [JsonProperty]
    private Vector3 targetPosition = new(0, 0, 0);

    [JsonIgnore]
    private EntityReference<Microbe> focusedPrey = new();

    [JsonIgnore]
    private Vector3? lastSmelledCompoundPosition;

    [JsonProperty]
    private float pursuitThreshold;

    /// <summary>
    ///   A value between 0.0f and 1.0f, this is the portion of the microbe's atp bar that needs to refill
    ///   before resuming motion.
    /// </summary>
    [JsonProperty]
    private float atpThreshold;

    /// <summary>
    ///   Stores the value of microbe.totalAbsorbedCompound at tick t-1 before it is cleared and updated at tick t.
    ///   Used for compounds gradient computation.
    /// </summary>
    /// <remarks>
    ///   Memory of the previous absorption step is required to compute gradient (which is a variation).
    ///   Values dictionary rather than single value as they will be combined with variable weights.
    /// </remarks>
    [JsonProperty]
    private Dictionary<Compound, float> previouslyAbsorbedCompounds;

    [JsonIgnore]
    private Dictionary<Compound, float> compoundsSearchWeights;

    [JsonIgnore]
    private float timeSinceSignalSniffing;

    [JsonIgnore]
    private EntityReference<Microbe> lastFoundSignalEmitter = new();

    [JsonIgnore]
    private MicrobeSignalCommand receivedCommand = MicrobeSignalCommand.None;

    [JsonProperty]
    private bool hasBeenNearPlayer;

    [JsonProperty]
    private Vector3? playerPositionAtSpawn = null;

    public MicrobeAI(Microbe microbe)
    {
        this.microbe = microbe ?? throw new ArgumentException("no microbe given", nameof(microbe));

        atp = SimulationParameters.Instance.GetCompound("atp");
        oxytoxy = SimulationParameters.Instance.GetCompound("oxytoxy");
        glycotoxy = SimulationParameters.Instance.GetCompound("glycotoxy");
        ammonia = SimulationParameters.Instance.GetCompound("ammonia");
        phosphates = SimulationParameters.Instance.GetCompound("phosphates");

        previouslyAbsorbedCompounds = new Dictionary<Compound, float>(microbe.TotalAbsorbedCompounds);
        compoundsSearchWeights = new Dictionary<Compound, float>();
    }

    private float SpeciesAggression => microbe.Species.Behaviour.Aggression *
        (receivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.5f : 1.0f);

    private float SpeciesFear => microbe.Species.Behaviour.Fear *
        (receivedCommand == MicrobeSignalCommand.BecomeAggressive ? 0.75f : 1.0f);

    private float SpeciesActivity => microbe.Species.Behaviour.Activity *
        (receivedCommand == MicrobeSignalCommand.BecomeAggressive ? 1.25f : 1.0f);

    private float SpeciesFocus => microbe.Species.Behaviour.Focus;
    private float SpeciesOpportunism => microbe.Species.Behaviour.Opportunism;

    public MicrobeAIResponse? Think(float delta, Random random, MicrobeAICommonData data)
    {
        // Disable most AI in a colony
        if (microbe.ColonyParent != null)
            return null;

        MicrobeAIResponse retval = new MicrobeAIResponse();

        if (microbe.Colony != null)
        {
            foreach (var child in microbe.ColonyChildren)
            {
                retval.DroneResponses.AddRange(DroneAIResponses(child, delta, random, data));
            }
        }

        // Don't go further as player
        if (microbe.IsPlayerMicrobe)
        {
            if (retval.DroneResponses.Count() > 0)
            {
                return retval;
            }
            else
            {
                return null;
            }
        }

        // For now don't think if immobile
        if (microbe is MicrobeSpecies && MicrobeInternalCalculations.CalculateSpeed(((MicrobeSpecies)microbe.Species).Organelles, microbe.Membrane.Type, ((MicrobeSpecies)microbe.Species).MembraneRigidity) <= 0.0f)
            return null;

        timeSinceSignalSniffing += delta;

        if (timeSinceSignalSniffing > Constants.MICROBE_AI_SIGNAL_REACT_INTERVAL)
        {
            timeSinceSignalSniffing = 0;

            if (microbe.HasSignalingAgent)
                DetectSignalingAgents(data.AllMicrobes.Where(m => m.Species == microbe.Species));
        }

        var signaler = lastFoundSignalEmitter.Value;

        if (signaler != null)
        {
            receivedCommand = signaler.SignalCommand;
        }

        ChooseActions(random, data, signaler, retval);

        // Store the absorbed compounds for run and rumble
        previouslyAbsorbedCompounds.Clear();
        foreach (var compound in microbe.TotalAbsorbedCompounds)
        {
            previouslyAbsorbedCompounds[compound.Key] = compound.Value;
        }

        // We clear here for update, this is why we stored above!
        microbe.TotalAbsorbedCompounds.Clear();

        return retval;
    }

    public List<DroneAIResponse> DroneAIResponses(Microbe root, float delta, Random random, MicrobeAICommonData data)
    {
        var retval = new List<DroneAIResponse>();

        retval.Add(root.DroneAIThink(delta, random, data));

        if (root.ColonyChildren != null)
        {
            foreach (var child in root.ColonyChildren)
            {
                retval.AddRange(DroneAIResponses(child, delta, random, data));
            }
        }

        return retval;
    }

    /// <summary>
    ///   Resets AI status when this AI controlled microbe is removed from a colony
    /// </summary>
    public void ResetAI()
    {
        previousAngle = 0;
        targetPosition = Vector3.Zero;
        focusedPrey.Value = null;
        pursuitThreshold = 0;
        microbe.MovementDirection = Vector3.Zero;
        microbe.TotalAbsorbedCompounds.Clear();
    }

    private void ChooseActions(Random random, MicrobeAICommonData data, Microbe? signaler, MicrobeAIResponse response)
    {
        if (microbe.IsBeingEngulfed)
        {
            MoveFullSpeed(response);
        }

        // If nothing is engulfing me right now, see if there's something that might want to hunt me
        // TODO: https://github.com/Revolutionary-Games/Thrive/issues/2323
        Vector3? predator = GetNearestPredatorItem(data.AllMicrobes)?.GlobalTransform.origin;
        if (predator.HasValue &&
            DistanceFromMe(predator.Value) < (1500.0 * SpeciesFear / Constants.MAX_SPECIES_FEAR))
        {
            FleeFromPredators(random, predator.Value, response);
            return;
        }

        // If this microbe is out of ATP, pick an amount of time to rest
        if (microbe.Compounds.GetCompoundAmount(atp) < 1.0f)
        {
            // Keep the maximum at 95% full, as there is flickering when near full
            atpThreshold = 0.95f * SpeciesFocus / Constants.MAX_SPECIES_FOCUS;
        }

        if (atpThreshold > 0.0f)
        {
            if (microbe.Compounds.GetCompoundAmount(atp) < microbe.Compounds.Capacity * atpThreshold
                && microbe.Compounds.Where(compound => MicrobeAIFunctions.IsVitalCompound(microbe, compound.Key) && compound.Value > 0.0f)
                    .Count() > 0)
            {
                Stop(response);
                return;
            }

            atpThreshold = 0.0f;
        }

        // Follow received commands if we have them
        // TODO: tweak the balance between following commands and doing normal behaviours
        // TODO: and also probably we want to add some randomness to the positions and speeds based on distance
        switch (receivedCommand)
        {
            case MicrobeSignalCommand.MoveToMe:
                if (signaler != null)
                {
                    MoveToLocation(signaler.Translation, response);
                    return;
                }

                break;
            case MicrobeSignalCommand.FollowMe:
                if (signaler != null && DistanceFromMe(signaler.Translation) > Constants.AI_FOLLOW_DISTANCE_SQUARED)
                {
                    MoveToLocation(signaler.Translation, response);
                    return;
                }

                break;
            case MicrobeSignalCommand.FleeFromMe:
                if (signaler != null && DistanceFromMe(signaler.Translation) < Constants.AI_FLEE_DISTANCE_SQUARED)
                {
                    response.State = Microbe.MicrobeState.Normal;
                    MoveFullSpeed(response);

                    // Direction is calculated to be the opposite from where we should flee
                    targetPosition = microbe.Translation + (microbe.Translation - signaler.Translation);
                    response.LookTarget = targetPosition;
                    MoveFullSpeed(response);
                    return;
                }

                break;
        }

        // If I'm very far from the player, and I have not been near the player yet, get on stage
        if (!hasBeenNearPlayer)
        {
            var player = data.AllMicrobes.Where(otherMicrobe => !otherMicrobe.Dead && otherMicrobe.IsPlayerMicrobe).FirstOrDefault();
            if (player != null)
            {
                if (playerPositionAtSpawn == null)
                {
                    playerPositionAtSpawn = player.GlobalTransform.origin;
                }

                if (DistanceFromMe((Vector3)playerPositionAtSpawn) > Math.Pow(Constants.SPAWN_SECTOR_SIZE, 2) * 0.75f)
                {
                    MoveToLocation((Vector3)playerPositionAtSpawn, response);
                    return;
                }
                else
                {
                    hasBeenNearPlayer = true;
                }
            }
        }

        // If there are no threats, look for a chunk to eat
        if (!microbe.CellTypeProperties.MembraneType.CellWall)
        {
            Vector3? targetChunk = GetNearestChunkItem(data.AllChunks, data.AllMicrobes, random)?.Translation;
            if (targetChunk.HasValue)
            {
                PursueAndConsumeChunks(targetChunk.Value, random, response);
                return;
            }
        }

        // If there are no chunks, look for living prey to hunt
        var possiblePrey = GetNearestPreyItem(data.AllMicrobes);
        if (possiblePrey != null)
        {
            
            Vector3? prey = possiblePrey.GlobalTransform.origin;

            EngagePrey(possiblePrey, random, response);
            return;
        }

        // There is no reason to be engulfing at this stage
        response.State = Microbe.MicrobeState.Normal;

        // Otherwise just wander around and look for compounds
        if (SpeciesActivity > Constants.MAX_SPECIES_ACTIVITY / 10)
        {
            SeekCompounds(random, data, response);
        }
        else
        {
            // This organism is sessile, and will not act until the environment changes
            Stop(response);
        }
    }

    private FloatingChunk? GetNearestChunkItem(List<FloatingChunk> allChunks, List<Microbe> allMicrobes, Random random)
    {
        FloatingChunk? chosenChunk = null;

        // If the microbe cannot absorb, no need for this
        if (microbe.Membrane.Type.CellWall)
        {
            return null;
        }

        // Retrieve nearest potential chunk
        foreach (var chunk in allChunks)
        {
            if (chunk.ContainedCompounds == null)
                continue;

            if (microbe.EngulfSize > chunk.Size * Constants.ENGULF_SIZE_RATIO_REQ
                && (chunk.Translation - microbe.Translation).LengthSquared()
                <= (20000.0 * SpeciesFocus / Constants.MAX_SPECIES_FOCUS) + 1500.0)
            {
                if (chunk.ContainedCompounds.Compounds.Any(x => microbe.Compounds.IsUseful(x.Key)))
                {
                    if (chosenChunk == null ||
                        (chosenChunk.Translation - microbe.Translation).LengthSquared() >
                        (chunk.Translation - microbe.Translation).LengthSquared())
                    {
                        chosenChunk = chunk;
                    }
                }
            }
        }

        // Don't bother with chunks when there's a lot of microbes to compete with
        if (chosenChunk != null)
        {
            var rivals = 0;
            var distanceToChunk = (microbe.Translation - chosenChunk.Translation).LengthSquared();
            foreach (var rival in allMicrobes)
            {
                if (rival != microbe)
                {
                    var rivalDistance = (rival.GlobalTransform.origin - chosenChunk.Translation).LengthSquared();
                    if (rivalDistance < 500.0f &&
                        rivalDistance < distanceToChunk)
                    {
                        rivals++;
                    }
                }
            }

            int rivalThreshold;
            if (SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM / 3)
            {
                rivalThreshold = 1;
            }
            else if (SpeciesOpportunism < Constants.MAX_SPECIES_OPPORTUNISM * 2 / 3)
            {
                rivalThreshold = 3;
            }
            else
            {
                rivalThreshold = 5;
            }

            // In rare instances, microbes will choose to be much more ambitious
            if (RollCheck(SpeciesFocus, Constants.MAX_SPECIES_FOCUS, random))
            {
                rivalThreshold *= 2;
            }

            if (rivals > rivalThreshold)
            {
                chosenChunk = null;
            }
        }

        return chosenChunk;
    }

    /// <summary>
    ///   Gets the nearest prey item. And builds the prey list
    /// </summary>
    /// <returns>The nearest prey item.</returns>
    /// <param name="allMicrobes">All microbes.</param>
    private Microbe? GetNearestPreyItem(List<Microbe> allMicrobes)
    {
        var focused = focusedPrey.Value;
        if (focused != null)
        {
            var distanceToFocusedPrey = DistanceFromMe(focused.GlobalTransform.origin);
            if (!focused.Dead && distanceToFocusedPrey <
                (3500.0f * SpeciesFocus / Constants.MAX_SPECIES_FOCUS))
            {
                if (distanceToFocusedPrey < pursuitThreshold)
                {
                    // Keep chasing, but expect to keep getting closer
                    LowerPursuitThreshold();
                    return focused;
                }

                // If prey hasn't gotten closer by now, it's probably too fast, or juking you
                // Remember who focused prey is, so that you don't fall for this again
                return null;
            }

            focusedPrey.Value = null;
        }

        Microbe? chosenPrey = null;

        foreach (var otherMicrobe in allMicrobes)
        {
            if (!otherMicrobe.Dead)
            {
                if (DistanceFromMe(otherMicrobe.GlobalTransform.origin) <
                    (2500.0f * SpeciesAggression / Constants.MAX_SPECIES_AGGRESSION)
                    && MicrobeAIFunctions.CanTryToEatMicrobe(microbe, otherMicrobe))
                {
                    if (chosenPrey == null ||
                        (chosenPrey.GlobalTransform.origin - microbe.Translation).LengthSquared() >
                        (otherMicrobe.GlobalTransform.origin - microbe.Translation).LengthSquared())
                    {
                        chosenPrey = otherMicrobe;
                    }
                }
            }
        }

        focusedPrey.Value = chosenPrey;

        if (chosenPrey != null)
        {
            pursuitThreshold = DistanceFromMe(chosenPrey.GlobalTransform.origin) * 3.0f;
        }

        return chosenPrey;
    }

    /// <summary>
    ///   Building the predator list and setting the scariest one to be predator
    /// </summary>
    /// <param name="allMicrobes">All microbes.</param>
    private Microbe? GetNearestPredatorItem(List<Microbe> allMicrobes)
    {
        Microbe? predator = null;
        foreach (var otherMicrobe in allMicrobes)
        {
            if (otherMicrobe == microbe)
                continue;

            // Based on species fear, threshold to be afraid ranges from 0.8 to 1.8 microbe size.
            if (MicrobeAIFunctions.IsAfraidOf(microbe, otherMicrobe))
            {
                if (predator == null || DistanceFromMe(predator.GlobalTransform.origin) >
                    DistanceFromMe(otherMicrobe.GlobalTransform.origin))
                {
                    predator = otherMicrobe;
                }
            }
        }

        return predator;
    }

    private void PursueAndConsumeChunks(Vector3 chunk, Random random, MicrobeAIResponse response)
    {
        // This is a slight offset of where the chunk is, to avoid a forward-facing part blocking it
        targetPosition = chunk + new Vector3(0.5f, 0.0f, 0.5f);
        response.LookTarget = targetPosition;
        SetEngulfIfClose(response);

        // Just in case something is obstructing chunk engulfing, wiggle a little sometimes
        if (random.NextDouble() < 0.05)
        {
            MoveWithRandomTurn(0.1f, 0.2f, random, response);
        }

        // If this Microbe is right on top of the chunk, stop instead of spinning
        if (DistanceFromMe(chunk) < Constants.AI_ENGULF_STOP_DISTANCE)
        {
            Stop(response);
        }
        else
        {
            MoveFullSpeed(response);
        }
    }

    private void FleeFromPredators(Random random, Vector3 predatorLocation, MicrobeAIResponse response)
    {
        response.State = Microbe.MicrobeState.Normal;

        targetPosition = (2 * (microbe.Translation - predatorLocation)) + microbe.Translation;

        response.LookTarget = targetPosition;

        // If the predator is right on top of the microbe, there's a chance to try and swing with a pilus.
        if (DistanceFromMe(predatorLocation) < 100.0f &&
            RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
        {
            MoveWithRandomTurn(2.5f, 3.0f, random, response);
        }

        // If prey is confident enough, it will try and launch toxin at the predator
        if (SpeciesAggression > SpeciesFear &&
            DistanceFromMe(predatorLocation) >
            300.0f - (5.0f * SpeciesAggression) + (6.0f * SpeciesFear) &&
            RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION, random))
        {
            TryToLaunchToxin(predatorLocation, response);
        }

        // No matter what, I want to make sure I'm moving
        MoveFullSpeed(response);
    }

    private void EngagePrey(Microbe target, Random random,  MicrobeAIResponse response)
    {
        bool engulf = microbe.CanEngulf(target) &&
                DistanceFromMe(target.GlobalTransform.origin) < 10.0f * microbe.EngulfSize;

        response.State = engulf ? Microbe.MicrobeState.Engulf : Microbe.MicrobeState.Normal;
        targetPosition = target.GlobalTransform.origin;
        response.LookTarget = targetPosition;
        if (MicrobeAIFunctions.CanShootToxin(microbe))
        {
            TryToLaunchToxin(targetPosition, response);

            if (RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION / 5, random))
            {
                MoveFullSpeed(response);
            }
        }
        else
        {
            MoveFullSpeed(response);

            if (engulf)
            {
                // Just in case something is obstructing prey engulfing, wiggle a little sometimes
                if (random.NextDouble() < 0.05)
                {
                    MoveWithRandomTurn(0.1f, 0.2f, random, response);
                }
            }
            else if (!microbe.CanEngulf(target))
            {
                // try to slash with a pilus
                if (MicrobeAIFunctions.HasPilus(microbe) && 
                    DistanceFromMe(targetPosition) < 8.0f * microbe.EngulfSize && RollCheck(SpeciesAggression, Constants.MAX_SPECIES_AGGRESSION * 2, random))
                {
                    MoveWithRandomTurn(0.05f, 0.15f, random, response);
                }
            }
        }
    }

    private void SeekCompounds(Random random, MicrobeAICommonData data, MicrobeAIResponse response)
    {
        // More active species just try to get distance to avoid over-clustering
        if (RollCheck(SpeciesActivity, Constants.MAX_SPECIES_ACTIVITY + (Constants.MAX_SPECIES_ACTIVITY / 2), random))
        {
            MoveFullSpeed(response);
            return;
        }

        if (random.Next(Constants.AI_STEPS_PER_SMELL) == 0)
        {
            SmellForCompounds(data);
        }

        // If the AI has smelled a compound (currently only possible with a chemoreceptor), go towards it.
        if (lastSmelledCompoundPosition != null)
        {
            var distance = DistanceFromMe(lastSmelledCompoundPosition.Value);

            // If the compound isn't getting closer, either something else has taken it, or we're stuck
            LowerPursuitThreshold();
            if (distance > pursuitThreshold)
            {
                lastSmelledCompoundPosition = null;
                RunAndTumble(random, response);
                return;
            }

            if (distance > 3.0f)
            {
                targetPosition = lastSmelledCompoundPosition.Value;
                response.LookTarget = targetPosition;
            }
            else
            {
                Stop(response);
                SmellForCompounds(data);
            }
        }
        else
        {
            RunAndTumble(random, response);
        }
    }

    private void SmellForCompounds(MicrobeAICommonData data)
    {
        ComputeCompoundsSearchWeights();

        var detections = microbe.GetDetectedCompounds(data.Clouds)
            .OrderBy(detection => compoundsSearchWeights.ContainsKey(detection.Compound) ?
                compoundsSearchWeights[detection.Compound] :
                0).ToList();

        if (detections.Count > 0)
        {
            lastSmelledCompoundPosition = detections[0].Target;
            pursuitThreshold = DistanceFromMe(lastSmelledCompoundPosition.Value)
                * (1 + (SpeciesFocus / Constants.MAX_SPECIES_FOCUS));
        }
        else
        {
            lastSmelledCompoundPosition = null;
        }
    }

    // For doing run and tumble
    /// <summary>
    ///   For doing run and tumble
    /// </summary>
    /// <param name="random">Random values to use</param>
    private void RunAndTumble(Random random, MicrobeAIResponse response)
    {
        // If this microbe is currently stationary, just initialize by moving in a random direction.
        // Used to get newly spawned microbes to move.
        if (microbe.MovementDirection.Length() == 0)
        {
            MoveWithRandomTurn(0, Mathf.Pi, random, response);
            return;
        }

        // Run and tumble
        // A biased random walk, they turn more if they are picking up less compounds.
        // The scientifically accurate algorithm has been flipped to account for the compound
        // deposits being a lot smaller compared to the microbes
        // https://www.mit.edu/~kardar/teaching/projects/chemotaxis(AndreaSchmidt)/home.htm

        ComputeCompoundsSearchWeights();

        float gradientValue = 0.0f;
        foreach (var compoundWeight in compoundsSearchWeights)
        {
            // Note this is about absorbed quantities (which is all microbe has access to) not the ones in the clouds.
            // Gradient computation is therefore cell-centered, and might be different for different cells.
            float compoundDifference = 0.0f;

            microbe.TotalAbsorbedCompounds.TryGetValue(compoundWeight.Key, out float quantityAbsorbedThisStep);
            previouslyAbsorbedCompounds.TryGetValue(compoundWeight.Key, out float quantityAbsorbedPreviousStep);

            compoundDifference += quantityAbsorbedThisStep - quantityAbsorbedPreviousStep;

            compoundDifference *= compoundWeight.Value;
            gradientValue += compoundDifference;
        }

        // Implement a detection threshold to possibly rule out too tiny variations
        // TODO: possibly include cell capacity correction
        float differenceDetectionThreshold = Constants.AI_GRADIENT_DETECTION_THRESHOLD;

        // If food density is going down, back up and see if there's some more
        if (gradientValue < -differenceDetectionThreshold && random.Next(0, 10) < 9)
        {
            MoveWithRandomTurn(2.5f, 3.0f, random, response);
        }

        // If there isn't any food here, it's a good idea to keep moving
        if (Math.Abs(gradientValue) <= differenceDetectionThreshold && random.Next(0, 10) < 5)
        {
            MoveWithRandomTurn(0.0f, 0.4f, random, response);
        }

        // If positive last step you gained compounds, so let's move toward the source
        if (gradientValue > differenceDetectionThreshold)
        {
            // There's a decent chance to turn by 90° to explore gradient
            // 180° is useless since previous position let you absorb less compounds already
            if (random.Next(0, 10) < 4)
            {
                MoveWithRandomTurn(0.0f, 1.5f, random, response);
            }
        }
    }

    /// <summary>
    ///   Prioritizing compounds that are stored in lesser quantities.
    ///   If ATP-producing compounds are low (less than half storage capacities),
    ///   non ATP-related compounds are discarded.
    ///   Updates compoundsSearchWeights instance dictionary.
    /// </summary>
    private void ComputeCompoundsSearchWeights()
    {
        IEnumerable<Compound> usefulCompounds = microbe.Compounds.Compounds.Keys;

        // If this microbe lacks vital compounds don't bother with ammonia and phosphate
        if (usefulCompounds.Any(
                compound => MicrobeAIFunctions.IsVitalCompound(microbe, compound) &&
                    microbe.Compounds.GetCompoundAmount(compound) < 0.5f * microbe.Compounds.Capacity))
        {
            usefulCompounds = usefulCompounds.Where(x => x != ammonia && x != phosphates);
        }

        compoundsSearchWeights.Clear();
        foreach (var compound in usefulCompounds)
        {
            // The priority of a compound is inversely proportional to its availability
            // Should be tweaked with consumption
            var compoundPriority = 1 - microbe.Compounds.GetCompoundAmount(compound) / microbe.Compounds.Capacity;

            compoundsSearchWeights.Add(compound, compoundPriority);
        }
    }

    private void SetEngulfIfClose(MicrobeAIResponse response)
    {
        // Turn on engulf mode if close
        // Sometimes "close" is hard to discern since microbes can range from straight lines to circles
        if ((microbe.Translation - targetPosition).LengthSquared() <= microbe.EngulfSize * 2.0f)
        {
            response.State = Microbe.MicrobeState.Engulf;
        }
        else
        {
            response.State = Microbe.MicrobeState.Normal;
        }
    }

    private void TryToLaunchToxin(Vector3 target, MicrobeAIResponse response)
    {
        if (microbe.Hitpoints > 0 && microbe.AgentVacuoleCount > 0 &&
            (microbe.Translation - target).LengthSquared() <= SpeciesFocus + microbe.EngulfSize * 10.0f)
        {
            if (MicrobeAIFunctions.CanShootToxin(microbe))
            {
                response.ToxinShootTarget = target;
            }
        }
    }

    private void MoveWithRandomTurn(float minTurn, float maxTurn, Random random, MicrobeAIResponse response)
    {
        var turn = random.Next(minTurn, maxTurn);
        if (random.Next(2) == 1)
        {
            turn = -turn;
        }

        var randDist = random.Next(SpeciesActivity, Constants.MAX_SPECIES_ACTIVITY);
        targetPosition = microbe.Translation
            + new Vector3(Mathf.Cos(previousAngle + turn) * randDist,
                0,
                Mathf.Sin(previousAngle + turn) * randDist);
        previousAngle += turn;
        response.LookTarget = targetPosition;
        MoveFullSpeed(response);
    }

    private void MoveToLocation(Vector3 location, MicrobeAIResponse response)
    {
        response.State = Microbe.MicrobeState.Normal;
        targetPosition = location;
        response.LookTarget = targetPosition;
        MoveFullSpeed(response);
    }

    private void DetectSignalingAgents(IEnumerable<Microbe> ownSpeciesMicrobes)
    {
        // We kind of simulate how strong the "smell" of a signal is by finding the closest active signal
        float? closestSignalSquared = null;
        Microbe? selectedMicrobe = null;

        var previous = lastFoundSignalEmitter.Value;

        if (previous != null && previous.SignalCommand != MicrobeSignalCommand.None)
        {
            selectedMicrobe = previous;
            closestSignalSquared = DistanceFromMe(previous.Translation);
        }

        foreach (var speciesMicrobe in ownSpeciesMicrobes)
        {
            if (speciesMicrobe.SignalCommand == MicrobeSignalCommand.None)
                continue;

            // Don't detect your own signals
            if (speciesMicrobe == microbe)
                continue;

            var distance = DistanceFromMe(speciesMicrobe.Translation);

            if (closestSignalSquared == null || distance < closestSignalSquared.Value)
            {
                selectedMicrobe = speciesMicrobe;
                closestSignalSquared = distance;
            }
        }

        // TODO: should there be a max distance after which the signaling agent is considered to be so weak that it
        // is not detected?

        lastFoundSignalEmitter.Value = selectedMicrobe;
    }

    private void MoveFullSpeed(MicrobeAIResponse response)
    {
        response.MovementDirection = new Vector3(0, 0, -Constants.AI_BASE_MOVEMENT);
    }

    private void Stop(MicrobeAIResponse response)
    {
        response.MovementDirection = null;
    }

    private void LowerPursuitThreshold()
    {
        pursuitThreshold *= 0.95f;
    }

    private float DistanceFromMe(Vector3 target)
    {
        return (target - microbe.Translation).LengthSquared();
    }

    private bool RollCheck(float ourStat, float dc, Random random)
    {
        return random.Next(0.0f, dc) <= ourStat;
    }

    private bool RollReverseCheck(float ourStat, float dc, Random random)
    {
        return ourStat <= random.Next(0.0f, dc);
    }

    private void DebugFlash()
    {
        microbe.Flash(1.0f, new Color(255.0f, 0.0f, 0.0f));
    }
}
