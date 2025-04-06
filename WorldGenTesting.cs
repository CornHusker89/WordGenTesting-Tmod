#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;
using WorldGenTesting.Types;
using WorldGenTesting.UI;

namespace WorldGenTesting;

public class WorldGenTesting : Mod {
    private readonly Queue<string> _consoleMessageQueue = new();
    private readonly MenuConsoleState _consoleUi = new();

    private readonly List<string> _inputHistory = [string.Empty];

    private readonly UserInterface _interface = new();
    public readonly List<Command> Commands = [];
    public readonly List<Test> Tests = [];

    /// <summary>Flag to trigger clearing the UIList on the next menu update.</summary>
    private bool _clearOutput;

    /// <summary>
    ///     0 represents a new blank input, 1 is their most recent input. The last element represents the first input the user
    ///     put in.
    /// </summary>
    private int _curInputHistoryIndex;

    private string _currentInput = string.Empty;

    private bool _isCancelingCommand;

    private bool _isExecutingCommand;

    private Keys[] _lastPressedKeys = [];

    /// <summary>Flag to trigger scrolling to the bottom of the UIList on the next menu update.</summary>
    private bool _scrollDown;

    public bool IsExecutingCommand {
        get => _isExecutingCommand;
        set {
            _isExecutingCommand = value;
            _consoleUi.TextInput.IsExecutingCommand = value;
        }
    }

    public bool IsCancelingCommand {
        get => _isCancelingCommand;
        set {
            _isCancelingCommand = value;
            _consoleUi.TextInput.IsCancelingCommand = value;
        }
    }

    public override void Load() {
        On_Main.UpdateMenu += Main_UpdateMenu;
        On_Main.DrawMenu += Main_DrawMenu;

        _consoleUi.Activate();
    }

    public override void Unload() {
        On_Main.UpdateMenu -= Main_UpdateMenu;
        On_Main.DrawMenu -= Main_DrawMenu;
    }

    private void Main_UpdateMenu(On_Main.orig_UpdateMenu orig) {
        orig();

        if (_scrollDown) {
            _consoleUi.Scrollbar.ViewPosition = _consoleUi.ScrollingList.GetTotalHeight() - 1;
            _scrollDown = false;
        }

        if (_consoleMessageQueue.Count > 0) {
            while (_consoleMessageQueue.Count > 0) {
                var text = _consoleMessageQueue.Dequeue();
                var output = new UIText(string.Empty, 0.8f);
                output.Height.Set(22.5f * (text.Split('\n').Length + 1), 0f);
                output.SetText(text);
                _consoleUi.ScrollingList.Add(output);
            }

            _scrollDown = true;
            _consoleUi.Scrollbar.Update(Main.gameTimeCache);
            _consoleUi.ScrollingList.Update(Main.gameTimeCache);
        }

        if (_clearOutput) {
            _consoleUi.ScrollingList.Clear();
            _clearOutput = false;
        }

        var keyboardState = Keyboard.GetState();
        var pressedKeys = keyboardState.GetPressedKeys();
        var justPressedKeys = pressedKeys; // an array of keys that just got pressed within in the last frame
        justPressedKeys = justPressedKeys.Except(_lastPressedKeys).ToArray();
        _lastPressedKeys = pressedKeys;

        if (justPressedKeys.Contains(Keys.OemTilde)) ToggleConsole();

        if (!_interface.IsVisible) return;

        PlayerInput.WritingText = true;
        Main.instance.HandleIME();

        if (keyboardState.PressingControl() && justPressedKeys.Contains(Keys.C)) {
            SendToOutput(" >> " + _currentInput + "C^");
            ClearInput();
            CancelCommand();
            return;
        }

        if (IsExecutingCommand || IsCancelingCommand) return;

        var newString = Main.GetInputText(_currentInput);
        newString = newString.Replace("`", "");
        if (newString != _currentInput) {
            _currentInput = newString;
            _consoleUi.TextInput.SetText(newString);
        }

        if (justPressedKeys.Contains(Keys.Enter)) {
            if (_currentInput != string.Empty && (_inputHistory.Count == 1 || _inputHistory[1] != _currentInput))
                _inputHistory.Insert(1, _currentInput);

            _curInputHistoryIndex = 0;

            SendToOutput(" >> " + _currentInput);
            if (_currentInput != string.Empty && !ProcessCommand(_currentInput))
                SendToOutput($"command \"{_currentInput}\" not recognized");
            ClearInput();
            return;
        }

        var indexChange = 0; // represents which arrow key got pressed this frame
        if (justPressedKeys.Contains(Keys.Up)) indexChange = 1;
        if (justPressedKeys.Contains(Keys.Down)) indexChange = -1;

        if (indexChange != 0) {
            _curInputHistoryIndex += indexChange;
            if (_curInputHistoryIndex < 0) _curInputHistoryIndex = 0;
            if (_curInputHistoryIndex > _inputHistory.Count - 1) _curInputHistoryIndex = _inputHistory.Count - 1;
            _currentInput = _inputHistory[_curInputHistoryIndex];
            _consoleUi.TextInput.SetText(_currentInput);
        }
    }

