﻿using System;
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

    public bool InsertSpecies(Species species)
    {
        return InsertSpecies(species, new List<Species>(),  new List<Species>());
    }

        /// <summary>
        ///   Inserts a species into any spots on the tree. where the species is a better fit than any current occupants
        /// </summary>
        /// <param name="species">new species being inserted</param>
        /// <param name="speciesBeat">a list of species that the species being inserted has surpassed in at least one selection pressure</param>
        public bool InsertSpecies(Species species, List<Species> speciesBeat, List<Species> disqualifyingSpecies)
    {
        if (IsLeafNode() && Occupant == null)
        {
            Occupant = species;
            return true;
        }

        var newSpeciesBeat = new List<Species>(speciesBeat);
        var newDisqualifyingSpecies = new List<Species>(disqualifyingSpecies);

        var speciesScore = Pressure.Score(species, new SimulationCache());

        foreach (var existingSpecies in AllOccupants())
        {
            var existingSpeciesScore = Pressure.Score(existingSpecies, new SimulationCache());
            if (speciesScore > existingSpeciesScore)
            {
                newSpeciesBeat.Add(existingSpecies);
            } 
            else if (speciesScore < existingSpeciesScore)
            {
                newDisqualifyingSpecies.Add(existingSpecies);
            }
        }

        if (speciesScore > 0 
            && newDisqualifyingSpecies.Count() < AllOccupants().Count())
        {
            var retval = false;

            // We know Occupant isn't null because of an earlier check
            if (IsLeafNode() && newSpeciesBeat.Contains(Occupant))
            {
                Occupant = species;
                retval = true;
            }

            // This could be in an else, but isn't nessicary
            foreach (var child in Children)
            {
                if (child.InsertSpecies(species, newSpeciesBeat, newDisqualifyingSpecies))
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
