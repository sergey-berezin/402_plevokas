using ProphetLibrary;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using YOLOv4MLNet;
using YOLOv4MLNet.DataStructures;

namespace ProphetUI
{
    public class ProphetHelper : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        string inputPath = "";
        CancellationTokenSource cst = new CancellationTokenSource();
        bool isProcessing = false;
        string processingState = "Обработка еще не началась";

        SynchronizationContext uiContext;
        YoloDict currDict = new YoloDict();
        static public ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>> yoloResults = new ConcurrentQueue<Tuple<string, IReadOnlyList<YoloV4Result>>>();
        public YoloDict objectClasses => currDict;
        public string InputPath
        {
            get => inputPath;
            set
            {
                inputPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("InputPath"));
            }
        }
        public string ProcessingState
        {
            get => processingState;
            set
            {
                processingState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ProcessingState"));
            }
        }

        public void StopProcess()
        {
            cst.Cancel();
            isProcessing = false;
            ProcessingState = "Обработка прервана";
        }
        public void StartProcessing()
        {
            if (isProcessing)
            {
                return;
            }
            if (inputPath == "")
            {
                return;
            }
            uiContext = SynchronizationContext.Current;
            currDict.Clear();
            _ = DetectObjectsAsync();
        }

        async Task DetectObjectsAsync()
        {
            isProcessing = true;
            ProcessingState = "Обработка началась";
            var prophetTask = Prophet.SuperImageProphet(inputPath, yoloResults, cst.Token);
            Tuple<string, IReadOnlyList<YoloV4Result>> result;

            var task2 = Task.Run(() =>
            {
                while (true)
                {
                    while (yoloResults.TryDequeue(out result))
                    {
                        var file_name = result.Item1;
                        var file_info = result.Item2;
                        foreach (var result_new in file_info)
                        {
                            uiContext.Send(x => currDict.Add(new YoloImage(result_new, InputPath + @"\" + file_name)), null);
                            Console.WriteLine(InputPath + file_name);
                        }
                    }
                }
            });

            await Task.WhenAll(prophetTask);
            isProcessing = false;
            ProcessingState = "Обработка закончена";
            yoloResults.Clear();
        }

    }

    public class YoloDict : IEnumerable<YoloList>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        Dictionary<string, YoloList> yoloDict = new Dictionary<string, YoloList>();
        public void Add(YoloV4Result item)
        {
            if (yoloDict.ContainsKey(item.Label))
                yoloDict[item.Label].Add(item);
            else
                yoloDict.Add(item.Label, new YoloList(item.Label));

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public void Clear()
        {
            yoloDict.Clear();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return yoloDict.Values.GetEnumerator();
        }

        public IEnumerator<YoloList> GetEnumerator()
        {
            return yoloDict.Values.GetEnumerator();
        }
    }

    public class YoloList : IEnumerable<YoloV4Result>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public string name;

        List<YoloV4Result> yoloList = new List<YoloV4Result>();
        public List<YoloV4Result> DataList => yoloList;
        public YoloList(string className)
        {
            name = className;
        }
        public void Add(YoloV4Result item)
        {
            yoloList.Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        public IEnumerator<YoloV4Result> GetEnumerator()
        {
            return yoloList.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return yoloList.GetEnumerator();
        }
        public override string ToString()
        {
            return yoloList.Count.ToString() + " - " + name;
        }
    }
    public class YoloImage : YoloV4Result
    {
        CroppedBitmap image;
        public CroppedBitmap Image => image;
        private string filename;
        public YoloImage(YoloV4Result item, string file_name) : base(item.BBox, item.Label, item.Confidence)
        {
            filename = file_name;
            CreateImage();
        }
        void CreateImage()
        {
            var uri = new Uri(filename, UriKind.RelativeOrAbsolute);
            var fileImage = new BitmapImage(uri);
            fileImage.Freeze();
            var rect = new Int32Rect((int)BBox[0], (int)BBox[1], (int)(BBox[2] - BBox[0]), (int)(BBox[3] - BBox[1]));
            image = new CroppedBitmap(fileImage, rect);
            image.Freeze();
        }
    }
    
}
