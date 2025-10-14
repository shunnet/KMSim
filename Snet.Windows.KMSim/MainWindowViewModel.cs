using CommunityToolkit.Mvvm.Input;
using ScottPlot.WPF;
using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.handler;
using Snet.Windows.Core.mvvm;
using Snet.Windows.KMSim.chart;
using Snet.Windows.KMSim.core;
using Snet.Windows.KMSim.utility;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using static Snet.Windows.KMSim.utility.SystemMonitoring;
using MessageBox = Snet.Windows.Controls.message.MessageBox;

namespace Snet.Windows.KMSim
{
    public class MainWindowViewModel : BindNotify
    {
        public MainWindowViewModel(GlobalKeyboardHook hook)
        {
            // 界面消息处理
            uiMessage.OnInfoEventAsync += async (object? sender, Model.data.EventInfoResult e) => Info = e.Message;
            _ = uiMessage.StartAsync().ConfigureAwait(false);

            // 获取鼠标坐标
            _ = GetXYAsync(globalToken.Token).ConfigureAwait(false);

            // 图表操作
            chartOperate = ChartOperate.Instance(new()
            {
                ChartControl = ChartControl,
                LineAdjust = true,
                HideGrid = true,
                YCrosshairText = true,
                RefreshTime = 10
            });
            chartOperate.On();
            chartOperate.Create(new() { SN = "Cpu", Title = "处理器", TitleEN = "Cpu" });
            chartOperate.Create(new() { SN = "CpuTemp", Title = "处理器温度", TitleEN = "CpuTemp" });
            chartOperate.Create(new() { SN = "Gpu", Title = "显卡", TitleEN = "Gpu" });
            chartOperate.Create(new() { SN = "GpuTemp", Title = "显卡温度", TitleEN = "GpuTemp" });
            chartOperate.Create(new() { SN = "NET", Title = "网络", TitleEN = "NET" });
            chartOperate.Create(new() { SN = "RAM", Title = "内存", TitleEN = "RAM" });

            // 系统监控
            systemMonitoring = SystemMonitoring.Instance();

            // 更新系统检测值
            _ = UpdateSystemMonitoringValueAsync(globalToken.Token);

            //设置键盘监听
            globalKeyboardHook = hook;
            globalKeyboardHook.SetHook();
            globalKeyboardHook.KeyEvent += KeyEvent;

        }

        #region 对象
        private GlobalKeyboardHook globalKeyboardHook;

        /// <summary>
        /// ui信息处理器
        /// </summary>
        private UiMessageHandler uiMessage = UiMessageHandler.Instance("Info");

        /// <summary>
        /// 图表操作
        /// </summary>
        private ChartOperate chartOperate;

        /// <summary>
        /// 系统信息监控
        /// </summary>
        private SystemMonitoring systemMonitoring;

        /// <summary>
        /// 全局的任务取消控制
        /// </summary>
        private CancellationTokenSource globalToken = new CancellationTokenSource();

        /// <summary>
        /// 命令窗口
        /// </summary>
        private CommandWindow commandWindow;

        #endregion 对象

        #region 属性

        /// <summary>
        /// 文件存储路径
        /// </summary>
        private string FilePath;

        /// <summary>
        /// 文件保存的路径
        /// </summary>
        private bool SavePath;

        /// <summary>
        /// 系统名称
        /// </summary>
        public string SystemName { get; set; }
        /// <summary>
        /// 系统版本
        /// </summary>
        public string SystemVer { get; set; }
        /// <summary>
        /// 系统运行时间
        /// </summary>
        public string SystemRunTime { get; set; }

        /// <summary>
        /// 控件
        /// </summary>
        public WpfPlot ChartControl
        {
            get => chartControl;
            set => SetProperty(ref chartControl, value);
        }
        private WpfPlot chartControl = new WpfPlot();

        /// <summary>
        /// 系统标题
        /// </summary>
        public string SystemTitle
        {
            get => GetProperty(() => SystemTitle);
            set => SetProperty(() => SystemTitle, value);
        }

