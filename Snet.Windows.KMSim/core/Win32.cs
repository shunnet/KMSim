using System.Runtime.InteropServices;
using System.Text;

namespace Snet.Windows.KMSim.core
{
    public static class Win32
    {
        /// <summary>
        /// 窗口客户区矩形
        /// </summary>
        public struct RECT
        {
            /// <summary>
            /// 左边界 X 坐标
            /// </summary>
            public uint Left;

            /// <summary>
            /// 上边界 Y 坐标
            /// </summary>
            public uint Top;

            /// <summary>
            /// 右边界 X 坐标
            /// </summary>
            public uint Right;

            /// <summary>
            /// 下边界 Y 坐标
            /// </summary>
            public uint Bottom;
        }

        /// <summary>
        /// 鼠标事件常量：鼠标移动
        /// </summary>
        public const int MOUSEEVENTF_MOVE = 0x0001;

        /// <summary>
        /// 鼠标事件常量：鼠标左键按下
        /// </summary>
        public const int MOUSEEVENTF_LEFTDOWN = 0x0002;

        /// <summary>
        /// 鼠标事件常量：鼠标左键抬起
        /// </summary>
        public const int MOUSEEVENTF_LEFTUP = 0x0004;

        /// <summary>
        /// 鼠标事件常量：鼠标右键按下
        /// </summary>
        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;

        /// <summary>
        /// 鼠标事件常量：鼠标右键抬起
        /// </summary>
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;

        /// <summary>
        /// 鼠标事件常量：鼠标中键按下
        /// </summary>
        public const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;

        /// <summary>
        /// 鼠标事件常量：鼠标中键抬起
        /// </summary>
        public const int MOUSEEVENTF_MIDDLEUP = 0x0040;

        /// <summary>
        /// 鼠标事件常量：绝对坐标模式<br/>
        /// 使用 mouse_event 时 dx,dy 的范围为 0~65535 对应整个屏幕
        /// </summary>
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;

        /// <summary>
        /// 鼠标事件常量：鼠标滚轮滚动<br/>
        /// cButtons 参数表示滚动量（120 为一个滚动单位）
        /// </summary>
        public const int MOUSEEVENTF_WHEEL = 0x0800;

        /// <summary>
        /// 键盘事件常量：按键抬起<br/>
        /// 与 keybd_event 配合使用
        /// </summary>
        public const int KEYEVENTF_KEYUP = 0x0002;

        /// <summary>
        /// 键盘事件常量：扩展键标志<br/>
        /// 一般用于区分增强型键盘的右侧键，如右 Ctrl、右 Alt、方向键、Insert、Delete、Home、End、PageUp、PageDown 等<br/>
        /// 与 keybd_event 或 SendInput 配合使用
        /// </summary>
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        /// <summary>
        /// 英文句号键（.）<br/>
        /// 键码：0xBE
        /// </summary>
        public const byte VK_OEM_PERIOD = 0xBE;

        /// <summary>
        /// 加号键（+，在部分键盘为 = 键）<br/>
        /// 键码：0xBB
        /// </summary>
        public const byte VK_OEM_PLUS = 0xBB;

        /// <summary>
        /// 减号键（-）<br/>
        /// 键码：0xBD
        /// </summary>
        public const byte VK_OEM_MINUS = 0xBD;

        /// <summary>
        /// 波浪号/反引号键（`~，位于数字 1 左侧）<br/>
        /// 键码：0xC0
        /// </summary>
        public const byte VK_OEM_3 = 0xC0;

        /// <summary>
        /// 斜杠键（/）<br/>
        /// 键码：0xBF
        /// </summary>
        public const byte VK_OEM_2 = 0xBF;

        /// <summary>
        /// 逗号键（,）<br/>
        /// 键码：0xBC
        /// </summary>
        public const byte VK_OEM_COMMA = 0xBC;

        /// <summary>
        /// 分号键（;）<br/>
        /// 键码：0xBA
        /// </summary>
        public const byte VK_OEM_1 = 0xBA;

        /// <summary>
        /// 左方括号键（[）<br/>
        /// 键码：0xDB
        /// </summary>
        public const byte VK_OEM_4 = 0xDB;

        /// <summary>
        /// 反斜杠键（\）<br/>
        /// 键码：0xDC
        /// </summary>
        public const byte VK_OEM_5 = 0xDC;

        /// <summary>
        /// 右方括号键（]）<br/>
        /// 键码：0xDD
        /// </summary>
        public const byte VK_OEM_6 = 0xDD;

