#nullable enable
using System;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using WorldGenTesting.Helpers;
using WorldGenTesting.Types;

namespace WorldGenTesting;

public class BuiltInCommands : ModSystem {
    public override void Load() {
        var mod = ModContent.GetInstance<WorldGenTesting>();

        mod.AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            inputStrings => {
                if (inputStrings.Length != 0) {
                    // output just one command
                    var targetCommand = mod.GetCommand(inputStrings[0]);
                    if (targetCommand is null)
                        mod.SendToOutput(
                            $"command not found with the given name/callback of \"{inputStrings[0]}\"");
                    else
                        mod.SendToOutput(targetCommand.ToString());
                }
                else {
                    // list all commands
                    var output = "currently loaded commands:\n\n";
                    foreach (var command in mod.Commands) output += command + "\n\n";
                    output = output.Remove(output.Length - 2, 2); // remove trailing newlines
                    mod.SendToOutput(output);
                }
            },
            "help", ["h"], "help <command_name>",
            "displays description of every currently loaded command. Pass the name/shorthand of a command to view only that command."
        ));

        mod.AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            inputStrings => {
                if (inputStrings.Length is 0) {
                    mod.SendToOutput("an argument (name) is required");
                    return;
                }

                // find test corresponding with name
                var foundTest = mod.Tests.FirstOrDefault(test => inputStrings[0] == test.Name);

                if (foundTest is null) mod.SendToOutput($"test of name \"{inputStrings[0]}\" not found");

                var runCount = 1;
                if (inputStrings.Length >= 2)
                    try {
                        runCount = int.Parse(inputStrings[1]);
                    }
                    catch (FormatException) {
                        mod.SendToOutput(
                            "param after name should be a number, for attempt counts. number assumed to be 1");
                    }

                mod.SendToOutput($"executing test {foundTest!.Name}, {runCount} times");
                for (var i = 0; i < runCount; i++) {
                    var output = string.Empty;
                    if (mod.IsCancelingCommand) return;

                    try {
                        var result = foundTest.TestCallback();

                        output += $"test #{i + 1} results - ";
                        if (result is null)
                            output += "success\n";
                        else
                            output += $"failure\n{result}\n";
                    }

                    catch (Exception e) {
                        output += $"failure. Test callbacks threw an unhandled exception. Exception:\n{e}";
                    }

                    // remove trailing newlines
                    mod.SendToOutput(output.Remove(output.Length - 1, 1));
                }
            },
            "test", ["t"], "test <name> <counts> <test parameters>", "runs test with given name. counts is optional, defaults to 1"
        ));

        mod.AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            inputStrings => {
                string? filter = null;
                if (inputStrings.Length >= 1) filter = inputStrings[0];

                var output = filter is null
                    ? "currently loaded tests:\n\n"
                    : $"currently loaded tests with \"{filter}\" in their name:\n\n";

                foreach (var test in mod.Tests)
                    if (filter is not null) {
                        if (test.Name.Contains(filter)) output += test + "\n\n";
                    }
                    else {
                        output += test + "\n\n";
                    }

                // remove trailing newlines
                output = output.Remove(output.Length - 2, 2);
                mod.SendToOutput(output);
            },
            "list_tests", ["lt"], "list_tests <filter>",
            "displays every loaded command. filter is optional, filters by name"
        ));

        mod.AddCommand(new Command(
            ModContent.GetInstance<WorldGenTesting>(),
            _ => { mod.ClearOutput(); },
            "clear", ["c"], "clear", "clears the console output"
        ));
    }
}