using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;

namespace WorldGenTesting.UI;

internal class CommandTextInput(float textScale) : UIPanel {
    private readonly float _textScale = textScale;
    private string _currentString = string.Empty;
    private int _textBlinkerCount;
    internal bool IsCancelingCommand;

    internal bool IsExecutingCommand;

    public void SetText(string newText) {
        _currentString = newText;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch) {
        var displayString = _currentString;
        if (++_textBlinkerCount / 20 % 2 == 0) displayString += "|";

        var space = GetDimensions();
        if (IsCancelingCommand)
            Utils.DrawBorderString(spriteBatch, "cancelling command...", new Vector2(space.X, space.Y), Color.Gray,
                _textScale);
        else if (IsExecutingCommand)
            Utils.DrawBorderString(spriteBatch, "command currently being executed. cancel with ctrl+c",
                new Vector2(space.X, space.Y), Color.Gray, _textScale);
        else
            Utils.DrawBorderString(spriteBatch, $"> {displayString}", new Vector2(space.X, space.Y), Color.White,
                _textScale);
    }
}