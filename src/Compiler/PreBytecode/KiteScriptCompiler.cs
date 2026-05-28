using System;
using System.Linq;
using System.Collections.Generic;
using PaintPower.Runtime.Bytecode;
using PaintPower.Vm.Processing;
using PaintPower.Logging;

namespace PaintPower.Compiler.PreBytecode
{
    /// <summary>
    /// Compiles KiteScript AST nodes into PaintPower bytecode.
    /// Handles variables, blocks, calls, and if/else control flow.
    /// </summary>
    public static class KiteScriptCompiler
    {
        // ------------------------------------------------------------
        // Resolve a variable name to a local slot index.
        // Searches from innermost scope outward.
        // ------------------------------------------------------------
        private static int ResolveLocal(string id, Stack<Dictionary<string, int>> scopes)
        {
            foreach (var scope in scopes.Reverse())
                if (scope.TryGetValue(id, out int slot))
                    return slot;

            throw new Exception("Unknown variable: " + id);
        }

        // ------------------------------------------------------------
        // Compile a single AST node.
        // Used by both the top-level compiler loop and nested structures.
        // ------------------------------------------------------------
        private static void CompileNode(KsNode node, InstructionSet set, Stack<Dictionary<string, int>> scopes)
        {
            switch (node)
            {
                case KsBlockStart:
                    scopes.Push(new Dictionary<string, int>());
                    break;

                case KsBlockEnd:
                    scopes.Pop();
                    break;

                case KsInstruction inst:
                    if (inst.Name == "define" && inst.Params[0] == "var")
                    {
                        string id = inst.Params[1];
                        int slot = set.AddLocal();
                        scopes.Peek()[id] = slot;
                    }
                    break;

                case KsCall call:
                    BuiltinRegistry.TryEmit(call.TargetId, set, call.Args);
                    break;

                case KsSet setNode:
                    int slotSet = ResolveLocal(setNode.Id, scopes);
                    int constIndex = set.AddConstant(setNode.Value);
                    set.Emit(OpCode.LoadConst, constIndex);
                    set.Emit(OpCode.StoreLocal, slotSet);
                    break;

                case KsGet getNode:
                    int slotGet = ResolveLocal(getNode.Id, scopes);
                    set.Emit(OpCode.LoadLocal, slotGet);
                    break;

                case KsIf ifNode:
                    CompileIf(ifNode, set, scopes);
                    break;

                case KsElse elseNode:
                    foreach (var n in elseNode.Body)
                        CompileNode(n, set, scopes);
                    break;
            }
        }

        // ------------------------------------------------------------
        // Compile an if node.
        // Emits conditional jumps and compiles nested bodies.
        // ------------------------------------------------------------
        private static void CompileIf(KsIf ifNode, InstructionSet set, Stack<Dictionary<string, int>> scopes)
        {
            // Compile condition expression
            CompileCondition(ifNode.ConditionRaw, set, scopes);

            // Emit JumpIfFalse with placeholder
            int jumpFalseIndex = set.EmitWithPlaceholder(OpCode.JumpIfFalse);

            // Compile IF body
            foreach (var n in ifNode.Body)
                CompileNode(n, set, scopes);

            if (ifNode.ElseBranch != null)
            {
                // Jump over ELSE
                int jumpEndIndex = set.EmitWithPlaceholder(OpCode.Jump);

                // Patch JumpIfFalse to ELSE start
                set.PatchJump(jumpFalseIndex);

                // Compile ELSE body
                foreach (var n in ifNode.ElseBranch.Body)
                    CompileNode(n, set, scopes);

                // Patch Jump to end
                set.PatchJump(jumpEndIndex);
            }
            else
            {
                // No ELSE: patch JumpIfFalse to end of IF block
                set.PatchJump(jumpFalseIndex);
            }
        }

        // ------------------------------------------------------------
        // Compile a condition expression.
        // Supports:
        //   - literal booleans
        //   - literal integers
        //   - variable names
        // ------------------------------------------------------------
        private static void CompileCondition(string cond, InstructionSet set, Stack<Dictionary<string, int>> scopes)
        {
            cond = cond.Trim();

            // 1. Check for binary relational operators
            if (TryParseBinaryCondition(cond, out string left, out string op, out string right))
            {
                // Compile left operand
                CompileCondition(left, set, scopes);

                // Compile right operand
                CompileCondition(right, set, scopes);

                // Emit comparison opcode
                switch (op)
                {
                    case "==": set.Emit(OpCode.CompareEqual); break;
                    case "!=": set.Emit(OpCode.CompareNotEqual); break;
                    case "<": set.Emit(OpCode.CompareLess); break;
                    case ">": set.Emit(OpCode.CompareGreater); break;
                    case "<=": set.Emit(OpCode.CompareLessEqual); break;
                    case ">=": set.Emit(OpCode.CompareGreaterEqual); break;
                }

                return;
            }

            // 2. Literal boolean
            if (cond == "true" || cond == "false")
            {
                int c = set.AddConstant(cond == "true");
                set.Emit(OpCode.LoadConst, c);
                return;
            }

            // 3. Literal integer
            if (int.TryParse(cond, out int num))
            {
                int c = set.AddConstant(num);
                set.Emit(OpCode.LoadConst, c);
                return;
            }

            // 4. Variable reference
            int slot = ResolveLocal(cond, scopes);
            set.Emit(OpCode.LoadLocal, slot);
        }

        private static bool TryParseBinaryCondition(
    string cond,
    out string left,
    out string op,
    out string right)
        {
            string[] ops = { "==", "!=", "<=", ">=", "<", ">" };

            foreach (var o in ops)
            {
                int idx = cond.IndexOf(o, StringComparison.Ordinal);
                if (idx > 0)
                {
                    left = cond.Substring(0, idx).Trim();
                    op = o;
                    right = cond.Substring(idx + o.Length).Trim();
                    return true;
                }
            }

            left = right = op = "";
            return false;
        }

        // ------------------------------------------------------------
        // Entry point: compile KiteScript text into bytecode.
        // ------------------------------------------------------------
        public static Bytecode Compile(string text)
        {
            var nodes = KiteScriptParser.Parse(text);
            var set = new InstructionSet();

            Stack<Dictionary<string, int>> scopes = new();
            scopes.Push(new Dictionary<string, int>());

            foreach (var node in nodes)
                CompileNode(node, set, scopes);

            set.Emit(OpCode.Return);

            return set.ToBytecode();
        }
    }
}
