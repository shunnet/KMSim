namespace Snet.Windows.KMSim.data
{
    /// <summary>
    /// 循环模型
    /// </summary>
    public class WhileModel
    {
        public bool EndlessLoop { get; set; }

        public int LoopCount { get; set; }

        public List<LogicModel> Logics { get; set; }
    }
}
