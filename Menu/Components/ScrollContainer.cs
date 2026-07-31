using Menu;
using RainMeadow.UI.Interfaces;
using RainMeadow.UI.Scrolling;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class ScrollContainer : RectangularMenuObject, ScrollAddOns.IScrollContainer, IPLEASEUPDATEME
    {
        public ObservableCollection<ScrollAddOns> elements = [];
        public FContainer itemContainer;
        public ScrollSystem scrollSystem;
        public Vector2 contentSize;
        public bool defaultSliderDown = false, isScrolling;
        protected float floatScrollSpeed, scrollSliderValueCap, scrollSliderValue;
        public float desiredScrollOffset, maxScrollSpeed = 1.2f, scrollSliderCapLerp = 0.02f, scrollSliderCapTick = 0.05f;
        public bool IsHidden { get; set; }
        public virtual float DesiredScrollOffset
        {
            get => desiredScrollOffset;
            set
            {
                desiredScrollOffset = value;
                desiredScrollOffset = Mathf.Clamp(desiredScrollOffset, 0, scrollSystem.MaxScroll);

            }
        }
        public ScrollContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, ScrollSystem scrollSystem) : this(menu, owner, pos, scrollSystem.CalculatePreSizeOfContainer(), scrollSystem)
        {
        }
        public ScrollContainer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, ScrollSystem scrollSystem) : base(menu, owner, pos, size)
        {
            (owner?.Container ?? menu.container).AddChild(myContainer = new());
            myContainer.AddChild(itemContainer = new());
            this.scrollSystem = scrollSystem;
            elements.CollectionChanged += OnElementCollectionChanged;
        }
        public void OnElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            scrollSystem.ContentSize = scrollSystem.GetContentSize(elements.Count);
            scrollSystem.UpdateMaxScroll();
        }
        public void MoveTo(ScrollSystem.Anchor anchor, bool moveInstantly = true)
        {
            DesiredScrollOffset = scrollSystem.GetAnchorScrollOffset(anchor);
            if (moveInstantly)
                InstantlySetScroll();
        }
        public bool IsAt(ScrollSystem.Anchor anchor)
        {
            float toCHeck = scrollSystem.GetAnchorScrollOffset(anchor);
            return DesiredScrollOffset == toCHeck;
        }
        public void ConstrainScroll(bool constrainInstantly = false)
        {
            float desired = Mathf.Clamp(DesiredScrollOffset, 0, scrollSystem.MaxScroll);
            DesiredScrollOffset = desired;
            if (constrainInstantly)
                InstantlySetScroll();
        }
        public virtual float GetCurrentScrollOffset() => DesiredScrollOffset;
        public void InstantlySetScroll()
        {
            scrollSystem.ScrollOffset = DesiredScrollOffset;
        }
        public void ScrollingUpdate(float yInput)
        {
            float newScrollInput = scrollSystem.AdjustScrollInput(yInput);
            if ((newScrollInput < 0 && DesiredScrollOffset > 0) || (newScrollInput > 0 && DesiredScrollOffset < scrollSystem.MaxScroll))
            {
                //scrolling up -, scrolling down +
                DesiredScrollOffset += newScrollInput;
                menu.PlaySound(SoundID.MENU_Scroll_Tick);
                isScrolling = true;
            }
        }
        /// <summary>
        /// the objects must be IMenuScrollObject or have scrolladdon (be warned have ur own container)
        /// </summary>
        /// <param name="objs"></param>
        public void AddScrollElements(params MenuObject[]? objs)
        {
            if (objs == null) return;
            for (int i = 0; i < objs.Length; i++)
            {
                var obj = objs[i];
                if (!obj.HasScrollAddOn(out var addOn)) throw new InvalidOperationException("Missing add on!");
                AddScrollElement(addOn);

            }
        }
        public void AddScrollElement(ScrollAddOns scrollAddOn)
        {
            elements.Add(scrollAddOn);
            subObjects.Add(scrollAddOn.menuObject);
            itemContainer.AddChild(scrollAddOn.menuObject.Container);
            scrollAddOn.AssignScrollBox(this);
            scrollSystem.elementsDirty = true;
        }
        public void RemoveAllScrollElements(bool constrainScroll = true)
        {
            this.ClearMenuObjectIList(elements.Select(x => x.menuObject));
            elements.Clear();
            if (constrainScroll) ConstrainScroll();
        }
        public void RemoveScrollElement(MenuObject menuObj, bool constrainScroll = true)
        {
            var elementToRemov = elements.FirstOrDefault(x => x.menuObject == menuObj);
            if (elementToRemov != null)
                RemoveScrollElement(elementToRemov, constrainScroll);
        }
        public void RemoveScrollElement(ScrollAddOns addOn,bool constrainScroll = true)
        {
            elements.Remove(addOn);
            addOn.AssignScrollBox(null);
            this.ClearMenuObject(addOn.menuObject);
            if (constrainScroll)
                ConstrainScroll();
        }
        public float GetAlphaOfElement(Vector2 elementPos, Vector2 elementSize)
        {
            var viewSize = scrollSystem.ViewSizeOfContainer;
            if (scrollSystem.isVertical)
            {
                float topOfElement = elementPos.y + elementSize.y;
                float mid = elementSize.y * 0.5f;
                if (elementPos.y < 0)
                    return Mathf.InverseLerp(0 - mid, 0, elementPos.y);
                if (topOfElement > viewSize.y)
                    return Mathf.InverseLerp(viewSize.y + mid, viewSize.y, topOfElement);
                return 1;
            }
            return 1;
        }
        //scroll subobjects call this on subobjects[i].update
        public virtual void UpdateDirtyElement(ScrollAddOns addOns)
        {
            if (!scrollSystem.elementsDirty) return;
            Vector2 origPos = Vector2.zero, origSize = Vector2.zero;
            PositionedMenuObject? posObj = null;
            RectangularMenuObject? rectObj = null;
            if (addOns.menuObject is PositionedMenuObject posObject)
            {
                posObj = posObject;
                origPos = posObject.pos;
                if (posObject is RectangularMenuObject rectObject)
                {
                    origSize = rectObject.size;
                    rectObj = rectObject;
                }
            }
            int index = elements.IndexOf(addOns);
            Vector2 finalPos = scrollSystem.GetRelativePositionOfScrollObject(elements.Count, index, origPos);
            Vector2 finalSize = scrollSystem.GetAdjustedElementSize(origSize);
            posObj?.pos = finalPos;
            rectObj?.size = finalSize;
            addOns.myAlpha = GetAlphaOfElement(finalPos, finalSize);
        }
        public override void Update()
        {
            scrollSystem.ViewSizeOfContainer = size;
            scrollSystem.ContentSize = scrollSystem.GetContentSize(elements.Count);
            scrollSystem.Update();
            if (!IsHidden && !menu.FreezeMenuFunctions && MouseOver && menu.manager.menuesMouseMode) 
                ScrollingUpdate(menu.mouseScrollWheelMovement);
            base.Update(); //updateDirtyElementPositionSize gets updated here
            scrollSystem.elementsDirty = false; //updated

            float currentScrollOffset = GetCurrentScrollOffset();
            scrollSystem.ScrollOffset = Custom.LerpAndTick(scrollSystem.ScrollOffset, currentScrollOffset, 0.01f, 0.01f);
            floatScrollSpeed *= Custom.LerpMap(Math.Abs(currentScrollOffset - scrollSystem.ScrollOffset), 0.25f, 1.5f, 0.45f, 0.99f);
            floatScrollSpeed += Mathf.Clamp(currentScrollOffset - scrollSystem.ScrollOffset, -2.5f, 2.5f) / 2.5f * 0.15f;
            floatScrollSpeed = Mathf.Clamp(floatScrollSpeed, -maxScrollSpeed, maxScrollSpeed);
            scrollSystem.ScrollOffset += floatScrollSpeed;

            var max = scrollSystem.MaxScroll;

            scrollSliderValueCap = Custom.LerpAndTick(scrollSliderValueCap, max, scrollSliderCapLerp, elements.Count / 40f);

            if (max == 0) scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, defaultSliderDown ? 1 : 0, scrollSliderCapLerp, scrollSliderCapTick);
            else scrollSliderValue = Custom.LerpAndTick(scrollSliderValue, Mathf.InverseLerp(0f, scrollSliderValueCap, scrollSystem.ScrollOffset), isScrolling ? Mathf.Max(0.9f, scrollSliderCapLerp) : scrollSliderCapLerp, scrollSliderCapTick);
        }

    }
}
