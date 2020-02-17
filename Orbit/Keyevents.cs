using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Orbit
{
    class ModifierKey
    {
        public bool Control { get; set; }
        public bool Shift { get; set; }
        public bool Alt { get; set; }

        public ModifierKey(bool control, bool shift, bool alt)
        {
            Control = control;
            Shift = shift;
            Alt = alt;
        }

        internal bool Matches()
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) != Control)
                return false;
            if ((Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)) != Shift)
                return false;
            if ((Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)) != Alt)
                return false;

            return true;
        }
    }
    static class Keyevents
    {
        static bool[] Debouncers_ = Array.Empty<bool>();
        static bool[] Togglers_ = Array.Empty<bool>();
        static Tuple<Key, ModifierKey, Action, Action>[] DebounceEvents_ = Array.Empty<Tuple<Key, ModifierKey, Action, Action>>();

        public static void AddToggleEvent(Key key, Action onEvent, Action offEvent, bool control = false, bool shift = false, bool alt = false)
        {
            PushArray(ref Togglers_, false);

            int index = Togglers_.Length - 1;
            AddDebouncedEvent(key, () =>
            {
                if (Togglers_[index])
                {
                    offEvent.Invoke();
                    Togglers_[index] = false;
                }
                else
                {
                    onEvent.Invoke();
                    Togglers_[index] = true;
                }
            }, null, control, shift, alt);
        }


        public static void AddDebouncedEvent(Key key, Action downEvent, Action upEvent = null, bool control = false, bool shift = false, bool alt = false)
        {
            PushArray(ref Debouncers_, false);
            PushArray(ref DebounceEvents_, new Tuple<Key, ModifierKey, Action, Action>(key, new ModifierKey(control, shift, alt), downEvent, upEvent));
        }

        private static void PushArray<T>(ref T[] arr, T value)
        {
            Array.Resize(ref arr, arr.Length + 1);
            arr[arr.Length - 1] = value;
        }

        public static void CheckKeys()
        {
            for (int i = 0; i < DebounceEvents_.Length; i++)
            {
                Tuple<Key, ModifierKey, Action, Action> item = DebounceEvents_[i];

                // Is clicked
                if (Keyboard.IsKeyDown(item.Item1))
                {
                    if (item.Item2.Matches())
                    {
                        // Debounced
                        if (Debouncers_[i] == false)
                        {
                            // Call keydown event
                            item.Item3?.Invoke();

                            Debouncers_[i] = true;
                        }
                    }
                }
                else
                {
                    // Only keyup event if debounced
                    if (Debouncers_[i])
                    {
                        // Debounce
                        Debouncers_[i] = false;

                        // Call keyup event
                        item.Item4?.Invoke();
                    }
                }
            }
        }


    }
}
