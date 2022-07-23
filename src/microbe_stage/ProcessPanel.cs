using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
///   Controls the process panel contents
/// </summary>
public class ProcessPanel : CustomDialog
{
    [Export]
    public NodePath ATPLabelPath = null!;

    [Export]
    public NodePath ProcessListPath = null!;

    [Export]
    public bool ShowCustomCloseButton;

    [Export]
    public NodePath CloseButtonContainerPath = null!;

    private Label atpLabel = null!;

    private ProcessList processList = null!;

    private Container closeButtonContainer = null!;

    [Signal]
    public delegate void OnClosed();

    public BiomeConditions Biome { get; set; }

    public Microbe Microbe { get; set; }

    public List<TweakedProcess>? ShownData { get; set; }

    public override void _Ready()
    {
        processList = GetNode<ProcessList>(ProcessListPath);
        atpLabel = GetNode<Label>(ATPLabelPath);
        closeButtonContainer = GetNode<Container>(CloseButtonContainerPath);

        closeButtonContainer.Visible = ShowCustomCloseButton;
    }

    public override void _Process(float delta)
    {
        if (!IsVisibleInTree())
            return;

        if (ShownData != null)
        {
            // Update the list object
            processList.ProcessesToShow = ShownData.Select(p => 
                (IProcessDisplayInfo)new StaticProcessDisplayInfo(p.Process.Name, MicrobeInternalCalculations.EnvironmentModifiedProcess(1.0f, Biome, p.Process, Microbe.Compounds, p, null)))
                .ToList();
        }
        else
        {
            processList.ProcessesToShow = null;
        }

        var osmoregulationCostDisplay = Mathf.Round(100 * Microbe.OsmoregulationCost(1.0f)) / 100;
        var movementCostDisplay = Microbe.MovementDirection.Length() > 0.0f ? Mathf.Round(100 * Microbe.MovementCost()) / 100 : 0.0f;

        atpLabel.Text = "Using " + osmoregulationCostDisplay
            + " ATP for osmoregulation\n"+
            "Using " + movementCostDisplay + " ATP for movement\n"+
            "Total: "+ (osmoregulationCostDisplay + movementCostDisplay);
    }

    protected override void OnHidden()
    {
        base.OnHidden();
        EmitSignal(nameof(OnClosed));
    }

    private void OnClosePressed()
    {
        GUICommon.Instance.PlayButtonPressSound();
        CustomHide();
    }
}

public class StaticProcessDisplayInfo : IProcessDisplayInfo
{
    string Name;
    IEnumerable<KeyValuePair<Compound, float>> Inputs;
    Dictionary<Compound, float> FullSpeedRequiredEnvironmentalInputs;
    IEnumerable<KeyValuePair<Compound, float>> Outputs;
    float CurrentSpeed;
    List<Compound>? LimitingCompounds;

    public StaticProcessDisplayInfo(string Name, TweakedProcess process)
    {
        this.Name = Name;
        Inputs = process.Process.Inputs.Select(pair => new KeyValuePair<Compound, float>(pair.Key, pair.Key.IsEnvironmental ? pair.Value : pair.Value * process.Rate));

        var temp = process.Process.Inputs.Where(x => x.Key.IsEnvironmental);
        FullSpeedRequiredEnvironmentalInputs = new Dictionary<Compound, float>();

        foreach (var pair in temp)
        {
            //FullSpeedRequiredEnvironmentalInputs[pair.Key] = pair.Value;
        }

        Outputs = process.Process.Outputs.Select(pair => new KeyValuePair<Compound, float>(pair.Key, pair.Value * process.Rate));
        CurrentSpeed = process.Rate;
        LimitingCompounds = null;
    }

    string IProcessDisplayInfo.Name => Name;

    IEnumerable<KeyValuePair<Compound, float>> IProcessDisplayInfo.Inputs => Inputs.Where(x => !x.Key.IsEnvironmental);

    IEnumerable<KeyValuePair<Compound, float>> IProcessDisplayInfo.EnvironmentalInputs => Inputs.Where(x => x.Key.IsEnvironmental);

    IReadOnlyDictionary<Compound, float> IProcessDisplayInfo.FullSpeedRequiredEnvironmentalInputs => FullSpeedRequiredEnvironmentalInputs;

    IEnumerable<KeyValuePair<Compound, float>> IProcessDisplayInfo.Outputs => Outputs;

    float IProcessDisplayInfo.CurrentSpeed => CurrentSpeed;

    IReadOnlyList<Compound>? IProcessDisplayInfo.LimitingCompounds => LimitingCompounds;
}