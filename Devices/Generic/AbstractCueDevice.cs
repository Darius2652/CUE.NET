﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Native;

namespace CUE.NET.Devices.Generic
{
    /// <summary>
    /// Represents a generic CUE-device. (keyboard, mouse, headset, ...)
    /// </summary>
    public abstract class AbstractCueDevice : ICueDevice
    {
        #region Properties & Fields

        /// <summary>
        /// Gets generic information provided by CUE for the device.
        /// </summary>
        public IDeviceInfo DeviceInfo { get; }

        private UpdateMode _updateMode = UpdateMode.AutoOnEffect;
        /// <summary>
        /// Gets or sets the update-mode for the device.
        /// </summary>
        public UpdateMode UpdateMode
        {
            get { return _updateMode; }
            set
            {
                _updateMode = value;
                CheckUpdateLoop();
            }
        }
        /// <summary>
        /// Gets or sets the update-frequency in seconds. (Calculate by using '1f / updates per second')
        /// </summary>
        public float UpdateFrequency { get; set; } = 1f / 30f;

        /// <summary>
        /// Gets a dictionary containing all LEDs of the device.
        /// </summary>
        protected Dictionary<int, CorsairLed> Leds { get; } = new Dictionary<int, CorsairLed>();

        /// <summary>
        /// Indicates if the device has an active effect to deal with.
        /// </summary>
        protected abstract bool HasEffect { get; }

        private CancellationTokenSource _updateTokenSource;
        private CancellationToken _updateToken;
        private Task _updateTask;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a catched exception is thrown inside the device.
        /// </summary>
        public event OnExceptionEventHandler OnException;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractCueDevice"/> class.
        /// </summary>
        /// <param name="info">The generic information provided by CUE for the device.</param>
        protected AbstractCueDevice(IDeviceInfo info)
        {
            this.DeviceInfo = info;

            CheckUpdateLoop();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the LED-Object with the specified id.
        /// </summary>
        /// <param name="ledId">The LED-Id to look for.</param>
        /// <returns></returns>
        protected CorsairLed GetLed(int ledId)
        {
            if (!Leds.ContainsKey(ledId))
                Leds.Add(ledId, new CorsairLed());

            return Leds[ledId];
        }

        /// <summary>
        /// Checks if automatic updates should occur and starts/stops the update-loop if needed.
        /// </summary>
        protected async void CheckUpdateLoop()
        {
            bool shouldRun;
            switch (UpdateMode)
            {
                case UpdateMode.Manual:
                    shouldRun = false;
                    break;
                case UpdateMode.AutoOnEffect:
                    shouldRun = HasEffect;
                    break;
                case UpdateMode.Continuous:
                    shouldRun = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (shouldRun && _updateTask == null) // Start task
            {
                _updateTokenSource?.Dispose();
                _updateTokenSource = new CancellationTokenSource();
                _updateTask = Task.Factory.StartNew(UpdateLoop, (_updateToken = _updateTokenSource.Token));
            }
            else if (!shouldRun && _updateTask != null) // Stop task
            {
                _updateTokenSource.Cancel();
                await _updateTask;
                _updateTask.Dispose();
                _updateTask = null;
            }
        }

        private void UpdateLoop()
        {
            while (!_updateToken.IsCancellationRequested)
            {
                long preUpdateTicks = DateTime.Now.Ticks;
                Update();
                int sleep = (int)((UpdateFrequency * 1000f) - ((DateTime.Now.Ticks - preUpdateTicks) / 10000f));
                if (sleep > 0)
                    Thread.Sleep(sleep);
            }
        }

        /// <summary>
        /// Perform an update for all dirty keys, or all keys if flushLeds is set to true.
        /// </summary>
        /// <param name="flushLeds">Specifies whether all keys (including clean ones) should be updated.</param>
        public virtual void Update(bool flushLeds = false)
        {
            IList<KeyValuePair<int, CorsairLed>> ledsToUpdate = (flushLeds ? Leds : Leds.Where(x => x.Value.IsDirty)).ToList();

            foreach (CorsairLed led in Leds.Values)
                led.Update();

            UpdateLeds(ledsToUpdate);
        }

        private static void UpdateLeds(ICollection<KeyValuePair<int, CorsairLed>> ledsToUpdate)
        {
            ledsToUpdate = ledsToUpdate.Where(x => x.Value.Color != Color.Transparent).ToList();

            if (!ledsToUpdate.Any())
                return; // CUE seems to crash if 'CorsairSetLedsColors' is called with a zero length array

            int structSize = Marshal.SizeOf(typeof(_CorsairLedColor));
            IntPtr ptr = Marshal.AllocHGlobal(structSize * ledsToUpdate.Count);
            IntPtr addPtr = new IntPtr(ptr.ToInt64());
            foreach (KeyValuePair<int, CorsairLed> led in ledsToUpdate)
            {
                _CorsairLedColor color = new _CorsairLedColor
                {
                    ledId = led.Key,
                    r = led.Value.Color.R,
                    g = led.Value.Color.G,
                    b = led.Value.Color.B
                };

                Marshal.StructureToPtr(color, addPtr, false);
                addPtr = new IntPtr(addPtr.ToInt64() + structSize);
            }
            _CUESDK.CorsairSetLedsColors(ledsToUpdate.Count, ptr);
            Marshal.FreeHGlobal(ptr);
        }

        /// <summary>
        /// Handles the needed event-calls for an exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <exception cref="Exception">A delegate callback throws an exception.</exception>
        protected void ManageException(Exception ex)
        {
            OnException?.Invoke(this, new OnExceptionEventArgs(ex));
        }

        #endregion
    }
}
