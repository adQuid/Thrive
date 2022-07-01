using System.Collections.Generic;
using Godot;

/// <summary>
///   Shows Compound balance information
/// </summary>
public class CompoundBalanceDisplay : VBoxContainer
{
    [Export]
    public NodePath CompoundListContainerPath = null!;

    private VBoxContainer compoundListContainer = null!;

    private ChildObjectCache<Compound, CompoundAmount> childCache = null!;

    public override void _Ready()
    {
        compoundListContainer = GetNode<VBoxContainer>(CompoundListContainerPath);

        childCache = new ChildObjectCache<Compound, CompoundAmount>(compoundListContainer,
            compound => new CompoundAmount { Compound = compound, PrefixPositiveWithPlus = true });
    }

    public void UpdateBalances(Dictionary<Compound, CompoundBalance> balances)
    {
        childCache.UnMarkAll();

        foreach (var entry in balances)
        {
            var compoundControl = childCache.GetChild(entry.Key);
            var amount = entry.Value.Production.SumValues() > 0.0f ?
                entry.Value.Production.SumValues() - entry.Value.Consumption.SumValues() / 2.0f
                : entry.Value.Balance;
            compoundControl.Amount = amount;

            compoundControl.ValueColour = amount < 0 ?
                CompoundAmount.Colour.Red :
                CompoundAmount.Colour.White;
        }

        childCache.DeleteUnmarked();
        childCache.ApplyOrder();
    }
}
