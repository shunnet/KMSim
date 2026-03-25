using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace Snet.Windows.KMSim.utility
{
    /// <summary>
    /// 全局键盘钩子类，通过 Win32 低级键盘钩子实现全局按键监听。
    /// 支持按下、松开事件与组合键检测（例如 Ctrl+F1）。
    /// 实现 IDisposable 以确保钩子在对象销毁时被正确卸载。
    /// </summary>
    public class GlobalKeyboardHook : IDisposable
    {
        /// <summary>
        /// 键盘事件类型枚举，区分按键按下与松开。
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum KeyboardEventType
        {
            /// <summary>按键按下</summary>
            KeyDown,
            /// <summary>按键松开</summary>
            KeyUp
        }

        /// <summary>
        /// 按键事件回调，参数为（按下的键, 事件类型）。
        /// </summary>
        public event Action<Key, KeyboardEventType>? KeyEvent;

        /// <summary>
        /// 组合键事件回调（例如 Ctrl+F1），参数为组合键描述字符串。
        /// </summary>
        public event Action<string>? ComboKeyEvent;

        /// <summary>
        /// 当前已按下的按键集合，用于组合键检测。
        /// 使用 lock 保护以确保线程安全。
        /// </summary>
        private readonly HashSet<Key> _pressedKeys = new();

        /// <summary>
        /// 按键集合操作锁对象
        /// </summary>
        private readonly object _keysLock = new();

        /// <summary>
        /// 钩子回调委托实例（必须持有引用，防止被 GC 回收导致崩溃）
        /// </summary>
        private readonly LowLevelKeyboardProc _proc;

        /// <summary>
        /// 钩子句柄，IntPtr.Zero 表示尚未安装钩子。
        /// </summary>
        private IntPtr _hookID = IntPtr.Zero;

        /// <summary>
        /// 资源释放标志
        /// </summary>
        private bool _disposed;

        // ---- Win32 常量定义 ----
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        /// <summary>
        /// 构造函数，初始化钩子回调委托。
        /// </summary>
        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// 安装全局低级键盘钩子。
        /// </summary>
        public void SetHook()
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        }

        /// <summary>
        /// 卸载全局键盘钩子并清空已按下按键集合。
        /// </summary>
        public void Unhook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            lock (_keysLock)
            {
                _pressedKeys.Clear();
            }
        }

        /// <summary>
        /// 低级键盘钩子回调，处理按键按下与松开事件并分发通知。
        /// </summary>
        /// <param name="nCode">钩子代码，>= 0 时处理消息</param>
        /// <param name="wParam">消息类型（WM_KEYDOWN / WM_KEYUP 等）</param>
        /// <param name="lParam">指向 KBDLLHOOKSTRUCT 的指针</param>
        /// <returns>下一个钩子的返回值</returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (int)wParam;

                if (msg is WM_KEYDOWN or WM_SYSKEYDOWN)
                {
                    KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    Key key = KeyInterop.KeyFromVirtualKey((int)kb.vkCode);

                    lock (_keysLock)
                    {
                        _pressedKeys.Add(key);
                    }

                    // 触发按下事件
                    KeyEvent?.Invoke(key, KeyboardEventType.KeyDown);

                    // 检查是否构成组合键
                    DetectComboKey();
                }
                else if (msg is WM_KEYUP or WM_SYSKEYUP)
                {
                    KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    Key key = KeyInterop.KeyFromVirtualKey((int)kb.vkCode);

                    lock (_keysLock)
                    {
                        _pressedKeys.Remove(key);
                    }

                    // 触发松开事件
                    KeyEvent?.Invoke(key, KeyboardEventType.KeyUp);
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 检测当前已按下的按键是否构成有效的组合键（需同时按下 2 个及以上按键）。
        /// 组合键字符串按字母排序后用 "+" 连接，例如 "LeftCtrl+F1"。
        /// </summary>
        private void DetectComboKey()
        {
            string? combo = null;

            lock (_keysLock)
            {
                if (_pressedKeys.Count > 1)
                {
                    combo = string.Join("+", _pressedKeys.OrderBy(k => k.ToString()));
                }
            }

            if (combo != null)
            {
                ComboKeyEvent?.Invoke(combo);
            }
        }

        /// <summary>
        /// 释放资源，卸载钩子。
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Unhook();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 低级键盘钩子结构体，对应 Win32 KBDLLHOOKSTRUCT。
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            /// <summary>虚拟键码</summary>
            public uint vkCode;
            /// <summary>扫描码</summary>
            public uint scanCode;
            /// <summary>标志位</summary>
            public uint flags;
            /// <summary>时间戳</summary>
            public uint time;
            /// <summary>附加信息指针</summary>
            public IntPtr dwExtraInfo;
        }

        #region PInvoke

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>低级键盘钩子回调委托</summary>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
