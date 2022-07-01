using System;
using System.Globalization;
using Godot;

/// <summary>
///   Shows a compound amount along with an icon
/// </summary>
public class CompoundAmount : HBoxContainer
{
    private Label? amountLabel;
    private TextureRect? icon;

    private Compound? compound;

    private int decimals = 3;
    private float amount1 = float.NegativeInfinity;
    private float? amount2 = float.NegativeInfinity;
    private bool prefixPositiveWithPlus;
    private bool usePercentageDisplay;
    private Colour valueColour = Colour.White;

    public enum Colour
    {
        White,
        Red,
    }

    /// <summary>
    ///   The compound to show
    /// </summary>
    public Compound Compound
    {
        set
        {
            if (value == null)
                throw new ArgumentNullException();

            if (compound == value)
                return;

            compound = value;

            if (icon != null)
            {
                UpdateIcon();
                UpdateTooltip();
            }
        }
    }

    /// <summary>
    ///   The compound amount to show
    /// </summary>
    public Tuple<float, float?> Amount
    {
        get => new Tuple<float, float?>(amount1, amount2);
        set
        {
            if (amount1 == value.Item1 && amount2 == value.Item2)
                return;

            amount1 = value.Item1;
            amount2 = value.Item2;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   Number of decimals to show in the amount
    /// </summary>
    public int Decimals
    {
        get => decimals;
        set
        {
            if (decimals == value)
                return;

            decimals = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   If true positive (>= 0) amounts are prefixed with a plus.
    /// </summary>
    public bool PrefixPositiveWithPlus
    {
        get => prefixPositiveWithPlus;
        set
        {
            if (prefixPositiveWithPlus == value)
                return;

            prefixPositiveWithPlus = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   If true  numbers are shown as percentages.
    /// </summary>
    public bool UsePercentageDisplay
    {
        get => usePercentageDisplay;
        set
        {
            if (usePercentageDisplay == value)
                return;

            usePercentageDisplay = value;
            if (amountLabel != null)
                UpdateLabel();
        }
    }

    /// <summary>
    ///   Colour of the amount display
    /// </summary>
    public Colour ValueColour
    {
        get => valueColour;
        set
        {
            if (valueColour == value)
                return;

            valueColour = value;
            UpdateColour();
        }
    }

    public override void _Ready()
    {
        base._Ready();

        if (compound == null)
            throw new InvalidOperationException($"Need to set {nameof(Compound)}");

        UpdateLabel();
        UpdateIcon();
        UpdateTooltip();

        // Only apply non-default colour here. If it is later changed, it is then applied
        if (ValueColour != Colour.White)
            UpdateColour();
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            UpdateTooltip();

            UpdateLabel();
        }
    }

    private void UpdateLabel()
    {
        if (amountLabel == null)
        {
            amountLabel = new Label();
            AddChild(amountLabel);
        }

        if (amount2 != null)
        {
            amountLabel.Text = NumberPart(amount1) + "/" + NumberPart(amount2.Value);
        }
        else
        {
            amountLabel.Text = NumberPart(amount1);
        }

    }

    private string NumberPart(float amount)
    {
        string retval;
        if (UsePercentageDisplay)
        {
            retval = string.Format(CultureInfo.CurrentCulture, TranslationServer.Translate("PERCENTAGE_VALUE"),
                Math.Round(amount * 100, 1));
        }
        else
        {
            retval = Math.Round(amount, decimals).ToString(CultureInfo.CurrentCulture);
        }

        if (PrefixPositiveWithPlus && amount >= 0)
        {
            return "+" + retval;
        }
        else
        {
            return retval;
        }
    }

    private void UpdateColour()
    {
        if (amountLabel == null)
            return;

        Color color;

        switch (ValueColour)
        {
            case Colour.White:
                color = new Color(1.0f, 1.0f, 1.0f);
                break;
            case Colour.Red:
                color = new Color(1.0f, 0.3f, 0.3f);
                break;
            default:
                throw new Exception("unhandled colour");
        }

        amountLabel.AddColorOverride("font_color", color);
    }

    private void UpdateIcon()
    {
        icon?.DetachAndFree();

        icon = GUICommon.Instance.CreateCompoundIcon(compound!.InternalName);
        AddChild(icon);
    }

    private void UpdateTooltip()
    {
        if (icon != null)
            icon.HintTooltip = compound!.Name;
    }
}