        /// <summary>
        /// 信息事件
        /// </summary>
        public string Info
        {
            get => GetProperty(() => Info);
            set => SetProperty(() => Info, value);
        }

        /// <summary>
        /// 内容
        /// </summary>
        public string EditText
        {
            get => GetProperty(() => EditText);
            set => SetProperty(() => EditText, value);
        }

        #endregion 属性

        #region 命令
        /// <summary>
        /// 信息框事件
        /// </summary>
        public IAsyncRelayCommand InfoTextChanged => new AsyncRelayCommand<TextChangedEventArgs>(InfoTextChangedAsync);
        /// <summary>
        /// 数据清空
        /// </summary>
        public IAsyncRelayCommand Clear => new AsyncRelayCommand(ClearAsync);
        /// <summary>
        /// 开始
        /// </summary>
        public IAsyncRelayCommand Start => new AsyncRelayCommand(StartAsync);
        /// <summary>
        /// 停止
        /// </summary>
        public IAsyncRelayCommand Stop => new AsyncRelayCommand(StopAsync);
        /// <summary>
        /// 重试
        /// </summary>
        public IAsyncRelayCommand Retry => new AsyncRelayCommand(RetryAsync);
        /// <summary>
        /// 命令集合
        /// </summary>
        public IAsyncRelayCommand Command => new AsyncRelayCommand(CommandAsync);
        /// <summary>
        /// 关于
        /// </summary>
        public IAsyncRelayCommand About => new AsyncRelayCommand(AboutAsync);
        /// <summary>
        /// 留言
        /// </summary>
        public IAsyncRelayCommand LeaveWord => new AsyncRelayCommand(LeaveWordAsync);
        /// <summary>
        /// 退出
        /// </summary>
        public IAsyncRelayCommand Exit => new AsyncRelayCommand(ExitAsync);
        /// <summary>
        /// 新建
        /// </summary>
        public IAsyncRelayCommand New => new AsyncRelayCommand(NewAsync);
        /// <summary>
        /// 打开
        /// </summary>
        public IAsyncRelayCommand Open => new AsyncRelayCommand(OpenAsync);
        /// <summary>
        /// 保存
        /// </summary>
        public IAsyncRelayCommand Save => new AsyncRelayCommand(SaveAsync);
        /// <summary>
        /// 关闭
        /// </summary>
        public IAsyncRelayCommand Close => new AsyncRelayCommand(CloseAsync);
        #endregion 命令

        #region 方法

