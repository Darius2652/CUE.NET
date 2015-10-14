﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using CUE.NET.Devices.Keyboard.Brushes;
using CUE.NET.Devices.Keyboard.Extensions;

namespace CUE.NET.Devices.Keyboard.Keys
{
    public abstract class BaseKeyGroup : IKeyGroup
    {
        #region Properties & Fields

        internal CorsairKeyboard Keyboard { get; }

        public IEnumerable<CorsairKey> Keys => new ReadOnlyCollection<CorsairKey>(GetGroupKeys());

        public IBrush Brush { get; set; }

        public int ZIndex { get; set; } = 0;

        #endregion

        #region Constructors

        protected BaseKeyGroup(CorsairKeyboard keyboard, bool autoAttach = true)
        {
            this.Keyboard = keyboard;

            if (autoAttach)
                this.Attach();
        }

        protected abstract IList<CorsairKey> GetGroupKeys();

        #endregion
    }
}
