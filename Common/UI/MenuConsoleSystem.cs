using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Microsoft.Xna.Framework.Input;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace WorldGenTesting.Common.UI;

public class MenuConsoleSystem : ModSystem
{    
    public class Test
    {
        public readonly string Name;
        public readonly string Description;
        public readonly Func<string[], string>[] TestCallbacks;
        internal readonly string AddedBy;
        
        /// <param name="mod">an instance of your mod</param>
        /// <param name="testCallback">the function that will be executed as a test. inputs all other params from input and output null if success, and any message if it's not</param>
        /// <param name="name">main name for command, can be called using this name. ex. "sampletest"</param>
        /// <param name="description">basic description of the test</param>
        public Test(Mod mod, Func<string[], string>[] testCallback, string name, string description = null)
        {
            Name = name;
            Description = description;
            TestCallbacks= testCallback ?? throw new ArgumentNullException(nameof(testCallback));
            AddedBy = mod.DisplayName;
        }
    }
    
    public class Command
    {
        public readonly string Name;
        public readonly string[] Shorthands;
        public readonly string Description;
        public readonly Action<string[]> Callback;
        internal readonly string AddedBy;
        
        /// <param name="mod">an instance of your mod</param>
        /// <param name="callback">the function that will be executed. provides all other values from input, and any output to console must happen within callback</param>
        /// <param name="name">main name for command, can be called using this name. ex. "test"</param>
        /// <param name="shorthands">any variations of the name to be used when calling from console. ex. ["t"]</param>
        /// <param name="description">basic description of the command, include any params or flags. ex. "executes a test using a given name"</param>
        public Command(Mod mod, Action<string[]> callback,string name, string[] shorthands = null, string description = null) 
        {
            Name = name;
            Shorthands = shorthands ?? [];
            Description = description;
            Callback = callback ?? throw new ArgumentNullException(nameof(callback));
            AddedBy = mod.DisplayName;
        }
        
        /// <returns>whether the string maps to this command</returns>
        public bool IsThisCommand(string input)
        {
            if (Name == input) return true;
            return Shorthands != null && Shorthands.Contains(input);
        }
    }
    
    public class ParentPanel : UIPanel
    {        
        public override void Recalculate()
        {
            base.Recalculate();
            // encountered issues with 4k screens, changing max values seemed to alleviate the issue
            MaxWidth.Set(float.MaxValue, 0);
            MaxHeight.Set(float.MaxValue, 0);
        }
    }
    
    public class TextInput(string hintText) : UIElement
    {
        private int _textBlinkerCount;
        private string _currentString = string.Empty;

        public delegate void EventHandler(object sender, EventArgs e);
        public event EventHandler OnTextChange;

        public void SetText(string newText) 
        {
            if (newText != _currentString) {
                _currentString = newText;
                OnTextChange?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            string displayString = _currentString;
            if (++_textBlinkerCount / 20 % 2 == 0)
                displayString += "|";

            CalculatedStyle space = GetDimensions();
            if (_currentString.Length == 0) {
                Utils.DrawBorderString(spriteBatch, hintText, new Vector2(space.X, space.Y), Color.Gray, 2.2f);
            }
            else {
                Utils.DrawBorderString(spriteBatch, displayString, new Vector2(space.X, space.Y), Color.White, 2.2f);
            }
        }
    }
        
    public class MenuConsole : UIState
    {
        public ParentPanel Panel;
        public TextInput Input;
        public UIText Output;
        
        public override void OnInitialize() {
            Panel = new ParentPanel();
            Append(Panel);
            
            Input = new TextInput("type commands here");
            Panel.Append(Input);

            Output = new UIText(string.Empty, 0.8f);
            Panel.Append(Output);
            
            UpdateSize();
        }
        
        public void UpdateSize()
        {
            int width = Main.instance.Window.ClientBounds.Width;
            int height = Main.instance.Window.ClientBounds.Height;
            Panel.Width.Set(width * 0.8f, 0);
            Panel.Height.Set(height * 0.8f, 0);
            Panel.Left.Set(width * 0.1f, 0);
            Panel.Top.Set(height * 0.05f, 0);
            
            Input.Left.Set(width * 0.01f, 0);
            Input.Top.Set(height * 0.75f, 0);
            Input.OverflowHidden = true;
            
            // Output.Left.Set(width * -0.053f, 0);
            // Output.Top.Set(height * -0.02f, 0);
            Output.Left.Set(0, -0.047f);
            Output.Top.Set(0, -0.015f);
            Output.OverflowHidden = true;
        }
    }
    
