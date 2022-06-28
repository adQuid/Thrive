namespace Tutorial
{
    using System;
    using Godot;

    /// <summary>
    ///   Welcome message and intro to the report tab
    /// </summary>
    public class EditorWelcome : TutorialPhase
    {
        private readonly string reportTab = EditorTab.Report.ToString();

        public override string ClosedByName { get; } = "MicrobeEditorReport";

        public bool TrustPlayer = false;

        public EditorWelcome()
        {
            ProcessWhileHidden = true;
        }

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.EditorEntryReportVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobePlayerReadyToEdit:
                {
                    Time = 0;
                    break;
                }

                case TutorialEventType.EnteredMicrobeEditor:
                {
                    // The player was quick to hit the button, and probably doesn't need help
                    TestPlayerTrust();

                    if (!HasBeenShown && CanTrigger)
                    {
                        Show();
                    }

                    break;
                }

                case TutorialEventType.MicrobeEditorTabChanged:
                {
                    var tab = ((StringEventArgs)args).Data;

                    // Hide when switched to another tab
                    if (tab != reportTab)
                    {
                        if (ShownCurrently)
                        {
                            Hide();
                        }
                    }

                    break;
                }

                case TutorialEventType.MicrobeEditorOrganellePlaced:
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }

                    break;
                }
            }

            return false;
        }

        public void TestPlayerTrust()
        {
            if (Time < Constants.MICROBE_EDITOR_BUTTON_QUICK)
            {
                Inhibit();
                TrustPlayer = true;
            }
        }
    }
}
