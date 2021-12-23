using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using YOLO;
using DataBase;
using System.ComponentModel;

namespace ProphetBack
{
    public class ProphetCenter : INotifyPropertyChanged
    {
        private string inputPath = "";
        private bool processing;
        private string processingState = "Обработка еще не началась.";
        public event PropertyChangedEventHandler PropertyChanged;
        CancellationTokenSource tokenSource = new();
        private readonly BufferBlock<IReadOnlyList<YoloResult>> finalResult = new();

        public ClassListView ClassListView { get; }
        public ImageListView ImageListView { get; }
        public DBManager DBManager { get; } = new();
        public string InputPath
        {
            get => inputPath;
            set
            {
                inputPath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InputPath)));
            }
        }
        public string ProcessingState
        {
            get => processingState;
            set
            {
                processingState = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProcessingState)));
            }
        }

        public ProphetCenter()
        {
            ClassListView = new(this);
            ImageListView = new(this);
        }

        public void StopHandler()
        {
            tokenSource.Cancel();
            processing = false;
            ProcessingState = "Обработка прервана пользователем.";
        }
        public void SelectorHandler(string arg)
        {
            if (arg == null)
            {
                return;
            }
            ImageListView.SelectClass(arg[(arg.IndexOf(' ') + 1)..]);
            ImageListView.RaiseCollectionChanged();
        }
        public void ClearHandler()
        {
            DBManager.Clear();
        }
        public void ExecuteHandler()
        {
            if (processing || (inputPath == ""))
            {
                return;
            }
            _ = StartProcessing();
        }

        private async Task StartProcessing()
        {
            processing = true;
            ProcessingState = "Обработка началась.";
            _ = ProphetImagePredictor.ExecuteAsync(inputPath, tokenSource.Token, finalResult);
            await DetectObjectsAsync(finalResult);
            processing = false;
            ProcessingState = "Обработка закончена.";
        }

        private async Task DetectObjectsAsync(ISourceBlock<IReadOnlyList<YoloResult>> dataSource)
        {
            while (await dataSource.OutputAvailableAsync())
            {
                IReadOnlyList<YoloResult> data = dataSource.Receive();

                foreach (YoloResult item in data)
                {
                    await DBManager.AddAsync(new YoloItem(item));
                    await Task.Delay(1);
                    // Тоже ускоряет
                }
                // Ускоряет производительность - еще с прошлой лабы
                await Task.Delay(1);
            }
        }
    }
}
