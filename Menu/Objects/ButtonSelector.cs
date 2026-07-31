using Menu;
using RainMeadow.UI.Components;
using RainMeadow.UI.Scrolling;
using System;
using System.ComponentModel;
using UnityEngine;
using static RainMeadow.UI.Scrolling.ScrollAddOns;

namespace RainMeadow
{
    //now includes the first button as another button to view
    //uses num of buttons to show now, the selector button is counted
    public class ButtonSelector : SimplerButton
    {
        public int NumberOfButtonsToShow { get => amtOfButtonsToShow; set => amtOfButtonsToShow = Mathf.Max(value, 2); } //this includes the open list button itself
        public float OrigDistanceBetweenButtonYPos => size.y + buttonSpacing;
        public float ListPosOffset => downwardsList ? -(buttonSpacing + listDownUpYOffset) : OrigDistanceBetweenButtonYPos + listDownUpYOffset; //readds the additional button spacing lost
        public float OrigStart => downwardsList ? -ButtonScroller.CalculateHeightBasedOnAmtOfButtons(NumberOfButtonsToShow - 1, size.y, buttonSpacing) : size.y;
        public float StartingYPoint => OrigStart + ListPosOffset;
        public ButtonSelector(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, int amtOfButtonsView, float spacingOfButton, string description = "") : base(menu, owner, displayText, pos, size, description)
        {
            NumberOfButtonsToShow = amtOfButtonsView;
            buttonSpacing = spacingOfButton;
            scrollSystem = new(size, new(0, buttonSpacing), NumberOfButtonsToShow - 1, true)
            {
                IncludeOtherPartOfSizePosSpacing = true,
            };
            OnClick += (_) =>
            {
                OpenCloseList(scrollContainer == null, true, false);
            };
        }
        public override void Update()
        {
            base.Update();
            if (scrollSystem == null) return;
            scrollSystem.ElementSize = size;
            scrollSystem.ElementSpacing = new(0, buttonSpacing);
            /* if (scroller != null)
            {
                scroller.buttonHeight = size.y;
                scroller.buttonSpacing = buttonSpacing;
            }*/
        }
        public virtual void RefreshScrollerList()
        {
            if (scrollContainer == null) return;
            scrollContainer.RemoveAllScrollElements(false);
            scrollContainer.AddScrollElements(populateList?.Invoke(this, scrollContainer));
            scrollContainer.ConstrainScroll();
            /*if (scroller != null)
            {
                scroller.RemoveAllButtons(false);
                scroller.AddScrollObjects(populateList?.Invoke(this, scroller));
                scroller.ConstrainScroll();
            }*/
        }
        public void OpenCloseList()
        {
            OpenCloseList(scrollContainer == null, true, true);
            //OpenCloseList(scroller == null, true, true);
        }
        public virtual void OpenCloseList(bool open, bool playSound, bool playSelectedIfClose)
        {
            if (open)
            {
                OpenList(playSound);
            }
            else
            {
                CloseList(playSound, playSelectedIfClose);
            }
        }
        public virtual void OpenList(bool playSound)
        {
            if (scrollContainer != null) return;
            scrollSystem.rowOrColumn = NumberOfButtonsToShow - 1;
            scrollContainer = new(menu, this, new(0, StartingYPoint), scrollSystem);
            scrollContainer.AddScrollElements(populateList?.Invoke(this, scrollContainer));
            scrollContainer.DesiredScrollOffset = scrollSystem.ScrollOffset;
            subObjects.Add(scrollContainer);
            if (playSound)
            {
                menu.PlaySound(SoundID.MENU_Checkbox_Check);
            }
            scrollContainer.Update();
            /*if (scroller == null)
            {
                scroller = new(menu, this, new(0, StartingYPoint), NumberOfButtonsToShow - 1, size.x, new(size.y, buttonSpacing));
                scroller.AddScrollObjects(populateList?.Invoke(this, scroller));
                subObjects.Add(scroller);
                if (playSound)
                {
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                }
            }*/
        }
        public virtual void CloseList(bool playSound, bool playSelected)
        {
            if (scrollContainer == null) return;
            if (playSound)
            {
                menu.PlaySound(playSelected ? SoundID.MENU_MultipleChoice_Clicked : SoundID.MENU_Checkbox_Uncheck);
            }
            this.ClearMenuObject(ref scrollContainer);
            UpdateUponClosingList();

            /*if (scroller != null)
            {
                if (playSound)
                {
                    menu.PlaySound(playSelected? SoundID.MENU_MultipleChoice_Clicked : SoundID.MENU_Checkbox_Uncheck);
                }
                this.ClearMenuObject(ref scroller);
                UpdateUponClosingList();
            }*/
        }
        public virtual void UpdateUponClosingList()
        {
           
        }
        public bool downwardsList = true;
        public float listDownUpYOffset, buttonSpacing;
        private int amtOfButtonsToShow;
        public ElementListSystem scrollSystem;
        public ScrollContainer? scrollContainer;
        public Func<ButtonSelector, ScrollContainer, MenuObject[]>? populateList;
        //public ButtonScroller? scroller;
        //public Func<ButtonSelector,ButtonScroller, ButtonScroller.IPartOfButtonScroller[]>? populateList;
    }
}