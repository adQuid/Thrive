using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

class PullSpeciesForPatch : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;
    public bool PlayerPatch;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public PullSpeciesForPatch(Patch patch, SimulationCache cache, bool playerPatch)
    {
        Patch = patch;
        Cache = cache;
        PlayerPatch = playerPatch;
    }

    public bool RunStep(RunResults results)
    {
        var variants = CandiateSpecies().ToList();

        PopulateForMiche(Patch, results.MicheByPatch[Patch], variants, results, Cache);

        return true;
    }

    /*
     * Finds or creates a species for every leaf node of the provided miche.
     */
    private static void PopulateForMiche(Patch patch, Miche miche, IEnumerable<Species> allSpecies, RunResults results, SimulationCache cache)
    {
        foreach (var curSpecies in allSpecies)
        {
            // Try to add existing species
            if (miche.Root().InsertSpecies(curSpecies))
            {

                results.MakeSureResultExistsForSpecies(curSpecies);

                // Mark the best pressures for hovering over in game
                foreach(var traversal in miche.TraversalsTerminatingInSpecies(curSpecies))
                {
                    results.results[curSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
                }
            }
        }

        FillEmptyMiches(allSpecies, results, patch, cache);
    }

    public static void FillEmptyMiches(IEnumerable<Species> allSpecies, RunResults results, Patch patch, SimulationCache cache)
    {
        // if there are any empty miches, try out other species to fill the gap
        foreach (var emptyMiche in results.MicheByPatch[patch].TraversalsTerminatingInSpecies(null))
        {
            FillEmptyMiche(emptyMiche, allSpecies, results, patch, cache);
        }
    }

    private static void FillEmptyMiche(IEnumerable<Miche> emptyMiche, IEnumerable<Species> allSpecies, RunResults results, Patch patch, SimulationCache cache)
    {
        var miche = results.MicheByPatch[patch];

        GD.Print("Searching to fill empty path: " + String.Join(",", emptyMiche.Select(x => x.Name)));
        foreach (var curSpecies in allSpecies)
        {
            var variants = ModifyExistingSpecies.ViableVariants(results, curSpecies, patch, new PartList(curSpecies), cache, emptyMiche.Select(x => x.Pressure).ToList());

            if (variants.Count() > 0)
            {
                // This may do nothing or be overwritten, and that's ok
                emptyMiche.Last().InsertSpecies(variants.First());
                results.AncestorDictionary.Add(variants.First(), curSpecies);
            }
        }

        var finalSpecies = emptyMiche.Last().Occupant;

        if (finalSpecies != null)
        {
            GD.Print("  and I found something: " + finalSpecies.FormattedName);
            results.MakeSureResultExistsForSpecies(finalSpecies);

            // Mark the best pressures for hovering over in game
            foreach (var traversal in miche.TraversalsTerminatingInSpecies(finalSpecies))
            {
                results.results[finalSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
            }
        }
    }

    private IEnumerable<Species> CandiateSpecies()
    {
        var retval = Patch.Adjacent.SelectMany(x => x.SpeciesInPatch.Keys).ToList();

        retval.AddRange(Patch.SpeciesInPatch.Select(x => x.Key));

        return retval;
    }
}
