// File: PaintPower.Vm.Runtime.Interpreter/RuntimeBridge.cs
using System;
using System.Collections.Generic;
using PaintPower.Vm;
using PaintPower.Vm.Runtime.Sprites;
using PaintPower.Logging;
using PaintPower.Vm.Runtime.ObjectModel;

namespace PaintPower.Vm.Runtime.Interpreter
{
    public sealed class RuntimeBridge
    {
        private readonly Vm.Vm _vm;

        public RuntimeBridge(Vm.Vm vm)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
        }

        public void SysPrint(string text)
        {
            Log.QuickLog(text);
        }

        public void SysLog(string text)
        {
            Log.QuickLog(text);
        }

        public object AllocObject(int typeId)
        {
            var desc = _vm.Types.Get(typeId);
            return new VmObject(desc);
        }

        public object? GetField(object obj, string fieldName)
        {
            if (obj is VmObject o)
            {
                int index = o.Type.FieldMap[fieldName];
                return o.GetField(index);
            }
            return null;
        }

        public void SetField(object obj, string fieldName, object? value)
        {
            if (obj is VmObject o)
            {
                int index = o.Type.FieldMap[fieldName];
                o.SetField(index, value);
            }
        }

        public void CenterSprite(object? spriteHandle)
        {
            if (spriteHandle is Sprite s)
            {
                s.x = s.StageWidth / 2.0;
                s.y = s.StageHeight / 2.0;
                s.SnapshotDirty = true;
            }
        }

        public void GlideSprite(object? spriteHandle, double durationSeconds, double x, double y)
        {
            if (spriteHandle is Sprite s)
            {
                // Simple immediate set for now; replace with animation scheduler
                s.x = x;
                s.y = y;
                s.SnapshotDirty = true;
            }
        }

        public void Broadcast(string message, VmThread origin)
        {
            // Find handlers in project (not implemented). For now log.
            Log.QuickLog($"Broadcast: {message}");
        }

        public void BroadcastAndWait(string message, VmThread origin)
        {
            // Minimal: same as Broadcast
            Broadcast(message, origin);
        }
    }
}
