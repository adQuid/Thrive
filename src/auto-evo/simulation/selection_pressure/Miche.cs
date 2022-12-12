using System;
using System.Collections.Generic;
using System.Linq;

public class Miche
{
    public String Name;
    public Miche? Parent = null;
    public List<Miche> Children = new();
    public SelectionPressure Pressure;
    public Species? Occupant = null; 

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

    public List<Species> AllOccupants()
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
}
