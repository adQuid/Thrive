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
            var samplingCount = 10f;

            var osmoregulationCost = Microbe.OsmoregulationCost(1);
            var movementCost = Microbe.MovementDirection.Length() > 0.0f ? Microbe.MovementCost(1) : 0.0f;
            var totalCost = osmoregulationCost + movementCost;

            var osmoregulationCostDisplay = Mathf.Round(100 * osmoregulationCost) / 100;
            var movementCostDisplay = Mathf.Round(100 * movementCost) / 100;

            atpLabel.Text = "Using " + osmoregulationCostDisplay
                + " ATP for osmoregulation\n" +
                "Using " + movementCostDisplay + " ATP for movement\n" +
                "Total: " + (osmoregulationCostDisplay + movementCostDisplay);

            var temp = MicrobeInternalCalculations.SlicedProcesses(Microbe.Compounds, Microbe.organelles.Select(x => x.Definition), Biome, totalCost);

            // Update the list object
            processList.ProcessesToShow = temp.Select(x => (IProcessDisplayInfo) new StaticProcessDisplayInfo(x.Process.Name, x)).ToList();
        }
        else
        {
            processList.ProcessesToShow = null;
        }
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

        Outputs = process.Process.Outputs.Select(pair => new KeyValuePair<Compound, float>(pair.Key, pair.Value * process.Rate));
        CurrentSpeed = 0.1f;
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