        #region 其余操作
        /// <summary>
        /// 按键
        /// </summary>
        /// <param name="key">键码</param>
        public void KeyEvent(Key key)
        {
            if (key == Key.F1)
            {
                _ = StartAsync().ConfigureAwait(false);
            }
            if (key == Key.F2)
            {
                _ = StopAsync().ConfigureAwait(false);
            }
            if (key == Key.F3)
            {
                _ = RetryAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 更新线条数据
        /// </summary>
        private void UpdateLineSeriesData(string name, double value)
        {
            chartOperate.Update(name, value);
        }

        /// <summary>
        /// 更新系统检测值
        /// </summary>
        private async Task UpdateSystemMonitoringValueAsync(CancellationToken token = default)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    HardwareData hardwareData = systemMonitoring.GetInfo();

                    await Task.Run(() =>
                    {
                        System.Windows.Application.Current?.Dispatcher.Invoke(new Action(() =>
                        {

                            SystemName = hardwareData.SystemName;
                            SystemVer = hardwareData.SystemVer;
                            SystemRunTime = hardwareData.SystemRunTime;

                            foreach (var iteminfolist in hardwareData.Info)
                            {
                                if (iteminfolist.Key.Equals("内存"))
                                {
                                    foreach (var item in iteminfolist.Vlaues)
                                    {
                                        if (item.Key.Equals("负载,Memory"))
                                        {
                                            UpdateLineSeriesData("RAM", double.Parse(item.Vlaue));
                                        }
                                    }
                                }
                                if (iteminfolist.Key.Equals("英伟达显卡") || iteminfolist.Key.Equals("因特尔显卡") || iteminfolist.Key.Equals("AMD显卡"))
                                {
                                    foreach (var item in iteminfolist.Vlaues)
                                    {
                                        if (item.Key.Equals("负载,GPU Core"))
                                        {
                                            UpdateLineSeriesData("Gpu", double.Parse(item.Vlaue));
                                        }
                                        if (item.Key.Equals("温度,GPU Core"))
                                        {
                                            UpdateLineSeriesData("GpuTemp", double.Parse(item.Vlaue));
                                        }
                                    }
                                }
                                if (iteminfolist.Key.Equals("处理器"))
                                {
                                    foreach (var item in iteminfolist.Vlaues)
                                    {
                                        if (item.Key.Equals("负载,CPU Total"))
                                        {
                                            UpdateLineSeriesData("Cpu", double.Parse(item.Vlaue));
                                        }
                                        if (item.Key.Equals("温度,Core Max"))
                                        {
                                            UpdateLineSeriesData("CpuTemp", double.Parse(item.Vlaue));
                                        }
                                    }
                                }

                                if (iteminfolist.Key.Equals("网络"))
                                {
                                    foreach (var item in iteminfolist.Vlaues)
                                    {
                                        if (item.Key.Equals("负载,Network Utilization"))
                                        {
                                            if (double.Parse(item.Vlaue) > 0)
                                            {
                                                UpdateLineSeriesData("NET", double.Parse(item.Vlaue));
                                            }
                                        }
                                    }
                                }
                            }

                        }));
                    }, token).ConfigureAwait(false);

                    await Task.Delay(300, token);
                }
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await MessageBox.Show($"error : {ex.Message}", "tips", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// 获取鼠标的YX坐标
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task GetXYAsync(CancellationToken token = default)
        {
            try
            {
                await Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (Win32.GetCursorPos(out Snet.Windows.KMSim.core.Win32.POINT desktopPoint))
                        {
                            // 显示到窗口标题
                            SystemTitle = $"{App.LanguageOperate.GetLanguageValue("SystemTitle")}  ‹ {desktopPoint.X:F0} , {desktopPoint.Y:F0} ›";
                        }
                        await Task.Delay(30, token);
                    }
                }, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await MessageBox.Show($"error : {ex.Message}", "tips", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Exclamation);
            }
        }

        /// <summary>
        /// 信息框事件
        /// 让滚动条一直处在最下方
        /// </summary>
        public Task InfoTextChangedAsync(TextChangedEventArgs? e)
        {
            System.Windows.Controls.TextBox textBox = e.Source.GetSource<System.Windows.Controls.TextBox>();
            textBox.SelectionStart = textBox.Text.Length;
            textBox.SelectionLength = 0;
            textBox.ScrollToEnd();
            return Task.CompletedTask;
        }

        /// <summary>
        /// 清空消息
        /// </summary>
        /// <returns></returns>
        public async Task ClearAsync()
        {
            await uiMessage.ClearAsync();
        }
        #endregion 其余操作

