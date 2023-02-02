using System;
using System.Collections.Generic;
using System.Linq;
using AutoEvo;
using Godot;

public class Miche
{
    public String Name;
    public Miche? Parent = null;
    public List<Miche> Children = new();
    // confusingly, this should always be null if the children array isn't empty
    public Species? Occupant = null;
    public SelectionPressure Pressure;

    public static Miche RootMiche()
    {
        return new Miche("Root", new NoOpSelectionPressure());
    }

    public Miche(String name, SelectionPressure pressure): this(name, pressure, new List<Miche>()) { }

    public Miche(String name, SelectionPressure pressure, List<Miche> children)
    {
        Name = name;
        Children = children;

        foreach (var child in children)
        {
            child.Parent = this;
        }

        Pressure = pressure;
    }

    public bool IsLeafNode()
    {
        return Children.Count() == 0;
    }

    public Miche Root()
    {
        if (Parent == null)
        {
            return this;
        }

        return Parent.Root();
    }

    public List<List<Miche>> AllTraversals()
    {
        if (IsLeafNode())
        {
            return new List<List<Miche>> { new List<Miche> { this } };
        }

        var traversals = Children.SelectMany(x => x.AllTraversals());
        var retval = new List<List<Miche>>();

        foreach (var list in traversals)
        {
            list.Insert(0, this);
            retval.Add(list);
        }

        return retval;
    }

    public IEnumerable<Species> AllOccupants()
    {
        var retval = new List<Species>();

        if (Occupant != null)
        {
            retval.Add(Occupant);
        }

        foreach (var child in Children)
        {
            retval.AddRange(child.AllOccupants());
        }

        return retval;
    }

    public IEnumerable<IEnumerable<Miche>> TraversalsTerminatingInSpecies(Species species)
    {
        return AllTraversals().Where(x => x.Last().Occupant == species);
    }

    /// <summary>
    ///   Inserts a species into any spots on the tree. where the species is a better fit than any current occupants
    /// </summary>
    /// <param name="species"></param>
    public bool InsertSpecies(Species species)
    {
        if (IsLeafNode() && Occupant == null)
        {
            Occupant = species;
            return true;
        }

        // TODO: store this somewhere
        var existingScores = AllOccupants().Select(x => Pressure.Score(x, new SimulationCache())).OrderBy(x => x);

        var speciesScore = Pressure.Score(species, new SimulationCache());
        if (speciesScore > 0 && (existingScores.Count() == 0 || speciesScore >= existingScores.First()))
        {
            // If this is a leaf, then there's only one species and the new species beats that.
            if (IsLeafNode())
            {
                Occupant = species;
            }

            var retval = false;

            // This could be in an else, but isn't nessicary
            foreach (var child in Children)
            {
                if (child.InsertSpecies(species))
                {
                    retval = true;
                }
            }

            return retval;
        }

        return false;
    }

    public void AddChild(Miche newChild)
    {
        Children.Add(newChild);
        newChild.Parent = this;
    }

    public void AddChildren(IEnumerable<Miche> newChildren)
    {
        foreach (var child in newChildren)
        {
            AddChild(child);
        }
    }
}
