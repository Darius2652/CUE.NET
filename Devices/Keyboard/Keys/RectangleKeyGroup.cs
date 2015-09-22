﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CUE.NET.Devices.Keyboard.Enums;
using CUE.NET.Helper;

namespace CUE.NET.Devices.Keyboard.Keys
{
    public class RectangleKeyGroup : BaseKeyGroup
    {
        #region Properties & Fields

        public RectangleF Rectangle { get; set; }
        public float MinOverlayPercentage { get; set; }

        #endregion

        #region Constructors

        public RectangleKeyGroup(CorsairKeyboard keyboard, CorsairKeyboardKeyId fromKey, CorsairKeyboardKeyId toKey, float minOverlayPercentage = 0.5f, bool autoAttach = true)
            : this(keyboard, keyboard[fromKey], keyboard[toKey], minOverlayPercentage, autoAttach)
        { }

        public RectangleKeyGroup(CorsairKeyboard keyboard, CorsairKey fromKey, CorsairKey toKey, float minOverlayPercentage = 0.5f, bool autoAttach = true)
            : this(keyboard, RectangleHelper.CreateRectangleFromRectangles(fromKey.KeyRectangle, toKey.KeyRectangle), minOverlayPercentage, autoAttach)
        { }

        public RectangleKeyGroup(CorsairKeyboard keyboard, PointF fromPoint, PointF toPoint, float minOverlayPercentage = 0.5f, bool autoAttach = true)
            : this(keyboard, RectangleHelper.CreateRectangleFromPoints(fromPoint, toPoint), minOverlayPercentage, autoAttach)
        { }

        public RectangleKeyGroup(CorsairKeyboard keyboard, RectangleF rectangle, float minOverlayPercentage = 0.5f, bool autoAttach = true)
            : base(keyboard, autoAttach)
        {
            this.Rectangle = rectangle;
            this.MinOverlayPercentage = minOverlayPercentage;
        }

        #endregion

        #region Methods

        protected override IList<CorsairKey> GetGroupKeys()
        {
            return Keyboard.Where(x => RectangleHelper.CalculateIntersectPercentage(x.KeyRectangle, Rectangle) >= MinOverlayPercentage).ToList();
        }

        #endregion
    }
}
