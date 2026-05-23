using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.SDL;

namespace NeoVeldrid.Sdl2
{
    internal static class Sdl2WindowRegistry
    {
        public static readonly object Lock = new object();

        private static readonly Dictionary<uint, Sdl2Window> _eventsByWindowID = new();

        // Subscribe once at type init, outside any lock. The event pump holds Sdl2Events' lock while
        // calling ProcessWindowEvent, which takes Lock below. Subscribing under Lock would create the
        // reverse acquisition order (Lock, then Sdl2Events' lock) and risk a deadlock.
        static Sdl2WindowRegistry()
        {
            Sdl2Events.Subscribe(ProcessWindowEvent);
        }

        public static void RegisterWindow(Sdl2Window window)
        {
            if (window.WindowID == 0)
            {
                throw new InvalidOperationException("SDL window creation failed: " + GetSdlError());
            }

            lock (Lock)
            {
                _eventsByWindowID.Add(window.WindowID, window);
            }
        }

        private static unsafe string GetSdlError()
        {
            return Marshal.PtrToStringUTF8((nint)Sdl2Window.SdlInstance.GetError()) ?? "unknown SDL error";
        }

        public static void RemoveWindow(Sdl2Window window)
        {
            lock (Lock)
            {
                _eventsByWindowID.Remove(window.WindowID);
            }
        }

        private static unsafe void ProcessWindowEvent(ref Event ev)
        {
            bool handled = false;
            uint windowID = 0;
            switch ((EventType)ev.Type)
            {
                case EventType.Quit:
                case EventType.AppTerminating:
                case EventType.Windowevent:
                case EventType.Keydown:
                case EventType.Keyup:
                case EventType.Textediting:
                case EventType.Textinput:
                case EventType.Keymapchanged:
                case EventType.Mousemotion:
                case EventType.Mousebuttondown:
                case EventType.Mousebuttonup:
                case EventType.Mousewheel:
                    // All of these event types have windowID at the same offset
                    // through the Window member of the union.
                    windowID = ev.Window.WindowID;
                    handled = true;
                    break;
                case EventType.Dropbegin:
                case EventType.Dropcomplete:
                case EventType.Dropfile:
                case EventType.Droptext:
                    DropEvent dropEvent = Unsafe.As<Event, DropEvent>(ref ev);
                    windowID = dropEvent.WindowID;
                    handled = true;
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
            {
                lock (Lock)
                {
                    if (_eventsByWindowID.TryGetValue(windowID, out Sdl2Window window))
                    {
                        window.AddEvent(ev);
                    }
                }
            }
        }
    }
}
