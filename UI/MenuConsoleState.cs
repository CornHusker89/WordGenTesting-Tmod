using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace WorldGenTesting.UI;

public class MenuConsoleState : UIState {
    internal UIText Output = null!;
    internal UIScrollbar Scrollbar = null!;
    internal UIList ScrollingList = null!;
    internal CommandTextInput TextInput = null!;

    public override void OnInitialize() {
        var panel = new UIPanel();
        panel.Width.Set(0f, 0.7f);
        panel.Height.Set(0f, 0.9f);
        panel.VAlign = 0.45f;
        panel.HAlign = 0.5f;

        TextInput = new CommandTextInput(0.8f);
        TextInput.Width.Set(0f, 0.98f);
        TextInput.Height.Set(0f, 0.03f);
        TextInput.VAlign = 1.00f;
        TextInput.HAlign = 0.06f;

        ScrollingList = new UIList();
        ScrollingList.Width.Set(-25f, 1f);
        ScrollingList.Height.Set(0f, 0.96f);
        ScrollingList.Top.Set(0f, 0f);
        ScrollingList.VAlign = 0f;
        ScrollingList.ListPadding = 1f;
        ScrollingList.ManualSortMethod = _ => { }; // remove sorting to keep the order they were inserted

        Output = new UIText(string.Empty, 0.8f);
        Output.Width.Set(0f, 0.96f);
        Output.Height.Set(0f, 0.96f);
        Output.VAlign = 0.98f;
        Output.HAlign = 0.98f;

        Scrollbar = new UIScrollbar();
        Scrollbar.SetView(100f, 1000f);
        Scrollbar.Height.Set(-50f, 1f);
        Scrollbar.Top.Set(50f, 0f);
        Scrollbar.HAlign = 1f;
        ScrollingList.SetScrollbar(Scrollbar);

        panel.Append(Scrollbar);
        panel.Append(ScrollingList);
        panel.Append(TextInput);
        Append(panel);

        Recalculate();
    }
}