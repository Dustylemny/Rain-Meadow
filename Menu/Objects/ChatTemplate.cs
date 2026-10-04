using UnityEngine;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RainMeadow.UI.Interfaces;
using System;

namespace RainMeadow
{
    public abstract class ChatTemplate : ButtonTemplate, ICanBeTypedIME
    {
        public Action<string> SetIMEComposition { get; set; }
        public Action<char> OnKeyDown { get; set; }
        public HSLColor labelColor;
        public AlignedMenuLabel menuLabel;
        public RoundedRect roundedRect;

        public int internalTextLimit = 100;
        public string _chatMessage = "", _compositionString = "", totalText = "";

        public FContainer textContainer;
        public UIMask uiMask;

        public FSprite _selection;
        public bool updateTextPosition = true, updateCursor = true, updateUnderline = true, totalTextDirty = true;
        public FSprite _cursor;
        public float _cursorWidth;
        public FSprite compositionUnderline;
        public float compositionUnderlineWidth;

        //cursorpos acts as anchor during selection
        // compositionPos is where compositioninput is inserted in normal message
        public int cursorPos, selectionPos = -1, compositionPos, compositionTextCounter;
        public Vector2 textPositionOffset, padding;

        public string WholeText
        {
            get
            {
                if (totalTextDirty)
                    totalText = CompositionActive ? LastSentMessage.Insert(compositionPos, CompositionString) : LastSentMessage;
                return totalText;
            }
        }
        public string CompositionString
        {
            get => _compositionString;
            set
            {
                if (_compositionString == value) return;
                bool prevActive = CompositionActive;
                string prevComposition = CompositionString;
                _compositionString = value;

                int offset = value.Length - prevComposition.Length;
                if (!prevActive && CompositionActive)
                {
                    if (SelectionActive)
                        DeleteSelection();
                    compositionPos = CursorPos;
                }
                else if (prevActive && !CompositionActive)
                {
                    offset = 0;
                    CursorPos = compositionPos + compositionTextCounter; //go to end of inputted composition text
                    compositionTextCounter = 0;
                    compositionPos = 0;
                    UpdateTextPositionOffset(true);
                }

                CursorPos += offset;

                _compositionString = value;
                updateUnderline = true;
                totalTextDirty = true;
            }
        }
        public string LastSentMessage
        {
            get => _chatMessage;
            set
            {
                string newVal = value ?? "";
                if (newVal == _chatMessage)
                    return;
                _chatMessage = newVal;
                updateCursor = true;
                updateTextPosition = true;
                totalTextDirty = true;
            }
        }
        public int CursorPos
        {
            get => cursorPos; set
            {
                int newVal = ClampCursorPos(value);
                if (newVal == cursorPos)
                    return;

                updateCursor = true;
                updateTextPosition = true;
                cursorPos = newVal;

            }
        }
        public int SelectionPos 
        { 
            get => selectionPos; 
            set
            {
                //block selection when compositionActive because we cant directly edit ime's raw string
                int newVal = CompositionActive || value <= -1? -1 : ClampCursorPos(value);
                if (newVal == selectionPos)
                    return;
                    updateCursor = true;
                    updateTextPosition = true;
                    selectionPos = newVal;           
            }
        }
        public bool CompositionActive => !string.IsNullOrEmpty(_compositionString);
        public bool CursorIsInMiddle => CursorPos < WholeText.Length;
        public bool SelectionActive => SelectionPos != -1;

        //for ime input positioning
        //used before ime input ui is created
        public virtual Vector2 CursorScreenPos
        {
            get
            {
                //incase deletion of selection is needed later
                Vector2 screenPos = menuLabel.ScreenPos;
                float textX = LabelTest.GetWidth(WholeText.Substring(0, SelectionActive? Mathf.Min(SelectionPos, CursorPos) : CursorPos));
                Vector2 global = new(screenPos.x + textX, screenPos.y);//_cursor._renderLayer._gameObject.transform.TransformPoint(_cursor._renderLayer._mesh.vertices[_cursor.firstFacetIndex * 4]);
                return global;
            }
        }
        public ChatTemplate(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.White);
            roundedRect = new RoundedRect(menu, owner, pos, size, true);
            Container.AddChild(textContainer = new());
            _selection = new FSprite("pixel", true);
            _selection.height = 14f;
            _selection.width = 0f;
            _selection.color = Color.grey;
            _selection.SetAnchor(0f, 0.5f);
            _selection.SetPosition(0f, (float)(this.size.y * 0.5) - 1f);
            textContainer.AddChild(_selection);

