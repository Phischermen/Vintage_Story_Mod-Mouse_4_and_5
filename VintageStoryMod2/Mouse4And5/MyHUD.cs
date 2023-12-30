using Vintagestory.API.Client;

namespace VintageStoryMod2;

public class MyHUD : HudElement
{
    private static string Hex(bool on) => on ? "#FFFF00" : "#FFFFFF";
    private static string HudText(string icon, bool on, string hk) => $"<font size='10' color='{Hex(on)}' align='center'>{icon}</font><br><font size='25' color='{Hex(on)}' align='center'><hk>{hk}</hk></font>";
    public void SetupDialog()
    {
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedPosition(-10, 0);
        
        if (this.SingleComposer?.Composed != null)
        {
            this.SingleComposer.Clear(dialogBounds);
        }
        
        ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, 150, 60);
        ElementBounds textBounds1 = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0, 0, 75, 60);
        ElementBounds textBounds2 = ElementBounds.Fixed(EnumDialogArea.LeftTop, 75, 0, 75, 60);

        this.SingleComposer = capi.Gui.CreateCompo(Mouse4And5.ModId, dialogBounds)
                .AddDialogBG(textBounds, false)
                .AddRichtext(string.Empty, CairoFont.WhiteSmallishText(), textBounds1, "text1")
                .AddRichtext(string.Empty, CairoFont.WhiteSmallishText(), textBounds2, "text2")
                .Compose()
            ;
    }

    public MyHUD(ICoreClientAPI capi) : base(capi)
    {
        SetupDialog();
        UpdateHudText();
        Mouse4And5.Mouse4DownChanged += UpdateHudText;
        Mouse4And5.Mouse5DownChanged += UpdateHudText;
    }

    public override void Dispose()
    {
        base.Dispose();
        Mouse4And5.Mouse4DownChanged -= UpdateHudText;
        Mouse4And5.Mouse5DownChanged -= UpdateHudText;
    }

    public void UpdateHudText()
    {
        var text1 = this.SingleComposer.GetElement("text1") as GuiElementRichtext;
        if (text1 != null)
        {
            text1.SetNewText(HudText(Mouse4And5.I18N("mouse4"), Mouse4And5.Mouse4Down, "shift"), CairoFont.WhiteSmallishText());
        }
        
        var text2 = this.SingleComposer.GetElement("text2") as GuiElementRichtext;
        if (text2 != null)
        {
            text2.SetNewText(HudText(Mouse4And5.I18N("mouse5"), Mouse4And5.Mouse5Down, "ctrl"), CairoFont.WhiteSmallishText());
        }
    }
}