using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Tesseract;
using System.IO;

namespace Temtem_EncounterTracker
{
    public class TemtemWindow
    {
        private Process temtemProcess;
        public TemtemWindow()
        {
            temtemProcess = Process.GetProcessesByName("Temtem")[0];
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public bool IsTemtemActive()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);
            return p.Id == temtemProcess.Id;
        }

        public async Task<byte[]> GetEncounterScreenshot()
        {
            var rect = new User32.Rect();
            User32.GetWindowRect(temtemProcess.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            //Only consider the text area of the screen
            int heightText = (int)(height * 0.05);
            int widthText = (int)(width * 0.6);
            int topText = rect.top + (int)(height * 0.83);
            int leftText = rect.left + (int)(width * 0.2);

            var bmp = new Bitmap(widthText, heightText, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen(leftText, topText, 0, 0, new Size(widthText, heightText), CopyPixelOperation.SourceCopy);
            }

            bmp = DrawBitmapWithBorder(bmp, 1);
            bmp.Save("Text.png", System.Drawing.Imaging.ImageFormat.Png);

            return ToByteArray(bmp, System.Drawing.Imaging.ImageFormat.Tiff);
        }

        private static Bitmap DrawBitmapWithBorder(Bitmap bmp, int borderSize = 10)
        {
            int newWidth = bmp.Width + (borderSize * 2);
            int newHeight = bmp.Height + (borderSize * 2);

            Image newImage = new Bitmap(newWidth, newHeight);
            using (Graphics gfx = Graphics.FromImage(newImage))
            {
                using (Brush border = new SolidBrush(Color.White))
                {
                    gfx.FillRectangle(border, 0, 0,
                        newWidth, newHeight);
                }
                gfx.DrawImage(bmp, new Rectangle(borderSize, borderSize, bmp.Width, bmp.Height));

            }
            return (Bitmap)newImage;
        }

        public async Task WaitForEncounter()
        {
            var rect = new User32.Rect();
            User32.GetWindowRect(temtemProcess.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            //Only consider the text area of the screen
            int heightText = (int)(height * 0.05);
            int widthText = (int)(width * 0.6);
            int topText = rect.top + (int)(height * 0.85);
            int leftText = rect.left + (int)(width * 0.2);

            //Wait for obvious encounter by checking if pixel is black
            while (true)
            {
                var pixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);

                using (Graphics graphics = Graphics.FromImage(pixel))
                {
                    graphics.CopyFromScreen(leftText, topText, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);
                }

                var color = pixel.GetPixel(0, 0);
                if (color.Name.Equals("ff000000")) break;

                await Task.Delay(20);
            }
        }

        public byte[] ToByteArray(Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public string GetScreenText(byte[] image)
        {
            var ocrtext = string.Empty;
            using (var engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default, @"tessdata\configs\quiet"))
            {
                var worked = engine.SetVariable("debug_file", "/dev/null");
                using (var img = Pix.LoadTiffFromMemory(image))
                {
                    using (var page = engine.Process(img))
                    {
                        ocrtext = page.GetText();
                    }
                }
            }

            return ocrtext;
        }
    }

    public class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
    }
}