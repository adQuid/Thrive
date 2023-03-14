using System.Linq;

namespace AutoEvo
{
    using System.Collections.Generic;

    /// <summary>
    ///   Caches some information in auto-evo runs to speed them up
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Some information will get outdated when data that the auto-evo relies on changes. If in the future
    ///     caching is moved to a higher level in the auto-evo, that needs to be considered.
    ///   </para>
    /// </remarks>
    public class SimulationCache
    {
        private readonly Dictionary<(Species, BiomeConditions), EnergyBalanceInfo> cachedEnergyBalances = new();
        private readonly Dictionary<Species, float> cachedBaseSpeeds = new();
        private readonly Dictionary<Species, float> cachedBaseHexSizes = new();

        private readonly Dictionary<(TweakedProcess, BiomeConditions), ProcessSpeedInformation> cachedProcessSpeeds =
            new();

        public SimulationCache()
        {
        }

        public EnergyBalanceInfo GetEnergyBalanceForSpecies(Species species, BiomeConditions biomeConditions)
        {


            var key = (species, biomeConditions);

            if (cachedEnergyBalances.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (species is MicrobeSpecies)
            {
                cached = MicrobeInternalCalculations.ComputeEnergyBalance(((MicrobeSpecies)species).Organelles, biomeConditions, ((MicrobeSpecies)species).MembraneType);
            }
            else
            {
                // base it on the worst cell, since they don't share ATP IIRC
                cached =
                    ((EarlyMulticellularSpecies)species).Cells.Select(cell => MicrobeInternalCalculations.ComputeEnergyBalance(cell.CellType.Organelles, biomeConditions, cell.CellType.MembraneType))
                    .OrderBy(energyBalanceInfo => energyBalanceInfo.FinalBalance).First();
            }

            cachedEnergyBalances.Add(key, cached);
            return cached;
        }

        public float GetBaseSpeedForSpecies(Species species)
        {
            if (cachedBaseSpeeds.TryGetValue(species, out var cached))
            {
                return cached;
            }

            cached = species.BaseSpeed;

            cachedBaseSpeeds.Add(species, cached);
            return cached;
        }

        public float GetBaseHexSizeForSpecies(MicrobeSpecies species)
        {
            if (cachedBaseHexSizes.TryGetValue(species, out var cached))
            {
                return cached;
            }

            cached = species.BaseHexSize;

            cachedBaseHexSizes.Add(species, cached);
            return cached;
        }

        public ProcessSpeedInformation GetProcessMaximumSpeed(TweakedProcess process, BiomeConditions biomeConditions)
        {
            var key = (process, biomeConditions);

            if (cachedProcessSpeeds.TryGetValue(key, out var cached))
            {
                return cached;
            }

            cached = MicrobeInternalCalculations.CalculateProcessMaximumSpeed(process, biomeConditions);

            cachedProcessSpeeds.Add(key, cached);
            return cached;
        }
    }
}
