using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ProphetBack
{
    public abstract class DBModel : IEnumerable, INotifyCollectionChanged
    {
        public DBModel(ProphetCenter vm)
        {
            VM = vm;
            VM.DBManager.DataChanged += RaiseCollectionChanged;
        }

        public ProphetCenter VM { get; }
        public event NotifyCollectionChangedEventHandler CollectionChanged;


        public void RaiseCollectionChanged()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public static BitmapImage GetBitmapImageFromByte(byte[] arr)
        {
            using (var ms = new System.IO.MemoryStream(arr))
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                return image;
            }
        }


        public abstract IEnumerator GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    public class ClassListView : DBModel
    {
        public ClassListView(ProphetCenter vm) : base(vm) { }

        public override IEnumerator<string> GetEnumerator()
        {
            return VM.DBManager.GetClasses().GetEnumerator();
        }
    }


    public class ImageListView : DBModel
    {
        public ImageListView(ProphetCenter vm) : base(vm) { }

        private string selectedClass;

        public void SelectClass(string neededClass)
        {
            selectedClass = neededClass;
        }

        public override IEnumerator<BitmapImage> GetEnumerator()
        {
            return VM.DBManager.GetImages(selectedClass).Select(x => GetBitmapImageFromByte(x)).GetEnumerator();
        }
    }
}
