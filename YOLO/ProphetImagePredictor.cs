using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;
using System.Drawing;
using System.Threading.Tasks.Dataflow;
using System.Threading;

namespace YOLO
{
    public static class ProphetImagePredictor
    {
        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };
        static readonly string modelPath = @"C:\Users\white\source\repos\Lab1\Lab1\yolov4.onnx";


        public static async Task ExecuteAsync(string directoryPath, CancellationToken ct, BufferBlock<IReadOnlyList<YoloResult>> output)
        {
            MLContext mlContext = new MLContext();

            var pipeline = mlContext.Transforms.ResizeImages(inputColumnName: "bitmap", outputColumnName: "input_1:0", imageWidth: 416, imageHeight: 416, resizing: ResizingKind.IsoPad)
                .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input_1:0", scaleImage: 1f / 255f, interleavePixelColors: true))
                .Append(mlContext.Transforms.ApplyOnnxModel(
                    shapeDictionary: new Dictionary<string, int[]>()
                        {
                            { "input_1:0", new[] { 1, 416, 416, 3 } },
                            { "Identity:0", new[] { 1, 52, 52, 3, 85 } },
                            { "Identity_1:0", new[] { 1, 26, 26, 3, 85 } },
                            { "Identity_2:0", new[] { 1, 13, 13, 3, 85 } },
                        },
                    inputColumnNames: new[]
                        {
                            "input_1:0"
                        },
                    outputColumnNames: new[]
                        {
                            "Identity:0",
                            "Identity_1:0",
                            "Identity_2:0"
                        },
                    modelFile: modelPath, recursionLimit: 100));

            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloBitmapData>()));

            string[] filenames = GetImageFiles(directoryPath);
            ActionBlock<string> processingImageActionBlock = new ActionBlock<string>(
                async filename =>
                {
                    await Task.Run(() =>
                    {
                        using Bitmap bitmap = new Bitmap(Image.FromFile(filename));
                        PredictionEngine<YoloBitmapData, YoloPrediction> predictionEngine = mlContext.Model.CreatePredictionEngine<YoloBitmapData, YoloPrediction>(model);
                        YoloPrediction predict = predictionEngine.Predict(new YoloBitmapData() { Image = bitmap });
                        IReadOnlyList<YoloResult> results = predict.GetResults(classesNames, 0.3f, 0.7f);

                        foreach (YoloResult item in results)
                        {
                            item.SetFilename(filename);
                        }

                        _ = output.Post(results);


                    });
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 4,
                    CancellationToken = ct
                }
            );

            foreach (string item in filenames)
                processingImageActionBlock.Post(item);

            processingImageActionBlock.Complete();
            await processingImageActionBlock.Completion;
            output.Complete();
        }
        public static string[] GetImageFiles(string path)
        {
            DirectoryInfo dir = new DirectoryInfo(path);
            string[] extensions = new[] { ".jpg", ".jpeg", ".tiff", ".bmp", ".png" };
            FileInfo[] files = dir.GetFiles().Where(f => extensions.Contains(f.Extension.ToLower())).ToArray();
            var images = new string[files.Length];
            for (int i = 0; i < images.Length; i++)
            {
                images[i] = files[i].FullName;
            }
            return images;
        }
    }
}
