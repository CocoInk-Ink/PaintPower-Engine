using System;
using System.Collections.Generic;
using PaintPower.Display.DisplayIntegration;
using PaintPower.Vm.Runtime.Ksa;
using PaintPower.Vm.Runtime.Sprites;

namespace PaintPower.Vm.Runtime.Primitives;

public class Primitives
{
	public void addPrimsTo(Dictionary<string?, Func<Stack<object?>, OpCode, int, DIItem, object?>> primTable)
	{
		// Operators.
		primTable["math:add"] = Arithmetic;
		primTable["math:sub"] = Arithmetic;
		primTable["math:mul"] = Arithmetic;
		primTable["math:div"] = Arithmetic;
        primTable["math:mod"] = Arithmetic;

        new SpritePrims.SpritePrimitives().addPrimsTo(primTable);
	}

	private static object? Arithmetic(Stack<object?> _eval, OpCode op, int operand, DIItem item)
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