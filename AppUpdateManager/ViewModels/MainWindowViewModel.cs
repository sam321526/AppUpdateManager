using HandyControl.Controls;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppUpdateManager.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        # region 屬性
        private bool _Step1Loading = false;
        public bool Step1Loading
        {
            get => _Step1Loading;
            set => SetProperty(ref _Step1Loading, value);
        }
        private bool _Step2Loading = false;
        public bool Step2Loading
        {
            get => _Step2Loading;
            set => SetProperty(ref _Step2Loading, value);
        }
        private bool _Step3Loading = false;
        public bool Step3Loading
        {
            get => _Step3Loading;
            set => SetProperty(ref _Step3Loading, value);
        }
        private int _stepindex = 0;
        public int StepIndex
        {
            get => _stepindex;
            set => SetProperty(ref _stepindex, value);
        }
        private string _title = "App Update Manager";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        private int _Version = 1;
        public int Version
        {
            get => _Version;
            set => SetProperty(ref _Version, value);
        }
        private string _SourcePath = "";
        public string SourcePath
        {
            get => _SourcePath;
            set => SetProperty(ref _SourcePath, value);
        }
        private string _UpdatePath = "";
        public string UpdatePath
        {
            get => _UpdatePath;
            set => SetProperty(ref _UpdatePath, value);
        }
        private string _Restart = "";
        public string Restart
        {
            get => _Restart;
            set => SetProperty(ref _Restart, value);
        }
        private string _UpdateListPath = "";
        private string _UpdateList = "";
        public string UpdateList
        {
            get => _UpdateList;
            set
            {
                if (string.IsNullOrEmpty(value))
                    _UpdateListPath = "";
                SetProperty(ref _UpdateList, value);
            }
        }
        private string _UpdateContent = "";
        public string UpdateContent
        {
            get => _UpdateContent;
            set => SetProperty(ref _UpdateContent, value);
        }
        #endregion 屬性
        #region Command
        private bool _canExecute = true;
        public bool CanExecute
        {
            set => SetProperty(ref _canExecute, value);
            get => _canExecute;
        }
        public DelegateCommand ExecuteSourcePath { get; private set; }
        public DelegateCommand ExecuteUpdatePath { get; private set; }
        public DelegateCommand ExecuteUpdateListFolder { get; private set; }
        public DelegateCommand ExecuteUpdateListFile { get; private set; }
        public DelegateCommand ExecuteUpdate { get; private set; }
        #endregion Command
        public MainWindowViewModel()
        {
            ExecuteUpdatePath = new DelegateCommand(SelectUpdatePath, () => CanExecute);
            ExecuteUpdateListFolder = new DelegateCommand(SelectUpdateListFolder, () => CanExecute);
            ExecuteUpdateListFile = new DelegateCommand(SelectUpdateListFile, () => CanExecute);
            ExecuteUpdate = new DelegateCommand(StartUpdate, () => CanExecute);
            ExecuteSourcePath = new DelegateCommand(SelecteSourcePath, () => CanExecute);
        }
        private void SelecteSourcePath()
        {
            // 先選擇資料夾
            var SourceFolder = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (SourceFolder.ShowDialog() == true)
            {
                SourcePath = SourceFolder.SelectedPath + @"\";
            }
        }
        private void SelectUpdatePath()
        {
            var UpdateFolder = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (UpdateFolder.ShowDialog() == true)
            {
                UpdatePath = UpdateFolder.SelectedPath;
                // 有設定檔就讀取近來
                if (File.Exists($@"{UpdatePath}\UpdateConfig.json"))
                {
                    using (StreamReader sr = new StreamReader($@"{UpdatePath}\UpdateConfig.json"))
                    {
                        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<AppUpdateModel.Model.Config>(sr.ReadToEnd());
                        Version = config.Version;
                        UpdateContent = config.UpdateContent;
                        Restart = config.Restart;
                        _UpdateListPath = config.UpdateList;
                        if (!string.IsNullOrEmpty(_UpdateListPath))
                        {
                            foreach (string temp in _UpdateListPath.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                UpdateList += Path.GetFileName(temp) + ";";
                            }
                        }
                    }
                }
            }
        }
        private void SelectUpdateListFolder()
        {
            var UpdateFolder = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            UpdateFolder.SelectedPath = SourcePath;

            if (UpdateFolder.ShowDialog() == true)
            {
                string Folder = UpdateFolder.SelectedPath.Replace(SourcePath, "");
                _UpdateListPath += $"{Folder};";
                UpdateList += Path.GetFileName(Folder) + ";";
            }
        }
        private void SelectUpdateListFile()
        {
            // 選擇檔案
            var UpdateFile = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            UpdateFile.Multiselect = true;
            UpdateFile.InitialDirectory = UpdatePath;
            if (UpdateFile.ShowDialog() == true)
            {
                string[] Files = UpdateFile.FileNames;
                foreach (string file in Files)
                {
                    _UpdateListPath += $"{file.Replace(SourcePath, "")};";
                    UpdateList += Path.GetFileName(file) + ";";
                }
            }
        }
        private void StartUpdate()
        {
            Task.Factory.StartNew(() =>
            {
                // move old file to old folder
                StepIndex = 0;
                Step1Loading = true;
                string oldFolder = $@"{UpdatePath}\old";
                if (Directory.Exists(oldFolder)) Directory.Delete(oldFolder, true);
                var Temps = Directory.EnumerateFileSystemEntries(UpdatePath, "*", SearchOption.AllDirectories).ToList();
                Directory.CreateDirectory(oldFolder);
                foreach (string temp in Temps)
                {
                    try
                    {
                        if (!temp.Equals(oldFolder))
                            Directory.Move(temp, $@"{oldFolder}\{Path.GetFileName(temp)}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                Thread.Sleep(500);
                Step1Loading = false;

                StepIndex = 1;
                Step2Loading = true;
                // create server config 
                AppUpdateModel.Model.Config serverConfig = new AppUpdateModel.Model.Config
                {
                    Version = ++Version,
                    UpdatePath = UpdatePath + "\\",
                    UpdateList = _UpdateListPath,
                    UpdateContent = UpdateContent,
                    Restart = Restart,
                    UpdateTime = DateTime.Now
                };
                using (StreamWriter sw = new StreamWriter($@"{UpdatePath}\UpdateConfig.json"))
                {
                    sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(serverConfig));
                    sw.Flush();
                }
                Thread.Sleep(500);
                Step2Loading = false;

                StepIndex = 2;
                Step3Loading = true;
                // upload file
                CopyFolderAndFile(_UpdateListPath.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries));
                Step3Loading = false;
                MessageBox.Show("上傳完成", "訊息", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            });
        }
        private void CopyFolderAndFile(string[] Source)
        {
            foreach (string temp in Source)
            {
                string fPath = temp;
                if (temp.Contains(SourcePath)) fPath = temp.Replace(SourcePath, "");
                if (Directory.Exists($@"{SourcePath}{fPath}"))
                {
                    Directory.CreateDirectory($@"{UpdatePath}\{fPath}");
                    CopyFolderAndFile(Directory.EnumerateFileSystemEntries($@"{SourcePath}{fPath}", "*", SearchOption.AllDirectories).ToArray());
                }
                else if (File.Exists($@"{SourcePath}{fPath}"))
                {
                    File.Copy($@"{SourcePath}{fPath}", $@"{UpdatePath}\{fPath}", overwrite: true);
                }
            }
        }
    }
}
