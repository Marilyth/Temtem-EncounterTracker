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

        public async Task<byte[]> GetTemtem(bool first = true)
        {
            var rect = new User32.Rect();
            User32.GetWindowRect(temtemProcess.MainWindowHandle, ref rect);

            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;

            //Only consider the text area of the screen
            int heightText = (int)(height * 0.03);
            int widthText = (int)(width * 0.1);
            int topText = rect.top + (int)(height * (first ? 0.11 : 0.06));
            int leftText = rect.left + (int)(width * (first ? 0.81 : 0.61));

            var bmp = new Bitmap(widthText, heightText, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bmp))
            {
                graphics.CopyFromScreen(leftText, topText, 0, 0, new Size(widthText, heightText), CopyPixelOperation.SourceCopy);
            }

            for(int i = 0; i < widthText; i++){
                for(int j = 0; j < heightText; j++){
                    if(bmp.GetPixel(i, j).Name.Equals("ffffffff")){
                        bmp.SetPixel(i, j, Color.Black);
                    } else {
                        bmp.SetPixel(i, j, Color.White);
                    }
                }
            }
            //bmp.Save("Temtem1.png", System.Drawing.Imaging.ImageFormat.Png);

            return ToByteArray(bmp, System.Drawing.Imaging.ImageFormat.Tiff);
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
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
    }
}