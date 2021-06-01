using Prism.Commands;
using Prism.Mvvm;
using System.IO;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using HandyControl.Controls;
using System.Threading;

namespace AppUpdateClient.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        #region 屬性
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
        private bool _Step4Loading = false;
        public bool Step4Loading
        {
            get => _Step4Loading;
            set => SetProperty(ref _Step4Loading, value);
        }
        private int _stepindex = 0;
        public int StepIndex
        {
            get => _stepindex;
            set => SetProperty(ref _stepindex, value);
        }
        private string _UpdateContent = "";
        public string UpdateContent
        {
            get => _UpdateContent;
            set => SetProperty(ref _UpdateContent, value);
        }
        private string _title = "App Update Client";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }
        #endregion 屬性
        #region Command
        private bool _CanExecute = true;
        public bool CanExecute
        {
            get => _CanExecute;
            set => SetProperty(ref _CanExecute, value);
        }
        public DelegateCommand ExecuteUpdate { get; private set; }
        public DelegateCommand ExecuteCheck { get; private set; }
        #endregion Command
        public MainWindowViewModel()
        {
            ExecuteUpdate = new DelegateCommand(StartUpdate, () => CanExecute);
            ExecuteCheck = new DelegateCommand(CheckUpdate, () => CanExecute);
        }
        private void CheckUpdate()
        {
            try
            {
                AppUpdateModel.Model.Config ClientConfig = null;
                AppUpdateModel.Model.Config ServerConfig = null;
                using (StreamReader sr = new StreamReader($@"{AppDomain.CurrentDomain.BaseDirectory}\UpdateConfig.json"))
                {
                    ClientConfig = JsonConvert.DeserializeObject<AppUpdateModel.Model.Config>(sr.ReadToEnd());
                }
                if (ClientConfig != null)
                {
                    using (StreamReader sr = new StreamReader($@"{ClientConfig.UpdatePath}\UpdateConfig.json"))
                    {
                        ServerConfig = JsonConvert.DeserializeObject<AppUpdateModel.Model.Config>(sr.ReadToEnd());
                    }
                    UpdateContent = ServerConfig?.UpdateContent;
                    if (ServerConfig != null && ClientConfig.Version < ServerConfig.Version)
                    {
                        MessageBox.Show("已有新版本!!", "訊息", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("已是最新版本!!", "訊息", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        private void StartUpdate()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    AppUpdateModel.Model.Config ClientConfig = null;
                    AppUpdateModel.Model.Config ServerConfig = null;
                    // check update config version            
                    StepIndex = 0;
                    Step1Loading = true;
                    using (StreamReader sr = new StreamReader($@"{AppDomain.CurrentDomain.BaseDirectory}\UpdateConfig.json"))
                    {
                        ClientConfig = JsonConvert.DeserializeObject<AppUpdateModel.Model.Config>(sr.ReadToEnd());
                    }
                    if (ClientConfig != null)
                    {
                        using (StreamReader sr = new StreamReader($@"{ClientConfig.UpdatePath}\UpdateConfig.json"))
                        {
                            ServerConfig = JsonConvert.DeserializeObject<AppUpdateModel.Model.Config>(sr.ReadToEnd());
                        }
                        if (ServerConfig != null && ClientConfig.Version < ServerConfig.Version)
                        {
                            Step1Loading = false;
                            // close application
                            StepIndex = 1;
                            Step2Loading = true;
                            var Processes = Process.GetProcessesByName(ClientConfig.Restart);
                            foreach(var pro in Processes)
                            {
                                using(pro)
                                {
                                    pro?.Kill();
                                    pro?.WaitForExit();
                                }
                            }                            
                            Thread.Sleep(500);
                            Step2Loading = false;

                            // download file
                            StepIndex = 2;
                            Step3Loading = true;
                            CopyFolderAndFile(ClientConfig.UpdateList.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries), ClientConfig.UpdatePath);
                            // update config
                            using (StreamWriter sw = new StreamWriter($@"{AppDomain.CurrentDomain.BaseDirectory}\UpdateConfig.json"))
                            {
                                sw.WriteLine(JsonConvert.SerializeObject(ServerConfig));
                                sw.Flush();
                            }
                            Thread.Sleep(500);
                            Step3Loading = false;

                            StepIndex = 3;
                            Step4Loading = true;
                            // restart application
                            using (Process p = new Process())
                            {
                                p.StartInfo.FileName = $@"{AppDomain.CurrentDomain.BaseDirectory}\{ClientConfig.Restart}.exe";
                                p.Start();
                            }
                            // update clientconfig
                            using (StreamWriter sw = new StreamWriter($@"{AppDomain.CurrentDomain.BaseDirectory}\UpdateConfig.json"))
                            {
                                sw.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(ServerConfig));
                                sw.Flush();
                            }
                            Thread.Sleep(500);
                            Step4Loading = false;
                            MessageBox.Show("更新完成", "訊息", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                        else if (ServerConfig == null)
                        {
                            MessageBox.Show("找不到更新伺服器!!", "訊息", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                        else if (ServerConfig !=null &&ClientConfig.Version >= ServerConfig.Version)
                        {
                            MessageBox.Show("已是最新版本!!", "訊息", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }
                    }
                    else return;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.StackTrace);
                }
            });
        }
        private void CopyFolderAndFile(string[] Source, string ServerPath)
        {
            try
            {
                foreach (string temp in Source)
                {
                    string fPath = temp;
                    if (temp.Contains(ServerPath)) fPath = temp.Replace(ServerPath, "");
                    if (Directory.Exists($@"{ServerPath}{fPath}"))
                    {
                        Directory.CreateDirectory($@"{AppDomain.CurrentDomain.BaseDirectory}\{fPath}");
                        CopyFolderAndFile(Directory.EnumerateFileSystemEntries($@"{ServerPath}{fPath}", "*", SearchOption.AllDirectories).ToArray(), ServerPath);
                    }
                    else if (File.Exists($@"{ServerPath}{fPath}"))
                    {
                        File.Copy($@"{ServerPath}{fPath}", $@"{AppDomain.CurrentDomain.BaseDirectory}\{fPath}", overwrite: true);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
    }
}
