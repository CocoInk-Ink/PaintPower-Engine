namespace PaintPower.Runtime.Bytecode;

public enum OpCode : byte
{
    Nop,            // Do nothing
    LoadConst,      // Push constant onto stack
    LoadLocal,      // Push local variable
    StoreLocal,     // Store into local variable
    Call,           // Call a function or service
    Return,         // Return from function
    Jump,           // Unconditional jump
    JumpIfFalse,    // Conditional jump
    SendMessage,    // VM message passing
    ReceiveMessage,  // Pull from message queue
    Print,            // Print to console
    CompareEqual,
    CompareNotEqual,
    CompareLess,
    CompareGreater,
    CompareLessEqual,
    CompareGreaterEqual,

}
