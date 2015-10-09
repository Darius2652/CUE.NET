﻿// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global

using System.Drawing;

namespace CUE.NET.Devices.Keyboard.Brushes.Gradient
{
    public class GradientStop
    {
        #region Properties & Fields

        public float Offset { get; set; }

        public Color Color { get; set; }

        #endregion

        #region Constructors

        public GradientStop(float offset, Color color)
        {
            this.Offset = offset;
            this.Color = color;
        }

        #endregion
    }
}
