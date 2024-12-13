#nullable enable
using System;
using System.Linq;
using Terraria.ModLoader;

namespace WorldGenTesting.Helpers;

public class Command
{
    public readonly string Name;
    public readonly string[]? Shorthands;
    public readonly string? Description;
    public readonly Action<string[]> Callback;
    internal readonly string AddedBy;
    
    /// <param name="mod">an instance of your mod</param>
    /// <param name="callback">the function that will be executed. provides all other values from input, and any output to console must happen within callback</param>
    /// <param name="name">main name for command, can be called using this name. ex. "test"</param>
    /// <param name="shorthands">any variations of the name to be used when calling from console. ex. ["t"]</param>
    /// <param name="description">basic description of the command, include any params or flags. ex. "executes a test using a given name"</param>
    public Command(Mod mod, Action<string[]> callback,string name, string[]? shorthands = null, string? description = null) 
    {
        Name = name;
        Shorthands = shorthands ?? [];
        Description = description;
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        AddedBy = $"{mod.DisplayName} ({mod.Name})";
    }
    
    /// <returns>whether the string maps to this command</returns>
    public bool IsThisCommand(string input)
    {
        if (Name == input) return true;
        return Shorthands != null && Shorthands.Contains(input);
    }
}