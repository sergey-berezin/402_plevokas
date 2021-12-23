using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using DataBase;
using YOLO;

namespace ProphetBack
{
    public class YOLOImage : YoloResult
    {
        public CroppedBitmap Image { get; private set; }
        public YOLOImage(YoloResult item) : base(item.BBox, item.Label)
        {
            filename = item.Filename;
            CreateImage();
        }

        private void CreateImage()
        {
            Uri filePath = new(filename, UriKind.RelativeOrAbsolute);
            BitmapImage fileImage = new(filePath);
            fileImage.Freeze();
            Int32Rect newArea = new Int32Rect((int)BBox[0], (int)BBox[1], (int)(BBox[2] - BBox[0]), (int)(BBox[3] - BBox[1]));
            Image = new CroppedBitmap(fileImage, newArea);
            Image.Freeze();
        }

        public static byte[] Converter(BitmapSource bitmapSource)
        {
            byte[] byteImage;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            //encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.QualityLevel = 100;
            // byte[] bit = new byte[0];
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                byteImage = ms.ToArray();
                ms.Close();
            }
            return byteImage;
        }
    }
    public class YoloItem : Item
    {
        public YoloItem(YoloResult res)
        {
            YOLOImage imageRes = new(res);  

            X = imageRes.BBox[0];
            Y = imageRes.BBox[1];
            Length = imageRes.BBox[2] - X;
            Width = imageRes.BBox[3] - Y;
            Image = YOLOImage.Converter(imageRes.Image);
            Label = imageRes.Label;
        }
    }
}
