namespace Snet.Windows.KMSim.data
{
    /// <summary>
    /// 循环模型，封装一个脚本循环块的执行参数和内部逻辑指令集合。
    /// </summary>
    public class WhileModel
    {
        /// <summary>
        /// 是否无限循环（true 表示持续执行直到手动停止）
        /// </summary>
        public bool EndlessLoop { get; set; }

        /// <summary>
        /// 循环次数（仅在 <see cref="EndlessLoop"/> 为 false 时有效）
        /// </summary>
        public int LoopCount { get; set; }

        /// <summary>
        /// 循环内部的逻辑指令集合，默认初始化为空列表以避免空引用。
        /// </summary>
        public List<LogicModel> Logics { get; set; } = [];
    }
}