    private void Main_DrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime) {
        if (_interface.IsVisible) {
            _interface.Draw(Main.spriteBatch, gameTime);
            _interface.Update(gameTime);

            Main.DrawThickCursor();
            Main.DrawCursor(new Vector2(2, 2));

            Main.spriteBatch.End();
        }
        else {
            orig(self, gameTime);
        }
    }

    /// <summary>
    ///     Gets command based on given string, null if nothing is found.
    /// </summary>
    internal Command? GetCommand(string name) {
        foreach (var command in Commands)
            if (command.IsThisCommand(name))
                return command;
        return null;
    }

    #region Console Functions

    /// <summary>
    ///     Calls command callback from command string in a separate thread.
    /// </summary>
    /// <returns>true if command successfully found. false otherwise</returns>
    internal bool ProcessCommand(string input) {
        var splitInput = input.Split(' ');

        var command = GetCommand(splitInput[0]);
        if (command is null) return false;
        splitInput = splitInput.Skip(1).ToArray();
        Task.Run(() => {
            IsExecutingCommand = true;
            command.Callback(splitInput);
            IsExecutingCommand = false;
            IsCancelingCommand = false;
        });

        return true;
    }

    /// <summary>
    ///     Clears the current user input.
    /// </summary>
    public void ClearInput() {
        _currentInput = string.Empty;
        _consoleUi.TextInput.SetText(_currentInput);
    }

    /// <summary>
    ///     Toggles console visibility.
    /// </summary>
    public void ToggleConsole() {
        if (_interface.CurrentState is null) {
            _interface.SetState(_consoleUi);
            _interface.IsVisible = true;
        }
        else {
            _interface.SetState(null);
            _interface.IsVisible = false;
        }
    }

    /// <summary>
    ///     Adds message to console.
    /// </summary>
    /// <param name="text">text to output</param>
    public void SendToOutput(string text) {
        _consoleMessageQueue.Enqueue(text);
    }

    /// <summary>
    ///     Tries to cancel the currently executing command. Will do nothing if no command is being executed.
    /// </summary>
    /// <remarks>
    ///     For tests, will only cancel between callbacks, no matter how long the callback takes.
    /// </remarks>
    public void CancelCommand() {
        if (!IsExecutingCommand) return;
        IsCancelingCommand = true;
    }

    /// <summary>
    ///     Clears the console output
    /// </summary>
    public void ClearOutput() {
        _clearOutput = true;
    }

    /// <summary>
    ///     Adds command to console
    /// </summary>
    /// <param name="command">the command to be added</param>
    /// <returns>whether adding the command was successful. If false, typically because of duplicate names/shorthands</returns>
    public bool AddCommand(Command command) {
        // ensure the name/shorthand doesn't already exist
        if (GetCommand(command.Name) != null) {
            var msg = $"command \"{command.Name}\" already exists, either as any other shorthand or name. skipping...";
            ModContent.GetInstance<WorldGenTesting>().Logger.Warn(msg);
            SendToOutput(msg);
            return false;
        }

        foreach (var shorthand in command.Shorthands)
            if (GetCommand(shorthand) != null) {
                var msg =
                    $"command \"{command.Name}\" shorthand \"{shorthand}\" already exists, either as another name or shorthand. skipping...";
                ModContent.GetInstance<WorldGenTesting>().Logger.Warn(msg);
                SendToOutput(msg);
                return false;
            }

        Commands.Add(command);
        return true;
    }

    /// <summary>
    ///     Add to test command
    /// </summary>
    /// <param name="test">the test to be added</param>
    /// <returns>whether adding the test was successful. If false, typically because of duplicate names</returns>
    public bool AddTest(Test test) {
        if (Tests.Any(existingTest => existingTest.Name == test.Name)) {
            var msg = $"test name \"{test.Name}\" already exists. skipping...";
            ModContent.GetInstance<WorldGenTesting>().Logger.Warn(msg);
            SendToOutput(msg);
            return false;
        }

        Tests.Add(test);
        return true;
    }

    #endregion
}