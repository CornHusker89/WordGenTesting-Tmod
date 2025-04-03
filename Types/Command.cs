#nullable enable
using System;
using System.Linq;
using Terraria.ModLoader;

namespace WorldGenTesting.Types;

public class Command {
    internal readonly string AddedBy;
    public readonly Action<string[]> Callback;
    public readonly string? Description;
    public readonly string Name;
    public readonly string[] Shorthands;
    public readonly string? Usage;

    /// <param name="mod">an instance of your mod</param>
    /// <param name="callback">
    ///     the function that will be executed. provides all other values from input, and any output to
    ///     console must happen within callback
    /// </param>
    /// <param name="name">main name for command, can be called using this name. ex. "test"</param>
    /// <param name="shorthands">any variations of the name to be used when calling from console. ex. ["t"]</param>
    /// <param name="usage">an example of the command, showcasing parameters. ex "test name (counts)"</param>
    /// <param name="description">
    ///     basic description of the command, include any params or flags. ex. "executes a test using a
    ///     given name"
    /// </param>
    public Command(Mod mod, Action<string[]> callback, string name, string[]? shorthands = null, string? usage = null,
        string? description = null) {
        Name = name;
        Shorthands = shorthands ?? [];
        Usage = usage;
        Description = description;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        AddedBy = $"{mod.DisplayName} ({mod.Name})";
    }

    /// <summary>
    ///     finds the command
    /// </summary>
    /// <returns>whether the string maps to this command</returns>
    public bool IsThisCommand(string input) {
        if (Name == input) return true;
        return Shorthands.Contains(input);
    }

    public override string ToString() {
        var output = $" - {Name} ";
        if (Shorthands.Length > 0) {
            output += "(";
            foreach (var shorthand in Shorthands)
                output += $"{shorthand}, ";
            output = output.Remove(output.Length - 2, 2); // remove trailing comma and space
            output += ")";
        }

        output += "\n";
        if (Usage is not null) {
            output += $"usage: {Usage}";
            output += "\n";
        }

        if (Description != null)
            output += $"{Description}\n";

        if (Name is "help" or "test" or "list_tests" or "clear" or "delete_world")
            output += "built-in command";
        else
            output += $"added by {AddedBy}";

        return output;
    }
}