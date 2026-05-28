// KiteScriptParser.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace PaintPower.Compiler.PreBytecode
{
    public static class KiteScriptParser
    {
        // ------------------------------------------------------------
        // TOKEN TYPES
        // ------------------------------------------------------------
        private enum TokenType
        {
            If,
            Else,
            ElseIf,
            BlockStart,
            BlockEnd,
            Call,
            Set,
            Get,
            Instruction
        }

        private readonly record struct Token(TokenType Type, string Data, int LineNumber);

        // ------------------------------------------------------------
        // PUBLIC ENTRY POINT
        // ------------------------------------------------------------
        public static List<KsNode> Parse(string text)
        {
            text = text.Replace("\"\"", "\"");

            var tokens = Tokenize(text);
            int i = 0;
            var nodes = ParseSequence(tokens, ref i);

            // If we ended early, there was an unmatched block
            if (i < tokens.Count && tokens[i].Type == TokenType.BlockEnd)
                throw SyntaxError("Unmatched [/#block]", tokens[i].LineNumber);

            return nodes;
        }

        // ------------------------------------------------------------
        // ERROR HELPER
        // ------------------------------------------------------------
        private static Exception SyntaxError(string message, int line)
        {
            return new Exception($"Syntax error on line {line}: {message}");
        }

        // ------------------------------------------------------------
        // TOKENIZER (NO REGEX)
        // ------------------------------------------------------------
        private static List<Token> Tokenize(string text)
        {
            var tokens = new List<Token>();

            var lines = text.Split('\n')
                            .Select((l, idx) => new { Text = l.Trim(), Line = idx + 1 })
                            .Where(x => x.Text.Length > 0 && !x.Text.StartsWith("@@!"))
                            .ToList();

            foreach (var entry in lines)
            {
                string line = entry.Text;
                int lineNum = entry.Line;

                // BLOCKS
                if (line == "[#block]")
                {
                    tokens.Add(new Token(TokenType.BlockStart, "", lineNum));
                    continue;
                }

                if (line == "[/#block]")
                {
                    tokens.Add(new Token(TokenType.BlockEnd, "", lineNum));
                    continue;
                }

                // IF
                if (line.StartsWith("[#if "))
                {
                    var cond = ExtractBetween(line, "[#if ", "]");
                    tokens.Add(new Token(TokenType.If, cond.Trim(), lineNum));
                    continue;
                }

                // ELSE IF
                if (line.StartsWith("[#else if "))
                {
                    var cond = ExtractBetween(line, "[#else if ", "]");
                    tokens.Add(new Token(TokenType.ElseIf, cond.Trim(), lineNum));
                    continue;
                }

                // ELSE
                if (line == "[#else]")
                {
                    tokens.Add(new Token(TokenType.Else, "", lineNum));
                    continue;
                }

                // CALL
                if (line.StartsWith("[#call "))
                {
                    tokens.Add(new Token(TokenType.Call, line, lineNum));
                    continue;
                }

                // SET
                if (line.StartsWith("[#set "))
                {
                    tokens.Add(new Token(TokenType.Set, line, lineNum));
                    continue;
                }

                // GET
                if (line.StartsWith("[#get "))
                {
                    tokens.Add(new Token(TokenType.Get, line, lineNum));
                    continue;
                }

                // INSTRUCTION
                if (line.StartsWith("[#"))
                {
                    tokens.Add(new Token(TokenType.Instruction, line, lineNum));
                    continue;
                }

                // Anything else is invalid
                throw SyntaxError("Unknown or malformed tag", lineNum);
            }

            return tokens;
        }

        // ------------------------------------------------------------
        // PARSE A SEQUENCE OF STATEMENTS
        // ------------------------------------------------------------
        private static List<KsNode> ParseSequence(List<Token> tokens, ref int i)
        {
            var list = new List<KsNode>();

            while (i < tokens.Count)
            {
                var t = tokens[i];

                if (t.Type == TokenType.BlockEnd)
                    break;

                if (t.Type == TokenType.If)
                {
                    list.Add(ParseIf(tokens, ref i));
                    continue;
                }

                if (t.Type == TokenType.Else)
                    throw SyntaxError("ELSE without matching IF", t.LineNumber);

                if (t.Type == TokenType.ElseIf)
                    throw SyntaxError("ELSE IF without matching IF", t.LineNumber);

                // BLOCK
                if (t.Type == TokenType.BlockStart)
                {
                    i++;
                    list.Add(new KsBlockStart());
                    var inner = ParseSequence(tokens, ref i);
                    list.AddRange(inner);

                    if (i >= tokens.Count || tokens[i].Type != TokenType.BlockEnd)
                        throw SyntaxError("Block not closed with [/#block]", t.LineNumber);

                    i++;
                    list.Add(new KsBlockEnd());
                    continue;
                }

                list.Add(ParseSingle(tokens[i]));
                i++;
            }

            return list;
        }

        // ------------------------------------------------------------
        // PARSE IF / ELSE IF / ELSE
        // ------------------------------------------------------------
        private static KsIf ParseIf(List<Token> tokens, ref int i)
        {
            var ifToken = tokens[i++];
            var ifNode = new KsIf { ConditionRaw = ifToken.Data };

            // Parse IF body
            ParseBody(tokens, ref i, ifNode.Body, ifToken.LineNumber);

            // Parse ELSE IF chain
            while (i < tokens.Count && tokens[i].Type == TokenType.ElseIf)
            {
                var elifToken = tokens[i++];
                var elifNode = new KsIf { ConditionRaw = elifToken.Data };

                ParseBody(tokens, ref i, elifNode.Body, elifToken.LineNumber);

                // Wrap ELSE IF inside an ELSE node
                if (ifNode.ElseBranch == null)
                    ifNode.ElseBranch = new KsElse();

                ifNode.ElseBranch.Body.Add(elifNode);
            }

            // Parse ELSE
            if (i < tokens.Count && tokens[i].Type == TokenType.Else)
            {
                var elseToken = tokens[i++];
                var elseNode = new KsElse();

                ParseBody(tokens, ref i, elseNode.Body, elseToken.LineNumber);

                // ELSE always attaches at the end of the chain
                ifNode.ElseBranch = elseNode;

                ifNode.ElseBranch = elseNode;
            }

            return ifNode;
        }

        // ------------------------------------------------------------
        // PARSE BODY OF IF/ELSE/ELSE IF
        // ------------------------------------------------------------
        private static void ParseBody(List<Token> tokens, ref int i, List<KsNode> body, int line)
        {
            if (i >= tokens.Count)
                throw SyntaxError("IF has no body", line);

            var t = tokens[i];

            // BLOCK BODY
            if (t.Type == TokenType.BlockStart)
            {
                i++;
                body.Add(new KsBlockStart());
                var inner = ParseSequence(tokens, ref i);
                body.AddRange(inner);

                if (i >= tokens.Count || tokens[i].Type != TokenType.BlockEnd)
                    throw SyntaxError("Block not closed with [/#block]", line);

                i++;
                body.Add(new KsBlockEnd());
                return;
            }

            // SINGLE STATEMENT BODY
            body.Add(ParseSingle(tokens[i]));
            i++;
        }

        // ------------------------------------------------------------
        // PARSE LEAF NODES
        // ------------------------------------------------------------
        private static KsNode ParseSingle(Token token)
        {
            return token.Type switch
            {
                TokenType.Call => ParseCall(token.Data, token.LineNumber),
                TokenType.Set => ParseSet(token.Data, token.LineNumber),
                TokenType.Get => ParseGet(token.Data, token.LineNumber),
                TokenType.Instruction => ParseInstruction(token.Data, token.LineNumber),
                _ => throw SyntaxError("Unexpected token", token.LineNumber)
            };
        }

        // ------------------------------------------------------------
        // CALL
        // ------------------------------------------------------------
        private static KsNode ParseCall(string line, int lineNum)
        {
            int close = line.IndexOf(']');
            if (close < 0)
                throw SyntaxError("CALL missing closing ']'", lineNum);

            int openParen = line.IndexOf('(', close);
            if (openParen < 0)
                throw SyntaxError("CALL missing '('", lineNum);

            int closeParen = line.LastIndexOf(')');
            if (closeParen < 0)
                throw SyntaxError("CALL missing ')'", lineNum);

            var id = ExtractBetween(line, "[#call ", "]");
            var argsPart = ExtractBetween(line, "](", ")");
            argsPart = TrimTrailingSemicolon(argsPart);

            // GET inside CALL
            if (argsPart.StartsWith("[#get "))
            {
                var getId = ExtractBetween(argsPart, "[#get \"", "\"]");
                return new KsGet { Id = getId };
            }

            var args = SplitArgs(argsPart);

            var call = new KsCall { TargetId = id.Trim('"') };
            call.Args.AddRange(args);
            return call;
        }

        // ------------------------------------------------------------
        // SET
        // ------------------------------------------------------------
        private static KsNode ParseSet(string line, int lineNum)
        {
            if (!line.Contains("\""))
                throw SyntaxError("SET missing quoted identifier", lineNum);

            var id = ExtractBetween(line, "[#set \"", "\"");
            var argsPart = ExtractBetween(line, "\"](", ")");
            argsPart = TrimTrailingSemicolon(argsPart);

            var value = argsPart.Trim();
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2);

            return new KsSet { Id = id, Value = value };
        }

        // ------------------------------------------------------------
        // GET
        // ------------------------------------------------------------
        private static KsNode ParseGet(string line, int lineNum)
        {
            if (!line.Contains("\""))
                throw SyntaxError("GET missing quoted identifier", lineNum);

            var id = ExtractBetween(line, "[#get \"", "\"]");
            return new KsGet { Id = id };
        }

        // ------------------------------------------------------------
        // INSTRUCTION
        // ------------------------------------------------------------
        private static KsNode ParseInstruction(string line, int lineNum)
        {
            var inner = line.Substring(2, line.Length - 3); // strip [# and ]
            var parts = inner.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                throw SyntaxError("Empty instruction", lineNum);

            var name = parts[0];
            var parameters = new List<string>();

            if (parts.Length > 1)
            {
                parameters.Add(parts[1]);
                parameters.AddRange(ExtractQuoted(inner));
            }

            var inst = new KsInstruction { Name = name };
            inst.Params.AddRange(parameters);
            return inst;
        }

        // ------------------------------------------------------------
        // STRING HELPERS
        // ------------------------------------------------------------
        private static string ExtractBetween(string text, string start, string end)
        {
            int s = text.IndexOf(start, StringComparison.Ordinal);
            if (s < 0) return "";
            s += start.Length;
            int e = text.IndexOf(end, s, StringComparison.Ordinal);
            if (e < 0) return "";
            return text.Substring(s, e - s);
        }

        private static string TrimTrailingSemicolon(string s)
        {
            s = s.Trim();
            if (s.EndsWith(";"))
                s = s.Substring(0, s.Length - 1).TrimEnd();
            return s;
        }

        private static List<string> SplitArgs(string argsPart)
        {
            var result = new List<string>();
            var s = argsPart.Trim();
            if (s.Length == 0)
                return result;

            var pieces = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in pieces)
            {
                var v = p.Trim();
                if (v.StartsWith("\"") && v.EndsWith("\""))
                    v = v.Substring(1, v.Length - 2);
                result.Add(v);
            }

            return result;
        }

        private static IEnumerable<string> ExtractQuoted(string text)
        {
            var list = new List<string>();
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '"')
                {
                    int start = i + 1;
                    int end = text.IndexOf('"', start);
                    if (end < 0) break;
                    list.Add(text.Substring(start, end - start));
                    i = end + 1;
                }
                else i++;
            }
            return list;
        }
    }
}
