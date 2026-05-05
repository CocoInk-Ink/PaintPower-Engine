// KsNode.cs

using System.Collections.Generic;

namespace PaintPower.Compiler.PreBytecode;

public abstract class KsNode {}

public sealed class KsInstruction : KsNode
{
    public string Name { get; init; } = "";
    public List<string> Params { get; } = new();
}

public sealed class KsCall : KsNode
{
    public string TargetId { get; init; } = "";
    public List<string> Args { get; } = new();
}
