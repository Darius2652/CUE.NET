﻿// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using CUE.NET.Devices.Generic;
using CUE.NET.Devices.Keyboard.Brushes;
using CUE.NET.Devices.Keyboard.Effects;
using CUE.NET.Devices.Keyboard.Enums;
using CUE.NET.Devices.Keyboard.Keys;
using CUE.NET.Helper;
using CUE.NET.Native;

namespace CUE.NET.Devices.Keyboard
{
    /// <summary>
    /// Represents the SDK for a corsair keyboard.
    /// </summary>
    public class CorsairKeyboard : AbstractCueDevice, IEnumerable<CorsairKey>, IKeyGroup
    {
        #region Properties & Fields

        #region Indexer

        /// <summary>
        /// Gets the <see cref="CorsairKey" /> with the specified ID.
        /// </summary>
        /// <param name="keyId">The ID of the key to get.</param>
        /// <returns>The key with the specified ID or null if no key is found.</returns>
        public CorsairKey this[CorsairKeyboardKeyId keyId]
        {
            get
            {
                CorsairKey key;
                return _keys.TryGetValue(keyId, out key) ? key : null;
            }
        }

        /// <summary>
        /// Gets the <see cref="CorsairKey" /> representing the given character by calling the SDK-method 'CorsairGetLedIdForKeyName'.<br />
        /// Note that this currently only works for letters.
        /// </summary>
        /// <param name="key">The character of the key.</param>
        /// <returns>The key representing the given character or null if no key is found.</returns>
        public CorsairKey this[char key]
        {
            get
            {
                CorsairKeyboardKeyId keyId = _CUESDK.CorsairGetLedIdForKeyName(key);
                CorsairKey cKey;
                return _keys.TryGetValue(keyId, out cKey) ? cKey : null;
            }
        }

        /// <summary>
        /// Gets the <see cref="CorsairKey" /> at the given physical location.
        /// </summary>
        /// <param name="location">The point to get the key from.</param>
        /// <returns>The key at the given point or null if no key is found.</returns>
        public CorsairKey this[PointF location] => _keys.Values.FirstOrDefault(x => x.KeyRectangle.Contains(location));

        /// <summary>
        /// Gets a list of <see cref="CorsairKey" /> inside the given rectangle.
        /// </summary>
        /// <param name="referenceRect">The rectangle to check.</param>
        /// <param name="minOverlayPercentage">The minimal percentage overlay a key must have with the <see cref="Rectangle" /> to be taken into the list.</param>
        /// <returns></returns>
        public IEnumerable<CorsairKey> this[RectangleF referenceRect, float minOverlayPercentage = 0.5f] => _keys.Values.Where(x => RectangleHelper.CalculateIntersectPercentage(x.KeyRectangle, referenceRect) >= minOverlayPercentage);

        #endregion

        private readonly LinkedList<IKeyGroup> _keyGroups = new LinkedList<IKeyGroup>();
        private readonly LinkedList<EffectTimeContainer> _effects = new LinkedList<EffectTimeContainer>();

        private Dictionary<CorsairKeyboardKeyId, CorsairKey> _keys = new Dictionary<CorsairKeyboardKeyId, CorsairKey>();

        /// <summary>
        /// Gets a read-only collection containing the keys of the keyboard.
        /// </summary>
        public IEnumerable<CorsairKey> Keys => new ReadOnlyCollection<CorsairKey>(_keys.Values.ToList());

        /// <summary>
        /// Gets specific information provided by CUE for the keyboard.
        /// </summary>
        public CorsairKeyboardDeviceInfo KeyboardDeviceInfo { get; }

        /// <summary>
        /// Gets the rectangle containing all keys of the keyboard.
        /// </summary>
        public RectangleF KeyboardRectangle { get; private set; }

        /// <summary>
        /// Gets or sets the background brush of the keyboard.
        /// </summary>
        public IBrush Brush { get; set; }

        /// <summary>
        /// Gets or sets the z-index of the background brush of the keyboard.<br />
        /// This value has absolutely no effect.
        /// </summary>
        public int ZIndex { get; set; } = 0;

