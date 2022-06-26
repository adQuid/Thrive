using Godot;

/// <summary>
///   Controls the screen fade transition
/// </summary>
public class ScreenFade : Control, ITransition
{
    private ColorRect? rect;
    private Label? label;
    private AnimationPlayer? animationPlayer;
    private Tween fader = null!;

    private FadeType currentFadeType;

    [Signal]
    public delegate void OnFinishedSignal();

    public enum FadeType
    {
        /// <summary>
        ///   Screen fades to white (transparent)
        /// </summary>
        FadeIn,

        /// <summary>
        ///   Screen fades to black
        /// </summary>
        FadeOut,
        StayBlack
    }

    public bool Finished { get; private set; }

    public float FadeDuration { get; set; }

    public FadeType CurrentFadeType
    {
        get => currentFadeType;
        set
        {
            currentFadeType = value;
            SetInitialColours();
        }
    }

    public override void _Ready()
    {
        rect = GetNode<ColorRect>("Rect");
        label = GetNode<Label>("Label");
        animationPlayer = GetNode<AnimationPlayer>("Label/AnimationPlayer");
        fader = GetNode<Tween>("Fader");

        fader.Connect("tween_all_completed", this, nameof(OnFinished));

        // Keep this node running while paused
        PauseMode = PauseModeEnum.Process;

        SetInitialColours();
        Hide();
    }

    public void FadeToBlack()
    {
        FadeTo(new Color(0, 0, 0, 1));
    }

    public void FadeToWhite()
    {
        FadeTo(new Color(0, 0, 0, 0));
    }

    public void FadeTo(Color final)
    {
        fader.InterpolateProperty(rect, "color", null, final, FadeDuration);

        fader.Start();
    }

    public void Begin()
    {
        Show();

        switch (CurrentFadeType)
        {
            case FadeType.FadeIn:
                FadeToWhite();
                break;
            case FadeType.FadeOut:
            case FadeType.StayBlack:
                FadeToBlack();
                break;
        }

        if (CurrentFadeType == FadeType.StayBlack)
        {
            animationPlayer.Play("FadeInOut");
        }
    }

    public void Skip()
    {
        OnFinished();
    }

    public void Clear()
    {
        this.DetachAndQueueFree();
    }

    private void SetInitialColours()
    {
        if (rect == null)
            return;

        // Apply initial colors
        if (currentFadeType == FadeType.FadeIn
            || currentFadeType == FadeType.StayBlack)
        {
            rect.Color = new Color(0, 0, 0, 1);
        }
        else if (currentFadeType == FadeType.FadeOut)
        {
            rect.Color = new Color(0, 0, 0, 0);
        }
    }

    private void OnFinished()
    {
        Finished = true;
    }
}
