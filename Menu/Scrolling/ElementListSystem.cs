using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Scrolling
{
    public class ElementListSystem : ScrollSystem
    {
       
        private bool includeOtherPartOfSizePosSpacing = false; //that means we dont ignore x if vertical or ignore y if horizontal on getting relative pos, or size
        private Vector2 elementSize, elementSpacing;
        public int rowOrColumn; //for viewsize calculation
        public bool anchorOnBottomOrRight = false, startEndRowSpacing, startEndColumnSpacing;
        public float ElementSpacingFactor => ElementFloatSpacing + ElementFloatSize;
        public float ElementFloatSpacing => isVertical ? ElementSpacingY : ElementSpacingX;
        public float ElementFloatSize => isVertical ? ElementSizeY : ElementSizeX;
        public Vector2 ElementSize
        {
            get => elementSize;
            set
            {
                if (ElementSize == value) return;
                ElementSizeX = value.x;
                ElementSizeY = value.y;
            }
        }
        public Vector2 ElementSpacing
        {
            get => elementSpacing;
            set
            {
                if (value == ElementSpacing)
                    return;
                ElementSpacingX = value.x;
                ElementSpacingY = value.y;
            }
        }
        public bool IncludeOtherPartOfSizePosSpacing
        {
            get => includeOtherPartOfSizePosSpacing;
            set
            {
                if (value == includeOtherPartOfSizePosSpacing) return;
                includeOtherPartOfSizePosSpacing = value;
                elementsDirty = true;
            }
        }
        public float ElementSpacingY
        {
            get => elementSpacing.y;
            set
            {
                if (elementSpacing.y == value) return;
                elementSpacing.y = value;
                if (isVertical || includeOtherPartOfSizePosSpacing)
                    elementsDirty = true;
            }
        }
        public float ElementSpacingX
        {
            get => elementSpacing.x;
            set
            {
                if (elementSize.x ==  value) return;
                elementSize.x = value;
                if (!isVertical || includeOtherPartOfSizePosSpacing)
                    elementsDirty = true;
            }
        }
        public float ElementSizeY
        {
            get => elementSize.y;
            set
            {
                if (elementSize.y == value) return;
                elementSize.y = value;
                if (isVertical || includeOtherPartOfSizePosSpacing)
                    elementsDirty = true;
            }
        }
        public float ElementSizeX
        {
            get => elementSize.x;
            set
            {
                if (elementSize.x == value) return;
                elementSize.x = value;
                if (!isVertical || includeOtherPartOfSizePosSpacing)
                    elementsDirty = true;
            }
        }
        public ElementListSystem(Vector2 elementSize, Vector2 elementSpacing, int rowOrColumn, bool isVertical = true) : base(isVertical)
        {
            this.elementSize = elementSize;
            this.elementSpacing = elementSpacing;
            this.rowOrColumn = rowOrColumn;
        }
        public float GetBoundSizeOffset()
        {
            if (isVertical)
            {
                return startEndColumnSpacing ? elementSpacing.y : -elementSpacing.y;
            }
            return startEndRowSpacing ? elementSpacing.x : -elementSpacing.x;
        }
        public float GetScrollSize()
        {
            return (isVertical ? ViewSizeOfContainer.y : ViewSizeOfContainer.x) - GetBoundSizeOffset();
        }
        public override void Update()
        {
            base.Update();
        }
        public override Vector2 GetContentSize(int count)
        {
            float actualContent = count * ElementSpacingFactor;
            return new Vector2(count, actualContent);
        }
        public override float GetMaxScroll(Vector2 contentSize) //x = actualbtn count
        {
            var f = GetScrollSize();
            return Mathf.Max(0, contentSize.x - (f / ElementSpacingFactor));
        }
        public override float AdjustScrollInput(float scrollInput)
        {
            if (isVertical && ScrollAnchor == Anchor.Bottom)
            {
                return -scrollInput;
            }
            return base.AdjustScrollInput(scrollInput);
        }
        public override Vector2 GetAdjustedElementSize(Vector2 origSize)
        {
            origSize.x = !isVertical || includeOtherPartOfSizePosSpacing? elementSize.x : origSize.x;
            origSize.y = isVertical || includeOtherPartOfSizePosSpacing ? elementSize.y : origSize.y;
            return origSize;
        }
        public override Vector2 CalculatePreSizeOfContainer()
        {
            int row = isVertical ? 1 : rowOrColumn;
            int column = isVertical ? rowOrColumn : 1;

            return new(ButtonScroller.CalculateHeightBasedOnAmtOfButtons(row, elementSize.x, elementSpacing.x, startEndRowSpacing),
                ButtonScroller.CalculateHeightBasedOnAmtOfButtons(column, elementSize.y, elementSpacing.y, startEndColumnSpacing));
        }
        public override Vector2 GetRelativePositionOfScrollObject(int count, int index, Vector2 origPos)
        {
            float x = origPos.x;
            float y = origPos.y;
            float factor = ElementSpacingFactor;
            if (isVertical)
            {
                if (ScrollAnchor == Anchor.Top)
                {
                    y = ViewSizeOfContainer.y - GetBoundSizeOffset() - ((index + 1) * factor);
                    y += ScrollOffset * factor;
                }
                else
                {
                    y = -GetBoundSizeOffset() + ((count - index - 1) * factor);
                    y -= ScrollOffset * factor;
                }
            }
            else
            {
            }
            return new(x, y);
        }

    }
}
