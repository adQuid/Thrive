using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

class PullSpeciesForPatch : IRunStep
{
    public Patch Patch;
    public SimulationCache Cache;

    public int TotalSteps => 1;

    public bool CanRunConcurrently => false;

    public PullSpeciesForPatch(Patch patch, SimulationCache cache)
    {
        Patch = patch;
        Cache = cache;
    }

    public bool RunStep(RunResults results)
    {
        GD.Print("Running 'pullspecies' for "+Patch.Name+". "+CandiateSpecies().Count()+" candidate species");
        results.Miches[Patch] = SelectionPressure.MichesForPatch(Patch, Cache);

        var variants = CandiateSpecies().ToList();
        foreach (var niche in results.Miches[Patch].SelectMany(x => x.AllTraversals()))
        {
            // TODO: Only check relivent species
            foreach (var species in CandiateSpecies())
            {
                //TODO: put this in a fixed place
                var partList = new PartList(species);

                variants.Add(ModifyExistingSpecies.ViableVariants(results, species, Patch, partList, Cache, niche.Select(x => x.Pressure).ToList()).First());
            }
        }

        PopulateMichesForPatch(results, Patch, variants, Cache);//hmm what do here?

        return true;
    }

    public static void PopulateMichesForPatch(RunResults results, Patch patch, IEnumerable<Species> allSpecies, SimulationCache cache)
    {
        // TODO: do this some other way
        var miches = SelectionPressure.MichesForPatch(patch, cache);

        foreach (var miche in miches)
        {
            PopulateForMiche(patch, miche, allSpecies, results, cache);
        }

        results.Miches[patch] = miches;
    }

    private static void PopulateForMiche(Patch patch, Miche miche, IEnumerable<Species> allSpecies, RunResults results, SimulationCache cache)
    {
        GD.Print(miche.AllTraversals().Count());

        foreach (var traversal in miche.AllTraversals())
        {
            var pressures = traversal.Select(x => x.Pressure);
            var qualifiedSpeciesScores = new Dictionary<Species, double>();

            foreach (var species in allSpecies)
            {
                qualifiedSpeciesScores[species] = 0;
            }

            foreach (var pressure in pressures)
            {
                var remainingQualifiedSpecies = new Dictionary<Species, double>(qualifiedSpeciesScores);

                foreach (var species in qualifiedSpeciesScores.Keys)
                {
                    var score = pressure.Score(species, cache);
                    if (score > 0)
                    {
                        remainingQualifiedSpecies[species] += score;
                    }
                    else
                    {
                        remainingQualifiedSpecies.Remove(species);
                        continue;
                    }
                }

                qualifiedSpeciesScores = remainingQualifiedSpecies;
            }

            // If anything is able to survive this path, put it on the leaf node.
            if (qualifiedSpeciesScores.Count > 0)
            {
                var speciesToAdd = qualifiedSpeciesScores.OrderByDescending(x => x.Value).First().Key;

                // This should work since it's a shallow copy, right?
                traversal.Last().Occupant = speciesToAdd;

                var population = new Dictionary<Patch, long>();
                population[patch] = 1000;

                if (!results.SpeciesHasResults(speciesToAdd))
                {
                    // TODO: Use the real parent, fool!
                    results.AddNewSpecies(speciesToAdd, population, RunResults.NewSpeciesType.FillNiche, speciesToAdd);
                }

                // Mark the best pressures for hovering over in game
                results.results[speciesToAdd].AddBestPressuresResults(patch, traversal.Select(x => x.Pressure).ToList());
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
