using Menu.Remix.MixedUI;
using RainMeadow.UI;
using RainMeadow.UI.Interfaces;
using Rewired.Utils.Classes.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RainMeadow
{
    public class ButtonTypingHandler : TypingHandler
    {
        public string lastCompositionString = "";
        public new string _lastInput = "";
        public new HashSet<ICanBeTyped> _assigned = new HashSet<ICanBeTyped>();
        public new ICanBeTyped? _focused, lastFocused;
        public static bool TypableIsFocused(ICanBeTyped typeable)
        {
            return (typeable is UIfocusable uIfocusable && uIfocusable.Focused) || (typeable is ICanBeTypedIME imeType && imeType.IsFocused());
        }
        public static Vector2 FixUIPosToScreen(Vector2 pos, bool isPositionPt = true)
        {
            Vector2 displayReso = new(Display.main.systemWidth, Display.main.systemHeight);
            Vector2 screenReso = new(Screen.width, Screen.height);
            Vector2 pixelReso = new(Futile.screen.pixelWidth, Futile.screen.pixelHeight);


            Vector2 cursorOffset =  new(0, 0);
            if (Screen.fullScreen && isPositionPt)
            {
                cursorOffset = (displayReso - screenReso) * (screenReso / displayReso);
            }
            pos = new Vector2(pos.x, pixelReso.y - pos.y) / pixelReso * screenReso;
            return new(pos.x, pos.y + cursorOffset.y);
        }
        public static Vector2 LocalCursorPosOfTypeable(ICanBeTyped typeable)
        {
            if (typeable is ICanBeTypedIME ime)
                return ime.CursorScreenPos;
            else if (typeable is UIfocusable focusable)
            {
                return focusable.ScreenPos;
            }
            return new(0, 0);
        }
        public static Vector2 GetTextCursorPos(ICanBeTyped typeable) =>  FixUIPosToScreen(LocalCursorPosOfTypeable(typeable));
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
                        break;
                    }
                }
                if (_focused != lastFocused)
                {
                    RainMeadow.Debug("ButtonTypingHandler: Focus changed from " + lastFocused + " to " + _focused);
                    lastFocused = _focused;
                    Input.imeCompositionMode = IMECompositionMode.Off; //reset compositionstring
                    Input.imeCompositionMode = _focused != null ? IMECompositionMode.On : IMECompositionMode.Auto;
                }
                if (_focused == null)
                {
                    _lastInput = "";
                    return;
                }
            }
            ICanBeTypedIME? imeFocusable = _focused as ICanBeTypedIME;
            Input.compositionCursorPos = GetTextCursorPos(_focused);
            string compositionString = Input.compositionString;
            string inputString = Input.inputString;
            HashSet<char> hashSet = [];

            for (int i = 0; i < _lastInput.Length; i++)
                hashSet.Add(_lastInput[i]);

            Queue<char> queue = [];
            for (int j = 0; j < inputString.Length; j++)
            {
                if (!hashSet.Contains(inputString[j]))
                {
                    queue.Enqueue(inputString[j]);
                    RainMeadow.Debug("Queued input " + inputString[j]);
                }
            }

            while (queue.Count > 0)
                _focused.OnKeyDown(queue.Dequeue());

            _lastInput = inputString;
            imeFocusable?.SetIMEComposition(compositionString);
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
            _assigned.Remove(typable);
        }
       
    }
}