    private UserInterface _interface;
    private MenuConsole _modConsole;
    
    private string _currentInput = string.Empty;
    private readonly List<string> _inputHistory = [string.Empty];
    
    /// <summary>
    /// 0 represents a new blank input, 1 is their most recent input, the last element represents the first input the user put in
    /// </summary>
    private int _inputHistoryIndex = 0;
    public string CurrentOutput = string.Empty;
    
    public readonly List<Command> Commands = [];
    public readonly List<Test> Tests = [];

    private Keys[] _lastPressedKeys = [];
    
    public override void Load()
    {
        On_Main.UpdateMenu += Main_UpdateMenu;
        On_Main.DrawMenu += Main_DrawMenu;
        
        if (!Main.dedServ) {
            _interface = new UserInterface();
            _modConsole = new MenuConsole();
            _modConsole.Activate();
        }
        
        // add built-in commands
        AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            inputStrings =>
            {
                string selfName = ModContent.GetInstance<WorldGenTesting>().DisplayName;
                string output = "currently loaded commands:\n\n";
                foreach (var command in Commands)
                {
                    output += $" - {command.Name} ";
                    if (command.Shorthands.Length > 0)
                    {
                        output += "(";
                        foreach (var shorthand in command.Shorthands)
                            output += $"{shorthand}, ";
                        output = output.Remove(output.Length - 2, 2); // remove trailing comma and space
                        output += ")";
                    }
                    output += "\n";
                    if (command.Description != null)
                        output += $"{command.Description}\n";
                    
                    if (command.AddedBy == selfName)
                        output += $"(built-in command)\n\n";
                    else
                        output += $"(added by {command.AddedBy})\n\n";
                }
                // remove trailing newlines
                SendToOutput(output.Remove(output.Length - 2, 2));
            },
            "help", ["h"], "displays description of every currently loaded command"
        ));

        AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            inputStrings =>
            {
                if (inputStrings.Length is 0)
                {
                    SendToOutput("an argument (the test's name) is required");
                    return;
                }

                // find test corresponding with name
                Test foundTest = Tests.FirstOrDefault(test => inputStrings[0] == test.Name);

                if (foundTest == null)
                    SendToOutput($"test of name \"{inputStrings[0]}\" not found");
                
                string[] testArgs = inputStrings.Skip(1).ToArray();
                string output = string.Empty;
                for (var i = 0; i < foundTest!.TestCallbacks.Length; i++)
                {
                    string result = foundTest!.TestCallbacks[i](testArgs);
                    output += $"test #{i + 1} results -";
                    if (result is null)
                        output += "success\n\n";
                    else
                        output += $"failure\n{result}\n\n";
                }
                // remove trailing newlines
                SendToOutput(output.Remove(output.Length - 2, 2));

            },
            "test", ["t"], "runs unit test with given name"
        ));

        AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            inputStrings =>
            {
                ClearOutput();
            },
            "clear", ["c"], "clears the console output"
        ));
    }

    public override void Unload()
    {
        On_Main.UpdateMenu -= Main_UpdateMenu;
        On_Main.DrawMenu -= Main_DrawMenu;
        _modConsole = null;
    }
    
    private void Main_UpdateMenu(On_Main.orig_UpdateMenu orig)
    {
        orig();
        
        KeyboardState keyboardState = Keyboard.GetState();
        var pressedKeys = keyboardState.GetPressedKeys();
        var justPressedKeys = pressedKeys; // an array of keys that just got pressed within in the last frame
        justPressedKeys = justPressedKeys.Except(_lastPressedKeys).ToArray();
        _lastPressedKeys = pressedKeys;
        
        if (justPressedKeys.Contains(Keys.OemTilde))
            ToggleConsole();
        
        if (_interface.IsVisible)
        {
            _interface.Update(Main.gameTimeCache);

            Terraria.GameInput.PlayerInput.WritingText = true;
            Main.instance.HandleIME();
            string newString = Main.GetInputText(_currentInput);
            newString = newString.Replace("`", "");
            
            if (keyboardState.PressingControl() && justPressedKeys.Contains(Keys.C))
            {
                SendToOutput(" >> " + _currentInput + "C^");
                ClearInput();
                return;
            }
            
            if (justPressedKeys.Contains(Keys.Enter))
            {                
                if (_currentInput != string.Empty && (_inputHistory.Count == 1 || _inputHistory[1] != _currentInput))
                {
                    _inputHistory.Insert(1, _currentInput);
                }

                _inputHistoryIndex = 0;
                
                SendToOutput(" >> " + _currentInput);
                if (_currentInput != string.Empty && !ProcessCommand(_currentInput))
                    SendToOutput($"command \"{_currentInput}\" not recognized");
                ClearInput();
                return;
            }
            
            var arrowKey = 0; // represents which arrow key got pressed this frame
            if (justPressedKeys.Contains(Keys.Up))
                arrowKey = 1;
            if (justPressedKeys.Contains(Keys.Down))
                arrowKey = -1;
            
            if (arrowKey != 0)
            {
                _inputHistoryIndex += arrowKey;
                if (_inputHistoryIndex < 0)
                    _inputHistoryIndex = 0;
                if (_inputHistoryIndex > _inputHistory.Count - 1)
                    _inputHistoryIndex = _inputHistory.Count - 1;
                _currentInput = _inputHistory[_inputHistoryIndex];
                _modConsole.Input.SetText(_currentInput);
            }
            else if (newString != _currentInput)
            {
                _currentInput = newString;
                _modConsole.Input.SetText(newString);
            }
        }
    }
    
    private void Main_DrawMenu(On_Main.orig_DrawMenu orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
        
        if (_interface?.IsVisible == true)
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise
            );
            _interface.Draw(Main.spriteBatch, Main.gameTimeCache);
            Main.spriteBatch.End();
        }
    }
        
    private Command FindCommand(string name)
    {
        foreach (var command in Commands)
            if (command.Name == name || command.Shorthands.Contains(name))
                return command;
        return null;
    }
        
    /// <returns>true if command successfully found. false otherwise</returns>
    private bool ProcessCommand(string input)
    {
        string[] splitInput = input.Split(' ');

        Command command = FindCommand(splitInput[0]);
        if (command == null)
            return false;
        splitInput = splitInput.Skip(1).ToArray();
        command.Callback(splitInput);
        return true;
    }
    
    private void ClearInput() 
    {
        _currentInput = string.Empty;
        _modConsole.Input.SetText(_currentInput);
    }

    /// <summary>
    /// Toggles console visibility
    /// </summary>
    public void ToggleConsole()
    {        
        _modConsole.UpdateSize();
        if (_interface?.CurrentState is null)
        {
            ClearInput();
            _interface?.SetState(_modConsole);
            if (_interface != null) _interface.IsVisible = true;
        }
        else
        {
            _interface?.SetState(null);
            if (_interface != null) _interface.IsVisible = false;
        }
    }
    
    /// <summary>
    /// Adds message to console. Intended for use inside command callbacks.
    /// </summary>
    /// <param name="text">text to output</param>
    public void SendToOutput(string text)
    {
        CurrentOutput += $"{text}\n";
        _modConsole.Output.SetText(CurrentOutput);
    }
    
    /// <summary>
    /// Clears the console output
    /// </summary>
    public void ClearOutput()
    {
        CurrentOutput = string.Empty;
        _modConsole.Output.SetText(CurrentOutput);
    }
        
    /// <summary>
    /// Adds command to console
    /// </summary>
    /// <param name="command">the command to be added</param>
    /// <returns>whether adding the command was successful. If false, typically because of duplicate names/shorthands</returns>
    public bool AddCommand(Command command)
    {
        // ensure the name/shorthand doesn't already exist
        if (FindCommand(command.Name) != null)
        {
            string msg = $"command \"{command.Name}\" already exists, either as any other shorthand or name. skipping...";
            ModContent.GetInstance<WorldGenTesting>().Logger.Warn(msg);
            SendToOutput(msg);
            return false;
        }
        foreach (string shorthand in command.Shorthands)
            if (FindCommand(shorthand) != null)
            {
                string msg = $"command \"{command.Name}\" shorthand \"{shorthand}\" already exists, either as any other shorthand or name. skipping...";
                ModContent.GetInstance<WorldGenTesting>().Logger.Warn(msg);
                SendToOutput(msg);
                return false;
            }
        Commands.Add(command);
        return true;
    }
    
    /// <summary>
    /// Adds unit test to test command
    /// </summary>
    /// <param name="test">the test to be added</param>
    /// <returns>whether adding the test was successful. If false, typically because of duplicate names</returns>
    public bool AddTest(Test test)
    {
        if (Tests.Any(existingTest => existingTest.Name == test.Name))
        {
            string msg = $"test name \"{test.Name}\" already exists. skipping...";
            ModContent.GetInstance<WorldGenTesting>().Logger.Warn(msg);
            SendToOutput(msg);
            return false;
        }
        Tests.Add(test);
        return true;
    }
}