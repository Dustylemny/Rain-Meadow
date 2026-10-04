using Menu;
using Menu.Remix.MixedUI;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json.Linq;
using RainMeadow.UI.Components;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public class ChatTextBox : ChatTemplate
    {
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        private bool isUnloading = false;
        private float DASDelay = 1f / 2f; //In seconds
        private float DASRepeatRate = 1f / 30f; //In seconds/proc
        private float backspaceHeld = 0f;
        private float backspaceRepeater = 0f;
        private float arrowHeld = 0f;
        private float arrowRepeater = 0f;
        private bool clipboardHeld = false;
        private bool tabHeld = false;
        public const int textLimit = 100;
        private static List<IDetour>? inputBlockers;
        public static bool blockInput = false;
        public static int historyCursor = -1;
        public static string lastTyped = "";

        public static List<string> messageHistory = new();
        public static List<string> completions = new();
        public static bool completed = false;
        public static int autoCompleteIndex = 0;
        public string lastCompletion = "";
        public int lastCompletionStart = -1;
        public int lastCompletionEnd = -1;

        public static event Action? OnShutDownRequest;
        public event Action? OnTextSubmit;

        public static string Clipboard
        {
            get
            {
                var contents = GUIUtility.systemCopyBuffer;
                RainMeadow.Debug($"Clipboard was accessed! Reading {contents.Length} chars from system clipboard!");
                return contents;
            }
            set
            {
                RainMeadow.Debug($"Clipboard was accessed! Writing {value.Length} chars to system clipboard!");
                GUIUtility.systemCopyBuffer = value;
            }
        }
        // Multiview Support
        public bool MultiView, focused, forceMenuMouseMode, lastFreezeMenuFunctions, lastMenuMouseMode, previouslySubmittedText;

        public bool Focused
        {
            get => focused;
            set
            {
                focused = value;
                forceMenuMouseMode = value ? menu.manager.menuesMouseMode : forceMenuMouseMode;
            }
        }
        public bool IgnoreSelect => (focused && !menu.manager.menuesMouseMode);
        public bool TypingOnOtherObjects => CanBeTypedExt._handler?._focused != null;
        public bool DontGetInputs => menu.FreezeMenuFunctions || lastFreezeMenuFunctions || !menu.Active || page != menu.pages.GetValueOrDefault(menu.currentPage);
        //
        public static bool AnyCtrl => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftApple);
        public static bool AnyShift => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Store matches found for the current cycle
        private List<string> completionMatches;
        // Track which match we are currently displaying
        private int completionIndex = 0;
        // Track where the current '@' started so we know what text to replace
        private int completionStartPos = -1;

        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, bool multiView = false) : base(menu, owner, displayText, pos, size)
        {
            MultiView = multiView;
            internalTextLimit = textLimit;
            LastSentMessage = "";
            CursorPos = 0;
            SelectionPos = -1;
            historyCursor = messageHistory.Count;
            this.menu = menu;
            gameObject ??= new GameObject();
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            typingHandler.Assign(this);
            ShouldCapture(true);
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                if (!MultiView)
                    cs.isInteracting = true;
                else
                    cs.isInteracting = Focused;
            }

        }
        public override void Clicked()
        {
            base.Clicked();
            if (!MultiView) return;
            if (previouslySubmittedText)
            {
                previouslySubmittedText = false;
                if (!menu.manager.menuesMouseMode) return;
            }
            if (IgnoreSelect) return;
            if (!buttonBehav.clicked) return;
            SetFocused(!Focused);
        }
        public void CheckToUnfocus()
        {
            if ((menu.pressButton && menu.manager.menuesMouseMode && !buttonBehav.clicked) || buttonBehav.greyedOut)
            {
                SetFocused(false, menu.selectedObject == null || buttonBehav.greyedOut ? null : SoundID.None);
                return;
            }
            if (TypingOnOtherObjects)
                SetFocused(false, SoundID.None);
        }
        public void HandleDeselect()
        {
            SetFocused(false);
            blockInput = false;
            previouslySubmittedText = !menu.manager.menuesMouseMode;
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                cs.isInteracting = false;
            }
        }
        public void SetFocused(bool focused, SoundID? overrideSoundID = null)
        {
            if (!MultiView) return;
            if (Focused != focused)
                menu.PlaySound(overrideSoundID ?? (focused ? SoundID.MENU_Button_Standard_Button_Pressed : SoundID.MENU_Checkbox_Uncheck));
            Focused = focused;
        }

        public void DelayedUnload(float delay)
        {
            if (!isUnloading)
            {
                CursorPos = 0;
                ShouldCapture(false);
                isUnloading = true;
                typingHandler.StartCoroutine(Unload(delay));
            }
        }
        private IEnumerator Unload(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (typingHandler != null)
            {
                typingHandler.Unassign(this);
                typingHandler.OnDestroy();
                blockInput = false;
            }

        }
        public override void SetIMECompositionString(string compositionString)
        {
            if (compositionString == CompositionString)
                return;


            blockInput = false;
            if (isUnloading) return;


            menu.PlaySound(SoundID.MENU_Checkbox_Check);

            if (SelectionActive)
                DeleteSelection();
            RainMeadow.Debug(compositionString);
                CompositionString = compositionString;


            blockInput = true;
        }
        public override void CaptureInputs(char input)
        {
            RainMeadow.Debug(input);
            // the "Delete" character, which is emitted by most - but not all - operating systems when ctrl and backspace are used together
            if (MultiView)
            {
                if (DontGetInputs) return;
                blockInput = false;
                if (!Focused)
                {
                    Player.InputPackage currentInput = RWInput.PlayerUIInput(-1); //race conditions when update isnt called on time
                    bool shouldActuallyGetInput = menu.selectedObject == null || (!menu.pressButton && !menu.holdButton && !menu.lastHoldButton && !menu.modeSwitch && !currentInput.jmp);
                    if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatButtonKey.Value) && shouldActuallyGetInput && !TypingOnOtherObjects)
                    {
                        if (AnyShift && RainMeadow.rainMeadowOptions.EnableChatLogErrorToggle.Value)
                        {
                            ChatLogManager.ToggleLogErrorInChat();
                        }
                        else
                        {
                            SetFocused(true);
                            forceMenuMouseMode = forceMenuMouseMode || lastMenuMouseMode;
                            if (!forceMenuMouseMode) menu.selectedObject = this;
                        }
                    }
                    return;
                }
            }
            if (input == '\u007F') return;
            string msg = LastSentMessage;
            blockInput = false;
            if (input == '\b')
            {
                if (CursorPos > 0 || SelectionPos != -1)
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    // selection position is -1 when nothing is selected
                    if (SelectionPos != -1)
                    {
                        // deletes the selected text
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        DeleteSelection();
                        if (CursorPos == LastSentMessage.Length) SetCursorSprite();
                    }
                    else
                    {
                        LastSentMessage = msg.Remove(CursorPos - 1, 1);
                        CursorPos--;
                    }
                }
            }
            else if (input == '\n' || input == '\r')
            {
                if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
                {
                    cs.isInteracting = false;
                }
                if (msg.Length > 0 && !string.IsNullOrWhiteSpace(msg))
                {
                    if (messageHistory.Count == 0 || messageHistory[messageHistory.Count - 1] != msg)
                    {
                        messageHistory.Add(msg);
                    }


                    if (SpecialEvents.EventActiveInLobby<SpecialEvents.AprilFools>() && UnityEngine.Random.Range(0, 100) == 1)
                    {
                        string coinBoast = "";
                        int coins = RainMeadow.rainMeadowOptions.MeadowCoins.Value;
                        switch (coins)
                        {
                            case > 1000:
                                coinBoast = $" i hacked to get all my meadow coins ({coins} coins)";
                                break;
                            case > 500:
                                coinBoast = $" i beat meadow ({coins} coins)";
                                break;
                            case > 100:
                                coinBoast = $" btw I have {coins} coins";
                                break;
                            case < 50:
                                coinBoast = $" im poor ({coins} coins)";
                                break;
                            default:
                                break;
                        }
                        msg += coinBoast;
                    }
                    MatchmakingManager.currentInstance.SendChatMessage(msg);
                    foreach (var player in OnlineManager.players)
                    {
                        player.InvokeRPC(RPCs.UpdateUsernameTemporarily, msg);
                    }
                    if (MultiView)
                    {
                        HandleTextSubmit();
                        return;
                    }
                }
                else
                {
                    if (MultiView) HandleDeselect();
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    RainMeadow.Debug("Could not send lastSentMessage because it had no text or only had whitespaces");
                    if (MultiView) return;
                }
                // only resets the chat text box if in a story lobby menu, otherwise the text box is just destroyed
                OnShutDownRequest?.Invoke();
                typingHandler.Unassign(this);
                LastSentMessage = "";
                completed = false;
                return;
            }
            else if (!isUnloading)
            {
                if (!CompositionActive)
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                AddTextAtPos(input.ToString());
            }
            if (!isUnloading) blockInput = true;
            UpdateLabel();
        }

        public override void Update()
        {
            base.Update();
            if (MultiView)
            {
                CheckToUnfocus();
                menu.allowSelectMove = !Focused;
                roundedRect.addSize = new Vector2(5f, 3f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14f)) * (buttonBehav.clicked && !IgnoreSelect ? 0 : 1);
                forceMenuMouseMode = (menu.holdButton || menu.pressButton || menu.modeSwitch || Focused) && forceMenuMouseMode;
                menu.manager.menuesMouseMode = forceMenuMouseMode || menu.manager.menuesMouseMode;
                lastMenuMouseMode = menu.manager.menuesMouseMode;
                lastFreezeMenuFunctions = menu.FreezeMenuFunctions;
            }
            else
            {
                menu.allowSelectMove = false;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (MultiView)
            {
                ShouldCapture(Focused);
            }
            var msg = LastSentMessage;
            var len = msg.Length;
            var hasText = len > 0;
            blockInput = false;
            if (MultiView && !Focused)
            {
                base.GrafUpdate(timeStacker);
                return;
            }
            // ctrl backspace stuff here instead of CaptureInputs, because ctrl + backspace doesn't always emit a capturable character on some operating systems
            if (Input.GetKey(KeyCode.Backspace) && (CursorPos > 0 || SelectionActive))
            {
                // reset @ blindly
                if (completionMatches != null)
                {
                    UpdateCompletions();

                }
                // no alt + backspace, because alt can be finnicky
                // activates on either the first frame the key is held, or for every (DASRepeatRate)th of a second after (DASDelay) seconds of being held
                if (AnyCtrl && (backspaceHeld == 0 || (backspaceHeld >= DASDelay && backspaceRepeater >= DASRepeatRate)))
                {
                    if (SelectionActive)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        DeleteSelection();
                        if (CursorPos == LastSentMessage.Length) SetCursorSprite();
                    }
                    else if (CursorPos > 0)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        int space = msg.Substring(0, CursorPos - 1).LastIndexOf(' ') + 1;
                        LastSentMessage = msg.Remove(space, CursorPos - space);
                        //UpdateLabel(lastSentMessage);
                        CursorPos = space;
                    }
                    backspaceRepeater %= DASRepeatRate; //Modulus instead of subtract so the repeater can't scale out of control if DeltaTime > DASRepeatRate.
                }
                backspaceHeld += Time.deltaTime;
                backspaceRepeater += Time.deltaTime;
            }

            else if (Input.GetKey(KeyCode.Delete))
            {
                if (SelectionActive)
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    DeleteSelection();
                }
                else if ((backspaceHeld == 0 || (backspaceHeld >= DASDelay && backspaceRepeater >= DASRepeatRate)) && CursorPos < msg.Length)
                {
                    if (AnyCtrl)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        int space = msg.Substring(CursorPos, Mathf.Max(len - CursorPos, 0)).IndexOf(' ');
                        LastSentMessage = msg.Remove(CursorPos, (space < 0 || space >= len) ? (space = Mathf.Max(len - CursorPos, 0)) : space + 1);
                        //UpdateLabel(lastSentMessage);

                    }
                    else
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        LastSentMessage = msg.Remove(CursorPos, 1);
                        //UpdateLabel(lastSentMessage);
                    }
                    backspaceRepeater %= DASRepeatRate;
                }
                if (CursorPos == LastSentMessage.Length) SetCursorSprite();
                backspaceHeld += Time.deltaTime;
                backspaceRepeater += Time.deltaTime;
            }

            else
            {
                backspaceHeld = 0f;
                backspaceRepeater = 0f;
                if (Input.GetKey(KeyCode.Home))
                {
                    bool changeSprite = CursorPos == len;
                    CursorPos = 0;
                    SelectionPos = -1;
                    if (changeSprite) SetCursorSprite();
                }

                else if (Input.GetKey(KeyCode.End) && CursorPos < len)
                {
                    CursorPos = len;
                    SelectionPos = -1;
                    SetCursorSprite();
                }
                // double check
                else if (Input.GetKey(KeyCode.A) && (AnyCtrl))
                {
                    if (CursorPos == len)
                    {
                        SetCursorSprite();
                    }
                    SelectionPos = 0;
                    CursorPos = msg.Length;
                }

                // CTRL + C / Command + C
                else if (Input.GetKey(KeyCode.C) && !clipboardHeld && (AnyCtrl))
                {
                    CopySelection();
                    clipboardHeld = true;
                }

                else if (Input.GetKeyDown(KeyCode.Tab))
                {
                    AutoComplete();
                }
                // CTRL + V / Command + V
                else if (Input.GetKey(KeyCode.V) && !clipboardHeld && (AnyCtrl))
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    LastSentMessage = Paste(msg);
                    CursorPos = Mathf.Min(LastSentMessage.Length, CursorPos + Clipboard.Length);
                    SelectionPos = -1;
                    clipboardHeld = true;
                }
                // CTRL + X / Command + X
                else if (Input.GetKey(KeyCode.X) && !clipboardHeld && (AnyCtrl))
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    CopySelection();
                    DeleteSelection();
                    clipboardHeld = true;
                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    // cursor position is used as the anchor for selection
                    if ((CursorPos > 0 || SelectionActive) && (arrowHeld == 0 || (arrowHeld >= DASDelay && arrowRepeater >= DASRepeatRate)))
                    {
                        var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                        if (SelectionActive && !shiftHeld)
                            SelectionPos = -1;
                        else
                        {
                            var newPos = CursorPos;
                            if (AnyCtrl && CursorPos > 0)
                            {
                                newPos = msg.Substring(0, newPos - 1).LastIndexOf(' ') + 1;
                                if (newPos < 0 || newPos > len) newPos = 0;
                            }
                            else newPos = Math.Max(0, newPos - 1);
                            if (shiftHeld)
                            {
                                if (newPos == SelectionPos)
                                    SelectionPos = -1; // stops the selection if it's on the same index as the anchor
                                else
                                    SelectionPos = SelectionActive? SelectionPos : CursorPos;
                            }
                            CursorPos = newPos;
                        }
                        arrowRepeater %= DASRepeatRate;
                    }
                    arrowHeld += Time.deltaTime;
                    arrowRepeater += Time.deltaTime;
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    if ((CursorPos < len || SelectionActive) && (arrowHeld == 0 || (arrowHeld >= DASDelay && arrowRepeater >= DASRepeatRate)))
                    {
                        var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                        if (SelectionActive && !shiftHeld)
                            SelectionPos = -1;
                        else
                        {
                            // starts from the end of the selection if a selection exists
                            if (!SelectionActive || SelectionPos < msg.Length)
                            {
                                var newPos = CursorPos;
                                if (AnyCtrl)
                                {
                                    int space = msg.Substring(newPos, len - newPos - 1).IndexOf(' ');
                                    if (space < 0 || space >= len) newPos = len;
                                    else newPos = space + newPos + 1;
                                }
                                else newPos++;
                                if (shiftHeld)
                                {
                                    if (newPos == SelectionPos)
                                        SelectionPos = -1; // stops the selection if it's on the same index as the anchor
                                    else
                                        SelectionPos = SelectionActive ? SelectionPos : CursorPos;
                                }
                                CursorPos = newPos;
                            }
                        }
                        arrowRepeater %= DASRepeatRate;
                    }
                    arrowHeld += Time.deltaTime;
                    arrowRepeater += Time.deltaTime;
                }
                else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
                {
                    if (AnyCtrl && (arrowHeld == 0 || (arrowHeld >= DASDelay && arrowRepeater >= DASRepeatRate)))
                    {
                        if (Input.GetKey(KeyCode.UpArrow))
                        {
                            GetMessageHistory(-1);
                        }
                        else if (Input.GetKey(KeyCode.DownArrow))
                        {
                            GetMessageHistory(1);
                        }
                        arrowRepeater %= DASRepeatRate;
                    }
                    arrowHeld += Time.deltaTime;
                    arrowRepeater += Time.deltaTime;
                    // Prevent arrowHeld & arrowRepeater from being reset.
                }
                else
                {
                    arrowHeld = 0f;
                    arrowRepeater = 0f;
                }
                if (!(Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.V) || Input.GetKey(KeyCode.X)))
                {
                    clipboardHeld = false;
                }
                if (!Input.GetKey(KeyCode.Tab))
                {
                    tabHeld = false;
                }
            }
            blockInput = true;
            base.GrafUpdate(timeStacker);
        }

        public void HandleTextSubmit()
        {
            LastSentMessage = "";
            CursorPos = 0;
            SelectionPos = -1;
            historyCursor = messageHistory.Count;
            lastCompletion = "";
            completed = false;
            HandleDeselect();
            OnTextSubmit?.Invoke();
        }

        private void CopySelection()
        {
            if (SelectionPos == -1) return;
            Clipboard = LastSentMessage.Substring(Mathf.Max(0, Mathf.Min(CursorPos, SelectionPos)), Mathf.Abs(SelectionPos - CursorPos));
        }

        private string Paste(string msg)
        {
            var paste = string.Copy(Clipboard);
            if (string.IsNullOrEmpty(paste))
            {
                RainMeadow.Debug("Clipboard was empty.");
                return msg;
            }

            paste = Clean(paste);

            int space = textLimit - msg.Length;

            if (space <= 0) return msg;
            if (paste.Length > space) paste = paste.Substring(0, space);

            RainMeadow.Debug($"Pasted {paste.Length} chars from clipboard.");
            return msg.Insert(Mathf.Clamp(CursorPos, 0, msg.Length), paste);
        }

        private string Clean(string msg)
        {
            return Regex.Replace(msg, @"\r\n?|\n", "");
        }

        private void GetMessageHistory(int dir)
        {
            int last = messageHistory.Count;
            int index = Mathf.Clamp(historyCursor + dir, 0, last);
            if (index == historyCursor) return;
            if (index == last)
            {
                historyCursor = last;
                LastSentMessage = lastTyped;
            }
            else
            {
                if (historyCursor == last)
                {
                    lastTyped = LastSentMessage;
                }

                historyCursor = index;
                LastSentMessage = messageHistory[index];
            }
            //UpdateLabel(lastSentMessage);
            CursorPos = LastSentMessage.Length;
            SelectionPos = -1;
        }

        private void AutoComplete()
        {
            int lastAt = LastSentMessage.LastIndexOf('@', CursorPos - 1 >= 0 ? CursorPos - 1 : 0);
            string currentSearchPrefix = "";

            if (lastAt != -1)
            {
                currentSearchPrefix = LastSentMessage.Substring(lastAt + 1, CursorPos - (lastAt + 1));
            }
            if (completionMatches != null && completionStartPos != lastAt)
            {
                completionMatches = null;
            }

            if (completionMatches == null || completionMatches.Count == 0)
            {
                if (lastAt != -1)
                {
                    completionStartPos = lastAt;
                    string searchPrefix = currentSearchPrefix;

                    completionMatches = OnlineManager.players
                        .Where(p => p.id.DisplayName.StartsWith(searchPrefix, System.StringComparison.InvariantCultureIgnoreCase))
                        .Select(p => p.id.DisplayName)
                        .OrderBy(n => n)
                        .Distinct()
                        .ToList();

                    completionIndex = 0;
                }
            }

            if (completionMatches != null && completionMatches.Count > 0)
            {
                string match = completionMatches[completionIndex % completionMatches.Count];

                string prefix = LastSentMessage.Substring(0, completionStartPos + 1);
                string suffix = LastSentMessage.Substring(CursorPos);

                LastSentMessage = prefix + match + suffix;
                CursorPos = prefix.Length + match.Length;

                completionIndex++;
                menu.PlaySound(SoundID.MENU_Button_Select_Gamepad_Or_Keyboard);
                return;
            }
            else
            {
                completionMatches = null;
                completionStartPos = -1;
            }
        }

        private void UpdateCompletions()
        {
            completionMatches = null;
            completionStartPos = -1;
        }

        private void ResetCompletions()
        {
            lastCompletionEnd = -1;
            lastCompletionStart = -1;
            lastCompletion = "";
            autoCompleteIndex = 0;
            completions.Clear();
        }

        private int GetStart(string text, int pos)
        {
            int i = pos - 1;
            while (i >= 0 && text[i] != ' ')
            {
                i--;
            }
            return i + 1;
        }

        private int GetEnd(string text, int pos)
        {
            int i = pos;
            while (i < text.Length && text[i] != ' ')
            {
                i++;
            }
            return i;
        }


        public static void InvokeShutDownChat()
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                cs.isInteracting = false;
            }
            OnShutDownRequest.Invoke();
        }

        // input blocker for the sake of dev tools/other outside processes that make use of input keys
        // thanks to SlimeCubed's dev console 
        public static void ShouldCapture(bool shouldCapture)
        {
            if (shouldCapture && !blockInput)
            {
                blockInput = true;
                if (inputBlockers == null)
                {
                    var input = typeof(Input);
                    var self = typeof(ChatTextBox);

                    Hook MakeHook(string method, params Type[] types)
                    {
                        Type[] toTypes = new Type[types.Length + 1];
                        types.CopyTo(toTypes, 1);
                        toTypes[0] = (types[0] == typeof(KeyCode)) ? typeof(Func<KeyCode, bool>) : typeof(Func<string, bool>);
                        return new Hook(
                            input.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, types, null),
                            self.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, toTypes, null)
                        );
                    }

                    inputBlockers = new List<IDetour>()
                    {
                        MakeHook(nameof(GetKey), typeof(string)),
                        MakeHook(nameof(GetKey), typeof(KeyCode)),
                        MakeHook(nameof(GetKeyDown), typeof(string)),
                        MakeHook(nameof(GetKeyDown), typeof(KeyCode)),
                        MakeHook(nameof(GetKeyUp), typeof(string)),
                        MakeHook(nameof(GetKeyUp), typeof(KeyCode)),
                    };
                }
            }
            else if (!shouldCapture && blockInput)
            {
                blockInput = false;
            }
        }

        public override bool IsFocused() => !MultiView || Focused;

        private static bool GetKey(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKey(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.UpArrow || code == KeyCode.DownArrow ||
                code == KeyCode.LeftControl || code == KeyCode.RightControl ||
                code == KeyCode.LeftApple) return orig(code);

            return blockInput ? false : orig(code);
        }
        private static bool GetKeyDown(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKeyDown(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.Return) return orig(code);

            return blockInput ? false : orig(code);
        }
        private static bool GetKeyUp(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKeyUp(Func<KeyCode, bool> orig, KeyCode code) => blockInput ? false : orig(code);
    }
}