namespace YOLO
{
    public class YoloResult
    {
        protected string filename;
        public float[] BBox { get; }
        public string Label { get; }
        public string Filename => filename;
        public YoloResult(float[] newBBox, string newLabel)
        {
            BBox = newBBox;
            Label = newLabel;
        }
        public void SetFilename(string newFilename)
        {
            filename = newFilename;
        }
    }
}
