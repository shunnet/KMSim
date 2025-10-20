using Snet.Core.handler;
using Snet.Utility;
using Snet.Windows.Controls.data;
using Snet.Windows.Core.mvvm;
using System.Collections.ObjectModel;

namespace Snet.Windows.KMSim
{
    /// <summary>
    /// 命令窗口查询视图模型
    /// </summary>
    public class CommandWindowViewModel : BindNotify
    {

        public CommandWindowViewModel()
        {
            ListViewItemsSource = new(App.EditModels);
            foreach (var item in App.EditModels)
            {
                SelectItems.Add(item.Name);
            }
        }

        /// <summary>
        /// 详情
        /// </summary>
        public string Details
        {
            get => GetProperty(() => Details);
            set => SetProperty(() => Details, value);
        }

        /// <summary>
        /// 选择的项
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
        /// 选择的项集合
        /// </summary>
        public IList<string> SelectItems
        {
            get => selectItems;
            set => SetProperty(ref selectItems, value);
        }
        private IList<string> selectItems = new List<string>();
        /// <summary>
        /// 视图项集合
        /// </summary>
        public ObservableCollection<EditModel> ListViewItemsSource
        {
            get => listViewItemsSource;
            set => SetProperty(ref listViewItemsSource, value);
        }
        private ObservableCollection<EditModel> listViewItemsSource = new ObservableCollection<EditModel>();
        /// <summary>
        /// 视图项集合已选中项
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
        /// 查询集合
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
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
        /// 显示详情
        /// </summary>
        public void ShowDetails(EditModel model)
        {
            if (model == null)
                return;

            Details = model.Name;
            Details += "\r\n\r\n";
            Details += $"{App.LanguageOperate.GetLanguageValue("描述")}：\r\n";
            Details += model.Description.Trims();
            Details += "\r\n\r\n";
            Details += $"{App.LanguageOperate.GetLanguageValue("原始数据")}：\r\n";
            Details += model.ToJson(true);


        }




    }
}
