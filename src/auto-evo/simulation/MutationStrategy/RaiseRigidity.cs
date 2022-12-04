using System;
using System.Collections.Generic;

class RaiseRigidity : IMutationStrategy<MicrobeSpecies>
{
    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, PartList partList)
    {
        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        newSpecies.MembraneRigidity = (newSpecies.MembraneRigidity + 1.0f) / 2.0f;

        return new List<MicrobeSpecies> { newSpecies };
    }
}