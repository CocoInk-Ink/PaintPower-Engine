using System;
using System.Collections.Generic;
using PaintPower.Runtime.Ksa;
using PaintPower.Sprites;

namespace PaintPower.Runtime.Primitives;

public class Primitives
{
	public void addPrimsTo(Dictionary<OpCode, Func<Stack<object?>, OpCode, int, Sprite, object?>> primTable)
	{
		// Operators.
		primTable[OpCode.Add] = Arithmetic;
		primTable[OpCode.Sub] = Arithmetic;
		primTable[OpCode.Mul] = Arithmetic;
		primTable[OpCode.Div] = Arithmetic;
	}

	private static object? Arithmetic(Stack<object?> _eval, OpCode op, int operand, Sprite s)
        {
			object? a, b;

			a = _eval.Pop();
			b = _eval.Pop();

            if (a is int ai && b is int bi)
            {
                return op switch
                {
                    OpCode.Add => ai + bi,
                    OpCode.Sub => ai - bi,
                    OpCode.Mul => ai * bi,
                    OpCode.Div => bi == 0 ? 0 : ai / bi,
                    _ => 0
                };
            }

            if (a is double || b is double)
            {
                double ad = Convert.ToDouble(a ?? 0);
                double bd = Convert.ToDouble(b ?? 0);
                return op switch
                {
                    OpCode.Add => ad + bd,
                    OpCode.Sub => ad - bd,
                    OpCode.Mul => ad * bd,
                    OpCode.Div => bd == 0 ? 0.0 : ad / bd,
                    _ => 0.0
                };
            }

            // string concat for Add
            if (op == OpCode.Add)
                return $"{a}{b}";

            return 0;
        }


}