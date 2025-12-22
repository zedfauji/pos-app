using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using System;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

namespace MagiDesk.Extensions
{
    /// <summary>
    /// Extension methods for UIElement to support XAML to image conversion
    /// </summary>
    public static class UIElementExtensions
    {
        /// <summary>
        /// Converts a UIElement to PNG byte array using RenderTargetBitmap
        /// Based on: https://xamlbrewer.wordpress.com/2023/03/09/displaying-xaml-controls-in-questpdf-with-winui/
        /// </summary>
        /// <param name="control">The UIElement to convert</param>
        /// <returns>PNG image as byte array</returns>
        public static async Task<byte[]> AsPng(this UIElement control)
        {
            // Ensure the control has been measured and arranged
            if (control.ActualSize.X == 0 || control.ActualSize.Y == 0)
            {
                throw new InvalidOperationException("UIElement must have ActualSize before rendering. Ensure it's been measured and arranged.");
            }

            // Get XAML Visual in BGRA8 format
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(control, (int)control.ActualSize.X, (int)control.ActualSize.Y);

            // Encode as PNG
            var pixelBuffer = (await rtb.GetPixelsAsync()).ToArray();
            IRandomAccessStream mraStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, mraStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)rtb.PixelWidth,
                (uint)rtb.PixelHeight,
                184, 184,
                pixelBuffer);
            await encoder.FlushAsync();

            // Transform to byte array
            var bytes = new byte[mraStream.Size];
            await mraStream.ReadAsync(bytes.AsBuffer(), (uint)mraStream.Size, InputStreamOptions.None);
            return bytes;
        }

        /// <summary>
        /// Converts a UIElement to PNG byte array with custom dimensions
        /// </summary>
        /// <param name="control">The UIElement to convert</param>
        /// <param name="width">Custom width for rendering</param>
        /// <param name="height">Custom height for rendering</param>
        /// <returns>PNG image as byte array</returns>
        public static async Task<byte[]> AsPng(this UIElement control, int width, int height)
        {
            // Get XAML Visual in BGRA8 format with custom dimensions
            var rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(control, width, height);

            // Encode as PNG
            var pixelBuffer = (await rtb.GetPixelsAsync()).ToArray();
            IRandomAccessStream mraStream = new InMemoryRandomAccessStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, mraStream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)rtb.PixelWidth,
                (uint)rtb.PixelHeight,
                184, 184,
                pixelBuffer);
            await encoder.FlushAsync();

            // Transform to byte array
            var bytes = new byte[mraStream.Size];
            await mraStream.ReadAsync(bytes.AsBuffer(), (uint)mraStream.Size, InputStreamOptions.None);
            return bytes;
        }
    }
}
