using System.Collections.Generic;
using UnityEngine;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;

namespace RainMeadow
{
    public class ButtonTypingHandler : TypingHandler
    {
        public string lastCompositionInput = "";
        public new string _lastInput = "";
        public new HashSet<ICanBeTyped> _assigned = new HashSet<ICanBeTyped>();
        public new ICanBeTyped? _focused;
        public static bool TypableIsFocused(ICanBeTyped typeable)
        {
            return (typeable is UIfocusable uIfocusable && uIfocusable.Focused) || (typeable is ICanBeTypedIME imeType && imeType.IsFocused());
        }
        public static Vector2 GetTextCursorPos(ICanBeTyped typeable)
        {
            Vector2 setCursorPos = new(0, 0);
            Vector2 displayReso = new(Display.main.systemWidth, Display.main.systemHeight);
            Vector2 screenReso = new(Screen.width, Screen.height);
            //position isnt transferred properly when fullscreen, and is offset up by diff in screen.resolution and actualresolution.
            //gotta offset it but with reference to Screen.height
            Vector2 cursorOffset = Screen.fullScreen? (displayReso - screenReso) * (screenReso / displayReso) : new(0, 0);
          
            if (typeable is ICanBeTypedIME ime)
                setCursorPos = ime.CursorScreenPos / Futile.screen.pixelWidth * Screen.width;
            else if (typeable is UIfocusable focusable)
                setCursorPos = focusable.ScreenPos;

            return new(setCursorPos.x + 100, Screen.height - setCursorPos.y + cursorOffset.y); 
            //ime input positions from top when y is 0
        }
        public new void Update()
        {
            if (_assigned.Count < 1)
            {
                Destroy(base.gameObject);
                return;
            }
            if (_focused != null)
                _focused = null;
            if (_focused == null)
            {
                foreach (ICanBeTyped canBeTyped in _assigned)
                {
                    if (TypableIsFocused(canBeTyped))
                    {
                        _focused = canBeTyped;
                        Input.imeCompositionMode = IMECompositionMode.On;
                        break;
                    }
                }
                if (_focused == null)
                {
                    Input.imeCompositionMode = IMECompositionMode.Auto;
                    _lastInput = "";
                    return;
                }
            }

            Input.compositionCursorPos = GetTextCursorPos(_focused);
            lastCompositionInput = Input.compositionString;

            string inputString = Input.inputString;
            HashSet<char> hashSet = new HashSet<char>();
            for (int i = 0; i < _lastInput.Length; i++)
            {
                hashSet.Add(_lastInput[i]);
            }
            Queue<char> queue = new Queue<char>();
            for (int j = 0; j < inputString.Length; j++)
            {
                if (!hashSet.Contains(inputString[j]))
                {
                    queue.Enqueue(inputString[j]);
                }
            }
            while (queue.Count > 0)
            {
                _focused.OnKeyDown(queue.Dequeue());
            }
            _lastInput = inputString;
        }
        public new void OnDestroy()
        {
            _assigned.Clear();
            _focused = null;
            Input.imeCompositionMode = IMECompositionMode.Auto;
            CanBeTypedExt._HandlerOnDestroy();
        }
        public new void Assign(ICanBeTyped typable)
        {
            _assigned.Add(typable);
        }
        public new void Unassign(ICanBeTyped typable)
        {
            _assigned.Clear();
            _assigned.Remove(typable);
        }
    }
}