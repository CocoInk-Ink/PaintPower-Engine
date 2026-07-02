// File: PaintPower.Runtime.Interpreter/RuntimeBridge.cs
using System;
using System.Collections.Generic;
using PaintPower.Vm;
using PaintPower.Sprites;
using PaintPower.Logging;

namespace PaintPower.Runtime.Interpreter
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

        public object? AllocObject(int typeId)
        {
            // Minimal: if typeId maps to DIItem, create Sprite; otherwise create a simple dictionary-backed object
            // You should replace this with your TypeSystem lookup.
            var sprite = new Sprite();
            // Add to DIPlay if you have a reference; for now return the Sprite instance as handle
            return sprite;
        }

        public object? GetField(object? obj, string fieldName)
        {
            if (obj is Sprite s)
            {
                return fieldName switch
                {
                    "x" => s.x,
                    "y" => s.y,
                    "Skin" => s.CurrentSkin,
                    _ => null
                };
            }

            // fallback for VmObject (not implemented here)
            return null;
        }

        public void SetField(object? obj, string fieldName, object? value)
        {
            if (obj is Sprite s)
            {
                switch (fieldName)
                {
                    case "x": s.x = Convert.ToDouble(value ?? 0.0); s.SnapshotDirty = true; break;
                    case "y": s.y = Convert.ToDouble(value ?? 0.0); s.SnapshotDirty = true; break;
                    default: break;
                }
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
