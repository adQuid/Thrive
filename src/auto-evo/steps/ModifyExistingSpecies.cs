using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

public class ModifyExistingSpecies : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;

    public ModifyExistingSpecies(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public int TotalSteps => 1;

    public bool CanRunConcurrently => true;

    public bool RunStep(RunResults results)
    {
        foreach(var species in Patch.Miche.AllOccupants())
        {
            foreach(var traversal in Patch.Miche.TraversalsTerminatingInSpecies(species))
            {
                var partlist = new MutationLibrary(species);

                var pressures = traversal.Select(x => x.Pressure).ToList();

                pressures.AddRange(SpeciesDependentPressures(Patch, species));

                var variants = ViableVariants(results, species, Patch, partlist, new SimulationCache(), pressures);

                if (variants.Count() > 0)
                {
                    var speciesToAdd = variants.First();

                    // TODO: Be less hacky with this
                    if (speciesToAdd.FormattedName != species.FormattedName)
                    {

                        results.AncestorDictionary.Add(speciesToAdd, species);
                        results.MicheByPatch[Patch].InsertSpecies(variants.First());

                        results.MakeSureResultExistsForSpecies(speciesToAdd);
                        // Mark the best pressures for hovering over in game
                        results.results[speciesToAdd].AddBestPressuresResults(Patch, traversal.Select(x => x.Pressure).ToList());
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    ///   Returns a new list of all possible species that might emerge in response to the provided pressures, as well as a copy of the origonal species.
    /// </summary>
    /// <param name="results">RunResults needed to find latest copy of provided species. This object is NOT modified.</param>
    /// <param name="species"></param>
    /// <param name="patch"></param>
    /// <param name="mutationLibrary"></param>
    /// <param name="cache"></param>
    /// <param name="niche"></param>
    /// <returns>List of viable variants, and the provided species</returns>
    public static List<MicrobeSpecies> ViableVariants(RunResults results, 
        Species species, 
        Patch patch, 
        MutationLibrary mutationLibrary, 
        SimulationCache cache, 
        List<SelectionPressure>? niche)
    {
        var baseSpecies = (MicrobeSpecies)results.LastestVersionForSpecies(species);

        var selectionPressures = MicheFactory.PressuresFromPatch(species, patch, mutationLibrary, cache, niche);

        // find the initial scores
        var pressureScores = new Dictionary<SelectionPressure, float>();
        foreach (var curPressure in selectionPressures)
        {
            pressureScores[curPressure] = curPressure.Score(baseSpecies, cache);
        }

        var viableVariants = new List<MicrobeSpecies> { baseSpecies };

        var pressuresSoFar = new List<SelectionPressure>();
        foreach (var curPressure in selectionPressures)
        {
            pressuresSoFar.Add(curPressure);

            // For each viable variant, get a new variants that at least improve score a little bit
            var potentialVariants = viableVariants.Select(startVariant =>
                curPressure.MicrobeMutations.Select(mutationStrategy => mutationStrategy.MutationsOf(startVariant, mutationLibrary))
                .SelectMany(x => x)
                )
                .SelectMany(x => x).ToList();
            potentialVariants.AddRange(viableVariants);

            // Prune variants that hurt the previous scores too much
            viableVariants = PruneInviableSpecies(potentialVariants, pressuresSoFar, baseSpecies, cache);
        }

        foreach (var variant in viableVariants)
        {
            MutationLogicFunctions.NameNewMicrobeSpecies(variant, baseSpecies);
        }

        return viableVariants.OrderByDescending(x =>
        selectionPressures.Select(pressure => (pressure.Score(x, cache) / pressureScores[pressure]) * pressure.Strength).Sum() + (x == baseSpecies ? 0.01f : 0.0f))
            .ToList();
    }

    /// <summary>
    ///   Returns new list containing only species from the provided list that don't score too badly in the provided list of selection pressures.
    /// </summary>
    /// <param name="potentialVariants"></param>
    /// <param name="selectionPressures"></param>
    /// <param name="baseSpecies"></param>
    /// <param name="cache"></param>
    /// <returns>List of species not ruled to be inviable.</returns>
    public static List<MicrobeSpecies> PruneInviableSpecies(List<MicrobeSpecies> potentialVariants,
        List<SelectionPressure>? selectionPressures,
        Species baseSpecies,
        SimulationCache cache)
    {
        var viableVariants = new List<MicrobeSpecies>();
        foreach (var potentialVariant in potentialVariants)
        {
            var combinedScores = 0.0;
            foreach (var pastPressure in selectionPressures)
            {
                combinedScores += pastPressure.WeightedComparedScores(potentialVariant, baseSpecies, cache);
            }

            if (combinedScores >= 0)
            {
                // TODO: Move this somewhere better
                ((MicrobeSpecies)potentialVariant).Colour = new Color((float)new Random().NextDouble(), 0.5f, 0.5f);

                viableVariants.Add(potentialVariant);
            }

        }

        return viableVariants;
    }

    private List<SelectionPressure> SpeciesDependentPressures(Patch patch, Species species)
    {
        return new List<SelectionPressure>(PredatorsOf(Patch.Miche, species).Select(x => new AvoidPredationSelectionPressure(x, 2.0f)).ToList());
    }


    /// <summary>
    ///   Returns a new list of all species that have filled a predation miche to eat the provided species.
    /// </summary>
    /// <param name="miche">Miche to search</param>
    /// <param name="species">Species to search for Predation miches of</param>
    /// <returns>List of species</returns>
    private List<Species> PredatorsOf(Miche miche, Species species)
    {
        var retval = new List<Species>();

        // TODO: Make this WAY more efficient
        foreach(var traversal in miche.AllTraversals())
        {
            foreach(var curMiche in traversal)
            {
                if (curMiche is PredationEffectivenessPressure && ((PredationEffectivenessPressure)curMiche.Pressure).Prey == species)
                {
                    retval.AddRange(curMiche.AllOccupants());
                }
            }
        }

        return retval;
    }
}
