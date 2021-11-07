using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;


namespace ProphetUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ProphetHelper prophetModel = new ProphetHelper();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = prophetModel;
        }
        private void buttonOpenDirectory(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    prophetModel.InputPath = dialog.SelectedPath;
                }
            }
        }
        private void buttonStartForecast(object sender, RoutedEventArgs e)
        {
            prophetModel.StartProcessing();
        }
        private void buttonStopForecast(object sender, RoutedEventArgs e)
        {
            prophetModel.StopProcess();
        }
    }

    
}
