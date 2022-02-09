using System;
using System.Drawing;
using System.Drawing.Imaging;
using Lumina.Data.Files;

namespace miningtools4
{
    public static class Util
    {
        public static void WriteImage(TexFile tex, string outPath)
        {
            Image image;
            unsafe
            {
                fixed (byte* p = tex.ImageData)
                {
                    var ptr = (IntPtr)p;
                    using var tempImage = new Bitmap(tex.Header.Width, tex.Header.Height, tex.Header.Width * 4, PixelFormat.Format32bppArgb, ptr);
                    image = new Bitmap(tempImage);
                }    
            }
            
            image.Save(outPath, ImageFormat.Png);
        }
    }
}