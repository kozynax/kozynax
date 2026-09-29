using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Silk.NET.SDL;
using Event = Silk.NET.SDL.Event;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// Collects SDL file/URI drop events. Matches WinForms: only a single item is accepted.
    /// SDL requires <see cref="DropEvent.File"/> to be freed with <c>SDL_free</c>.
    /// </summary>
    public sealed unsafe class SdlDropFileHandler
    {
        private readonly Sdl _sdl;
        private readonly List<(string Value, bool IsFile)> _pending = new List<(string, bool)>();
        private bool _batching;

        public SdlDropFileHandler(Sdl sdl)
        {
            _sdl = sdl ?? throw new ArgumentNullException(nameof(sdl));
        }

        public event Action<string, bool> Dropped;

        public void Enable()
        {
            _sdl.EventState((uint)EventType.Dropfile, Sdl.Enable);
            _sdl.EventState((uint)EventType.Droptext, Sdl.Enable);
            _sdl.EventState((uint)EventType.Dropbegin, Sdl.Enable);
            _sdl.EventState((uint)EventType.Dropcomplete, Sdl.Enable);
        }

        public bool TryHandle(in Event e)
        {
            switch ((EventType)e.Type)
            {
                case EventType.Dropbegin:
                    _batching = true;
                    _pending.Clear();
                    return true;
                case EventType.Dropfile:
                    Accept(ReadAndFree(e), isFile: true);
                    return true;
                case EventType.Droptext:
                    Accept(ReadAndFree(e), isFile: false);
                    return true;
                case EventType.Dropcomplete:
                    RaiseIfSingle();
                    _pending.Clear();
                    _batching = false;
                    return true;
                default:
                    return false;
            }
        }

        private void Accept(string value, bool isFile)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            value = value.Trim();
            if (_batching)
            {
                _pending.Add((value, isFile));
                return;
            }

            Dropped?.Invoke(value, isFile);
        }

        private void RaiseIfSingle()
        {
            if (_pending.Count != 1)
                return;
            var item = _pending[0];
            Dropped?.Invoke(item.Value, item.IsFile);
        }

        private string ReadAndFree(in Event e)
        {
            var file = e.Drop.File;
            if (file == null)
                return null;
            try
            {
                return Marshal.PtrToStringUTF8((IntPtr)file);
            }
            finally
            {
                _sdl.Free(file);
            }
        }
    }
}
