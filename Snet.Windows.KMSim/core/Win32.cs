using System.Runtime.InteropServices;
using System.Text;

namespace Snet.Windows.KMSim.core
{
    public static class Win32
    {
        /// <summary>
        /// POINT 结构体，用于存储鼠标坐标
        /// 对应 Win32 API 中的 POINT 结构
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// 鼠标在屏幕上的 X 坐标（以主显示器左上角为原点 0,0）
            /// </summary>
            public int X;

            /// <summary>
            /// 鼠标在屏幕上的 Y 坐标（以主显示器左上角为原点 0,0）
            /// </summary>
            public int Y;
        }

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

        // 特殊 Z 顺序值（用于 SetWindowPos 的 hWndInsertAfter 参数）
        /// <summary>
        /// 将窗口置于所有非顶层窗口的最前面（不一定置顶）
        /// </summary>
        public static readonly IntPtr HWND_TOP = new IntPtr(0);

        /// <summary>
        /// 将窗口置于所有窗口的最底层
        /// </summary>
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);

        /// <summary>
        /// 将窗口置于所有顶层窗口的最前面（始终置顶）
        /// </summary>
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        /// <summary>
        /// 将窗口从顶层状态移除，但保持在非顶层窗口之上
        /// </summary>
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // SWP 标志（用于 SetWindowPos 的 Flags 参数）
        /// <summary>保留窗口当前大小，不改变</summary>
        public const uint SWP_NOSIZE = 0x0001;

        /// <summary>保留窗口当前坐标位置，不移动</summary>
        public const uint SWP_NOMOVE = 0x0002;

        /// <summary>不改变窗口的 Z 顺序</summary>
        public const uint SWP_NOZORDER = 0x0004;

        /// <summary>不重绘窗口</summary>
        public const uint SWP_NOREDRAW = 0x0008;

        /// <summary>窗口被置于非激活状态</summary>
        public const uint SWP_NOACTIVATE = 0x0010;

        /// <summary>强制应用窗口的新的边框样式（如修改了 WS_BORDER）</summary>
        public const uint SWP_FRAMECHANGED = 0x0020;

        /// <summary>显示窗口（如果之前隐藏）</summary>
        public const uint SWP_SHOWWINDOW = 0x0040;

        /// <summary>隐藏窗口</summary>
        public const uint SWP_HIDEWINDOW = 0x0080;

        /// <summary>丢弃原窗口图像，不保留之前内容</summary>
        public const uint SWP_NOCOPYBITS = 0x0100;

        /// <summary>不改变所有者窗口的 Z 顺序</summary>
        public const uint SWP_NOOWNERZORDER = 0x0200;

        /// <summary>不发送 WM_WINDOWPOSCHANGING 消息</summary>
        public const uint SWP_NOSENDCHANGING = 0x0400;

        /// <summary>与 SWP_FRAMECHANGED 相同，用于兼容旧代码</summary>
        public const uint SWP_DRAWFRAME = SWP_FRAMECHANGED;

        /// <summary>与 SWP_NOOWNERZORDER 相同，用于兼容旧代码</summary>
        public const uint SWP_NOREPOSITION = SWP_NOOWNERZORDER;

        /// <summary>延迟擦除窗口背景</summary>
        public const uint SWP_DEFERERASE = 0x2000;

        /// <summary>异步窗口位置操作，不等待系统处理完再返回</summary>
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;

        /// <summary>
        /// 获取窗体的宽高
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <param name="lpRect">矩形对象</param>
        /// <returns></returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// 获取窗口宽高
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <returns>宽,高</returns>
        public static (int Width, int Height) GetWindowSize(IntPtr hWnd)
        {
            if (GetWindowRect(hWnd, out RECT rect))
            {
                int width = (int)(rect.Right - rect.Left);
                int height = (int)(rect.Bottom - rect.Top);
                return (width, height);
            }
            return (0, 0);
        }

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
        /// <param name="hWndInsertAfter">在此窗口之后插入（可控制置顶/置底）</param>
        /// <param name="X">新位置 X 坐标</param>
        /// <param name="Y">新位置 Y 坐标</param>
        /// <param name="cx">新宽度</param>
        /// <param name="cy">新高度</param>
        /// <param name="Flags">窗口位置更新标志（SWP 常量组合）</param>
        /// <returns>操作结果：true 成功，false 失败</returns>
        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint Flags);

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
        /// 调用 Windows 用户32库（user32.dll）中的 GetCursorPos 函数
        /// 用于获取鼠标在整个屏幕（桌面）的绝对坐标
        /// </summary>
        /// <param name="lpPoint">
        /// 输出参数，返回鼠标的坐标点
        /// 通过 POINT 结构体表示 X 和 Y 坐标
        /// </param>
        /// <returns>
        /// 返回 bool 类型：
        /// true  表示成功获取鼠标位置
        /// false 表示获取失败
        /// </returns>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(out POINT lpPoint);

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

        /// <summary>
        /// 枚举当前系统中所有顶级窗口（不包括子窗口），并为每个窗口调用回调函数。
        /// </summary>
        /// <param name="lpEnumFunc">
        /// 回调函数（delegate），在每个窗口句柄上被调用。
        /// 如果回调返回 true，则继续枚举下一个窗口；
        /// 如果返回 false，则停止枚举。
        /// </param>
        /// <param name="lParam">
        /// 用户自定义参数，可用于向回调函数传递上下文信息，一般填 <see cref="IntPtr.Zero"/>。
        /// </param>
        /// <returns>
        /// 若成功枚举所有窗口或中途被终止返回 true；
        /// 若发生错误返回 false（可使用 GetLastError() 获取错误代码）。
        /// </returns>
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// 枚举窗口回调函数委托定义。
        /// </summary>
        /// <param name="hWnd">当前枚举到的窗口句柄。</param>
        /// <param name="lParam">传递给 <see cref="EnumWindows"/> 的用户参数。</param>
        /// <returns>
        /// 返回 true 继续枚举下一个窗口；
        /// 返回 false 停止枚举。
        /// </returns>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// 获取指定窗口的标题文本（窗口名称）。
        /// </summary>
        /// <param name="hWnd">目标窗口句柄。</param>
        /// <param name="lpString">用于接收窗口标题文本的缓冲区（StringBuilder 对象）。</param>
        /// <param name="nMaxCount">缓冲区最大长度（单位：字符）。</param>
        /// <returns>
        /// 返回复制到缓冲区的字符数（不包括终止符）。
        /// 若窗口无标题或发生错误则返回 0。
        /// </returns>
        /// <remarks>
        /// 注意：此函数只能获取当前进程有访问权限的窗口标题；
        /// 若窗口属于其他高完整性级别的进程（如管理员进程），
        /// 可能会返回空字符串。
        /// </remarks>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        /// <summary>
        /// 判断指定句柄对应的窗口是否“可见”。
        /// </summary>
        /// <param name="hWnd">窗口句柄。</param>
        /// <returns>
        /// 如果窗口可见则返回 true；
        /// 如果窗口被隐藏或无效则返回 false。
        /// </returns>
        /// <remarks>
        /// 此函数仅判断可见状态，不判断窗口是否最小化或被其他窗口遮挡。
        /// </remarks>
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        /// <summary>
        /// 枚举指定父窗口的所有子窗口。
        /// </summary>
        /// <param name="hWnd">
        /// 父窗口句柄。  
        /// 如果传入顶层窗口句柄，则枚举该窗口下的所有子窗口。  
        /// 如果传入 IntPtr.Zero，则枚举桌面下所有顶层窗口（通常不这么用）。
        /// </param>
        /// <param name="lpEnumFunc">
        /// 回调函数，每找到一个子窗口都会调用该函数一次。  
        /// 返回 true 表示继续枚举，返回 false 表示停止枚举。  
        /// 委托定义如下：
        ///     delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        /// 其中 hWnd 为当前子窗口句柄，lParam 为用户自定义参数。
        /// </param>
        /// <param name="lParam">
        /// 用户自定义参数，会原样传入回调函数。可以用来传递状态、对象引用等。
        /// </param>
        /// <returns>
        /// 如果函数成功，则返回 true；如果失败，则返回 false。可使用 Marshal.GetLastWin32Error() 获取错误码。
        /// </returns>
        /// <remarks>
        /// - 此函数不会枚举隐藏的或不可见窗口，除非通过回调自己处理。  
        /// - 常用于获取浏览器、Electron 或 WPF 嵌套窗口的子窗口句柄。  
        /// - 回调中不要执行耗时操作，否则可能影响枚举性能。  
        /// - 调用此函数需要引用 System.Runtime.InteropServices。
        /// </remarks>
        [DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWnd, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    }
}
