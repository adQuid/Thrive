namespace Tutorial
{
    using System;

    /// <summary>
    ///   Introduction to the cell editor
    /// </summary>
    public class CellEditorIntroduction : TutorialPhase
    {
        private readonly string cellEditorTab = EditorTab.CellEditor.ToString();

        public override string ClosedByName { get; } = "CellEditorIntroduction";

        public override void ApplyGUIState(MicrobeEditorTutorialGUI gui)
        {
            gui.CellEditorIntroductionVisible = ShownCurrently;
        }

        public override bool CheckEvent(TutorialState overallState, TutorialEventType eventType, EventArgs args,
            object sender)
        {
            switch (eventType)
            {
                case TutorialEventType.MicrobeEditorTabChanged:
                {
                    if (!HasBeenShown && CanTrigger && ((StringEventArgs)args).Data == cellEditorTab && !overallState.EditorWelcome.TrustPlayer && overallState.EditorWelcome.Complete)
                    {
                        Show();
                    }

                    break;
                }

                case TutorialEventType.MicrobeEditorOrganellePlaced:
                {
                    if (!HasBeenShown && CanTrigger && (overallState.EditorWelcome.ShownCurrently || overallState.EditorWelcome.Complete))
                    {
                        Show();
                    }

                    break;
                }

                /*case TutorialEventType.EnteredMicrobeStage:
                {
                    if (ShownCurrently)
                    {
                        Hide();
                    }

                    break;
                }*/
            }

            return false;
        }
    }
}
