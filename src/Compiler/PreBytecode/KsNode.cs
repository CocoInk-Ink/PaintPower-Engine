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

public sealed class KsBlockStart : KsNode {}
public sealed class KsBlockEnd : KsNode {}

public sealed class KsSet : KsNode
{
    public string Id { get; init; } = "";
    public string Value { get; init; } = "";
}

public sealed class KsGet : KsNode
{
    public string Id { get; init; } = "";
}

public sealed class KsIf : KsNode
{
    public string ConditionRaw { get; init; } = "";
    public List<KsNode> Body { get; } = new();
    public KsElse? ElseBranch { get; set; }
}

public sealed class KsElse : KsNode
{
    public List<KsNode> Body { get; } = new();
}
