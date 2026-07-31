using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RainMeadow.UI.Scrolling.ElementListSystem;

namespace RainMeadow.UI.Scrolling
{
    //helps out with element position, spacing, basically also startUp for btn scroller
    public abstract class ScrollSystem
    {
        public enum Anchor
        {
            Left,
            Right,
            Top,
            Bottom,
        }
        public bool isVertical, elementsDirty, maxScrollDirty;
        private Anchor anchor;
        private float scrollOffset, maxScroll;
        private Vector2 viewSizeOfContainer, contentSizeOfContainer;
        public Vector2 ContentSize
        {
            get => contentSizeOfContainer;
            set
            {
                if (value == contentSizeOfContainer) return;
                contentSizeOfContainer = value;
                maxScrollDirty = true;
            }
        }
        public Vector2 ViewSizeOfContainer
        {
            get => viewSizeOfContainer;
            set
            {
                if (value == viewSizeOfContainer) return;
                viewSizeOfContainer = value;
                elementsDirty = true;
                maxScrollDirty = true;
            }
        }
        public float MaxScroll => maxScroll;
        public float ScrollOffset
        {
            get => scrollOffset;
            set
            {
                if (value == scrollOffset) return;
                scrollOffset = value;
                elementsDirty = true;
            }
        }
        public Anchor ScrollAnchor
        {
            get => anchor;
            set
            {
                if (anchor == value) return;
                if (!IsAnchorValid(value, isVertical))
                    throw new ArgumentException();
                anchor = value;
                elementsDirty = true;
            }
        }
        public ScrollSystem(bool isVertical)
        {
            this.isVertical = isVertical;
            anchor = isVertical ? Anchor.Top : Anchor.Left;
        }
        public static bool IsAnchorValid(Anchor anchor,bool verticalAlignment)
        {
            return (verticalAlignment && (anchor == Anchor.Bottom || anchor == Anchor.Top)) || (!verticalAlignment && (anchor == Anchor.Left || anchor == Anchor.Right));
        }
        public virtual void Update()
        {
            if (maxScrollDirty)
                UpdateMaxScroll();
        }
        public void UpdateMaxScroll()
        {
            maxScrollDirty = false;
            maxScroll = GetMaxScroll(ContentSize);
        }
        public float GetAnchorScrollOffset(Anchor anchor)
        {
            if (!IsAnchorValid(anchor, isVertical))
                throw new ArgumentException();
            return GetScrollOffsetOfAnchor(anchor);
        }
        protected virtual float GetScrollOffsetOfAnchor(Anchor anchor)
        {
            if (anchor == ScrollAnchor)
                return 0;
            else return MaxScroll;
        }
        public virtual float AdjustScrollInput(float scrollInput)
        {
            return scrollInput;
        }
        public abstract Vector2 GetContentSize(int count);
        public abstract float GetMaxScroll(Vector2 contentSize);
        public abstract Vector2 GetAdjustedElementSize(Vector2 origSize);
        public abstract Vector2 CalculatePreSizeOfContainer();
        public abstract Vector2 GetRelativePositionOfScrollObject(int count, int index, Vector2 origPos);
    }
}
