
public class MicrobeAIFunctions
{
    /// <summary>
    ///   Tells if a compound is vital to this microbe.
    ///   Vital compounds are *direct* ATP producers
    ///   TODO: what is used here is a shortcut linked to the current game state:
    ///     such compounds could be used for other processes in future versions
    /// </summary>
    public static bool IsVitalCompound(Microbe microbe, Compound compound)
    {
        return microbe.Compounds.IsUseful(compound) &&
            (compound == Compound.ByName("glucose") || compound == Compound.ByName("iron"));
    }

    public static bool CanTryToEatMicrobe(Microbe microbe, Microbe targetMicrobe)
    {
        return targetMicrobe.Species != microbe.Species 
            && (WouldTryToToxinHuntBiggerPrey(microbe.Species.Behaviour.Opportunism) && CanShootToxin(microbe)
            || CouldEngulf(microbe.EngulfSize, targetMicrobe.EngulfSize));
    }

    public static bool WouldTryToToxinHuntBiggerPrey(float opportunism)
    {
        return opportunism / Constants.MAX_SPECIES_OPPORTUNISM > 0.3f;
    }

    public static bool CouldEngulf(float predatorSize, float preySize)
    {
        return predatorSize / preySize >= Constants.ENGULF_SIZE_RATIO_REQ;
    }

    public static bool CanShootToxin(Microbe microbe)
    {
        return microbe.Compounds.GetCompoundAmount(Compound.ByName("oxytoxy")) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT +
            Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * NormalizedFocus(microbe)
            ||
            microbe.Compounds.GetCompoundAmount(Compound.ByName("glycotoxy")) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT +
            Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * NormalizedFocus(microbe);
    }

    public static bool IsAfraidOf(Microbe microbe, Microbe otherMicrobe)
    {
        var fleeThreshold = 3.0f - (2 * NormalizedFear(microbe) *
            (10 - (9 * microbe.Hitpoints / microbe.MaxHitpoints)));

        return otherMicrobe.Species != microbe.Species
                && !otherMicrobe.Dead
                && otherMicrobe.EngulfSize > microbe.EngulfSize * fleeThreshold;
    }

    private static float NormalizedFocus(Microbe microbe)
    {
        return microbe.Species.Behaviour.Focus / Constants.MAX_SPECIES_FOCUS;
    }

    private static float NormalizedOpportunism(Microbe microbe)
    {
        return microbe.Species.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
    }

    private static float NormalizedFear(Microbe microbe)
    {
        return microbe.Species.Behaviour.Fear / Constants.MAX_SPECIES_FEAR;
    }
}