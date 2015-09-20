﻿using System.Drawing;

namespace CUE.NET.Devices.Generic
{
    public class CorsairLed
    {
        #region Properties & Fields

        public bool IsDirty => RequestedColor != _color;
        public bool IsUpdated { get; private set; }

        public Color RequestedColor { get; private set; } = Color.Transparent;

        private Color _color = Color.Transparent;
        public Color Color
        {
            get { return _color; }
            set
            {
                if (!IsLocked)
                {
                    RequestedColor = value;
                    IsUpdated = true;
                }
            }
        }

        public bool IsLocked { get; set; } = false;

        //TODO DarthAffe 19.09.2015: Add effects and stuff

        #endregion

        #region Constructors

        internal CorsairLed() { }

        #endregion

        #region Methods

        internal void Update()
        {
            _color = RequestedColor;
            IsUpdated = false;
        }

        #endregion
    }
}