        /// <summary>
        /// 单引号键（'）<br/>
        /// 键码：0xDE
        /// </summary>
        public const byte VK_OEM_7 = 0xDE;

        /// <summary>
        /// 未定义/备用键（部分键盘不存在）<br/>
        /// 键码：0xDF
        /// </summary>
        public const byte VK_OEM_8 = 0xDF;

        /// <summary>
        /// 韩文/Hangul 键<br/>
        /// 键码：0x15
        /// </summary>
        public const byte VK_HANGUL = 0x15;

        /// <summary>
        /// 国际键盘（102 键布局）专用键<br/>
        /// 键码：0xE2
        /// </summary>
        public const byte VK_OEM_102 = 0xE2;


        /// <summary>
        /// 设置鼠标位置到指定屏幕坐标
        /// </summary>
        /// <param name="x">屏幕 X 坐标</param>
        /// <param name="y">屏幕 Y 坐标</param>
        /// <returns>操作结果：非零成功，零失败</returns>
        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);

        /// <summary>
        /// 设置目标窗体在屏幕上的位置与 Z 顺序
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        /// <param name="hWndlnsertAfter">在此窗口之后插入（可控制置顶/置底）</param>
        /// <param name="X">新位置 X 坐标</param>
        /// <param name="Y">新位置 Y 坐标</param>
        /// <param name="cx">新宽度</param>
        /// <param name="cy">新高度</param>
        /// <param name="Flags">窗口位置更新标志（SWP 常量组合）</param>
        /// <returns>操作结果：true 成功，false 失败</returns>
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, int hWndlnsertAfter, int X, int Y, int cx, int cy, uint Flags);

        /// <summary>
        /// 模拟鼠标事件
        /// </summary>
        /// <param name="dwFlags">事件类型（MOUSEEVENTF_* 常量）</param>
        /// <param name="dx">X 坐标或移动增量</param>
        /// <param name="dy">Y 坐标或移动增量</param>
        /// <param name="cButtons">鼠标按钮数据（滚轮滚动量等）</param>
        /// <param name="dwExtraInfo">附加信息</param>
        /// <returns>操作结果：非零成功，零失败</returns>
        [DllImport("user32.dll")]
        public static extern int mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        /// <summary>
        /// 模拟键盘事件
        /// </summary>
        /// <param name="bVk">虚拟键码</param>
        /// <param name="bScan">硬件扫描码</param>
        /// <param name="dwFlags">键盘事件标志（0 按下，KEYEVENTF_KEYUP 抬起）</param>
        /// <param name="dwExtraInfo">附加信息</param>
        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        /// <summary>
        /// 获取窗口标题文本
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="lpString">存放标题的缓冲区</param>
        /// <param name="nMaxCount">缓冲区最大字符数</param>
        /// <returns>返回拷贝到缓冲区的字符数量</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hwnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// 根据屏幕坐标获取窗口句柄
        /// </summary>
        /// <param name="p">屏幕坐标点</param>
        /// <returns>窗口句柄</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        /// <summary>
        /// 获取窗口类名
        /// </summary>
        /// <param name="hwnd">窗口句柄</param>
        /// <param name="s">存放类名的缓冲区</param>
        /// <param name="nMaxCount">缓冲区大小</param>
        [DllImport("user32.dll")]
        public static extern void GetClassName(IntPtr hwnd, StringBuilder s, int nMaxCount);



        /// <summary>
        /// 获取窗口客户区矩形（不含边框/标题栏）
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="lpRect">输出 RECT 结构体</param>
        /// <returns>true 成功，false 失败</returns>
        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 修改目标窗口的位置与大小
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="x">新的 X 坐标</param>
        /// <param name="y">新的 Y 坐标</param>
        /// <param name="nWidth">新宽度</param>
        /// <param name="nHeight">新高度</param>
        /// <param name="BRePaint">是否立即重绘</param>
        /// <returns>非零成功，零失败</returns>
        [DllImport("user32.dll")]
        public static extern int MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool BRePaint);

        /// <summary>
        /// 获取按键状态
        /// </summary>
        /// <param name="nVirtKey">虚拟键码</param>
        /// <returns>
        /// 高位表示当前按键状态（1 = 按下，0 = 未按下）<br/>
        /// 低位表示切换状态（CapsLock/NumLock/ScrollLock）<br/>
        /// 1 = 开启，0 = 关闭
        /// </returns>
        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

    }
}
