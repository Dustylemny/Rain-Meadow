using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Scrolling
{
    public class ScrollAddOns
    {
        public interface IScrollContainer
        {
            public void UpdateDirtyElement(ScrollAddOns addOn);
        }
        public static ConditionalWeakTable<MenuObject, ScrollAddOns> addOns = new();
        public IScrollContainer? myScrollBox;
        public ScrollAddOns? myActualParentToScrollContainer;
        public MenuObject menuObject;
        //relativetoscrollbox is when your owner or previous owners are paired to scrollbox while alpha is used for when u are paired to scrollBox
        public float alphaRelativeToScrollBox, myAlpha = 1;
        public ScrollAddOns(MenuObject scrollObj)
        {
            if (addOns.TryGetValue(scrollObj, out _))
                throw new ArgumentException("ScrolalbleMenuObject has an assigned scrollAddon");
            menuObject = scrollObj;
            addOns.Add(menuObject, this);
            if (menuObject.myContainer != null) return;
            menuObject.myContainer = new();
            (menuObject.owner.Container ?? scrollObj.menu.container).AddChild(menuObject.myContainer);
        }
        public float GetActualAlpha()
        {
            if (myActualParentToScrollContainer != null)
                return alphaRelativeToScrollBox;
            return myAlpha;
        }
        public void CallSubObjectsToSetScrollBox(MenuObject subObj)
        {
            for (int i = 0; i < subObj.subObjects.Count; i++)
            {
                var subsubObj = subObj.subObjects[i];
                if (subsubObj.HasScrollAddOn(out var addON)) //should be true as always
                {
                    addON.myScrollBox = myScrollBox;
                    addON.myActualParentToScrollContainer = myScrollBox != null? this : null;
                }
                CallSubObjectsToSetScrollBox(subsubObj);
            }
        }
        public void AssignScrollBox(IScrollContainer? scrollBox)
        {
            myScrollBox = scrollBox;
            CallSubObjectsToSetScrollBox(menuObject);
        }

        //this runs before myObject's orig update
        public void Update()
        {
            if (myScrollBox == null)
            {
                myAlpha = 1;
                return;
            }
            if (myActualParentToScrollContainer == null)
            { //we are directly connected to scrollBox
                myScrollBox.UpdateDirtyElement(this);
            }
            else
            {
                alphaRelativeToScrollBox = myActualParentToScrollContainer.myAlpha;
            }
        }
        public void GrafUpdate(float timeStacker)
        {
            menuObject.Container.alpha = myAlpha;
        }
    }
}
