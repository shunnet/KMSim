using System.Runtime.InteropServices;
using System.Windows.Input;

namespace Snet.Windows.KMSim.utility
{
    /// <summary>
    /// 全局键盘钩子类
    /// 用于监听全局键盘按键（窗口失去焦点也能监听）
    /// 支持按下、松开事件，可扩展组合键检测
    /// </summary>
    public class GlobalKeyboardHook
    {
        /// <summary>
        /// 低级键盘钩子回调委托类型
        /// SetWindowsHookEx 需要的回调函数
        /// </summary>
        /// <param name="nCode">钩子代码，>=0 表示处理消息</param>
        /// <param name="wParam">键盘消息类型，例如 WM_KEYDOWN、WM_KEYUP</param>
        /// <param name="lParam">指向 KBDLLHOOKSTRUCT 结构体的指针</param>
        /// <returns>返回 CallNextHookEx 的值</returns>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 钩子回调委托实例，必须保持引用，防止 GC 回收
        /// </summary>
        private LowLevelKeyboardProc _proc;

        /// <summary>
        /// 钩子句柄，SetWindowsHookEx 返回的值
        /// 用于卸载钩子 UnhookWindowsHookEx
        /// </summary>
        private IntPtr _hookID = IntPtr.Zero;

        /// <summary>
        /// 按键信息抛出
        /// </summary>
        public Action<Key>? KeyEvent;

        // 低级键盘钩子常量
        private const int WH_KEYBOARD_LL = 13;  // 低级键盘钩子
        private const int WM_KEYDOWN = 0x0100;  // 按键按下消息
        private const int WM_KEYUP = 0x0101;    // 按键松开消息

        /// <summary>
        /// 构造函数，初始化钩子回调委托
        /// </summary>
        public GlobalKeyboardHook()
        {
            _proc = HookCallback; // 将回调方法绑定到委托
        }

        /// <summary>
        /// 安装全局键盘钩子
        /// 调用 SetWindowsHookEx 注册钩子
        /// </summary>
        public void SetHook()
        {
            // 第三个参数 hMod 可以传 IntPtr.Zero，表示当前模块
            // 第四个参数线程ID为0表示全局钩子
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        }

        /// <summary>
        /// 卸载全局键盘钩子
        /// 调用 UnhookWindowsHookEx
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
        /// 钩子回调方法
        /// 当键盘事件发生时被调用
        /// </summary>
        /// <param name="nCode">钩子代码</param>
        /// <param name="wParam">消息类型</param>
        /// <param name="lParam">指向 KBDLLHOOKSTRUCT 的指针</param>
        /// <returns>调用下一个钩子返回值</returns>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // nCode >= 0 表示可以处理
            if (nCode >= 0)
            {
                // 只处理按下事件（WM_KEYDOWN）
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    // 将 lParam 转换为结构体，获取按键信息
                    KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    // 将虚拟键码转换为 WPF Key 枚举
                    Key key = KeyInterop.KeyFromVirtualKey((int)kb.vkCode);
                    //抛出按键信息
                    KeyEvent?.Invoke(key);
                }
            }

            // 调用下一个钩子，保持钩子链完整
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 低级键盘结构体，封装按键相关信息
        /// 对应 Windows API KBDLLHOOKSTRUCT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;     // 虚拟键码
            public uint scanCode;   // 扫描码
            public uint flags;      // 标志位（扩展键、注入事件等）
            public uint time;       // 时间戳
            public IntPtr dwExtraInfo; // 扩展信息
        }

        #region P/Invoke 导入 Windows API

        /// <summary>
        /// 安装钩子
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>
        /// 卸载钩子
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>
        /// 调用下一个钩子
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}
