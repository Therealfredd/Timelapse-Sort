using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Timelapse_Sort
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _updating = false;
        private string _folderPath = "";
        private string _parentFolderPath = "";
        private string _folderPrefix = "set_";
        private string _fileExtension = ".arw";
        private int _currentFolderNumber = 0;
        private int _currentFileCount = 0;
        private int _updatingInterval = 5;
        CancellationTokenSource source = new CancellationTokenSource();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Start_button(object sender, RoutedEventArgs e)
        {
            if (_updating)
            {
                source.Cancel();
                _updating = false;
                Button.Content = "Start!";
            }
            else
            {
                
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
                if (dialog.ShowDialog(this).GetValueOrDefault())
                {
                    _folderPath = dialog.SelectedPath;
                    _parentFolderPath = Path.GetDirectoryName(_folderPath);
                    source = new CancellationTokenSource();
                    InitializeFileUpdate();
                    Button.Content = "Stop!";
                }
            }
        }
        
        private async Task CheckForFileUpdate(Action onTick,
            TimeSpan interval, 
            CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                _updating = true;
                onTick?.Invoke();

                if(interval > TimeSpan.Zero)
                {
                    await Task.Delay(interval, token);
                }
            }
            _updating = false;
        }
        
        private void InitializeFileUpdate()
        {
            if (_updating)
            {
                return;
            }

            _currentFileCount = 0;
            _currentFolderNumber = FindMaxPrefixFolder();
            var interval = TimeSpan.FromSeconds(_updatingInterval);
            _ = CheckForFileUpdate(DailyDiaryUpdate, interval, source.Token);
        }

        private void DailyDiaryUpdate()
        {
            foreach (var file in Directory.GetFiles(_folderPath))
            {
                if (!file.ToLower().Contains(_fileExtension))
                {
                    return;
                }
                var dest = Path.Combine(_parentFolderPath, _folderPrefix+_currentFolderNumber);
                if (!Directory.Exists(dest))
                {
                    Directory.CreateDirectory(dest);
                }
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)));
                _currentFileCount++;
                if (_currentFileCount >= 9)
                {
                    _currentFileCount = 0;
                    _currentFolderNumber++;
                }
            }
        }

        private int FindMaxPrefixFolder()
        {
            return (from folder in Directory.GetDirectories(_parentFolderPath, _folderPrefix + "*", SearchOption.TopDirectoryOnly) 
                let split = folder.LastIndexOf('_') 
                select folder.Substring(split + 1, folder.Length - 1 - split) 
                into str select Convert.ToInt32(str)).Prepend(0).Max();
        }
    }
}
