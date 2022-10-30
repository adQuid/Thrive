using System;
using System.Collections.Generic;
using System.Linq;

public class Miche
{
    public String Name;
    public List<Miche>? Children;
    public SelectionPressure Pressure;

    public Miche(String name, SelectionPressure pressure, List<Miche>? children)
    {
        Name = name;
        Children = children;
        Pressure = pressure;
    }

    public bool IsLeafNode()
    {
        return Children == null;
    }

    public List<List<SelectionPressure>> AllTraversals()
    {
        if (IsLeafNode())
        {
            return new List<List<SelectionPressure>> { new List<SelectionPressure> { Pressure } };
        }

        var traversals = Children.SelectMany(x => x.AllTraversals());
        var retval = new List<List<SelectionPressure>>();

        foreach (var list in traversals)
        {
            list.Insert(0, Pressure);
            retval.Add(list);
        }

        return retval;
    }
}
