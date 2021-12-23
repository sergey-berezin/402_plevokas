using System.Windows;
using System.Windows.Controls;
using ProphetBack;

namespace RecognizerUI
{
    public partial class MainWindow : Window
    {
        private readonly ProphetCenter prophetModel = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = prophetModel;
        }

        public void OnClickChooseDirectory(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    prophetModel.InputPath = dialog.SelectedPath;
                }
            }
        }

        public void OnClickExecute(object sender, RoutedEventArgs e)
        {
            prophetModel.ExecuteHandler();
        }

        public void OnClickStop(object sender, RoutedEventArgs e)
        {
            prophetModel.StopHandler();
        }

        public void OnClickClear(object sender, RoutedEventArgs e)
        {
            prophetModel.ClearHandler();
        }

        public void OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            prophetModel.SelectorHandler(((ListBox)sender).SelectedItem as string);
        }
    }

}
