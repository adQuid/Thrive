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

            var osmoregulationCost = Microbe.OsmoregulationCost(1/samplingCount);
            var movementCost = Microbe.MovementDirection.Length() > 0.0f ? Microbe.MovementCost(1 / samplingCount) : 0.0f;
            var totalCost = osmoregulationCost + movementCost;

            // Pretend we have one second of osmoregulation less so we report the processes that must have happened
            var modifiedCompoundBag = new CompoundBag(Microbe.Compounds);

            var osmoregulationCostDisplay = Mathf.Round(100 * osmoregulationCost * samplingCount) / 100;
            var movementCostDisplay = Mathf.Round(100 * movementCost * samplingCount) / 100;

            atpLabel.Text = "Using " + osmoregulationCostDisplay
                + " ATP for osmoregulation\n" +
                "Using " + movementCostDisplay + " ATP for movement\n" +
                "Total: " + (osmoregulationCostDisplay + movementCostDisplay);

            var allProcesses = new List<TweakedProcess>();
            for (var iteration = 0; iteration < samplingCount; iteration++)
            {
                if (modifiedCompoundBag.Compounds.ContainsKey(Compound.ByName("atp")))
                {
                    // Not using the normal method here in order to allow negative values
                    modifiedCompoundBag.Compounds[Compound.ByName("atp")] -= totalCost;
                }

                foreach (var data in ShownData)
                {
                    var proc = MicrobeInternalCalculations
                    .EnvironmentModifiedProcess(1 / samplingCount, Biome, data.Process, modifiedCompoundBag, data, null);

                    // Consume inputs
                    foreach (var entry in proc.Process.Inputs)
                    {
                        if (entry.Key.IsEnvironmental)
                            continue;

                        var inputRemoved = entry.Value * proc.Rate;

                        // This should always succeed (due to the earlier check) so it is always assumed here that this succeeded
                        modifiedCompoundBag.TakeCompound(entry.Key, inputRemoved);
                    }

                    // Add outputs
                    foreach (var entry in proc.Process.Outputs)
                    {
                        if (entry.Key.IsEnvironmental)
                            continue;

                        var outputGenerated = entry.Value * proc.Rate;

                        modifiedCompoundBag.AddCompound(entry.Key, outputGenerated);
                    }

                    allProcesses.Add(proc);
                }
            }

            var temp = allProcesses.GroupBy(process => process.Process.Name).Select(group => group.First());
            var newRates = new Dictionary<TweakedProcess, float>();

            foreach (var process in temp)
            {
                newRates[process] = allProcesses.Where(x => process.Process.InternalName == x.Process.InternalName).Sum(x => x.Rate);
            }

            foreach (var pair in newRates)
            {
                pair.Key.Rate = pair.Value;
            }

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