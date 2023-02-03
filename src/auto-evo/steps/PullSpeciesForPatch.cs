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
        var foreignSpecies = NeighboringSpecies().ToList();

        List<Species> newSpecies = PopulateForMiche(Patch, results.MicheByPatch[Patch], foreignSpecies, results, Cache);

        // At the first cycle also add species that were already present
        newSpecies.AddRange(results.MicheByPatch[Patch].AllOccupants().Where(x => !newSpecies.Contains(x)));

        GD.Print(results.MicheByPatch[Patch].AllOccupants().Count() + " species here now: " + string.Join(",", newSpecies.Select(x => x.FormattedName)));

        // TODO: Replace this with an energy-based calculation
        var iteration = 1;
        while (newSpecies.Count() > 0 && iteration < 3)
        {
            results.MicheByPatch[Patch].AddChildren(DerivativeMiches(Patch, newSpecies, Cache));
            newSpecies = PopulateForMiche(Patch, results.MicheByPatch[Patch], foreignSpecies, results, Cache);
            iteration++;
        }

        return true;
    }

    private static List<Miche> DerivativeMiches(Patch patch, List<Species> speciesToEat, SimulationCache cache)
    {
        return SelectionPressure.PredationMiches(patch, speciesToEat.ToHashSet(), cache);
    }

    /*
     * Finds or creates a species for every leaf node of the provided miche.
     */
    private static List<Species> PopulateForMiche(Patch patch, Miche miche, IEnumerable<Species> foreignSpecies, RunResults results, SimulationCache cache)
    {
        List<Species> retval = new();
        GD.Print(results.MicheByPatch[patch].TraversalsTerminatingInSpecies(null).Count());
        // Try to add native species to any new openings
        var nativeSpecies = patch.SpeciesInPatch.Select(x => x.Key).ToList();
        nativeSpecies.AddRange(miche.AllOccupants().Where(x => !nativeSpecies.Contains(x)));
        foreach (var curSpecies in nativeSpecies)
        {
            if (miche.Root().InsertSpecies(curSpecies))
            {
                results.MakeSureResultExistsForSpecies(curSpecies);

                // Mark the best pressures for hovering over in game
                foreach(var traversal in miche.TraversalsTerminatingInSpecies(curSpecies))
                {
                    results.results[curSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
                }

                retval.Add(curSpecies);
            }
        }
        GD.Print(results.MicheByPatch[patch].TraversalsTerminatingInSpecies(null).Count());

        // Then outside species have a chance to migrate in
        foreach (var curSpecies in foreignSpecies)
        {
            if (miche.Root().InsertSpecies(curSpecies))
            {
                results.MakeSureResultExistsForSpecies(curSpecies);

                // Mark the best pressures for hovering over in game
                foreach (var traversal in miche.TraversalsTerminatingInSpecies(curSpecies))
                {
                    results.results[curSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
                }

                retval.Add(curSpecies);
            }
        }

        GD.Print(results.MicheByPatch[patch].TraversalsTerminatingInSpecies(null).Count());
        // If no existing species can do the job, make a new one
        retval.AddRange(FillEmptyMiches(foreignSpecies, results, patch, cache));

        return retval;
    }

    public static List<Species> FillEmptyMiches(IEnumerable<Species> foreignSpecies, RunResults results, Patch patch, SimulationCache cache)
    {
        List<Species> retval = new();

        // if there are any empty miches, try out other species to fill the gap
        foreach (var emptyMiche in results.MicheByPatch[patch].TraversalsTerminatingInSpecies(null))
        {
            var toAdd = FillEmptyMiche(emptyMiche, foreignSpecies, results, patch, cache);

            if (toAdd != null)
            {
                retval.Add(toAdd);
            }
        }

        return retval;
    }

    private static Species? FillEmptyMiche(IEnumerable<Miche> emptyMiche, IEnumerable<Species> foreignSpecies, RunResults results, Patch patch, SimulationCache cache)
    {
        var miche = results.MicheByPatch[patch];

        var pointer = emptyMiche.Last();
        // This should always be null at this stage
        var finalSpecies = emptyMiche.Last().Occupant;

        while (pointer.Parent != null && finalSpecies == null)
        {
            pointer = pointer.Parent;

            foreach (var curSpecies in pointer.AllOccupants())
            {
                var variants = ModifyExistingSpecies.ViableVariants(results, curSpecies, patch, new PartList(curSpecies), cache, emptyMiche.Select(x => x.Pressure).ToList());

                if (variants.Count() > 0)
                {
                    // This may do nothing or be overwritten, and that's ok
                    emptyMiche.Last().InsertSpecies(variants.First());
                    if (!results.AncestorDictionary.ContainsKey(variants.First()))
                    {
                        // TODO: Figure out a cleaner way to deal with sibling mutations
                        var ancestor = results.AncestorDictionary.ContainsKey(curSpecies) ? results.AncestorDictionary[curSpecies] : curSpecies;
                        results.AncestorDictionary.Add(variants.First(), ancestor);
                    }
                }
            }

            finalSpecies = emptyMiche.Last().Occupant;
        }

        // If no native species rose to the challange, then a foreign species gets a chance
        if (finalSpecies == null)
        {
            foreach (var curSpecies in foreignSpecies)
            {
                var variants = ModifyExistingSpecies.ViableVariants(results, curSpecies, patch, new PartList(curSpecies), cache, emptyMiche.Select(x => x.Pressure).ToList());

                if (variants.Count() > 0)
                {
                    // This may do nothing or be overwritten, and that's ok
                    emptyMiche.Last().InsertSpecies(variants.First());
                    if (!results.AncestorDictionary.ContainsKey(variants.First()))
                    {
                        // TODO: Figure out a cleaner way to deal with sibling mutations
                        var ancestor = results.AncestorDictionary.ContainsKey(curSpecies) ? results.AncestorDictionary[curSpecies] : curSpecies;
                        results.AncestorDictionary.Add(variants.First(), ancestor);
                    }
                }
            }
            finalSpecies = emptyMiche.Last().Occupant;
        }

        if (finalSpecies != null)
        {
            results.MakeSureResultExistsForSpecies(finalSpecies);

            // Mark the best pressures for hovering over in game
            foreach (var traversal in miche.TraversalsTerminatingInSpecies(finalSpecies))
            {
                results.results[finalSpecies].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
            }
        }

        return finalSpecies;
    }

    private IEnumerable<Species> NeighboringSpecies()
    {
        var retval = Patch.Adjacent.SelectMany(x => x.SpeciesInPatch.Keys).ToList();

        return retval;
    }
}
