using Snet.Windows.KMSim.core;
using System.Windows.Input;

namespace Snet.Windows.KMSim.data
{
    /// <summary>
    /// 配置数据模型
    /// </summary>
    public class ConfigModel
    {
        /// <summary>
        /// 休息时间，单位：毫秒
        /// </summary>
        public static int RestTime { get; set; } = 10;

        /// <summary>
        /// 符号映射表
        /// </summary>
        public static readonly Dictionary<char, (byte key, bool shift)> SymbolMap = new()
        {
            { '.', (Win32.VK_OEM_PERIOD, false) },
            { ',', (Win32.VK_OEM_COMMA, false) },
            { '<', (Win32.VK_OEM_COMMA, true) },
            { '>', (Win32.VK_OEM_PERIOD, true) },
            { '+', (Win32.VK_OEM_PLUS, true) },
            { '=', (Win32.VK_OEM_PLUS, false) },
            { '-', (Win32.VK_OEM_MINUS, false) },
            { '_', (Win32.VK_OEM_MINUS, true) },
            { '"', (Win32.VK_OEM_7, true) },
            { '\'', (Win32.VK_OEM_7, false) },
            { '*', ((byte)Key.D8, true) },
            { '~', (Win32.VK_OEM_3, true) },
            { '`', (Win32.VK_OEM_3, false) },
            { '!', ((byte)Key.D1, true) },
            { '@', ((byte)Key.D2, true) },
            { '#', ((byte)Key.D3, true) },
            { '$', ((byte)Key.D4, true) },
            { '%', ((byte)Key.D5, true) },
            { '^', ((byte)Key.D6, true) },
            { '&', ((byte)Key.D7, true) },
            { '(', ((byte)Key.D9, true) },
            { ')', ((byte)Key.D0, true) },
            { '|', (Win32.VK_OEM_5, true) },
            { '\\', (Win32.VK_OEM_5, false) },
            { '{', (Win32.VK_OEM_4, true) },
            { '}', (Win32.VK_OEM_6, true) },
            { '[', (Win32.VK_OEM_4, false) },
            { ']', (Win32.VK_OEM_6, false) },
            { '?', (Win32.VK_OEM_2, true) },
            { '/', (Win32.VK_OEM_2, false) },
            { ':', (Win32.VK_OEM_1, true) },
            { ';', (Win32.VK_OEM_1, false) }
        };
    }
}
