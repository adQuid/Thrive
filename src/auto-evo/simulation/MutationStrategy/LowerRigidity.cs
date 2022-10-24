using System.Collections.Generic;

namespace Thrive.src.auto_evo.simulation.MutationStrategy
{
    class LowerRigidity : IMutationStrategy<MicrobeSpecies>
    {
        public List<MicrobeSpecies> MutationsOf(MicrobeSpecies baseSpecies)
        {
            var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

            newSpecies.MembraneRigidity = (newSpecies.MembraneRigidity + -1.0f) / 2.0f;

            return new List<MicrobeSpecies> { newSpecies };
        }
    }
}
