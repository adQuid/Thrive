using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IMutationStrategy<T> where T: Species
{
    public abstract List<T> MutationsOf(T baseSpecies);
}