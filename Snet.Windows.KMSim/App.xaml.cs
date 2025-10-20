using Microsoft.Extensions.DependencyInjection;
using Snet.Core.handler;
using Snet.Log;
using Snet.Model.data;
using Snet.Windows.Controls.data;
using Snet.Windows.Core.handler;
using Snet.Windows.KMSim.core;
using Snet.Windows.KMSim.handler;
using Snet.Windows.KMSim.utility;
using System.Reflection;
using System.Windows;

namespace Snet.Windows.KMSim
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 语言操作
        /// </summary>
        public readonly static LanguageModel LanguageOperate = new LanguageModel("Snet.Windows.KMSim", "Language", "Snet.Windows.KMSim.dll");

        /// <summary>
        /// 搜索的方式
        /// </summary>
        public readonly static BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// 键鼠模拟核心
        /// </summary>
        public readonly static KMSimCore KMSimCore = new KMSimCore();

        /// <summary>
        /// 编辑框模型集合
        /// </summary>
        public readonly static List<EditModel> EditModels = GetEditModels();

        /// <summary>
        /// 获取编辑框模型集合
        /// </summary>
        /// <returns></returns>
        private static List<EditModel> GetEditModels()
        {
            List<EditModel> models = new List<EditModel>();
            models.Add(new EditModel
            {
                Name = "=",
                Color = "#FF0000",
                Description = "命令与数据分隔符"
            });
            models.Add(new EditModel
            {
                Name = "‹",
                Color = "#D8A50F",
                Description = "嵌套数据使用的左括号"
            });
            models.Add(new EditModel
            {
                Name = "›",
                Color = "#D8A50F",
                Description = "嵌套数据使用的右括号"
            });
            models.Add(new EditModel
            {
                Name = "While",
                Color = "#27A5F7",
                Description = "流程循环多少次\r\n( true 为一直循环执行且为异步，支持多组 While ‹ Int32|Bool › )"
            });
            models.AddRange(KMSimCore.GetType().Get("Snet.Windows.KMSim.xml", "#27A5F7"));

            return models;
        }

        /// <summary>
        /// 在应用程序关闭时发生
        /// </summary>
        private void OnExit(object sender, ExitEventArgs e)
        {
            InjectionWpf.ClearService();
            GC.SuppressFinalize(this);
            GC.Collect();
        }

        /// <summary>
        /// 在加载应用程序时发生
        /// </summary>
        private void OnStartup(object sender, StartupEventArgs e)
        {
            //启动全局异常捕捉
            RegisterEvents();

            //注入钩子
            InjectionWpf.AddService(s => s.AddSingleton<GlobalKeyboardHook>());

            //打开主窗口
            InjectionWpf.Window<MainWindow, MainWindowViewModel>(true).Show();


        }

        #region 全局异常捕捉

        /// <summary>
        /// 全局异常捕捉
        /// </summary>
        private void RegisterEvents()
        {
            //Task线程内未捕获异常处理事件
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            //UI线程未捕获异常处理事件（UI主线程）
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;

            //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        //Task线程报错
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            try
            {
                var exception = e.Exception as Exception;
                if (exception.HResult == -2146233088)
                    return;

                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                e.SetObserved();
            }
        }

        //非UI线程未捕获异常处理事件(例如自己创建的一个子线程)
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null)
                {
                    HandleException(exception);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                //ignore
            }
        }

        //UI线程未捕获异常处理事件（UI主线程）
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                HandleException(e.Exception);
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally
            {
                //处理完后，我们需要将Handler=true表示已此异常已处理过
                e.Handled = true;
            }
        }

        /// <summary>
        /// 处理异常到界面显示与本地日志记录
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task HandleException(Exception e)
        {
            string source = e.Source ?? string.Empty;
            string message = e.Message ?? string.Empty;
            string stackTrace = e.StackTrace ?? string.Empty;
            string msg;
            if (!string.IsNullOrEmpty(source))
            {
                msg = source;
                if (!string.IsNullOrEmpty(message))
                    msg += $"\r\n{message}";
                if (!string.IsNullOrEmpty(stackTrace))
                    msg += $"\r\n\r\n{stackTrace}";
            }
            else if (!string.IsNullOrEmpty(message))
            {
                msg = message;
                if (!string.IsNullOrEmpty(stackTrace))
                    msg += $"\r\n\r\n{stackTrace}";
            }
            else if (!string.IsNullOrEmpty(stackTrace))
                msg = stackTrace;
            else
                msg = "未知异常";
            if (Application.Current == null)
                return;
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                await Snet.Windows.Controls.message.MessageBox.Show(msg, LanguageOperate.GetLanguageValue("全局异常捕获"), Snet.Windows.Controls.@enum.MessageBoxButton.OK, Snet.Windows.Controls.@enum.MessageBoxImage.Exclamation);
            }
            , System.Windows.Threading.DispatcherPriority.Loaded);

            LogHelper.Error(msg, "Snet.Iot.Tool.log", e);
        }

        #endregion
    }

}
