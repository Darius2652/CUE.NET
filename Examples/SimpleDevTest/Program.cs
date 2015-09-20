﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using CUE.NET;
using CUE.NET.Devices.Generic.Enums;
using CUE.NET.Devices.Keyboard;
using CUE.NET.Devices.Keyboard.Enums;
using CUE.NET.Devices.Keyboard.Extensions;
using CUE.NET.Devices.Keyboard.Keys;
using CUE.NET.Exceptions;

namespace SimpleDevTest
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Press any key to exit ...");
            Task.Factory.StartNew(
                () =>
                {
                    Console.ReadKey();
                    Environment.Exit(0);
                });

            try
            {
                // Initialize CUE-SDK
                CueSDK.Initialize();

                // Get connected keyboard or throw exception if there is no light controllable keyboard connected
                CorsairKeyboard keyboard = CueSDK.KeyboardSDK;
                if (keyboard == null)
                    throw new WrapperException("No keyboard found");

                //Ink all numbers on the keypad except the '5' purple, we want that to be gray
                SimpleKeyGroup purpleGroup = new RectangleKeyGroup(keyboard, CorsairKeyboardKeyId.Keypad7, CorsairKeyboardKeyId.Keypad3)
                { Color = Color.Purple }
                .Exclude(CorsairKeyboardKeyId.Keypad5);
                keyboard[CorsairKeyboardKeyId.Keypad5].Led.Color = Color.Gray;

                // Ink the Keys 'r', 'g', 'b' in their respective color
                // The char access seems to fail for everything except letters (SDK doesn't return a valid keyId)
                keyboard['R'].Led.Color = Color.Red;
                keyboard[CorsairKeyboardKeyId.G].Led.Color = Color.Green;
                keyboard['B'].Led.Color = Color.Blue;

                // Lock the 'r', 'g', 'b' keys. We want them to stay like this forever
                keyboard['R'].Led.IsLocked = true;
                keyboard['G'].Led.IsLocked = true;
                keyboard['B'].Led.IsLocked = true;

                // Ink the letters of 'white' white
                SimpleKeyGroup whiteGroup = new SimpleKeyGroup(keyboard, CorsairKeyboardKeyId.W, CorsairKeyboardKeyId.H, CorsairKeyboardKeyId.I, CorsairKeyboardKeyId.T, CorsairKeyboardKeyId.E)
                { Color = Color.White };

                // Ink the keys '1' to '0' yellow
                RectangleKeyGroup yellowGroup = new RectangleKeyGroup(keyboard, CorsairKeyboardKeyId.D1, CorsairKeyboardKeyId.D0)
                { Color = Color.Yellow };

                // Update the keyboard to show the configured colors, (your CUE settings defines the rest)
                keyboard.UpdateLeds();

                // Wait 5 sec
                for (int i = 5; i > 0; i--)
                {
                    Console.WriteLine(i);
                    Thread.Sleep(1000);
                }


                // ---------------------------------------------------------------------------
                // Now let us move a orange point random over the keyboard
                // Something like this could become some sort of effect

                // Initialize needed stuff
                const float SPEED = 8f; // mm/tick
                Random random = new Random();

                // Remove all the groups we created above to clear the keyboard
                purpleGroup.Detach();
                whiteGroup.Detach();
                yellowGroup.Detach();

                // Flash whole keyboard three times to ... well ... just to make it happen
                for (int i = 0; i < 3; i++)
                {
                    keyboard.Color = Color.Aquamarine;
                    keyboard.UpdateLeds();
                    Thread.Sleep(160);
                    keyboard.Color = Color.Black;
                    keyboard.UpdateLeds();
                    Thread.Sleep(200);
                }

                // Set keyboard 'background' to something fancy
                keyboard.Color = Color.DarkSlateBlue;

                // Spawn our point (rectangle since circles are too hard to calculate :p) in the top-left corner (right over G1 or on ESC depending on your keyboard)
                RectangleF point = new RectangleF(keyboard.KeyboardRectangle.X, keyboard.KeyboardRectangle.Y, 40, 40);
                // Target of our movement
                PointF target = new PointF(point.X, point.Y);
                while (true)
                {
                    // Choose new target if we arrived
                    if (point.Contains(target))
                        target = new PointF((float)(keyboard.KeyboardRectangle.X + (random.NextDouble() * keyboard.KeyboardRectangle.Width)),
                                (float)(keyboard.KeyboardRectangle.Y + (random.NextDouble() * keyboard.KeyboardRectangle.Height)));
                    else
                        point.Location = Interpolate(point.Location, target, SPEED); // It would be better to calculate from the center of our rectangle but the easy way is enough here

                    IEnumerable<CorsairKey> keys = keyboard[point, 0.1f];
                    if (keys != null)
                        foreach (CorsairKey key in keys)
                            key.Led.Color = Color.Orange;

                    keyboard.UpdateLeds();

                    // 20 updates per sec should be enought for this
                    Thread.Sleep(50);
                }
            }
            catch (CUEException ex)
            {
                Console.WriteLine("CUE Exception! ErrorCode: " + Enum.GetName(typeof(CorsairError), ex.Error));
            }
            catch (WrapperException ex)
            {
                Console.WriteLine("Wrapper Exception! Message:" + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception! Message:" + ex.Message);
            }

            while (true)
                Thread.Sleep(1000); // Don't exit after exception
        }

        private static PointF Interpolate(PointF p1, PointF p2, float length)
        {
            float distance = (float)(Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)));
            if (distance > length)
            {
                float t = length / distance;
                float xt = (1 - t) * p1.X + (t * p2.X);
                float yt = (1 - t) * p1.Y + (t * p2.Y);
                return new PointF(xt, yt);
            }
            return p2;
        }
    }
}