            padding = new(10, 0);
            //menuLabel = new(menu, owner, displayText, new(-roundedRect.size.x / 2 + 10f + pos.x, size.y / 2), size, false);
            menuLabel = new(menu, this, displayText, padding, new(0, size.y), false)
            {labelPosAlignment = FLabelAlignment.Left};
            menuLabel.label.alignment = FLabelAlignment.Left;
            textContainer.AddChild(menuLabel.myContainer);

            _cursor = new FSprite("modInputCursor", true)
            {
                anchorX = 0,
            };
            _cursor.SetPosition(menuLabel.size.x, (float)(this.size.y * 0.5));
            textContainer.AddChild(_cursor);

            compositionUnderline = new("pixel", true)
            {
                anchorX = 0,
                anchorY = 0,
                scaleY = 1.5f,
            };
            textContainer.AddChild(compositionUnderline);


            uiMask = new(menu, this, new(0, 0), size, textContainer, true)
            {
                CamViewPosOffset = new(2.5f, 0),
                CamViewSizeOffset = new(-5, 0)
            };
            subObjects.AddRange([roundedRect, uiMask, menuLabel]);

            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            SetIMEComposition = (Action<string>)Delegate.Combine(SetIMEComposition, new Action<string>(SetIMECompositionString));
        }
        public void AddTextAtPos(string t)
        {
            if (SelectionActive)
                DeleteSelection();
            string finalT = t.Substring(0, Mathf.Clamp(internalTextLimit - LastSentMessage.Length, 0, t.Length));
            if (finalT.Length == 0) 
                return;

            if (CompositionActive)
            {
                LastSentMessage = LastSentMessage.Insert(compositionPos + compositionTextCounter, finalT);
                compositionTextCounter += finalT.Length;
            }
            else
            {
                LastSentMessage = LastSentMessage.Insert(CursorPos, finalT);
                CursorPos += finalT.Length;
            }
            if (CursorPos == WholeText.Length)
                SetCursorSprite();
        }
        public void DeleteSelection()
        {
            LastSentMessage = LastSentMessage.Remove(Mathf.Min(CursorPos, SelectionPos), Mathf.Abs(SelectionPos - CursorPos));
            //UpdateLabel(lastSentMessage);
            if (SelectionPos < CursorPos) CursorPos = SelectionPos;
            SelectionPos = -1;
            UpdateTextPositionOffset();
        }
        public abstract void SetIMECompositionString(string compositionString);
        public abstract void CaptureInputs(char c);
        public int ClampCursorPos(int pos)
        {
            if (CompositionActive)
                return compositionPos + CompositionString.Length; //must always be at the end of compositionstring
            return Mathf.Clamp(pos, 0, LastSentMessage.Length);
        }
        public void UpdateLabel()
        {
            /* int firstLetterViewed = cursorPos > maxVisibleLength ? cursorPos - maxVisibleLength : 0,
               lastLetterViewed = Mathf.Max(0, cursorPos > maxVisibleLength ? maxVisibleLength : Mathf.Min(maxVisibleLength, text.Length));

           menuLabel.text = text.Substring(firstLetterViewed, lastLetterViewed);*/
            menuLabel.text = WholeText;
            if (updateCursor)
                SetCursorSprite();
            if (updateTextPosition)
                UpdateTextPositionOffset();
            if (updateUnderline)
                UpdateUnderline();

        }
        public void UpdateUnderline()
        {
            updateUnderline = false;
            if (!CompositionActive) return;
            compositionUnderlineWidth = LabelTest.GetWidth(WholeText.Substring(0, compositionPos));
            compositionUnderline.scaleX = LabelTest.GetWidth(WholeText.Substring(compositionPos, CompositionString.Length));

        }
        public void SetCursorSprite()
        {
            updateCursor = false;


            float width = LabelTest.GetWidth(WholeText.Substring(0, CursorPos));

            string element = CursorIsInMiddle? "pixel" : "modInputCursor";
            float height = CursorIsInMiddle ? 13f : 6f;
            float cursorScaleX = 1;
            float anchor = !CursorIsInMiddle || CursorPos > 0? 0 : 1;


            _cursorWidth = width;
            _cursor.element = Futile.atlasManager.GetElementWithName(element);
            _cursor.height = height;
            _cursor.scaleX = cursorScaleX;
            _cursor.anchorX = anchor;


        }
        public void UpdateTextPositionOffset(bool forceTextToTheEnd = false)
        {
            updateTextPosition = false;


            float padding = this.padding.x;

            //cursor is at start of text -> use padding, else remove it so cursor going back won't cause text to move too early to the left when it hasnt been blocked yet
            float startPosOfText = CursorPos > 0 ? 0 : padding;

            float sizeOffset = (CursorIsInMiddle || SelectionActive ? 0 : 10) + padding;
            float sizeToReference = size.x - sizeOffset;
            float labelWithCursorWidth = LabelTest.GetWidth(WholeText.Substring(0, CursorPos));


            //add padding to include menulabel original offset

            float cursorPosX = textPositionOffset.x + labelWithCursorWidth;
            float cursorPosXwithPadding = cursorPosX + padding;

            if (cursorPosXwithPadding < startPosOfText)
                textPositionOffset.x += startPosOfText - cursorPosXwithPadding;
            else if (cursorPosXwithPadding > sizeToReference || (forceTextToTheEnd && cursorPosXwithPadding < sizeToReference))
                textPositionOffset.x -= cursorPosXwithPadding - sizeToReference;

            textPositionOffset.x = Mathf.Min(textPositionOffset.x, 0);
        }
        public override void RemoveSprites()
        {
            textContainer.RemoveAllChildren();
            textContainer.RemoveFromContainer();
            base.RemoveSprites();
        }
        public override void GrafUpdate(float timeStacker)
        {
            uiMask.size = size;
            for (int i = 0; i < 9; i++)
                roundedRect.sprites[i].color = Color.black;

            UpdateLabel();
            menuLabel.pos = textPositionOffset + padding;

            Vector2 menuLabelTextPos = menuLabel.DrawPos(timeStacker);
            _cursor.x = _cursorWidth + menuLabelTextPos.x;
            _cursor.y = menuLabelTextPos.y + size.y / 2;
            _cursor.alpha = IsFocused() ? Mathf.PingPong(Time.time * 2f, 1f) : 0;
            _cursor.isVisible = !SelectionActive;

            compositionUnderline.x = menuLabelTextPos.x + compositionUnderlineWidth;
            compositionUnderline.y = menuLabelTextPos.y + menuLabel.label.textRect.y - 3;
            compositionUnderline.isVisible = CompositionActive;

            if (SelectionActive)
            {
                var lowest =  Mathf.Min(CursorPos, SelectionPos);
                float selectionPosX = LabelTest.GetWidth(LastSentMessage.Substring(0, lowest));
                float width = LabelTest.GetWidth(menuLabel.text.Substring(lowest, Mathf.Abs(SelectionPos - CursorPos)), false);
                _selection.isVisible = true;
                _selection.x = selectionPosX + menuLabelTextPos.x;
                _selection.y = menuLabelTextPos.y + size.y / 2;
                _selection.width = width;
            }
            else
                _selection.isVisible = false;
            _selection.alpha = IsFocused() ? 1f : 0f;
                base.GrafUpdate(timeStacker);
            this.roundedRect.fillAlpha = 1.0f;
        }

        public virtual bool IsFocused()
        {
            return true;
        }

     
    }
}
