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

    public Microbe Microbe { get; set; }

    public ProcessStatistics? ShownData { get; set; }

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
            processList.ProcessesToShow = ShownData.Processes.Select(p => p.Value.ComputeAverageValues()).ToList();
        }
        else
        {
            processList.ProcessesToShow = null;
        }

        atpLabel.Text = "Using " + Microbe.OsmoregulationCost(1.0f) + " ATP for osmoregulation, 0 ATP for movement";
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
