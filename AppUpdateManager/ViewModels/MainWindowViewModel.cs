using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Linq;
using System.Threading;

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
            set => SetProperty(ref _UpdateList, value);
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
        public DelegateCommand ExecuteUpdatePath { get; private set; }
        public DelegateCommand ExecuteUpdateList { get; private set; }
        public DelegateCommand ExecuteUpdate { get; private set; }
        #endregion Command
        public MainWindowViewModel()
        {
            ExecuteUpdatePath = new DelegateCommand(SelectUpdatePath, () => CanExecute);
            ExecuteUpdateList = new DelegateCommand(SelectUpdateList, () => CanExecute);
            ExecuteUpdate = new DelegateCommand(StartUpdate, () => CanExecute);
        }
        private void SelectUpdatePath()
        {            
            var UpdateFolder = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (UpdateFolder.ShowDialog() == true)
            {
                UpdatePath = UpdateFolder.SelectedPath;
                if (File.Exists($@"{UpdatePath}\UpdateConfig.json"))
                {
                    using (StreamReader sr = new StreamReader($@"{UpdatePath}\UpdateConfig.json"))
                    {
                        var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Model.Config>(sr.ReadToEnd());
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
        private void SelectUpdateList()
        {            
            UpdateList = "";
            _UpdateListPath = "";
            // 先選擇資料夾
            var UpdateFolder = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            UpdateFolder.SelectedPath = AppDomain.CurrentDomain.BaseDirectory;
            if (UpdateFolder.ShowDialog() == true)
            {
                UpdateList = Path.GetFileName(UpdateFolder.SelectedPath) + ";";
                _UpdateListPath = UpdateFolder.SelectedPath + ";";
            }
            // 再選擇檔案
            var UpdateFile = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            UpdateFile.Multiselect = true;

            if (UpdateFile.ShowDialog() == true)
            {
                string[] Files = UpdateFile.FileNames;
                _UpdateListPath += string.Join(";", Files);

                foreach (string file in Files)
                {
                    UpdateList += Path.GetFileName(file) + ";";
                }
            }
        }
        private void StartUpdate()
        {
            // move old file to old folder
            Step1Loading = true;
            string oldFolder = $@"{UpdatePath}\old";
            if (Directory.Exists(oldFolder)) Directory.Delete(oldFolder, true);
            var Temps = Directory.EnumerateFileSystemEntries(UpdatePath, "*", SearchOption.AllDirectories).ToList();
            Directory.CreateDirectory(oldFolder);
            foreach (string temp in Temps)
            {
                if (!temp.Equals(oldFolder))
                    Directory.Move(temp, $@"{oldFolder}\{Path.GetFileName(temp)}");
            }
            Thread.Sleep(200);
            Step1Loading = false;

            StepIndex = 1;
            Step2Loading = true;
            // create server config 
            Model.Config serverConfig = new Model.Config();
            serverConfig.Version = ++Version;
            serverConfig.UpdatePath = UpdatePath;
            serverConfig.UpdateList = _UpdateListPath;
            serverConfig.UpdateContent = UpdateContent;
            serverConfig.Restart = Restart;
            serverConfig.UpdateTime = DateTime.Now;
            using (StreamWriter sw = new StreamWriter($@"{UpdatePath}\UpdateConfig.json"))
            {
                sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(serverConfig));
                sw.Flush();
            }
            Thread.Sleep(200);
            Step2Loading = false;

            StepIndex = 2;
            Step3Loading = true;
            // upload file
            CopyFolderAndFile(_UpdateListPath.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries));
            Step3Loading = false;
        }
        private void CopyFolderAndFile(string[] Source)
        {
            foreach (string temp in Source)
            {
                if (Directory.Exists(temp))
                {
                    Directory.CreateDirectory($@"{UpdatePath}\{temp.Replace($@"{AppDomain.CurrentDomain.BaseDirectory}", "")}");
                    CopyFolderAndFile(Directory.EnumerateFileSystemEntries(temp, "*", SearchOption.AllDirectories).ToArray());
                }
                else if (File.Exists(temp))
                {
                    File.Copy(temp, $@"{UpdatePath}\{temp.Replace($@"{AppDomain.CurrentDomain.BaseDirectory}", "")}", overwrite: true);
                }
            }
        }
    }
}