        #region 帮助操作
        /// <summary>
        /// 关于
        /// </summary>
        /// <returns></returns>
        private async Task AboutAsync()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://blog.shunnet.top/",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// 留言
        /// </summary>
        /// <returns></returns>
        private async Task LeaveWordAsync()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://blog.shunnet.top/leaveword",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        public async Task ExitAsync()
        {
            globalToken.Cancel();
            commandWindow?.Close();
            await uiMessage.StopAsync().ConfigureAwait(false);
            globalKeyboardHook?.KeyEvent -= KeyEvent;
            globalKeyboardHook?.Unhook();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// 命令
        /// </summary>
        /// <returns></returns>
        private async Task CommandAsync()
        {
            if (commandWindow == null)
            {
                commandWindow = new CommandWindow();
                commandWindow.Show();
                commandWindow.Closed += (s, e) =>
                {
                    commandWindow = null;
                };
            }
            commandWindow.Topmost = true;
            commandWindow.Topmost = false;
        }
        #endregion 帮助操作

        #region 逻辑操作
        /// <summary>
        /// 开始
        /// </summary>
        /// <returns></returns>
        private async Task StartAsync()
        {
            await MessageBox.Show($"StartAsync", "tips", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
            await uiMessage.ShowAsync("StartAsync");
        }
        /// <summary>
        /// 停止
        /// </summary>
        /// <returns></returns>
        private async Task StopAsync()
        {
            await uiMessage.ShowAsync("StopAsync");
        }
        /// <summary>
        /// 重试
        /// </summary>
        /// <returns></returns>
        private async Task RetryAsync()
        {
            await uiMessage.ShowAsync("RetryAsync");
        }
        #endregion 逻辑操作

        #region 文件操作


        /// <summary>
        /// 新建
        /// </summary>
        /// <returns></returns>
        private async Task NewAsync()
        {
            if (!EditText.IsNullOrWhiteSpace() && !FilePath.IsNullOrWhiteSpace())
            {
                FileHandler.StringToFile(FilePath, EditText);
                await uiMessage.ShowAsync($"已保存至：{FilePath}");
            }


            if (!EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                if (await MessageBox.Show("检测到有输入，是否保存？", "提示", Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect("成功保存至"))
                    {
                        await MessageBox.Show("已取消操作", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                        return;
                    }
                }
            }

            if (!await saveSelect("新建成功"))
            {
                await MessageBox.Show("已取消操作", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
            }

            EditText = string.Empty;
        }
        /// <summary>
        /// 打开
        /// </summary>
        /// <returns></returns>
        private async Task OpenAsync()
        {
            FilePath = Win32Handler.Select("请选择文件", false, new() { { $"(*.ini)", $" *.ini" } });
            if (!FilePath.IsNullOrWhiteSpace())
            {
                EditText = FileHandler.FileToString(FilePath);
                await MessageBox.Show("打开成功", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
            }
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        private async Task SaveAsync()
        {
            if (EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                await MessageBox.Show("文件路径为空，请先新建", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                return;
            }

            if (!EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                if (await MessageBox.Show("检测到有输入，是否需要保存到文件？", "提示", Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect("成功保存至"))
                    {
                        await MessageBox.Show("已取消操作", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                    }
                }
                return;
            }
            FileHandler.StringToFile(FilePath, EditText);
            await uiMessage.ShowAsync($"保存成功");
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        private async Task CloseAsync()
        {
            if (EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                return;
            }

            if (!EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                if (await MessageBox.Show("检测到有输入，是否需要保存到文件？", "提示", Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect("成功保存至"))
                    {
                        await MessageBox.Show("已取消操作", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                    }
                }
            }

            if (!EditText.IsNullOrWhiteSpace() && !FilePath.IsNullOrWhiteSpace())
            {
                FileHandler.StringToFile(FilePath, EditText);
            }

            EditText = string.Empty;
            FilePath = string.Empty;
            await uiMessage.ShowAsync($"关闭成功");
        }

        /// <summary>
        /// 私有保存
        /// </summary>
        /// <returns></returns>
        private async Task<bool> saveSelect(string message)
        {
            string path = Win32Handler.Select("请选择文件夹", true);
            if (!string.IsNullOrEmpty(path))
            {
                string fileName = $"{App.LanguageOperate.GetLanguageValue("逻辑")}[{DateTime.Now.ToString("yyyyMMddHHmmss")}].ini";
                FilePath = Path.Combine(path, fileName);
                FileHandler.StringToFile(FilePath, EditText);
                await MessageBox.Show($"{message}：{fileName}", "提示", Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                return true;
            }
            return false;
        }
        #endregion 文件操作

        #endregion 方法

    }
}
