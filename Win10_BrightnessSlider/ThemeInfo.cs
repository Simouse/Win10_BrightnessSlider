﻿using System;
using System.Windows;
using System.Windows.Media;
using System.Runtime.InteropServices;

namespace Win10_BrightnessSlider
{
    public class ThemeInfo
    {
        private static System.Drawing.Color ConvertToDrawingColor(System.Windows.Media.Color c)
        {
            return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        // Drawbacks:
        // 1. Color is fixed once program is launched
        // 2. Color is not as close to audio loudness slider as GetVariantThemeColor()
        private static System.Drawing.Color GetThemeColor()
        {
            SolidColorBrush brush = (SolidColorBrush)SystemParameters.WindowGlassBrush;
            return ConvertToDrawingColor(brush.Color);
        }

        public static System.Drawing.Color GetVariantThemeColor()
        {
            var chromeColor = GetChromeColor();
            return chromeColor == null ? GetThemeColor() : ConvertToDrawingColor((Color)chromeColor);
        }

        // https://stackoverflow.com/a/24600956/13785815
        private static Color? GetChromeColor()
        {
            bool isEnabled;
            var hr1 = DwmIsCompositionEnabled(out isEnabled);
            if ((hr1 != 0) || !isEnabled) // 0 means S_OK.
                return null;

            DWMCOLORIZATIONPARAMS parameters;
            try
            {
                // This API is undocumented and so may become unusable in future versions of OSes.
                var hr2 = DwmGetColorizationParameters(out parameters);
                if (hr2 != 0) // 0 means S_OK.
                    return null;
            }
            catch
            {
                return null;
            }

            // Convert colorization color parameter to Color ignoring alpha channel.
             var targetColor = Color.FromRgb(
                (byte)(parameters.colorizationColor >> 16),
                (byte)(parameters.colorizationColor >> 8),
                (byte)parameters.colorizationColor
             );

            // Prepare base color.
            var baseColor = Color.FromRgb(0, 0, 0);

            // Blend the two colors using colorization color balance parameter.
            // var balance = (double)(100 - parameters.colorizationColorBalance);
            var balance = 25.0;
            return BlendColor(targetColor, baseColor, balance);
        }

        private static Color BlendColor(Color color1, Color color2, double color2Perc)
        {
            if ((color2Perc < 0) || (100 < color2Perc))
                throw new ArgumentOutOfRangeException("color2Perc");

            return Color.FromRgb(
                BlendColorChannel(color1.R, color2.R, color2Perc),
                BlendColorChannel(color1.G, color2.G, color2Perc),
                BlendColorChannel(color1.B, color2.B, color2Perc));
        }

        private static byte BlendColorChannel(double channel1, double channel2, double channel2Perc)
        {
            var buff = channel1 + (channel2 - channel1) * channel2Perc / 100D;
            return Math.Min((byte)Math.Round(buff), (byte)255);
        }

        [DllImport("Dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled([MarshalAs(UnmanagedType.Bool)] out bool pfEnabled);

        [DllImport("Dwmapi.dll", EntryPoint = "#127")] // Undocumented API
        private static extern int DwmGetColorizationParameters(out DWMCOLORIZATIONPARAMS parameters);

        [StructLayout(LayoutKind.Sequential)]
        private struct DWMCOLORIZATIONPARAMS
        {
            public uint colorizationColor;
            public uint colorizationAfterglow;
            public uint colorizationColorBalance; // Ranging from 0 to 100
            public uint colorizationAfterglowBalance;
            public uint colorizationBlurBalance;
            public uint colorizationGlassReflectionIntensity;
            public uint colorizationOpaqueBlend;
        }

    }
}
