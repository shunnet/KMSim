using CommunityToolkit.Mvvm.Input;
using ScottPlot.WPF;
using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.handler;
using Snet.Windows.Core.mvvm;
using Snet.Windows.KMSim.chart;
using Snet.Windows.KMSim.core;
using Snet.Windows.KMSim.data;
using Snet.Windows.KMSim.handler;
using Snet.Windows.KMSim.utility;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using static Snet.Windows.KMSim.utility.GlobalKeyboardHook;
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
            uiMessage.StartAsync().ConfigureAwait(false);

            // 获取鼠标坐标
            GetXYAsync(globalToken.Token).ConfigureAwait(false);

            // 图表操作
            chartOperate = ChartOperate.Instance(new()
            {
                ChartControl = ChartControl,
                LineAdjust = true,
                HideGrid = true,
                YCrosshairText = true,
                RefreshTime = _interval
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
            UpdateSystemMonitoringValueAsync(globalToken.Token).ConfigureAwait(false);

            //设置键盘监听
            globalKeyboardHook = hook;
            globalKeyboardHook.SetHook();
            globalKeyboardHook.KeyEvent += KeyEvent;

            //显示最后一次的操作
            SGAsync().ConfigureAwait(false);
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
        /// 执行逻辑时的取消控制
        /// </summary>
        private CancellationTokenSource? operateToken;

        /// <summary>
        /// 命令窗口
        /// </summary>
        private CommandWindow commandWindow;

        // 各功能独立的操作状态标志
        private bool _isStarting = false;
        private bool _isStopping = false;
        private bool _isRetrying = false;
        /// <summary>
        /// 间隔
        /// </summary>
        private int _interval = 100;
        /// <summary>
        /// 锁
        /// </summary>
        private readonly object _sync = new object();
        /// <summary>
        /// 任务
        /// </summary>
        private Task? _runTask;

        /// <summary>
        /// 配置文件
        /// </summary>
        private string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "KMSim.json");

        #endregion 对象

        #region 属性

        /// <summary>
        /// 文件存储路径
        /// </summary>
        private string FilePath;

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
        public IAsyncRelayCommand InfoTextChanged => p_InfoTextChanged ??= new AsyncRelayCommand<TextChangedEventArgs>(InfoTextChangedAsync);
        IAsyncRelayCommand p_InfoTextChanged;
        /// <summary>
        /// 数据清空
        /// </summary>
        public IAsyncRelayCommand Clear => p_Clear ??= new AsyncRelayCommand(ClearAsync);
        IAsyncRelayCommand p_Clear;
        /// <summary>
        /// 开始
        /// </summary>
        public IAsyncRelayCommand Start => p_Start ??= new AsyncRelayCommand(StartAsync);
        IAsyncRelayCommand p_Start;
        /// <summary>
        /// 停止
        /// </summary>
        public IAsyncRelayCommand Stop => p_Stop ??= new AsyncRelayCommand(StopAsync);
        IAsyncRelayCommand p_Stop;
        /// <summary>
        /// 重试
        /// </summary>
        public IAsyncRelayCommand Retry => p_Retry ??= new AsyncRelayCommand(RetryAsync);
        IAsyncRelayCommand p_Retry;
        /// <summary>
        /// 命令集合
        /// </summary>
        public IAsyncRelayCommand Command => p_Command ??= new AsyncRelayCommand(CommandAsync);
        IAsyncRelayCommand p_Command;
        /// <summary>
        /// 关于
        /// </summary>
        public IAsyncRelayCommand About => p_About ??= new AsyncRelayCommand(AboutAsync);
        IAsyncRelayCommand p_About;
        /// <summary>
        /// 留言
        /// </summary>
        public IAsyncRelayCommand LeaveWord => p_LeaveWord ??= new AsyncRelayCommand(LeaveWordAsync);
        IAsyncRelayCommand p_LeaveWord;
        /// <summary>
        /// 退出
        /// </summary>
        public IAsyncRelayCommand Exit => p_Exit ??= new AsyncRelayCommand(ExitAsync);
        IAsyncRelayCommand p_Exit;
        /// <summary>
        /// 新建
        /// </summary>
        public IAsyncRelayCommand New => p_New ??= new AsyncRelayCommand(NewAsync);
        IAsyncRelayCommand p_New;
        /// <summary>
        /// 打开
        /// </summary>
        public IAsyncRelayCommand Open => p_Open ??= new AsyncRelayCommand(OpenAsync);
        IAsyncRelayCommand p_Open;
        /// <summary>
        /// 保存
        /// </summary>
        public IAsyncRelayCommand Save => p_Save ??= new AsyncRelayCommand(SaveAsync);
        IAsyncRelayCommand p_Save;
        /// <summary>
        /// 关闭
        /// </summary>
        public IAsyncRelayCommand Close => p_Close ??= new AsyncRelayCommand(CloseAsync);
        IAsyncRelayCommand p_Close;
        #endregion 命令

        #region 方法

        #region 其余操作

        /// <summary>
        /// 窗体关闭时,保存文件位置,以便下次打开软件读取文件
        /// </summary>
        /// <returns></returns>
        public async Task CSAsync()
        {
            if (!FilePath.IsNullOrWhiteSpace() && !EditText.IsNullOrWhiteSpace())
            {
                CCModel config = new()
                {
                    StoragePath = FilePath,
                    LogicCode = EditText
                };
                FileHandler.StringToFile(configFile, config.ToJson(true));
                FileHandler.StringToFile(FilePath, EditText);
            }
        }

        /// <summary>
        /// 打开时通过配置获取显示数据
        /// </summary>
        /// <returns></returns>
        public async Task SGAsync()
        {
            if (File.Exists(configFile))
            {
                string json = FileHandler.FileToString(configFile);
                CCModel config = json.ToJsonEntity<CCModel>();
                FilePath = config.StoragePath;
                EditText = config.LogicCode;
            }
            if (EditText.IsNullOrWhiteSpace())
            {
                //显示默认数据
                EditText = @"CopyContentAsync = 记事本
LWinAsync
DelayAsync = 1000
PasteAsync
DelayAsync = 1000
EnterAsync
DelayAsync = 1000
CopyContentAsync = 欢迎使用 Shunnet.top 键鼠模拟器 
PasteAsync
DelayAsync = 2000
SelectAllAsync
BackAsync

While = true
PasteAsync
OemCommaAsync
DelayAsync = 1000
EnterAsync
";
            }
        }

        /// <summary>
        /// 按键事件处理（同类操作仅允许一个进行）
        /// </summary>
        /// <param name="key">按下的按键</param>
        /// <param name="keyboard">键盘事件类型（按下/松开）</param>
        public async void KeyEvent(Key key, KeyboardEventType keyboard)
        {
            // 仅在按下时处理
            if (keyboard != KeyboardEventType.KeyDown)
                return;

            try
            {
                if (key == Key.F10)
                {
                    // 若已在执行开始操作，则忽略
                    if (_isStarting) return;

                    _isStarting = true;
                    await StartAsync().ConfigureAwait(false);
                }
                else if (key == Key.F11)
                {
                    // 若已在执行停止操作，则忽略
                    if (_isStopping) return;

                    _isStopping = true;
                    await StopAsync().ConfigureAwait(false);
                }
                else if (key == Key.F12)
                {
                    // 若已在执行重试操作，则忽略
                    if (_isRetrying) return;

                    _isRetrying = true;
                    await RetryAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("异常")}: {ex.Message}");
            }
            finally
            {
                // 无论是否异常，操作结束后释放对应标志
                if (key == Key.F10) _isStarting = false;
                else if (key == Key.F11) _isStopping = false;
                else if (key == Key.F12) _isRetrying = false;
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
        /// 显示设备信息
        /// </summary>
        private async Task ShowDeviceInfoAsync(string info)
        {
            if (info.Contains("；"))
            {
                foreach (var item in info.Split('；'))
                {
                    await uiMessage.ShowAsync(item, withTime: false);
                }
            }
            else
            {
                await uiMessage.ShowAsync(info, withTime: false);
            }
        }

        /// <summary>
        /// 更新系统检测值
        /// </summary>
        private async Task UpdateSystemMonitoringValueAsync(CancellationToken token = default)
        {
            try
            {
                await Task.Run(async () =>
                {
                    HardwareData hardwareData = systemMonitoring.GetInfo(true);

                    await ShowDeviceInfoAsync(hardwareData.SystemName);
                    await ShowDeviceInfoAsync(hardwareData.SystemVer);
                    await ShowDeviceInfoAsync(hardwareData.SystemRunTime);
                    await ShowDeviceInfoAsync(hardwareData.CpuInfo);
                    await ShowDeviceInfoAsync(hardwareData.GpuInfo);
                    await ShowDeviceInfoAsync(hardwareData.MemoryInfo);
                    await ShowDeviceInfoAsync(hardwareData.DiskInfo);
                    await ShowDeviceInfoAsync(hardwareData.BiosInfo);
                    await ShowDeviceInfoAsync(hardwareData.NetworkInfo);

                    while (!token.IsCancellationRequested)
                    {
                        hardwareData = systemMonitoring.GetInfo();

                        foreach (var iteminfolist in hardwareData.Info)
                        {
                            if (iteminfolist.Key.Equals("内存"))
                            {
                                foreach (var item in iteminfolist.Values)
                                {
                                    double value = double.Parse(item.Value);
                                    if (item.Key.Equals("负载,Memory") && value > 0)
                                    {
                                        UpdateLineSeriesData("RAM", value);
                                    }
                                }
                            }
                            if (iteminfolist.Key.Equals("英伟达显卡") || iteminfolist.Key.Equals("因特尔显卡") || iteminfolist.Key.Equals("AMD显卡"))
                            {
                                foreach (var item in iteminfolist.Values)
                                {
                                    double value = double.Parse(item.Value);
                                    if (item.Key.Equals("负载,GPU Core") && value > 0)
                                    {
                                        UpdateLineSeriesData("Gpu", value);
                                    }
                                    if (item.Key.Equals("温度,GPU Core") && value > 0)
                                    {
                                        UpdateLineSeriesData("GpuTemp", value);
                                    }
                                }
                            }
                            if (iteminfolist.Key.Equals("处理器"))
                            {
                                foreach (var item in iteminfolist.Values)
                                {
                                    double value = double.Parse(item.Value);
                                    if (item.Key.Equals("负载,CPU Total") && value > 0)
                                    {
                                        UpdateLineSeriesData("Cpu", value);
                                    }
                                    if (item.Key.Equals("温度,Core Max") && value > 0)
                                    {
                                        UpdateLineSeriesData("CpuTemp", value);
                                    }
                                }
                            }

                            if (iteminfolist.Key.Equals("网络"))
                            {
                                foreach (var item in iteminfolist.Values)
                                {
                                    if (item.Key.Equals("负载,Network Utilization"))
                                    {
                                        double value = double.Parse(item.Value);
                                        if (value > 0)
                                        {
                                            UpdateLineSeriesData("NET", value);
                                        }
                                    }
                                }
                            }
                        }
                        await Task.Delay(_interval, token);
                    }
                }, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await uiMessage.ShowAsync(ex.Message);
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
                        await Task.Delay(_interval, token);
                    }
                }, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await MessageBox.Show($"{App.LanguageOperate.GetLanguageValue("异常")} : {ex.Message}", App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Exclamation);
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
            if (!EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                if (await MessageBox.Show(App.LanguageOperate.GetLanguageValue("检测到有输入，是否保存？"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect(App.LanguageOperate.GetLanguageValue("成功保存至")))
                    {
                        await MessageBox.Show(App.LanguageOperate.GetLanguageValue("已取消操作"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                    }
                }
            }

            if (!EditText.IsNullOrWhiteSpace() && !FilePath.IsNullOrWhiteSpace())
            {
                FileHandler.StringToFile(FilePath, EditText);
            }

            globalToken?.Cancel();
            operateToken?.Cancel();
            commandWindow?.Close();
            await uiMessage.StopAsync().ConfigureAwait(false);
            globalKeyboardHook?.Unhook();
            System.Windows.Application.Current.Shutdown();
            await CSAsync().ConfigureAwait(false);
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
                commandWindow.DataContext = new CommandWindowViewModel();
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
        /// 开始执行（线程安全，可重复调用）
        /// </summary>
        private Task StartAsync()
        {
            Task? previousTask;

            lock (_sync)
            {
                // 保存旧任务
                previousTask = _runTask;

                // 取消并释放旧 CTS
                operateToken?.Cancel();
                operateToken?.Dispose();
                operateToken = new CancellationTokenSource();

                // 启动新的任务
                _runTask = RunLoopAsync(operateToken.Token);
            }

            // 异步等待旧任务完成（不会阻塞 UI）
            previousTask?.ContinueWith(t =>
            {
                try { t.WaitAsync(operateToken.Token).ConfigureAwait(false); } catch { }
            }, TaskScheduler.Default).ConfigureAwait(false);

            return _runTask;
        }

        /// <summary>
        /// 停止执行（线程安全）
        /// </summary>
        private async Task StopAsync()
        {
            Task? running;
            lock (_sync)
            {
                operateToken?.Cancel();
                running = _runTask;
            }

            if (running != null)
            {
                try { await running.ConfigureAwait(false); }
                catch { } // 忽略任务异常
            }
        }

        /// <summary>
        /// 重试：先停止再启动
        /// </summary>
        private async Task RetryAsync()
        {
            await StopAsync().ConfigureAwait(false);
            await StartAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// 实际运行循环（死循环后台执行，顺序命令正常执行）
        /// </summary>
        private async Task RunLoopAsync(CancellationToken token)
        {
            List<Task> endlessTasks = new();

            try
            {
                if (EditText.IsNullOrWhiteSpace())
                    return;

                List<WhileModel> models = EditText.Parse();

                foreach (var item in models)
                {
                    if (item.EndlessLoop)
                    {
                        // 死循环任务单独并发执行
                        var t = Task.Run(async () =>
                        {
                            try
                            {
                                while (!token.IsCancellationRequested)
                                {
                                    foreach (var logic in item.Logics)
                                    {
                                        token.ThrowIfCancellationRequested();
                                        await ExecuteMethodAsync(logic.MethodName, logic.Parameters, token).ConfigureAwait(false);
                                    }
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // 安全退出
                            }
                            catch (Exception ex)
                            {
                                // 可以记录日志，不影响其他任务
                                await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("死循环任务异常")}：{ex}");
                            }
                        }, token);

                        endlessTasks.Add(t);
                    }
                    else
                    {
                        // 普通循环同步执行
                        for (int i = 0; i < item.LoopCount; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            foreach (var logic in item.Logics)
                            {
                                token.ThrowIfCancellationRequested();
                                await ExecuteMethodAsync(logic.MethodName, logic.Parameters, token).ConfigureAwait(false);
                            }
                        }
                    }
                }

                // 等待所有死循环任务（可选，如果希望 RunLoopAsync 直到手动停止才结束）
                if (endlessTasks.Count > 0)
                    await Task.WhenAll(endlessTasks);
            }
            catch (OperationCanceledException)
            {
                await uiMessage.ShowAsync(App.LanguageOperate.GetLanguageValue("流程已停止"));
            }
            catch (Exception ex)
            {
                await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("流程异常")}：{ex}");
            }
            finally
            {
                lock (_sync)
                {
                    operateToken?.Dispose();
                    operateToken = null;
                    _runTask = null;
                }
            }
        }




        /// <summary>
        /// 执行指定方法（反射 + 异步）
        /// </summary>
        private async Task ExecuteMethodAsync(string methodName, object[]? parameters, CancellationToken token = default)
        {
            try
            {
                object? result = await App.KMSimCore.InvokeMethodAsync(methodName, parameters, uiMessage, token);

                if (result != null)
                    await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("返回")} ：{result}");
                else
                    await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("成功")}");
            }
            catch (OperationCanceledException)
            {
                await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("已取消")}");
            }
            catch (Exception ex)
            {
                await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("执行")} {methodName} {App.LanguageOperate.GetLanguageValue("出错")} ：{ex.Message}");
            }
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
                await uiMessage.ShowAsync($"{App.LanguageOperate.GetLanguageValue("已保存至")}：{FilePath}");
            }


            if (!EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                if (await MessageBox.Show(App.LanguageOperate.GetLanguageValue("检测到有输入，是否保存？"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect(App.LanguageOperate.GetLanguageValue("成功保存至")))
                    {
                        await MessageBox.Show(App.LanguageOperate.GetLanguageValue("已取消操作"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                        return;
                    }
                }
            }

            if (!await saveSelect(App.LanguageOperate.GetLanguageValue("新建成功")))
            {
                await MessageBox.Show(App.LanguageOperate.GetLanguageValue("已取消操作"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                return;
            }

            EditText = string.Empty;
        }
        /// <summary>
        /// 打开
        /// </summary>
        /// <returns></returns>
        private async Task OpenAsync()
        {
            FilePath = Win32Handler.Select(App.LanguageOperate.GetLanguageValue("请选择文件"), false, new() { { $"(*.ini)", $" *.ini" } });
            if (!FilePath.IsNullOrWhiteSpace())
            {
                EditText = FileHandler.FileToString(FilePath);
                await MessageBox.Show(App.LanguageOperate.GetLanguageValue("打开成功"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
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
                await MessageBox.Show(App.LanguageOperate.GetLanguageValue("文件路径为空，请先新建"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                return;
            }

            if (!EditText.IsNullOrWhiteSpace() && FilePath.IsNullOrWhiteSpace())
            {
                if (await MessageBox.Show(App.LanguageOperate.GetLanguageValue("检测到有输入，是否保存？"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect(App.LanguageOperate.GetLanguageValue("成功保存至")))
                    {
                        await MessageBox.Show(App.LanguageOperate.GetLanguageValue("已取消操作"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                    }
                }
                return;
            }
            await CSAsync();
            await uiMessage.ShowAsync(App.LanguageOperate.GetLanguageValue("保存成功"));
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
                if (await MessageBox.Show(App.LanguageOperate.GetLanguageValue("检测到有输入，是否保存？"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.YesNo, Controls.@enum.MessageBoxImage.Question))
                {
                    if (!await saveSelect(App.LanguageOperate.GetLanguageValue("成功保存至")))
                    {
                        await MessageBox.Show(App.LanguageOperate.GetLanguageValue("已取消操作"), App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                        return;
                    }
                }
            }

            if (!EditText.IsNullOrWhiteSpace() && !FilePath.IsNullOrWhiteSpace())
            {
                await CSAsync();


            }

            EditText = string.Empty;
            FilePath = string.Empty;
            await uiMessage.ShowAsync(App.LanguageOperate.GetLanguageValue("关闭成功"));
        }

        /// <summary>
        /// 私有保存
        /// </summary>
        /// <returns></returns>
        private async Task<bool> saveSelect(string message)
        {
            string path = Win32Handler.Select(App.LanguageOperate.GetLanguageValue("请选择文件夹"), true);
            if (!string.IsNullOrEmpty(path))
            {
                string fileName = $"{App.LanguageOperate.GetLanguageValue("逻辑")}[{DateTime.Now.ToString("yyyyMMddHHmmss")}].ini";
                FilePath = Path.Combine(path, fileName);
                await CSAsync().ConfigureAwait(false);
                await MessageBox.Show($"{message}：{fileName}", App.LanguageOperate.GetLanguageValue("提示"), Controls.@enum.MessageBoxButton.OK, Controls.@enum.MessageBoxImage.Information);
                return true;
            }
            return false;
        }

        #endregion 文件操作

        #endregion 方法
    }
}
