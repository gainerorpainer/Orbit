using System;
using System.Drawing;

namespace Orbit
{
    class Clickable
    {
        public string Label { get; set; }
        public Action OnClick { get; set; }
        public Action OnUnClick { get; set; }
        public Rectangle Rectangle { get; set; }
        public bool Toggle { get; set; }
        public bool ToggleState { get; internal set; }

        public void HandleClickEvent()
        {
            if (Toggle == false)
            {
                OnClick();
                return;
            }
            else
            {
                if (ToggleState == false)
                    OnClick();
                else
                    OnUnClick();

                ToggleState = !ToggleState;
            }
        }

        public void UnClick()
        {
            if (ToggleState == true)
            {
                OnUnClick();
                ToggleState = false;
            }
        }
    }
}
