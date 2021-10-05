using Microsoft.ML;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using YOLOv4MLNet.DataStructures;
using static Microsoft.ML.Transforms.Image.ImageResizingEstimator;

namespace LabMainBlock
{
    public class Program1
    {

        const string modelPath = @"C:\Users\white\source\repos\Lab1\Lab1\yolov4.onnx";

        public static ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>> yoloResults = new ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>>();

        public static bool isEnded;

        //const string imageFolder = @"Assets\Images";

        static readonly string[] classesNames = new string[] { "person", "bicycle", "car", "motorbike", "aeroplane", "bus", "train", "truck", "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench", "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra", "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee", "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove", "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup", "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange", "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "sofa", "pottedplant", "bed", "diningtable", "toilet", "tvmonitor", "laptop", "mouse", "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink", "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier", "toothbrush" };

        public static async Task MainBlock(string path)
        {

            MLContext mlContext = new MLContext();
            isEnded = false;
            // Define scoring pipeline
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

            // Fit on empty list to obtain input data schema
            var model = pipeline.Fit(mlContext.Data.LoadFromEnumerable(new List<YoloV4BitmapData>()));

            // Create prediction engine

            // save model
            //mlContext.Model.Save(model, predictionEngine.OutputSchema, Path.ChangeExtension(modelPath, "zip"));
            //var sw = new Stopwatch();

            //string pathVariable = @"Assets\Images";


            DirectoryInfo dir = new DirectoryInfo(path);
            string[] extensions = new[] { ".jpg", ".tiff", ".bmp", ".png" };
            FileInfo[] files = dir.GetFiles().Where(f => extensions.Contains(f.Extension.ToLower())).ToArray();
            var images = new ConcurrentBag<string>();
            foreach (FileInfo f in files)
            {
                images.Add(f.Name);
            }

            //sw.Start();
            var cts = new CancellationTokenSource();
            var ct = cts.Token;

            // var ab = new TransformBlock<string, Tuple<string, IReadOnlyList<YoloV4Result>>>(async imageName =>
            var ab = new ActionBlock<string>(async imageName =>
            {
                using (var bitmap = new Bitmap(Image.FromFile(Path.Combine(path, imageName))))
                {
                    IReadOnlyList<YoloV4Result> results;
                    // predict
                    var predictionEngine = mlContext.Model.CreatePredictionEngine<YoloV4BitmapData, YoloV4Prediction>(model);
                    var predict = await AsyncGetPrediction(predictionEngine, bitmap);
                    results = predict.GetResults(classesNames, 0.3f, 0.7f);
                    Tuple<string, IReadOnlyList<YoloV4Result>> tuple = new Tuple<string, IReadOnlyList<YoloV4Result>>(imageName, results);
                    yoloResults.Enqueue(tuple);
                    //return tuple;
                }
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 4,
                CancellationToken = ct
            });


            var th = new Thread(new ThreadStart(() => {
                Console.WriteLine("Dear user, input 's' or 'S' to stop work\n");
                string input = Console.ReadLine();
                if ((input == "s") | (input == "S"))
                {
                    cts.Cancel();
                }
            }));
            th.Start();
            Parallel.ForEach(images, imageName => ab.Post(imageName));

            /*
            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    var receivedData = ab.Receive();
                    Console.WriteLine(receivedData.Item1);
                    foreach (var obj in receivedData.Item2)
                    {
                        Console.WriteLine(obj.Label + " " + obj.BBox[0] + " " + obj.BBox[1] + " " + obj.BBox[2] + " " + obj.BBox[3]);
                    }
                    Console.WriteLine(" ");
                }
                catch (InvalidOperationException exception)
                {
                    Console.WriteLine("Process aborted inside for-cycle");
                }
                catch
                {
                    Console.WriteLine("Unexpected behaviour");
                }
            }
            */
            ab.Complete();
            try
            {
                await ab.Completion;
                //sw.Stop();

                //Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms.");
                isEnded = true;

                //Console.WriteLine("\nDear user, work finished, press enter to continue");
            }
            catch (TaskCanceledException except)
            {
                Console.WriteLine("Abort successful");
                //sw.Stop();
                //Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms.");
                isEnded = true;
                //Console.WriteLine("\nDear user, work finished, press enter to continue");
            }
            catch
            {
                Console.WriteLine("Unexpected behaviour");
                //sw.Stop();
                //Console.WriteLine($"Done in {sw.ElapsedMilliseconds}ms.");
                isEnded = true;
                //Console.WriteLine("\nDear user, work finished, press enter to continue");
            }
        }
        static async Task<YoloV4Prediction> AsyncGetPrediction(PredictionEngine<YoloV4BitmapData, YoloV4Prediction> predictionEngine, Bitmap bitmap)
        {
            return await Task<YoloV4Prediction>.Factory.StartNew(() =>
            {
                return predictionEngine.Predict(new YoloV4BitmapData() { Image = bitmap }); ;
            });
        }
    }
}
