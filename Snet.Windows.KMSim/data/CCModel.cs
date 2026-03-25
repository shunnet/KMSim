namespace Snet.Windows.KMSim.data
{
    /// <summary>
    /// 内容配置模型，用于持久化当前打开的脚本文件路径及其逻辑代码内容。
    /// </summary>
    public class CCModel
    {
        /// <summary>
        /// 脚本文件的存储路径（磁盘全路径），默认为空字符串以避免空引用。
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// 脚本的逻辑代码文本内容，默认为空字符串以避免空引用。
        /// </summary>
        public string LogicCode { get; set; } = string.Empty;
    }
}
