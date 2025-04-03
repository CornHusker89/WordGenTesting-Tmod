#nullable enable
using System;
using Terraria.ModLoader;

namespace WorldGenTesting.Types;

public class Test {
    internal readonly string AddedBy;
    public readonly string? Description;
    public readonly string Name;
    public readonly Func<string?>[] TestCallbacks;

    /// <param name="mod">an instance of your mod</param>
    /// <param name="testCallbacks">
    ///     the function that will be executed as a test. inputs all other params from input and output null if success,
    ///     and any message if it's not. has multiple string outputs if you want to have multiple messages(tests)
    /// </param>
    /// <param name="name">main name for command, can be called using this name. ex. "sample_test"</param>
    /// <param name="description">basic description of the test</param>
    public Test(Mod mod, Func<string?>[] testCallbacks, string name, string? description = null) {
        Name = name;
        Description = description;
        TestCallbacks = testCallbacks ?? throw new ArgumentNullException(nameof(testCallbacks));
        AddedBy = mod.DisplayName;
    }

    public override string ToString() {
        var output = $" - {Name} ";
        if (Description != null)
            output += $"{Description}";
        return output;
    }
}