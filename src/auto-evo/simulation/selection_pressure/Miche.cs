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

    public void InsertSpecies(Species species)
    {
        //GD.Print("Inserting " + species.FormattedName + " into miche "+Name);

        if (IsLeafNode() && Occupant == null)
        {
            Occupant = species;
        }

        // TODO: store this somewhere
        var existingScores = AllOccupants().Select(x => Pressure.Score(x, new SimulationCache())).OrderBy(x => x);

        if (existingScores.Count() == 0 || Pressure.Score(species, new SimulationCache()) >= existingScores.First())
        {
            // If this is a leaf, then there's only one species and the new species beats that.
            if (IsLeafNode())
            {
                Occupant = species;
            }

            // This could be in an else, but isn't nessicary
            foreach (var child in Children)
            {
                child.InsertSpecies(species);
            }
        }
    }

    public void AddChild(Miche newChild)
    {
        Children.Add(newChild);
    }

    public void AddChildren(IEnumerable<Miche> newChildren)
    {
        foreach (var child in newChildren)
        {
            AddChild(child);
        }
    }
}