        /// <summary>
        /// Gets a value indicating if the keyboard has an active effect to deal with or not.
        /// </summary>
        protected override bool HasEffect
        {
            get
            {
                lock (_effects)
                    return _effects.Any();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CorsairKeyboard"/> class.
        /// </summary>
        /// <param name="info">The specific information provided by CUE for the keyboard</param>
        internal CorsairKeyboard(CorsairKeyboardDeviceInfo info)
            : base(info)
        {
            this.KeyboardDeviceInfo = info;

            InitializeKeys();
            KeyboardRectangle = RectangleHelper.CreateRectangleFromRectangles(this.Select(x => x.KeyRectangle));
        }

        #endregion

        #region Methods

        #region Update

        /// <summary>
        /// Updates all groups and effects and perform an update for all dirty keys, or all keys if flushLeds is set to true.
        /// </summary>
        /// <param name="flushLeds">Specifies whether all keys (including clean ones) should be updated.</param>
        public override void Update(bool flushLeds = false)
        {
            UpdateKeyGroups();
            UpdateEffects();

            // Perform 'real' update
            base.Update(flushLeds);
        }

        private void UpdateKeyGroups()
        {
            if (Brush != null)
                ApplyBrush(this.ToList(), Brush);

            lock (_keyGroups)
            {
                foreach (IKeyGroup keyGroup in _keyGroups.OrderBy(x => x.ZIndex))
                    ApplyBrush(keyGroup.Keys.ToList(), keyGroup.Brush);
            }
        }

        private void UpdateEffects()
        {
            List<IEffect> effectsToRemove = new List<IEffect>();
            lock (_effects)
            {
                long currentTicks = DateTime.Now.Ticks;
                foreach (EffectTimeContainer effect in _effects.OrderBy(x => x.ZIndex))
                {
                    try
                    {
                        float deltaTime;
                        if (effect.TicksAtLastUpdate < 0)
                        {
                            effect.TicksAtLastUpdate = currentTicks;
                            deltaTime = 0f;
                        }
                        else
                            deltaTime = (currentTicks - effect.TicksAtLastUpdate) / 10000000f;

                        effect.TicksAtLastUpdate = currentTicks;
                        effect.Effect.Update(deltaTime);

                        ApplyBrush((effect.Effect.KeyList ?? this).ToList(), effect.Effect.EffectBrush);

                        if (effect.Effect.IsDone)
                            effectsToRemove.Add(effect.Effect);
                    }
                    // ReSharper disable once CatchAllClause
                    catch (Exception ex) { ManageException(ex); }
                }
            }

            foreach (IEffect effect in effectsToRemove)
                DetachEffect(effect);
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local - idc
        private void ApplyBrush(ICollection<CorsairKey> keys, IBrush brush)
        {
            try
            {
                RectangleF brushRectangle = RectangleHelper.CreateRectangleFromRectangles(keys.Select(x => x.KeyRectangle));
                foreach (CorsairKey key in keys)
                    key.Led.Color = brush.GetColorAtPoint(brushRectangle, key.KeyRectangle.GetCenter());
            }
            // ReSharper disable once CatchAllClause
            catch (Exception ex) { ManageException(ex); }
        }

        #endregion

        /// <summary>
        /// Attaches the given keygroup.
        /// </summary>
        /// <param name="keyGroup">The keygroup to attach.</param>
        /// <returns><c>true</c> if the keygroup could be attached; otherwise, <c>false</c>.</returns>
        public bool AttachKeyGroup(IKeyGroup keyGroup)
        {
            lock (_keyGroups)
            {
                if (keyGroup == null || _keyGroups.Contains(keyGroup)) return false;

                _keyGroups.AddLast(keyGroup);
                return true;
            }
        }

        /// <summary>
        /// Detaches the given keygroup.
        /// </summary>
        /// <param name="keyGroup">The keygroup to detached.</param>
        /// <returns><c>true</c> if the keygroup could be detached; otherwise, <c>false</c>.</returns>
        public bool DetachKeyGroup(IKeyGroup keyGroup)
        {
            lock (_keyGroups)
            {
                if (keyGroup == null) return false;

                LinkedListNode<IKeyGroup> node = _keyGroups.Find(keyGroup);
                if (node == null) return false;

                _keyGroups.Remove(node);
                return true;
            }
        }

        /// <summary>
        /// Attaches the given effect.
        /// </summary>
        /// <param name="effect">The effect to attach.</param>
        /// <returns><c>true</c> if the effect could be attached; otherwise, <c>false</c>.</returns>
        public bool AttachEffect(IEffect effect)
        {
            bool retVal = false;
            lock (_effects)
            {
                if (effect != null && _effects.All(x => x.Effect != effect))
                {
                    effect.OnAttach();
                    _effects.AddLast(new EffectTimeContainer(effect, -1));
                    retVal = true;
                }
            }

            CheckUpdateLoop();
            return retVal;
        }

        /// <summary>
        /// Detaches the given effect.
        /// </summary>
        /// <param name="effect">The effect to detached.</param>
        /// <returns><c>true</c> if the effect could be detached; otherwise, <c>false</c>.</returns>
        public bool DetachEffect(IEffect effect)
        {
            bool retVal = false;
            lock (_effects)
            {
                if (effect != null)
                {
                    EffectTimeContainer val = _effects.FirstOrDefault(x => x.Effect == effect);
                    if (val != null)
                    {
                        effect.OnDetach();
                        _effects.Remove(val);
                        retVal = true;
                    }
                }
            }
            CheckUpdateLoop();
            return retVal;
        }

        private void InitializeKeys()
        {
            _CorsairLedPositions nativeLedPositions = (_CorsairLedPositions)Marshal.PtrToStructure(_CUESDK.CorsairGetLedPositions(), typeof(_CorsairLedPositions));
            int structSize = Marshal.SizeOf(typeof(_CorsairLedPosition));
            IntPtr ptr = nativeLedPositions.pLedPosition;
            for (int i = 0; i < nativeLedPositions.numberOfLed; i++)
            {
                _CorsairLedPosition ledPosition = Marshal.PtrToStructure<_CorsairLedPosition>(ptr);
                CorsairLed led = GetLed((int)ledPosition.ledId);
                _keys.Add(ledPosition.ledId, new CorsairKey(ledPosition.ledId, led,
                    new RectangleF((float)ledPosition.left, (float)ledPosition.top, (float)ledPosition.width, (float)ledPosition.height)));

                ptr = new IntPtr(ptr.ToInt64() + structSize);
            }
        }

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates over all keys of the keyboard.
        /// </summary>
        /// <returns>An enumerator for all keys of the keyboard.</returns>
        public IEnumerator<CorsairKey> GetEnumerator()
        {
            return _keys.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #endregion
    }
}
