using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Snet.Windows.KMSim.utility
{
    /// <summary>
    /// 全局键盘钩子类
    /// 支持按下、松开事件与组合键检测（例如 Ctrl+F1）
    /// </summary>
    public class GlobalKeyboardHook
    {
        /// <summary>
        /// 键盘事件类型
        /// </summary>
        public enum KeyboardEventType
        {
            KeyDown,
            KeyUp
        }

        /// <summary>
        /// 按键事件回调（按键，事件类型）
        /// </summary>
        public event Action<Key, KeyboardEventType>? KeyEvent;

        /// <summary>
        /// 组合键事件回调（例如 Ctrl+F1）
        /// </summary>
        public event Action<string>? ComboKeyEvent;

        /// <summary>
        /// 当前已按下的按键集合
        /// </summary>
        private readonly HashSet<Key> _pressedKeys = new();

        /// <summary>
        /// 钩子委托实例
        /// </summary>
        private readonly LowLevelKeyboardProc _proc;

        /// <summary>
        /// 钩子句柄
        /// </summary>
        private IntPtr _hookID = IntPtr.Zero;

        // 常量定义
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
        }

        /// <summary>
        /// 安装全局键盘钩子
        /// </summary>
        public void SetHook()
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        }

        /// <summary>
        /// 卸载钩子
        /// </summary>
        public void Unhook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 钩子回调
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var msg = (int)wParam;

                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    Key key = KeyInterop.KeyFromVirtualKey((int)kb.vkCode);

                    // 防止重复加入
                    if (!_pressedKeys.Contains(key))
                        _pressedKeys.Add(key);

                    // 抛出按下事件
                    KeyEvent?.Invoke(key, KeyboardEventType.KeyDown);

                    // 检查是否构成组合键
                    DetectComboKey();
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    Key key = KeyInterop.KeyFromVirtualKey((int)kb.vkCode);

                    // 移除释放的键
                    _pressedKeys.Remove(key);

                    // 抛出松开事件
                    KeyEvent?.Invoke(key, KeyboardEventType.KeyUp);
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 检测当前是否构成有效的组合键（例如 Ctrl+F1）
        /// </summary>
        private void DetectComboKey()
        {
            // 这里可以根据需要定义你关心的组合键
            // 例如 Ctrl+F1、Ctrl+Shift+S 等

            // 简单构造当前按下的组合键字符串
            if (_pressedKeys.Count > 1)
            {
                string combo = string.Join("+", _pressedKeys.OrderBy(k => k.ToString()));
                ComboKeyEvent?.Invoke(combo);
            }
        }

        /// <summary>
        /// 键盘结构体
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
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

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
