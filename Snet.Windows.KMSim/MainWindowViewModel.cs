using CommunityToolkit.Mvvm.Input;
using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.handler;
using Snet.Windows.Core.mvvm;
using Snet.Windows.KMSim.core;
using System.Windows.Controls;

namespace Snet.Windows.KMSim
{
    public class MainWindowViewModel : BindNotify
    {
        public MainWindowViewModel()
        {
            uiMessage.OnInfoEventAsync += async (object? sender, Model.data.EventInfoResult e) => Info = e.Message;
            uiMessage.StartAsync().ConfigureAwait(false);
            GetXYAsync().ConfigureAwait(false);
        }

        #region 对象
        /// <summary>
        /// ui信息处理器
        /// </summary>
        private UiMessageHandler uiMessage = UiMessageHandler.Instance("Info");
        #endregion 对象

        #region 属性
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
                }, token);
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
        }
        /// <summary>
        /// 信息框事件
        /// 让滚动条一直处在最下方
        /// </summary>
        public Task InfoTextChangedAsync(TextChangedEventArgs? e)
        {
            TextBox textBox = e.Source.GetSource<TextBox>();
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
        /// <summary>
        /// 开始
        /// </summary>
        /// <returns></returns>
        private async Task StartAsync()
        {
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
        /// <summary>
        /// 命令
        /// </summary>
        /// <returns></returns>
        private async Task CommandAsync()
        {
            await uiMessage.ShowAsync("CommandAsync");
        }
        /// <summary>
        /// 关于
        /// </summary>
        /// <returns></returns>
        private async Task AboutAsync()
        {
            await uiMessage.ShowAsync("AboutAsync");
        }
        /// <summary>
        /// 留言
        /// </summary>
        /// <returns></returns>
        private async Task LeaveWordAsync()
        {
            await uiMessage.ShowAsync("LeaveWordAsync");
        }
        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        private async Task ExitAsync()
        {
            await uiMessage.ShowAsync("ExitAsync");
        }
        /// <summary>
        /// 新建
        /// </summary>
        /// <returns></returns>
        private async Task NewAsync()
        {
            await uiMessage.ShowAsync("NewAsync");
        }
        /// <summary>
        /// 打开
        /// </summary>
        /// <returns></returns>
        private async Task OpenAsync()
        {
            await uiMessage.ShowAsync("OpenAsync");
        }
        /// <summary>
        /// 保存
        /// </summary>
        /// <returns></returns>
        private async Task SaveAsync()
        {
            await uiMessage.ShowAsync("SaveAsync");
        }
        /// <summary>
        /// 关闭
        /// </summary>
        /// <returns></returns>
        private async Task CloseAsync()
        {
            await uiMessage.ShowAsync("CloseAsync");
        }

        #endregion 方法













    }
}
