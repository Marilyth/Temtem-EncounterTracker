using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading.Tasks;
using Tesseract;
using System.IO;
using System.Collections.Generic;

namespace Temtem_EncounterTracker
{
    public class TemtemWindow
    {
        private Process temtemProcess;
        private int count = 0;

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr ClientToScreen(IntPtr hWnd, ref Point rect);

        public async Task WaitForTemtemStart(){
            while(Process.GetProcessesByName("Temtem").Length == 0){
                await Task.Delay(1000);
            }
            temtemProcess = Process.GetProcessesByName("Temtem")[0];
        }

        public bool IsTemtemActive()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process p = Process.GetProcessById((int)pid);
            return p.Id == temtemProcess.Id;
        }

        public async Task<byte[]> GetTemtem(bool first = true, int textMargin = 100, bool doHeavyCutting = true)
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

            AspectRatio ratio = Aspect.GetRatio(width, height);

            //Only consider the text area of the screen
            int heightText = (int)(height * Aspect.NameHeightPercentage(ratio));
            int widthText = (int)(width * Aspect.NameWidthPercentage(ratio));
            int topText = rect.top + (int)(height * (first ? Aspect.Temtem1PercentageTop(ratio) : Aspect.Temtem2PercentageTop(ratio)));
            int leftText = rect.left + (int)(width * (first ? Aspect.Temtem1PercentageLeft(ratio) : Aspect.Temtem2PercentageLeft(ratio)));

            var bmp = new Bitmap(widthText, heightText, PixelFormat.Format32bppArgb);
            //var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen(leftText, topText, 0, 0, new Size(widthText, heightText), CopyPixelOperation.SourceCopy);
                //graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            }

            int trueHeight = heightText < 40 ? 40 : heightText;
            int trueWidth = (int)(widthText * (trueHeight/heightText));

            bmp = new Bitmap(bmp, trueWidth, trueHeight);

            //Preprocess
            int widthWithoutHit = 0;
            for (int i = 0; i < trueWidth; i++)
            {
                bool wasHit = false;
                bool reachedGender = false;
                //Only accept whites in the direct vicinity of blackish colours, or already found text
                for (int j = 0; j < trueHeight; j++)
                {
                    if(widthWithoutHit >= 20){
                        bmp.SetPixel(i, j, Color.Black);
                        continue;
                    }

                    var pixel = bmp.GetPixel(i, j);
                    List<Color> toCheck = new List<Color>();
                    toCheck.Add(bmp.GetPixel(i > 0 ? i - 1 : i, j));
                    toCheck.Add(bmp.GetPixel(i, j > 0 ? j - 1 : j));
                    toCheck.Add(bmp.GetPixel(i > 0 ? i - 1 : i, j < heightText - 1 ? j + 1 : j));
                    toCheck.Add(bmp.GetPixel(i > 0 ? i - 1 : i, j > 0 ? j - 1 : j));
                    var temtemBlack = Color.FromArgb(30, 30, 30);
                    bool isText = false;
                    foreach (var col in toCheck)
                    {
                        if (ColorDifference(col, temtemBlack) < 100 || ColorDifference(col, Color.White) < 1)
                            isText = true;
                    }
                    if (ColorDifference(pixel, Color.White) < textMargin && (doHeavyCutting ? isText : true))
                    {
                        bmp.SetPixel(i, j, Color.White);
                        wasHit = true;
                        widthWithoutHit = 0;
                    }
                    else if (ColorDifference(pixel, Color.FromArgb(247, 180, 99)) < 20)
                    {
                        reachedGender = true;
                    }
                }
                if (!wasHit)
                {
                    ++widthWithoutHit;
                }
                if (reachedGender) break;
            }

            //Grayscale
            for (int i = 0; i < trueWidth; i++)
            {
                for (int j = 0; j < trueHeight; j++)
                {
                    var pixel = bmp.GetPixel(i, j);

                    if (ColorDifference(pixel, Color.White) < 1)
                    {
                        bmp.SetPixel(i, j, Color.Black);
                    }
                    else
                    {
                        bmp.SetPixel(i, j, Color.White);
                    }
                }
            }

            //Remove suspicious wide black clusters
            for (int j = 0; j < trueHeight; j++)
            {
                var blackWidth = 0;
                var iStart = 0;
                for (int i = 0; i < trueWidth; i++)
                {
                    var pixel = bmp.GetPixel(i, j);

                    if (ColorDifference(pixel, Color.Black) < 1)
                    {
                        if (blackWidth == 0) iStart = i;
                        blackWidth++;
                    }
                    else
                    {
                        if (blackWidth > trueWidth * 0.15)
                        {
                            for (int eraser = iStart; eraser <= i; eraser++)
                            {
                                bmp.SetPixel(eraser, j, Color.White);
                            }
                        }
                        blackWidth = 0;
                    }
                }

                if (blackWidth > trueWidth * 0.15)
                {
                    for (int eraser = iStart; eraser < trueWidth; eraser++)
                    {
                        bmp.SetPixel(eraser, j, Color.White);
                    }
                }
            }
            //bmp.Save($"Temtem{(count++) % 10}.png", System.Drawing.Imaging.ImageFormat.Png);

            return ToByteArray(bmp, System.Drawing.Imaging.ImageFormat.Tiff);
        }

        public bool IsInEncounter()
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

            AspectRatio ratio = Aspect.GetRatio(width, height);

            int minimap1y = rect.top + (int)(height * Aspect.Map1TopPercentage(ratio));
            int minimap1x = rect.left + (int)(width * Aspect.Map1LeftPercentage(ratio));
            int minimap2y = rect.top + (int)(height * Aspect.Map2TopPercentage(ratio));
            int minimap2x = rect.left + (int)(width * Aspect.Map2LeftPercentage(ratio));

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
            var mapColour = Color.FromArgb(60, 232, 234);
            return !(ColorDifference(pixel1, mapColour) < 10 && ColorDifference(pixel2, mapColour) < 10);
        }

        public double ColorDifference(Color c1, Color c2)
        {
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