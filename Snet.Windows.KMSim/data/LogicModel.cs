namespace Snet.Windows.KMSim.data
{
    /// <summary>
    /// 逻辑模型，描述单条脚本指令的方法名及其调用参数。
    /// </summary>
    public class LogicModel
    {
        /// <summary>
        /// 待调用的方法名称，默认为空字符串以避免空引用。
        /// </summary>
        public string MethodName { get; set; } = string.Empty;

        /// <summary>
        /// 方法调用时传入的参数数组，为 null 表示无参数。
        /// </summary>
        public object[]? Parameters { get; set; }
    }
}
