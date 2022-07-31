using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class MicrobeAIFunctions
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
        var sizeRatio = microbe.EngulfSize / targetMicrobe.EngulfSize;

        return targetMicrobe.Species != microbe.Species 
            && ((NormalizedOpportunism(microbe) > 0.3f && MicrobeAIFunctions.CanShootToxin(microbe))
            || (sizeRatio >= Constants.ENGULF_SIZE_RATIO_REQ));
    }

    public static bool CanShootToxin(Microbe microbe)
    {
        return microbe.Compounds.GetCompoundAmount(Compound.ByName("oxytoxy")) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT +
            Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * NormalizedFocus(microbe)
            ||
            microbe.Compounds.GetCompoundAmount(Compound.ByName("glycotoxy")) >= Constants.MINIMUM_AGENT_EMISSION_AMOUNT +
            Constants.MAXIMUM_AGENT_EMISSION_AMOUNT * NormalizedFocus(microbe);
    }

    private static float NormalizedFocus(Microbe microbe)
    {
        return microbe.Species.Behaviour.Focus / Constants.MAX_SPECIES_FOCUS;
    }

    private static float NormalizedOpportunism(Microbe microbe)
    {
        return microbe.Species.Behaviour.Opportunism / Constants.MAX_SPECIES_OPPORTUNISM;
    }
}