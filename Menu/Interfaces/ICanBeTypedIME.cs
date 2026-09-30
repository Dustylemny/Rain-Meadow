using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Interfaces
{
    public interface ICanBeTypedIME : ICanBeTyped
    {
        public Vector2 CursorScreenPos { get; }
        public bool IsFocused();
    }
}
