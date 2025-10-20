using Snet.Utility;
using Snet.Windows.Controls.handler;
using Snet.Windows.Core;
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
            new EditHandler(edit, App.EditModels, maxCompletionRows: 10, color: ("#414141", "#FFFFFF"));
            this.Closing += (object? sender, System.ComponentModel.CancelEventArgs e) =>
            {
                _ = this.DataContext.GetSource<MainWindowViewModel>().ExitAsync().ConfigureAwait(false);
            };
        }
    }
}