using Snet.Windows.Controls.handler;
using Snet.Windows.Core;
using Snet.Windows.KMSim.core;
using Snet.Windows.KMSim.handler;

namespace Snet.Windows.KMSim
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        public MainWindow()
        {
            InitializeComponent();
            new EditHandler(edit, (new KMSimCore()).GetType().Get("Snet.Windows.KMSim.xml"), 10, color: ("#414141", "#FFFFFF"));
        }
    }
}