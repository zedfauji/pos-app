using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MagiDesk.Frontend.Services
{
    public static class PrintService
    {
        // Renders a XAML visual to BMP and invokes default system print.
        // Pragmatic for WinUI 3 desktop until native print pipeline is added.
        public static async Task<bool> PrintVisualAsync(FrameworkElement element)
        {
            try
            {
                if (element == null) return false;
                
                // CRITICAL FIX: Ensure RenderTargetBitmap.RenderAsync() is called from UI thread
                // This is a COM interop call that requires UI thread context
                RenderTargetBitmap? rtb = null;
                var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
                
                // CRITICAL FIX: Always ensure we have a DispatcherQueue for COM interop calls
                if (dispatcherQueue == null)
                {
                    throw new InvalidOperationException("No DispatcherQueue available. RenderTargetBitmap.RenderAsync() requires UI thread context.");
                }
                
                // Use DispatcherQueue to ensure we're on the UI thread
                var tcs = new TaskCompletionSource<RenderTargetBitmap?>();
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        rtb = new RenderTargetBitmap();
                        await rtb.RenderAsync(element);
                        tcs.SetResult(rtb);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
                
                rtb = await tcs.Task;
                
                if (rtb == null) return false;
                
                var buffer = await rtb.GetPixelsAsync();
                var bytes = new byte[buffer.Length];
                
                // CRITICAL FIX: Ensure DataReader.FromBuffer() is called from UI thread
                // This is a Windows Runtime COM call that requires UI thread context
                var dataReaderTcs = new TaskCompletionSource<bool>();
                dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        Windows.Storage.Streams.DataReader.FromBuffer(buffer).ReadBytes(bytes);
                        dataReaderTcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        dataReaderTcs.SetException(ex);
                    }
                });
                await dataReaderTcs.Task;
                int width = rtb.PixelWidth;
                int height = rtb.PixelHeight;
                if (width <= 0 || height <= 0) return false;

                using var mem = new MemoryStream();
                using (var writer = new BinaryWriter(mem, System.Text.Encoding.UTF8, leaveOpen: true))
                {
                    WriteBmp(writer, width, height, bytes);
                }

                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagiDesk");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"receipt_{DateTime.Now:yyyyMMdd_HHmmss}.bmp");
                
                // CRITICAL FIX: Ensure File.WriteAllBytesAsync() is called from UI thread
                // This prevents potential thread context issues
                var fileWriteTcs = new TaskCompletionSource<bool>();
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        await File.WriteAllBytesAsync(path, mem.ToArray());
                        fileWriteTcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        fileWriteTcs.SetException(ex);
                    }
                });
                await fileWriteTcs.Task;

                // CRITICAL FIX: Ensure Process.Start() is called from UI thread
                // This prevents potential thread context issues
                var processStartTcs = new TaskCompletionSource<bool>();
                dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = path,
                            Verb = "print",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        processStartTcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        processStartTcs.SetException(ex);
                    }
                });
                await processStartTcs.Task;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void WriteBmp(BinaryWriter bw, int width, int height, byte[] bgra)
        {
            int rowSize = width * 4;
            int pixelDataSize = rowSize * height;
            int headerSize = 14 + 40; // BITMAPFILEHEADER + BITMAPINFOHEADER
            int fileSize = headerSize + pixelDataSize;

            // BITMAPFILEHEADER
            bw.Write((byte)'B');
            bw.Write((byte)'M');
            bw.Write(fileSize);
            bw.Write(0);
            bw.Write(headerSize);

            // BITMAPINFOHEADER
            bw.Write(40); // biSize
            bw.Write(width);
            bw.Write(-height); // top-down
            bw.Write((short)1); // planes
            bw.Write((short)32); // bitcount
            bw.Write(0); // compression BI_RGB
            bw.Write(pixelDataSize);
            bw.Write(2835); // XPelsPerMeter ~72dpi
            bw.Write(2835); // YPelsPerMeter
            bw.Write(0); // clrUsed
            bw.Write(0); // clrImportant

            // Pixel data (already BGRA from RenderTargetBitmap)
            bw.Write(bgra);
        }
    }
}
