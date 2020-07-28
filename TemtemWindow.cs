using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using Tesseract;
using System.IO;

namespace Temtem_EncounterTracker
{
    public class TemtemWindow
    {
        private Process temtemProcess;
        private int count = 0;
        public TemtemWindow()
        {
            temtemProcess = Process.GetProcessesByName("Temtem")[0];
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr ClientToScreen(IntPtr hWnd, ref Point rect);

        public bool IsTemtemActive()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);
            return p.Id == temtemProcess.Id;
        }

        public async Task<byte[]> GetTemtem(bool first = true, bool fixedRBG = true)
        {
            var rect = new User32.Rect();
            User32.GetClientRect(temtemProcess.MainWindowHandle, ref rect);
            var point = new Point();
            ClientToScreen(temtemProcess.MainWindowHandle, ref point);
            rect.top = point.Y;
            rect.left = point.X;
            rect.right = point.X + rect.right;
            rect.bottom = point.Y + rect.bottom;

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            //Only consider the text area of the screen
            int heightText = (int)(height * 0.03);
            int widthText = (int)(width * 0.1);
            int topText = rect.top + (int)(height * (first ? 0.078 : 0.028));
            int leftText = rect.left + (int)(width * (first ? 0.81 : 0.61));

            var bmp = new Bitmap(widthText, heightText, PixelFormat.Format32bppArgb);
            //var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen(leftText, topText, 0, 0, new Size(widthText, heightText), CopyPixelOperation.SourceCopy);
                //graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            for(int i = 0; i < widthText; i++){
                for(int j = 0; j < heightText; j++){
                    var pixel = bmp.GetPixel(i, j);
                    if(ColorDifference(pixel, Color.White) < 50 && (pixel.R == pixel.G && pixel.G == pixel.B)){
                        bmp.SetPixel(i, j, Color.Black);
                    } else {
                        bmp.SetPixel(i, j, Color.White);
                    }
                }
            }
            //bmp.Save($"Temtem{count++}.png", System.Drawing.Imaging.ImageFormat.Png);

            return ToByteArray(bmp, System.Drawing.Imaging.ImageFormat.Tiff);
        }

        public bool IsInEncounter(){
            var rect = new User32.Rect();
            User32.GetClientRect(temtemProcess.MainWindowHandle, ref rect);
            var point = new Point();
            ClientToScreen(temtemProcess.MainWindowHandle, ref point);
            rect.top = point.Y;
            rect.left = point.X;
            rect.right = point.X + rect.right;
            rect.bottom = point.Y + rect.bottom;

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            int minimap1y = rect.top + (int)(height * 0.0692);
            int minimap1x = rect.left + (int)(width * 0.8741);
            int minimap2y = rect.top + (int)(height * 0.1927);
            int minimap2x = rect.left + (int)(width * 0.977);

            var bmp1 = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            var bmp2 = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp1))
            {
                graphics.CopyFromScreen(minimap1x, minimap1y, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);
                //graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            using (Graphics graphics = Graphics.FromImage(bmp2))
            {
                graphics.CopyFromScreen(minimap2x, minimap2y, 0, 0, new Size(1, 1), CopyPixelOperation.SourceCopy);
                //graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }
            
            var pixel1 = bmp1.GetPixel(0, 0);
            var pixel2 = bmp2.GetPixel(0, 0);
            return !(pixel1.R == 60 && pixel1.G == 232 && pixel1.B == 234 &&
                   pixel2.R == 60 && pixel2.G == 232 && pixel2.B == 234);
        }

        public double ColorDifference(Color c1, Color c2){
            return (int)Math.Sqrt((c1.R - c2.R) * (c1.R - c2.R)
                                + (c1.G - c2.G) * (c1.G - c2.G)
                                + (c1.B - c2.B) * (c1.B - c2.B));
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
            using (var engine = new TesseractEngine(@"tessdata", "eng", EngineMode.Default, @"tessdata\configs\temtem"))
            {
                using (var img = Pix.LoadTiffFromMemory(image))
                {
                    using (var page = engine.Process(img, PageSegMode.SingleBlock))
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
        public static extern IntPtr GetClientRect(IntPtr hWnd, ref Rect rect);
    }
}