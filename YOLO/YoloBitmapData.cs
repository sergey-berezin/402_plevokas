using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Image;
using System.Drawing;

namespace YOLO
{
    public class YoloBitmapData
    {
        [ColumnName("bitmap")]
        [ImageType(416, 416)]
        public Bitmap Image { get; set; }

        [ColumnName("width")]
        public float ImageWidth => Image.Width;

        [ColumnName("height")]
        public float ImageHeight => Image.Height;
    }
}