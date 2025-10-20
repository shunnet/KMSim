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
        /// 符号映射表<br/>
        /// (需要按下的键,组合键,大小写状态)
        /// </summary>
        public static readonly Dictionary<char, (byte key1, byte? key2, bool toggleCase)> SymbolMap = new()
        {
            // ----------- 常见符号 -----------
            { '.', (Win32.VK_OEM_PERIOD, null,false) },
            { ',', (Win32.VK_OEM_COMMA, null,false) },
            { '<', (Win32.VK_OEM_COMMA, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '>', (Win32.VK_OEM_PERIOD, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '+', (Win32.VK_OEM_PLUS, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '=', (Win32.VK_OEM_PLUS, null,false) },
            { '-', (Win32.VK_OEM_MINUS, null,false) },
            { '_', (Win32.VK_OEM_MINUS, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '"', (Win32.VK_OEM_7, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '\'', (Win32.VK_OEM_7, null,false) },
            { '*', ((byte)KeyInterop.VirtualKeyFromKey(Key.D8), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '~', (Win32.VK_OEM_3, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '`', (Win32.VK_OEM_3, null,false) },
            { '!', ((byte)KeyInterop.VirtualKeyFromKey(Key.D1), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '@', ((byte)KeyInterop.VirtualKeyFromKey(Key.D2), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '#', ((byte)KeyInterop.VirtualKeyFromKey(Key.D3), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '$', ((byte)KeyInterop.VirtualKeyFromKey(Key.D4), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '%', ((byte)KeyInterop.VirtualKeyFromKey(Key.D5), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '^', ((byte)KeyInterop.VirtualKeyFromKey(Key.D6), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '&', ((byte)KeyInterop.VirtualKeyFromKey(Key.D7), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '(', ((byte)KeyInterop.VirtualKeyFromKey(Key.D9), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { ')', ((byte)KeyInterop.VirtualKeyFromKey(Key.D0), (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '|', (Win32.VK_OEM_5, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '\\', (Win32.VK_OEM_5, null,false) },
            { '{', (Win32.VK_OEM_4, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '}', (Win32.VK_OEM_6, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '[', (Win32.VK_OEM_4, null,false) },
            { ']', (Win32.VK_OEM_6,  null,false) },
            { '?', (Win32.VK_OEM_2, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { '/', (Win32.VK_OEM_2,  null,false) },
            { ':', (Win32.VK_OEM_1, (byte)KeyInterop.VirtualKeyFromKey(Key.LeftShift),false) },
            { ';', (Win32.VK_OEM_1, null,false) },

            // ----------- 数字 0–9 -----------
            { '0', ((byte)KeyInterop.VirtualKeyFromKey(Key.D0), null, false) },
            { '1', ((byte)KeyInterop.VirtualKeyFromKey(Key.D1), null, false) },
            { '2', ((byte)KeyInterop.VirtualKeyFromKey(Key.D2), null, false) },
            { '3', ((byte)KeyInterop.VirtualKeyFromKey(Key.D3), null, false) },
            { '4', ((byte)KeyInterop.VirtualKeyFromKey(Key.D4), null, false) },
            { '5', ((byte)KeyInterop.VirtualKeyFromKey(Key.D5), null, false) },
            { '6', ((byte)KeyInterop.VirtualKeyFromKey(Key.D6), null, false) },
            { '7', ((byte)KeyInterop.VirtualKeyFromKey(Key.D7), null, false) },
            { '8', ((byte)KeyInterop.VirtualKeyFromKey(Key.D8), null, false) },
            { '9', ((byte)KeyInterop.VirtualKeyFromKey(Key.D9), null, false) },

             // ----------- 小写字母 a–z -----------
            { 'a', ((byte)KeyInterop.VirtualKeyFromKey(Key.A), null, false) },
            { 'b', ((byte)KeyInterop.VirtualKeyFromKey(Key.B), null, false) },
            { 'c', ((byte)KeyInterop.VirtualKeyFromKey(Key.C), null, false) },
            { 'd', ((byte)KeyInterop.VirtualKeyFromKey(Key.D), null, false) },
            { 'e', ((byte)KeyInterop.VirtualKeyFromKey(Key.E), null, false) },
            { 'f', ((byte)KeyInterop.VirtualKeyFromKey(Key.F), null, false) },
            { 'g', ((byte)KeyInterop.VirtualKeyFromKey(Key.G), null, false) },
            { 'h', ((byte)KeyInterop.VirtualKeyFromKey(Key.H), null, false) },
            { 'i', ((byte)KeyInterop.VirtualKeyFromKey(Key.I), null, false) },
            { 'j', ((byte)KeyInterop.VirtualKeyFromKey(Key.J), null, false) },
            { 'k', ((byte)KeyInterop.VirtualKeyFromKey(Key.K), null, false) },
            { 'l', ((byte)KeyInterop.VirtualKeyFromKey(Key.L), null, false) },
            { 'm', ((byte)KeyInterop.VirtualKeyFromKey(Key.M), null, false) },
            { 'n', ((byte)KeyInterop.VirtualKeyFromKey(Key.N), null, false) },
            { 'o', ((byte)KeyInterop.VirtualKeyFromKey(Key.O), null, false) },
            { 'p', ((byte)KeyInterop.VirtualKeyFromKey(Key.P), null, false) },
            { 'q', ((byte)KeyInterop.VirtualKeyFromKey(Key.Q), null, false) },
            { 'r', ((byte)KeyInterop.VirtualKeyFromKey(Key.R), null, false) },
            { 's', ((byte)KeyInterop.VirtualKeyFromKey(Key.S), null, false) },
            { 't', ((byte)KeyInterop.VirtualKeyFromKey(Key.T), null, false) },
            { 'u', ((byte)KeyInterop.VirtualKeyFromKey(Key.U), null, false) },
            { 'v', ((byte)KeyInterop.VirtualKeyFromKey(Key.V), null, false) },
            { 'w', ((byte)KeyInterop.VirtualKeyFromKey(Key.W), null, false) },
            { 'x', ((byte)KeyInterop.VirtualKeyFromKey(Key.X), null, false) },
            { 'y', ((byte)KeyInterop.VirtualKeyFromKey(Key.Y), null, false) },
            { 'z', ((byte)KeyInterop.VirtualKeyFromKey(Key.Z), null, false) },

            // ----------- 大写字母 A–Z (需 Shift) -----------
            { 'A', ((byte)KeyInterop.VirtualKeyFromKey(Key.A), null, true) },
            { 'B', ((byte)KeyInterop.VirtualKeyFromKey(Key.B), null, true) },
            { 'C', ((byte)KeyInterop.VirtualKeyFromKey(Key.C), null, true) },
            { 'D', ((byte)KeyInterop.VirtualKeyFromKey(Key.D), null, true) },
            { 'E', ((byte)KeyInterop.VirtualKeyFromKey(Key.E), null, true) },
            { 'F', ((byte)KeyInterop.VirtualKeyFromKey(Key.F), null, true) },
            { 'G', ((byte)KeyInterop.VirtualKeyFromKey(Key.G), null, true) },
            { 'H', ((byte)KeyInterop.VirtualKeyFromKey(Key.H), null, true) },
            { 'I', ((byte)KeyInterop.VirtualKeyFromKey(Key.I), null, true) },
            { 'J', ((byte)KeyInterop.VirtualKeyFromKey(Key.J), null, true) },
            { 'K', ((byte)KeyInterop.VirtualKeyFromKey(Key.K), null, true) },
            { 'L', ((byte)KeyInterop.VirtualKeyFromKey(Key.L), null, true) },
            { 'M', ((byte)KeyInterop.VirtualKeyFromKey(Key.M), null, true) },
            { 'N', ((byte)KeyInterop.VirtualKeyFromKey(Key.N), null, true) },
            { 'O', ((byte)KeyInterop.VirtualKeyFromKey(Key.O), null, true) },
            { 'P', ((byte)KeyInterop.VirtualKeyFromKey(Key.P), null, true) },
            { 'Q', ((byte)KeyInterop.VirtualKeyFromKey(Key.Q), null, true) },
            { 'R', ((byte)KeyInterop.VirtualKeyFromKey(Key.R), null, true) },
            { 'S', ((byte)KeyInterop.VirtualKeyFromKey(Key.S), null, true) },
            { 'T', ((byte)KeyInterop.VirtualKeyFromKey(Key.T), null, true) },
            { 'U', ((byte)KeyInterop.VirtualKeyFromKey(Key.U), null, true) },
            { 'V', ((byte)KeyInterop.VirtualKeyFromKey(Key.V), null, true) },
            { 'W', ((byte)KeyInterop.VirtualKeyFromKey(Key.W), null, true) },
            { 'X', ((byte)KeyInterop.VirtualKeyFromKey(Key.X), null, true) },
            { 'Y', ((byte)KeyInterop.VirtualKeyFromKey(Key.Y), null, true) },
            { 'Z', ((byte)KeyInterop.VirtualKeyFromKey(Key.Z), null, true) },
        };
    }
}
