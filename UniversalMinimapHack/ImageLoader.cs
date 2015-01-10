using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using LeagueSharp;
using Image = SharpDX.Toolkit.Graphics.Image;

namespace UniversalMinimapHack
{
    public class ImageLoader
    {

        public static Bitmap Load(string championName)
        {
            string cachedPath = GetCachedPath(championName);
            if (File.Exists(cachedPath))
            {
                return ChangeOpacity(new Bitmap(cachedPath));
            }
            Bitmap bitmap = Properties.Resources.ResourceManager.GetObject(championName + "_Square_0") as Bitmap ?? Properties.Resources.Nami_Square_0;
            Bitmap finalBitmap = CreateFinalImage(bitmap);
            finalBitmap.Save(cachedPath);
            return ChangeOpacity(finalBitmap);
        }

        private static Bitmap DownloadImage(string championName)
        {
            return null; //TODO
        }

        private static string GetCachedPath(string championName)
        {
            string path = Path.GetTempPath() + "UniversalMinimapHack";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            path = Path.Combine(path, Game.Version);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return Path.Combine(path, championName + ".png");
        }

        private static Bitmap CreateFinalImage(Bitmap srcBitmap)
        {

            Bitmap img = new Bitmap(srcBitmap.Width, srcBitmap.Width);
            Rectangle cropRect = new Rectangle(0, 0,
                srcBitmap.Width, srcBitmap.Width);

            using (Bitmap sourceImage = srcBitmap)
            using (Bitmap croppedImage = sourceImage.Clone(cropRect, sourceImage.PixelFormat))
            using (TextureBrush tb = new TextureBrush(croppedImage))
            using (Graphics g = Graphics.FromImage(img))
            {
                g.FillEllipse(tb, 0, 0, srcBitmap.Width, srcBitmap.Width);
                Pen p = new Pen(System.Drawing.Color.DarkRed, 10) { Alignment = PenAlignment.Inset };
                g.DrawEllipse(p, 0, 0, srcBitmap.Width, srcBitmap.Width);
            }
            srcBitmap.Dispose();
            return img;
        }

        private static Bitmap ChangeOpacity(Bitmap img)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height); // Determining Width and Height of Source Image
            Graphics graphics = Graphics.FromImage(bmp);
            ColorMatrix colormatrix = new ColorMatrix { Matrix33 = MinimapHack.Instance().Menu.IconOpacity };
            ImageAttributes imgAttribute = new ImageAttributes();
            imgAttribute.SetColorMatrix(colormatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            graphics.DrawImage(img, new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, img.Width,
                img.Height, GraphicsUnit.Pixel, imgAttribute);
            graphics.Dispose(); // Releasing all resource used by graphics
            img.Dispose();
            return bmp;
        }
    }
}
