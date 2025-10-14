using Snet.Utility;
using Snet.Windows.Controls.data;
using Snet.Windows.Controls.handler;
using Snet.Windows.Core;
using Snet.Windows.KMSim.core;
using Snet.Windows.KMSim.handler;
using Snet.Windows.KMSim.utility;

namespace Snet.Windows.KMSim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        public MainWindow(GlobalKeyboardHook hook)
        {
            InitializeComponent();

            List<EditModel> model = (new KMSimCore()).GetType().Get("Snet.Windows.KMSim.xml", "#27A5F7");
            model.Add(new EditModel
            {
                Name = "=",
                Color = "#FF0000",
                Description = "命令与数据分隔符"
            });
            new EditHandler(edit, model, maxCompletionRows: 10, color: ("#414141", "#FFFFFF"));


            this.Closing += (object? sender, System.ComponentModel.CancelEventArgs e) =>
            {
                _ = this.DataContext.GetSource<MainWindowViewModel>().ExitAsync().ConfigureAwait(false);
            };
        }
    }
}