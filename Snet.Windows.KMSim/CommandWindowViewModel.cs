using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.data;
using Snet.Windows.Core.mvvm;
using System.Collections.ObjectModel;
using System.Text;

namespace Snet.Windows.KMSim
{
    /// <summary>
    /// 命令窗口查询视图模型，提供命令列表的搜索过滤与详情展示功能。
    /// </summary>
    public class CommandWindowViewModel : BindNotify
    {
        /// <summary>
        /// 构造函数，初始化视图项集合数据源与下拉选择项。
        /// </summary>
        public CommandWindowViewModel()
        {
            ListViewItemsSource = new(App.EditModels);
            foreach (var item in App.EditModels)
            {
                SelectItems.Add(item.Name);
            }
        }

        /// <summary>
        /// 命令详情文本，用于在详情区域显示选中项的名称、描述和原始数据。
        /// </summary>
        public string Details
        {
            get => GetProperty(() => Details);
            set => SetProperty(() => Details, value);
        }

        /// <summary>
        /// 当前选中的筛选项名称，变更时自动触发列表查询过滤。
        /// </summary>
        public string SelectItem
        {
            get => GetProperty(() => SelectItem);
            set
            {
                SetProperty(() => SelectItem, value);
                QueryList(value);
            }
        }

        /// <summary>
        /// 下拉选择项集合，包含所有可选的命令名称。
        /// </summary>
        public IList<string> SelectItems
        {
            get => selectItems;
            set => SetProperty(ref selectItems, value);
        }
        private IList<string> selectItems = new List<string>();

        /// <summary>
        /// 视图项集合数据源，绑定到 ListView 控件。
        /// </summary>
        public ObservableCollection<EditModel> ListViewItemsSource
        {
            get => listViewItemsSource;
            set => SetProperty(ref listViewItemsSource, value);
        }
        private ObservableCollection<EditModel> listViewItemsSource = new ObservableCollection<EditModel>();

        /// <summary>
        /// 视图列表中当前选中的项，变更时自动触发详情展示。
        /// </summary>
        public EditModel ListViewSelectedItem
        {
            get => GetProperty(() => ListViewSelectedItem);
            set
            {
                SetProperty(() => ListViewSelectedItem, value);
                ShowDetails(value);
            }
        }

        /// <summary>
        /// 根据名称模糊查询并过滤命令列表。
        /// 当名称为空时恢复显示全部命令。
        /// </summary>
        /// <param name="name">查询的名称关键字</param>
        public void QueryList(string name)
        {
            if (!name.IsNullOrWhiteSpace())
            {
                ListViewItemsSource = new(App.EditModels.Where(c => c.Name.Contains(name.Trim())));
            }
            else
            {
                ListViewItemsSource = new(App.EditModels);
            }
        }

        /// <summary>
        /// 显示选中命令的详细信息，包括名称、描述和 JSON 格式的原始数据。
        /// 使用 StringBuilder 避免多次字符串拼接带来的性能开销。
        /// </summary>
        /// <param name="model">选中的编辑模型</param>
        public void ShowDetails(EditModel model)
        {
            if (model == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine(model.Name);
            sb.AppendLine();
            sb.AppendLine($"{App.LanguageOperate.GetLanguageValue("描述")}：");
            sb.AppendLine(model.Description.Trims());
            sb.AppendLine();
            sb.AppendLine($"{App.LanguageOperate.GetLanguageValue("原始数据")}：");
            sb.Append(model.ToJson(true));

            Details = sb.ToString();
        }
    }
}
