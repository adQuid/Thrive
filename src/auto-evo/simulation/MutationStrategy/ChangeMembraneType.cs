using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class ChangeMembraneType : IMutationStrategy<MicrobeSpecies>
{
    MembraneType Type;
    public ChangeMembraneType(MembraneType type)
    {
        Type = type;
    }

    public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies, PartList partList)
    {
        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        newSpecies.MembraneType = Type;

        return new List<MicrobeSpecies> { newSpecies };
    }
}