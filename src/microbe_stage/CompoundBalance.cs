﻿using System.Collections.Generic;
using Godot;

/// <summary>
///   Balance of a given compound. Lists the organelles that contribute to the balance
/// </summary>
public class CompoundBalance
{
    public readonly Dictionary<string, float> Consumption = new();

    public readonly Dictionary<string, float> Production = new();

    /// <summary>
    ///   Total balance of this compound
    /// </summary>
    public float Balance;

    public CompoundBalance(float consumption, float production)
    {
        AddConsumption("all", consumption);
        AddProduction("all", production);
    }

    public void AddConsumption(string organelleName, float amount)
    {
        Consumption.TryGetValue(organelleName, out var existing);

        Consumption[organelleName] = existing + amount;

        Balance -= amount;
    }

    public void AddProduction(string organelleName, float amount)
    {
        Production.TryGetValue(organelleName, out var existing);

        Production[organelleName] = existing + amount;

        Balance += amount;
    }
}
