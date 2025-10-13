using Snet.Windows.KMSim.data;
using Snet.Windows.KMSim.@enum;
using System.Text;
using System.Windows.Input;

namespace Snet.Windows.KMSim.core
{
    /// <summary>
    /// 键鼠模拟核心
    /// </summary>
    public class KMSimCore
    {
        /// <summary>
        /// 异步拷贝内容到剪贴板
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="token">取消任务的 Token</param>
        /// <returns>任务对象</returns>
        public async Task CopyContentAsync(string content, CancellationToken token = default)
        {
            if (content == null)
                return;
            await Task.Run(() =>
            {
                System.Windows.Clipboard.SetDataObject(content);
            }, token);
        }

        /// <summary>
        /// 异步延迟执行任务（支持取消操作）
        /// </summary>
        /// <param name="ms">
        /// 延迟的时间，单位为毫秒（milliseconds）。<br/>
        /// 例如：传入 <c>1000</c> 表示延迟 1 秒再继续执行后续代码。
        /// </param>
        /// <param name="token">
        /// 用于取消当前延迟的 <see cref="CancellationToken"/>。<br/>
        /// 当该 Token 被触发时，延迟任务会提前中断并抛出 <see cref="TaskCanceledException"/> 异常。<br/>
        /// 可选参数，默认值为 <see cref="CancellationToken.None"/>（即不支持取消）。  
        /// </param>
        /// <returns>
        /// 一个可等待的 <see cref="Task"/> 对象。<br/>
        /// 调用方可使用 <c>await</c> 等待延迟完成，或通过 <paramref name="token"/> 提前取消等待。  
        /// </returns>
        /// <remarks>
        /// ✅ **功能说明：**  
        /// 本方法是对 <see cref="Task.Delay(int, CancellationToken)"/> 的轻量封装，  
        /// 主要用于在异步流程中插入非阻塞延时逻辑。  
        ///  
        /// ⚙️ **特点：**
        /// <list type="bullet">
        /// <item>不会阻塞当前线程（区别于 <c>Thread.Sleep</c>）。</item>
        /// <item>支持取消操作，适合在循环或任务调度中安全退出。</item>
        /// <item>内部使用 <see cref="Task.Delay"/>，线程安全且高性能。</item>
        /// </list>
        /// 
        /// ⚠️ **注意事项：**
        /// <list type="bullet">
        /// <item>若传入的 <paramref name="ms"/> 小于 0，将抛出 <see cref="ArgumentOutOfRangeException"/>。</item>
        /// <item>若 <paramref name="token"/> 在延迟过程中被触发，任务会提前结束并抛出取消异常。</item>
        /// <item>若在 UI 线程中调用，请使用 <c>await</c> 避免界面卡顿。</item>
        /// </list>
        /// </remarks>
        public async Task DelayAsync(int ms, CancellationToken token = default)
            => await Task.Delay(ms, token);


        /// <summary>
        /// 异步模糊查找窗口句柄（根据窗口标题匹配）
        /// </summary>
        /// <param name="name">
        /// 窗口标题关键字（支持模糊匹配，不区分大小写）
        /// </param>
        /// <param name="token">
        /// 用于取消任务的 <see cref="CancellationToken"/>，
        /// 在长时间查找时可提前终止任务。
        /// </param>
        /// <returns>
        /// 若找到匹配窗口则返回窗口句柄（<see cref="nint"/> 类型）；
        /// 未找到则返回 0（即 <see cref="IntPtr.Zero"/>）。
        /// </returns>
        /// <remarks>
        /// 本方法通过调用 Win32 API <see cref="Win32.EnumWindows"/> 枚举系统中所有顶级窗口，
        /// 并使用 <see cref="Win32.GetWindowText"/> 获取窗口标题进行模糊匹配。<br/>
        /// 当匹配到第一个符合条件的窗口后即立即停止枚举，以提升性能。<br/><br/>
        /// 注意：
        /// <list type="bullet">
        /// <item>仅能枚举当前会话下的可见顶级窗口。</item>
        /// <item>若目标窗口标题为空或属于更高完整性级别进程（如管理员窗口），可能无法获取。</item>
        /// <item>搜索过程中可通过 <paramref name="token"/> 取消以节约资源。</item>
        /// </list>
        /// </remarks>
        public async Task<nint> GetWindowsHandleAsync(string name, CancellationToken token = default)
        {
            // 参数检查：防止传入空字符串造成无效枚举
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            return await Task.Run(() =>
            {
                nint found = 0; // 匹配到的句柄
                bool stop = false; // 控制是否提前结束枚举

                // 枚举系统所有顶级窗口
                Win32.EnumWindows((hWnd, lParam) =>
                {
                    // 若任务被取消，则立即停止枚举
                    if (token.IsCancellationRequested)
                    {
                        stop = true;
                        return false;
                    }

                    // 跳过不可见窗口
                    if (!Win32.IsWindowVisible(hWnd))
                        return true;

                    // 优化性能：使用 StringBuilder 复用（分配开销较小）
                    Span<char> buffer = stackalloc char[256];
                    StringBuilder sb = new(256);
                    Win32.GetWindowText(hWnd, sb, sb.Capacity);

                    string title = sb.ToString();
                    if (string.IsNullOrEmpty(title))
                        return true;

                    // 模糊匹配窗口标题（忽略大小写）
                    if (title.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        found = hWnd;
                        return false; // 停止枚举
                    }

                    return true; // 继续下一个窗口
                }, IntPtr.Zero);

                // 如果任务中途被取消则直接抛出异常（符合 Task 模式）
                token.ThrowIfCancellationRequested();

                return found;
            }, token);
        }


        /// <summary>
        /// 异步设置窗口位置/大小/显示状态（支持置顶/非置顶/隐藏等操作）
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="hWndInsertAfter">
        /// 窗口在 Z 序中的位置：<br/>
        /// -1 = HWND_TOPMOST（始终置顶）<br/>
        ///  0 = HWND_NOTOPMOST（取消置顶）<br/>
        ///  1 = HWND_TOP（置于最前）<br/>
        ///  2 = HWND_BOTTOM（置于最后）<br/>
        /// </param>
        /// <param name="x">新位置的 X 坐标（相对屏幕左上角），默认 0</param>
        /// <param name="y">新位置的 Y 坐标（相对屏幕左上角），默认 0</param>
        /// <param name="cx">窗口新的宽度（默认 0，不改变）</param>
        /// <param name="cy">窗口新的高度（默认 0，不改变）</param>
        /// <param name="uFlags">
        /// 窗口显示选项（可以位或组合），常用值：<br/>
        /// 0x0001 SWP_NOSIZE —— 忽略 cx、cy，不改变大小<br/>
        /// 0x0002 SWP_NOMOVE —— 忽略 x、y，不改变位置<br/>
        /// 0x0004 SWP_NOZORDER —— 忽略 hWndInsertAfter，不改变 Z 序<br/>
        /// 0x0010 SWP_NOACTIVATE —— 不激活窗口<br/>
        /// 0x0040 SWP_SHOWWINDOW —— 显示窗口<br/>
        /// 0x0080 SWP_HIDEWINDOW —— 隐藏窗口
        /// </param>
        /// <param name="token">取消任务的 Token</param>
        /// <returns>任务对象</returns>
        public async Task WindowsSetPosAsync(IntPtr hWnd, int hWndInsertAfter = -1, int x = 0, int y = 0, int cx = 0, int cy = 0, uint uFlags = 0x0001 | 0x0002, CancellationToken token = default)
            => await Task.Run(() => Win32.SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags), token);

        /// <summary>
        /// 移动窗口
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="token">取消通知</param>
        public async Task MoveWindowsAsync(IntPtr hWnd, int x, int y, int width, int height, CancellationToken token = default)
            => await Task.Run(() => Win32.MoveWindow(hWnd, x, y, width, height, true));

        #region 鼠标操作

        /// <summary>
        /// 移动鼠标
        /// </summary>
        public async Task MouseMoveAsync(int x, int y, CancellationToken token = default)
            => await Task.Run(() => Win32.SetCursorPos(x, y), token);

        /// <summary>
        /// 鼠标左键点击（按下 + 延迟 + 松开）
        /// </summary>
        public async Task MouseClickLeftAsync(CancellationToken token = default)
        {
            await MouseLeftDownAsync(token);
            await Task.Delay(ConfigModel.RestTime, token);
            await MouseLeftUpAsync(token);
        }

        /// <summary>
        /// 鼠标右键点击（按下 + 延迟 + 松开）
        /// </summary>
        public async Task MouseClickRightAsync(CancellationToken token = default)
        {
            await MouseRightDownAsync(token);
            await Task.Delay(ConfigModel.RestTime, token);
            await MouseRightUpAsync(token);
        }

        /// <summary>
        /// 鼠标中键点击（按下 + 延迟 + 松开）
        /// </summary>
        public async Task MouseClickMiddleAsync(CancellationToken token = default)
        {
            await MouseMiddleDownAsync(token);
            await Task.Delay(ConfigModel.RestTime, token);
            await MouseMiddleUpAsync(token);
        }

        /// <summary>
        /// 实现滚轮 向上滚动
        /// </summary>
        public async Task MouseRollerUpAsync(CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                Win32.mouse_event(Win32.MOUSEEVENTF_WHEEL, 0, 0, 100, 0);
                await Task.Delay(ConfigModel.RestTime * 20, token);
            }, token);
        }

        /// <summary>
        /// 实现滚轮 向下滚动
        /// </summary>
        public async Task MouseRollerDownAsync(CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                Win32.mouse_event(Win32.MOUSEEVENTF_WHEEL, 0, 0, -100, 0);
                await Task.Delay(ConfigModel.RestTime * 20, token);
            }, token);
        }

        /// <summary>
        /// 鼠标左键按下
        /// </summary>
        public async Task MouseLeftDownAsync(CancellationToken token = default)
        {
            await Task.Run(() => Win32.mouse_event(Win32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0), token);
        }

        /// <summary>
        /// 鼠标左键松开
        /// </summary>
        public async Task MouseLeftUpAsync(CancellationToken token = default)
        {
            await Task.Run(() => Win32.mouse_event(Win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0), token);
        }

        /// <summary>
        /// 鼠标右键按下
        /// </summary>
        public async Task MouseRightDownAsync(CancellationToken token = default)
        {
            await Task.Run(() => Win32.mouse_event(Win32.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0), token);
        }

        /// <summary>
        /// 鼠标右键松开
        /// </summary>
        public async Task MouseRightUpAsync(CancellationToken token = default)
        {
            await Task.Run(() => Win32.mouse_event(Win32.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0), token);
        }

        /// <summary>
        /// 鼠标中键按下
        /// </summary>
        public async Task MouseMiddleDownAsync(CancellationToken token = default)
        {
            await Task.Run(() => Win32.mouse_event(Win32.MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0), token);
        }

        /// <summary>
        /// 鼠标中键松开
        /// </summary>
        public async Task MouseMiddleUpAsync(CancellationToken token = default)
        {
            await Task.Run(() => Win32.mouse_event(Win32.MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0), token);
        }
        #endregion

        #region 键盘操作

        /// <summary>
        /// 切换大小写锁定状态（CapsLock）
        /// </summary>
        /// <param name="state">目标状态：true 表示开启大写锁定，false 表示关闭大写锁定</param>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 会先检查当前 CapsLock 键状态，如果与目标状态一致，则不操作；
        /// 2. 如果状态不一致，则模拟按下 CapsLock 键，再延迟一段时间后松开；
        /// 3. 延迟时间通过 ConfigModel.RestTime 配置，以保证系统能够正确识别按键事件；
        /// 4. 适用于需要在自动化操作中保证大小写输入正确的场景。
        /// </remarks>
        public async Task ToggleCase(bool state, CancellationToken token = default)
        {
            if (state != await GetKeyStateAsync(Key.CapsLock, token))
            {
                await KeyboardPressKeyAsync(Key.CapsLock); // 按下 CapsLock
                await Task.Delay(ConfigModel.RestTime, token); // 等待系统响应
                await KeyboardLoosenKeyAsync(Key.CapsLock); // 松开 CapsLock
            }
        }

        /// <summary>
        /// 执行复制操作（Ctrl + C）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 C 键，实现标准复制操作；
        /// 2. 适用于选中文本或文件后自动复制到剪贴板的场景；
        /// 3. 会调用 KeyboardPressAsync 方法处理按下、松开事件，确保操作稳定。
        /// </remarks>
        public async Task CopyAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LeftCtrl, Key.C, token);

        /// <summary>
        /// 执行保存操作（Ctrl + S）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 S 键，实现保存当前文档或文件操作；
        /// 2. 适用于自动化保存功能，例如定时保存文档或编辑内容；
        /// 3. 使用 KeyboardPressAsync 方法保证按键顺序和时长正确。
        /// </remarks>
        public async Task SaveAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LeftCtrl, Key.S, token);

        /// <summary>
        /// 执行粘贴操作（Ctrl + V）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 V 键，将剪贴板内容粘贴到当前光标位置；
        /// 2. 适用于自动化文本输入、文件路径粘贴等场景；
        /// 3. KeyboardPressAsync 方法会自动处理按下、松开逻辑，确保粘贴成功。
        /// </remarks>
        public async Task PasteAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LeftCtrl, Key.V, token);

        /// <summary>
        /// 执行全选操作（Ctrl + A）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 A 键，选中当前窗口或文本框中的全部内容；
        /// 2. 适用于自动化文本编辑、批量操作或数据处理场景；
        /// 3. KeyboardPressAsync 方法会自动处理按下、松开事件，确保全选动作生效。
        /// </remarks>
        public async Task SelectAllAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LeftCtrl, Key.A, token);

        #region 键盘其余按键
        /// <summary>
        /// 按下并松开回车<br/>
        /// 该方法会模拟按下并松开回车键，通常用于触发提交或确认操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EnterAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Enter, token);

        /// <summary>
        /// 按下回车<br/>
        /// 该方法会模拟按下回车键，通常用于触发提交或确认操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EnterPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Enter, token);

        /// <summary>
        /// 松开回车<br/>
        /// 该方法会模拟松开回车键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EnterLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Enter, token);

        /// <summary>
        /// 按下并松开Tab<br/>
        /// 该方法会模拟按下并松开Tab键，通常用于在控件之间切换焦点。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task TabAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Tab, token);

        /// <summary>
        /// 按下Tab<br/>
        /// 该方法会模拟按下Tab键，通常用于在控件之间切换焦点。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task TabPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Tab, token);

        /// <summary>
        /// 松开Tab<br/>
        /// 该方法会模拟松开Tab键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task TabLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Tab, token);

        /// <summary>
        /// 按下并松开CapsLock<br/>
        /// 该方法会模拟按下并松开CapsLock键，通常用于开启或关闭大写字母锁定。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task CapsLockAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.CapsLock, token);

        /// <summary>
        /// 按下CapsLock<br/>
        /// 该方法会模拟按下CapsLock键，通常用于开启或关闭大写字母锁定。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task CapsLockPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.CapsLock, token);

        /// <summary>
        /// 松开CapsLock<br/>
        /// 该方法会模拟松开CapsLock键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task CapsLockLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.CapsLock, token);

        /// <summary>
        /// 按下并松开回退键<br/>
        /// 该方法会模拟按下并松开回退键，通常用于删除光标前的一个字符。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task BackAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Back, token);

        /// <summary>
        /// 按下回退键<br/>
        /// 该方法会模拟按下回退键，通常用于删除光标前的一个字符。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task BackPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Back, token);

        /// <summary>
        /// 松开回退键<br/>
        /// 该方法会模拟松开回退键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task BackLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Back, token);

        /// <summary>
        /// 按下并松开空格<br/>
        /// 该方法会模拟按下并松开空格键，通常用于在输入框中插入空格。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task SpaceAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Space, token);

        /// <summary>
        /// 按下空格<br/>
        /// 该方法会模拟按下空格键，通常用于在输入框中插入空格。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task SpacePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Space, token);

        /// <summary>
        /// 松开空格<br/>
        /// 该方法会模拟松开空格键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task SpaceLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Space, token);

        /// <summary>
        /// 按下并松开左Ctrl<br/>
        /// 该方法会模拟按下并松开左侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LeftCtrlAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LeftCtrl, token);

        /// <summary>
        /// 按下左Ctrl<br/>
        /// 该方法会模拟按下左侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LeftCtrlPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.LeftCtrl, token);

        /// <summary>
        /// 松开左Ctrl<br/>
        /// 该方法会模拟松开左侧Ctrl键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LeftCtrlLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.LeftCtrl, token);

        /// <summary>
        /// 按下并松开右Ctrl<br/>
        /// 该方法会模拟按下并松开右侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RightCtrlAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.RightCtrl, token);

        /// <summary>
        /// 按下右Ctrl<br/>
        /// 该方法会模拟按下右侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RightCtrlPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.RightCtrl, token);

        /// <summary>
        /// 松开右Ctrl<br/>
        /// 该方法会模拟松开右侧Ctrl键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RightCtrlLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.RightCtrl, token);

        /// <summary>
        /// 按下并松开左Win<br/>
        /// 该方法会模拟按下并松开左侧Win键，通常用于打开开始菜单或触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LWinAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LWin, token);

        /// <summary>
        /// 按下左Win<br/>
        /// 该方法会模拟按下左侧Win键，通常用于打开开始菜单或触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LWinPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.LWin, token);

        /// <summary>
        /// 松开左Win<br/>
        /// 该方法会模拟松开左侧Win键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LWinLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.LWin, token);

        /// <summary>
        /// 按下并松开右Win<br/>
        /// 该方法会模拟按下并松开右侧Win键，通常用于触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RWinAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.RWin, token);

        /// <summary>
        /// 按下右Win<br/>
        /// 该方法会模拟按下右侧Win键，通常用于触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RWinPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.RWin, token);

        /// <summary>
        /// 松开右Win<br/>
        /// 该方法会模拟松开右侧Win键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RWinLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.RWin, token);

        /// <summary>
        /// 按下并松开左Alt<br/>
        /// 该方法会模拟按下并松开左侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LeftAltAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.LeftAlt, token);

        /// <summary>
        /// 按下左Alt<br/>
        /// 该方法会模拟按下左侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LeftAltPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.LeftAlt, token);

        /// <summary>
        /// 松开左Alt<br/>
        /// 该方法会模拟松开左侧Alt键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task LeftAltLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.LeftAlt, token);

        /// <summary>
        /// 按下并松开右Alt<br/>
        /// 该方法会模拟按下并松开右侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RightAltAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.RightAlt, token);

        /// <summary>
        /// 按下右Alt<br/>
        /// 该方法会模拟按下右侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RightAltPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.RightAlt, token);

        /// <summary>
        /// 松开右Alt<br/>
        /// 该方法会模拟松开右侧Alt键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task RightAltLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.RightAlt, token);

        /// <summary>
        /// 按下并松开Delete<br/>
        /// 该方法会模拟按下并松开Delete键，通常用于删除光标后的字符或选中内容。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task DeleteAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Delete, token);

        /// <summary>
        /// 按下Delete<br/>
        /// 该方法会模拟按下Delete键，通常用于删除光标后的字符或选中内容。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task DeletePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Delete, token);

        /// <summary>
        /// 松开Delete<br/>
        /// 该方法会模拟松开Delete键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task DeleteLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Delete, token);

        /// <summary>
        /// 按下并松开Apps（菜单键）<br/>
        /// 该方法会模拟按下并松开Apps键，通常用于打开上下文菜单。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task AppsAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Apps, token);

        /// <summary>
        /// 按下Apps（菜单键）<br/>
        /// 该方法会模拟按下Apps键，通常用于打开上下文菜单。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task AppsPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Apps, token);

        /// <summary>
        /// 松开Apps（菜单键）<br/>
        /// 该方法会模拟松开Apps键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task AppsLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Apps, token);

        /// <summary>
        /// 按下并松开PageUp<br/>
        /// 该方法会模拟按下并松开PageUp键，通常用于向上翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PageUpAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.PageUp, token);

        /// <summary>
        /// 按下PageUp<br/>
        /// 该方法会模拟按下PageUp键，通常用于向上翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PageUpPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.PageUp, token);

        /// <summary>
        /// 松开PageUp<br/>
        /// 该方法会模拟松开PageUp键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PageUpLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.PageUp, token);

        /// <summary>
        /// 按下并松开PageDown<br/>
        /// 该方法会模拟按下并松开PageDown键，通常用于向下翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PageDownAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.PageDown, token);

        /// <summary>
        /// 按下PageDown<br/>
        /// 该方法会模拟按下PageDown键，通常用于向下翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PageDownPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.PageDown, token);

        /// <summary>
        /// 松开PageDown<br/>
        /// 该方法会模拟松开PageDown键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PageDownLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.PageDown, token);

        /// <summary>
        /// 按下并松开Insert<br/>
        /// 该方法会模拟按下并松开Insert键，通常用于切换插入/覆盖模式。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task InsertAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Insert, token);

        /// <summary>
        /// 按下Insert<br/>
        /// 该方法会模拟按下Insert键，通常用于切换插入/覆盖模式。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task InsertPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Insert, token);

        /// <summary>
        /// 松开Insert<br/>
        /// 该方法会模拟松开Insert键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task InsertLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Insert, token);

        /// <summary>
        /// 按下并松开Home<br/>
        /// 该方法会模拟按下并松开Home键，通常用于将光标移动到行首或页面顶部。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task HomeAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Home, token);

        /// <summary>
        /// 按下Home<br/>
        /// 该方法会模拟按下Home键，通常用于将光标移动到行首或页面顶部。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task HomePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Home, token);

        /// <summary>
        /// 松开Home<br/>
        /// 该方法会模拟松开Home键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task HomeLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Home, token);

        /// <summary>
        /// 按下并松开End<br/>
        /// 该方法会模拟按下并松开End键，通常用于将光标移动到行尾或页面底部。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EndAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.End, token);

        /// <summary>
        /// 按下End<br/>
        /// 该方法会模拟按下End键，通常用于将光标移动到行尾或页面底部。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EndPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.End, token);

        /// <summary>
        /// 松开End<br/>
        /// 该方法会模拟松开End键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EndLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.End, token);

        /// <summary>
        /// 按下并松开小键盘Enter<br/>
        /// 该方法会模拟按下并松开小键盘Enter键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumEnterAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Enter, token);

        /// <summary>
        /// 按下小键盘Enter<br/>
        /// 该方法会模拟按下小键盘Enter键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumEnterPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Enter, token);

        /// <summary>
        /// 松开小键盘Enter<br/>
        /// 该方法会模拟松开小键盘Enter键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumEnterLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Enter, token);

        /// <summary>
        /// 按下并松开Pause/Break<br/>
        /// 该方法会模拟按下并松开Pause/Break键，通常用于暂停程序或调试。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PauseAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Pause, token);

        /// <summary>
        /// 按下Pause/Break<br/>
        /// 该方法会模拟按下Pause/Break键，通常用于暂停程序或调试。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PausePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Pause, token);

        /// <summary>
        /// 松开Pause/Break<br/>
        /// 该方法会模拟松开Pause/Break键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PauseLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Pause, token);

        /// <summary>
        /// 按下并松开PrintScreen<br/>
        /// 该方法会模拟按下并松开PrintScreen键，通常用于截屏操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PrintScreenAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.PrintScreen, token);

        /// <summary>
        /// 按下PrintScreen<br/>
        /// 该方法会模拟按下PrintScreen键，通常用于截屏操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PrintScreenPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.PrintScreen, token);

        /// <summary>
        /// 松开PrintScreen<br/>
        /// 该方法会模拟松开PrintScreen键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task PrintScreenLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.PrintScreen, token);

        /// <summary>
        /// 按下并松开ScrollLock<br/>
        /// 该方法会模拟按下并松开ScrollLock键，通常用于滚动锁定操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task ScrollLockAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Scroll, token);

        /// <summary>
        /// 按下ScrollLock<br/>
        /// 该方法会模拟按下ScrollLock键，通常用于滚动锁定操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task ScrollLockPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Scroll, token);

        /// <summary>
        /// 松开ScrollLock<br/>
        /// 该方法会模拟松开ScrollLock键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task ScrollLockLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Scroll, token);

        /// <summary>
        /// 按下并松开NumLock<br/>
        /// 该方法会模拟按下并松开NumLock键，通常用于切换小键盘输入模式。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumLockAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumLock, token);

        /// <summary>
        /// 按下NumLock<br/>
        /// 该方法会模拟按下NumLock键，通常用于切换小键盘输入模式。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumLockPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumLock, token);

        /// <summary>
        /// 松开NumLock<br/>
        /// 该方法会模拟松开NumLock键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumLockLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumLock, token);

        /// <summary>
        /// 按下并松开Escape<br/>
        /// 该方法会模拟按下并松开Escape键，通常用于取消操作或退出界面。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EscapeAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Escape, token);

        /// <summary>
        /// 按下Escape<br/>
        /// 该方法会模拟按下Escape键，通常用于取消操作或退出界面。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EscapePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Escape, token);

        /// <summary>
        /// 松开Escape<br/>
        /// 该方法会模拟松开Escape键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task EscapeLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Escape, token);


        #endregion

        #region 方向
        /// <summary>
        /// 模拟按下并松开 ↑ 键<br/>
        /// 该方法会模拟按下并松开 ↑ 键，通常用于模拟按键动作，例如在键盘事件或自动化测试中使用。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。调用者可以通过传递 CancellationToken 来取消该任务。</param>
        /// <returns>表示异步操作的任务。该任务在按键操作完成后返回。</returns>
        public async Task UpAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Up, token);

        /// <summary>
        /// 模拟按下 ↑ 键<br/>
        /// 该方法会模拟按下 ↑ 键，但不会松开它，通常用于需要持续按住某个键的场景。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。可以在操作中途取消按键事件。</param>
        /// <returns>表示异步操作的任务。该任务在按键按下操作完成后返回。</returns>
        public async Task UpPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Up, token);

        /// <summary>
        /// 模拟松开 ↑ 键<br/>
        /// 该方法会模拟松开 ↑ 键，通常用于在按键操作后释放键盘按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。可以在操作中途取消松开操作。</param>
        /// <returns>表示异步操作的任务。该任务在松开操作完成后返回。</returns>
        public async Task UpLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Up, token);

        /// <summary>
        /// 模拟按下并松开 ↓ 键<br/>
        /// 该方法会模拟按下并松开 ↓ 键，通常用于模拟按键动作，类似于按键事件的触发。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。如果需要取消操作，可以传递一个 CancellationToken。</param>
        /// <returns>表示异步操作的任务。任务会在按键动作完成后返回。</returns>
        public async Task DownAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Down, token);

        /// <summary>
        /// 模拟按下 ↓ 键<br/>
        /// 该方法会模拟按下 ↓ 键并保持按下状态，通常用于需要持续按下某个键的场景。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。可以在按键按下时通过取消令牌中断操作。</param>
        /// <returns>表示异步操作的任务。任务会在按下操作完成后返回。</returns>
        public async Task DownPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Down, token);

        /// <summary>
        /// 模拟松开 ↓ 键<br/>
        /// 该方法会模拟松开 ↓ 键，通常用于按键释放后事件的触发。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。通过传递取消令牌，调用者可以取消该操作。</param>
        /// <returns>表示异步操作的任务。任务会在松开操作完成后返回。</returns>
        public async Task DownLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Down, token);

        /// <summary>
        /// 模拟按下并松开 ← 键<br/>
        /// 该方法会模拟按下并松开 ← 键，用于模拟标准的键盘按下并松开事件。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。可以通过传递取消令牌来取消该任务。</param>
        /// <returns>表示异步操作的任务。任务在键盘按下并松开后完成。</returns>
        public async Task LeftAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Left, token);

        /// <summary>
        /// 模拟按下 ← 键<br/>
        /// 该方法会模拟按下 ← 键，并保持按下状态，适用于需要长时间按住某个键的操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。通过传递取消令牌，可以中断操作。</param>
        /// <returns>表示异步操作的任务。该任务在按键按下操作完成后返回。</returns>
        public async Task LeftPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Left, token);

        /// <summary>
        /// 模拟松开 ← 键<br/>
        /// 该方法会模拟松开 ← 键，适用于释放键盘按键的操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。通过传递取消令牌，调用者可以取消该任务。</param>
        /// <returns>表示异步操作的任务。任务会在松开操作完成后返回。</returns>
        public async Task LeftLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Left, token);

        /// <summary>
        /// 模拟按下并松开 → 键<br/>
        /// 该方法会模拟按下并松开 → 键，通常用于触发键盘按下松开事件。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。调用者可以通过传递 CancellationToken 来取消任务。</param>
        /// <returns>表示异步操作的任务。任务会在键盘按下并松开后完成。</returns>
        public async Task RightAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Right, token);

        /// <summary>
        /// 模拟按下 → 键<br/>
        /// 该方法会模拟按下 → 键并保持按下状态，适用于长时间按住某个键的操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。调用者可以传递取消令牌来中断操作。</param>
        /// <returns>表示异步操作的任务。任务会在按下操作完成后返回。</returns>
        public async Task RightPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Right, token);

        /// <summary>
        /// 模拟松开 → 键<br/>
        /// 该方法会模拟松开 → 键，通常用于按键操作的结束或释放事件。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。可以在任务执行过程中通过传递取消令牌来终止操作。</param>
        /// <returns>表示异步操作的任务。任务会在松开操作完成后返回。</returns>
        public async Task RightLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Right, token);

        #endregion

        #region F1~F12
        /// <summary>
        /// 模拟按下并松开 F1 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F1Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F1, token);

        /// <summary>
        /// 模拟按下 F1 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F1PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F1, token);

        /// <summary>
        /// 模拟松开 F1 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F1LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F1, token);

        /// <summary>
        /// 模拟按下并松开 F2 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F2Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F2, token);

        /// <summary>
        /// 模拟按下 F2 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F2PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F2, token);

        /// <summary>
        /// 模拟松开 F2 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F2LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F2, token);

        /// <summary>
        /// 模拟按下并松开 F3 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F3Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F3, token);

        /// <summary>
        /// 模拟按下 F3 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F3PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F3, token);

        /// <summary>
        /// 模拟松开 F3 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F3LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F3, token);

        /// <summary>
        /// 模拟按下并松开 F4 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F4Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F4, token);

        /// <summary>
        /// 模拟按下 F4 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F4PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F4, token);

        /// <summary>
        /// 模拟松开 F4 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F4LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F4, token);

        /// <summary>
        /// 模拟按下并松开 F5 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F5Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F5, token);

        /// <summary>
        /// 模拟按下 F5 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F5PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F5, token);

        /// <summary>
        /// 模拟松开 F5 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F5LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F5, token);

        /// <summary>
        /// 模拟按下并松开 F6 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F6Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F6, token);

        /// <summary>
        /// 模拟按下 F6 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F6PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F6, token);

        /// <summary>
        /// 模拟松开 F6 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F6LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F6, token);

        /// <summary>
        /// 模拟按下并松开 F7 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F7Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F7, token);

        /// <summary>
        /// 模拟按下 F7 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F7PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F7, token);

        /// <summary>
        /// 模拟松开 F7 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F7LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F7, token);

        /// <summary>
        /// 模拟按下并松开 F8 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F8Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F8, token);

        /// <summary>
        /// 模拟按下 F8 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F8PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F8, token);

        /// <summary>
        /// 模拟松开 F8 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F8LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F8, token);

        /// <summary>
        /// 模拟按下并松开 F9 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F9Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F9, token);

        /// <summary>
        /// 模拟按下 F9 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F9PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F9, token);

        /// <summary>
        /// 模拟松开 F9 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F9LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F9, token);

        /// <summary>
        /// 模拟按下并松开 F10 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F10Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F10, token);

        /// <summary>
        /// 模拟按下 F10 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F10PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F10, token);

        /// <summary>
        /// 模拟松开 F10 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F10LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F10, token);

        /// <summary>
        /// 模拟按下并松开 F11 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F11Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F11, token);

        /// <summary>
        /// 模拟按下 F11 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F11PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F11, token);

        /// <summary>
        /// 模拟松开 F11 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F11LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F11, token);

        /// <summary>
        /// 模拟按下并松开 F12 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F12Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F12, token);

        /// <summary>
        /// 模拟按下 F12 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F12PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F12, token);

        /// <summary>
        /// 模拟松开 F12 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task F12LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F12, token);
        #endregion

        #region 数字

        /// <summary>
        /// 模拟按下并松开小键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad0Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad0, token);

        /// <summary>
        /// 模拟按下小键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad0PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad0, token);

        /// <summary>
        /// 模拟松开小键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad0LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad0, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad1Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad1, token);

        /// <summary>
        /// 模拟按下小键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad1PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad1, token);

        /// <summary>
        /// 模拟松开小键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad1LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad1, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad2Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad2, token);

        /// <summary>
        /// 模拟按下小键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad2PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad2, token);

        /// <summary>
        /// 模拟松开小键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad2LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad2, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad3Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad3, token);

        /// <summary>
        /// 模拟按下小键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad3PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad3, token);

        /// <summary>
        /// 模拟松开小键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad3LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad3, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad4Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad4, token);

        /// <summary>
        /// 模拟按下小键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad4PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad4, token);

        /// <summary>
        /// 模拟松开小键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad4LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad4, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad5Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad5, token);

        /// <summary>
        /// 模拟按下小键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad5PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad5, token);

        /// <summary>
        /// 模拟松开小键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad5LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad5, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad6Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad6, token);

        /// <summary>
        /// 模拟按下小键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad6PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad6, token);

        /// <summary>
        /// 模拟松开小键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad6LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad6, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad7Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad7, token);

        /// <summary>
        /// 模拟按下小键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad7PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad7, token);

        /// <summary>
        /// 模拟松开小键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad7LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad7, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad8Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad8, token);

        /// <summary>
        /// 模拟按下小键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad8PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad8, token);

        /// <summary>
        /// 模拟松开小键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad8LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad8, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad9Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.NumPad9, token);

        /// <summary>
        /// 模拟按下小键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad9PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.NumPad9, token);

        /// <summary>
        /// 模拟松开小键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NumPad9LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.NumPad9, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D0Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D0, token);

        /// <summary>
        /// 模拟按下主键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D0PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D0, token);

        /// <summary>
        /// 模拟松开主键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D0LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D0, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D1Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D1, token);

        /// <summary>
        /// 模拟按下主键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D1PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D1, token);

        /// <summary>
        /// 模拟松开主键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D1LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D1, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D2Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D2, token);

        /// <summary>
        /// 模拟按下主键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D2PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D2, token);

        /// <summary>
        /// 模拟松开主键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D2LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D2, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D3Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D3, token);

        /// <summary>
        /// 模拟按下主键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D3PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D3, token);

        /// <summary>
        /// 模拟松开主键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D3LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D3, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D4Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D4, token);

        /// <summary>
        /// 模拟按下主键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D4PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D4, token);

        /// <summary>
        /// 模拟松开主键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D4LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D4, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D5Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D5, token);

        /// <summary>
        /// 模拟按下主键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D5PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D5, token);

        /// <summary>
        /// 模拟松开主键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D5LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D5, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D6Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D6, token);

        /// <summary>
        /// 模拟按下主键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D6PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D6, token);

        /// <summary>
        /// 模拟松开主键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D6LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D6, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D7Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D7, token);

        /// <summary>
        /// 模拟按下主键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D7PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D7, token);

        /// <summary>
        /// 模拟松开主键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D7LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D7, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D8Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D8, token);

        /// <summary>
        /// 模拟按下主键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D8PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D8, token);

        /// <summary>
        /// 模拟松开主键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D8LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D8, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D9Async(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D9, token);

        /// <summary>
        /// 模拟按下主键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D9PressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D9, token);

        /// <summary>
        /// 模拟松开主键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task D9LoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D9, token);

        #endregion

        #region 符号

        /// <summary>
        /// 按下并松开小键盘加号<br/>
        /// 该方法会模拟按下并松开小键盘加号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumAddAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Add, token);

        /// <summary>
        /// 按下小键盘加号<br/>
        /// 该方法会模拟按下小键盘加号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumAddPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Add, token);

        /// <summary>
        /// 松开小键盘加号<br/>
        /// 该方法会模拟松开小键盘加号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumAddLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Add, token);

        /// <summary>
        /// 按下并松开小键盘减号<br/>
        /// 该方法会模拟按下并松开小键盘减号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumSubtractAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Subtract, token);

        /// <summary>
        /// 按下小键盘减号<br/>
        /// 该方法会模拟按下小键盘减号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumSubtractPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Subtract, token);

        /// <summary>
        /// 松开小键盘减号<br/>
        /// 该方法会模拟松开小键盘减号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumSubtractLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Subtract, token);

        /// <summary>
        /// 按下并松开小键盘乘号<br/>
        /// 该方法会模拟按下并松开小键盘乘号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumMultiplyAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Multiply, token);

        /// <summary>
        /// 按下小键盘乘号<br/>
        /// 该方法会模拟按下小键盘乘号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumMultiplyPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Multiply, token);

        /// <summary>
        /// 松开小键盘乘号<br/>
        /// 该方法会模拟松开小键盘乘号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumMultiplyLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Multiply, token);

        /// <summary>
        /// 按下并松开小键盘除号<br/>
        /// 该方法会模拟按下并松开小键盘除号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumDivideAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Divide, token);

        /// <summary>
        /// 按下小键盘除号<br/>
        /// 该方法会模拟按下小键盘除号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumDividePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Divide, token);

        /// <summary>
        /// 松开小键盘除号<br/>
        /// 该方法会模拟松开小键盘除号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumDivideLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Divide, token);

        /// <summary>
        /// 按下并松开小键盘小数点<br/>
        /// 该方法会模拟按下并松开小键盘小数点键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumDecimalAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Decimal, token);

        /// <summary>
        /// 按下小键盘小数点<br/>
        /// 该方法会模拟按下小键盘小数点键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumDecimalPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Decimal, token);

        /// <summary>
        /// 松开小键盘小数点<br/>
        /// 该方法会模拟松开小键盘小数点键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消异步操作。</param>
        /// <returns>表示异步操作的任务。</returns>
        public async Task NumDecimalLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Decimal, token);

        /// <summary>
        /// 模拟按下并松开 ` ~ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemTildeAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemTilde, token);

        /// <summary>
        /// 模拟按下 ` ~ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemTildePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemTilde, token);

        /// <summary>
        /// 模拟松开 ` ~ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemTildeLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemTilde, token);

        /// <summary>
        /// 模拟按下并松开 - _ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemMinusAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemMinus, token);

        /// <summary>
        /// 模拟按下 - _ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemMinusPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemMinus, token);

        /// <summary>
        /// 模拟松开 - _ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemMinusLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemMinus, token);

        /// <summary>
        /// 模拟按下并松开 = + 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPlusAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemPlus, token);

        /// <summary>
        /// 模拟按下 = + 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPlusPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemPlus, token);

        /// <summary>
        /// 模拟松开 = + 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPlusLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemPlus, token);

        /// <summary>
        /// 模拟按下并松开 [ { 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemOpenBracketsAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemOpenBrackets, token);

        /// <summary>
        /// 模拟按下 [ { 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemOpenBracketsPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemOpenBrackets, token);

        /// <summary>
        /// 模拟松开 [ { 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemOpenBracketsLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemOpenBrackets, token);

        /// <summary>
        /// 模拟按下并松开 ] } 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemCloseBracketsAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemCloseBrackets, token);

        /// <summary>
        /// 模拟按下 ] } 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemCloseBracketsPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemCloseBrackets, token);

        /// <summary>
        /// 模拟松开 ] } 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemCloseBracketsLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemCloseBrackets, token);

        /// <summary>
        /// 模拟按下并松开 \ | 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPipeAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemPipe, token);

        /// <summary>
        /// 模拟按下 \ | 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPipePressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemPipe, token);

        /// <summary>
        /// 模拟松开 \ | 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPipeLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemPipe, token);

        /// <summary>
        /// 模拟按下并松开 ; : 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemSemicolonAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemSemicolon, token);

        /// <summary>
        /// 模拟按下 ; : 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemSemicolonPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemSemicolon, token);

        /// <summary>
        /// 模拟松开 ; : 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemSemicolonLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemSemicolon, token);

        /// <summary>
        /// 模拟按下并松开 ' " 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemQuotesAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemQuotes, token);

        /// <summary>
        /// 模拟按下 ' " 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemQuotesPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemQuotes, token);

        /// <summary>
        /// 模拟松开 ' " 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemQuotesLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemQuotes, token);

        /// <summary>
        /// 模拟按下并松开 , < 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemCommaAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemComma, token);

        /// <summary>
        /// 模拟按下 , < 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemCommaPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemComma, token);

        /// <summary>
        /// 模拟松开 , < 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemCommaLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemComma, token);

        /// <summary>
        /// 模拟按下并松开 . > 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPeriodAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemPeriod, token);

        /// <summary>
        /// 模拟按下 . > 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPeriodPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemPeriod, token);

        /// <summary>
        /// 模拟松开 . > 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemPeriodLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemPeriod, token);

        /// <summary>
        /// 模拟按下并松开 / ? 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemQuestionAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.OemQuestion, token);

        /// <summary>
        /// 模拟按下 / ? 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemQuestionPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.OemQuestion, token);

        /// <summary>
        /// 模拟松开 / ? 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OemQuestionLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.OemQuestion, token);

        #endregion

        #region 字母
        /// <summary>
        /// 模拟按下并松开键盘上的 A 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task AAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.A, token);

        /// <summary>
        /// 模拟按下键盘上的 A 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task APressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.A, token);

        /// <summary>
        /// 模拟松开键盘上的 A 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ALoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.A, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 B 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task BAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.B, token);

        /// <summary>
        /// 模拟按下键盘上的 B 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task BPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.B, token);

        /// <summary>
        /// 模拟松开键盘上的 B 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task BLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.B, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 C 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.C, token);

        /// <summary>
        /// 模拟按下键盘上的 C 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.C, token);

        /// <summary>
        /// 模拟松开键盘上的 C 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task CLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.C, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 D 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.D, token);

        /// <summary>
        /// 模拟按下键盘上的 D 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.D, token);

        /// <summary>
        /// 模拟松开键盘上的 D 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task DLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.D, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 E 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task EAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.E, token);

        /// <summary>
        /// 模拟按下键盘上的 E 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task EPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.E, token);

        /// <summary>
        /// 模拟松开键盘上的 E 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ELoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.E, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 F 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task FAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.F, token);

        /// <summary>
        /// 模拟按下键盘上的 F 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task FPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.F, token);

        /// <summary>
        /// 模拟松开键盘上的 F 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task FLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.F, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 G 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task GAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.G, token);

        /// <summary>
        /// 模拟按下键盘上的 G 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task GPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.G, token);

        /// <summary>
        /// 模拟松开键盘上的 G 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task GLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.G, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 H 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task HAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.H, token);

        /// <summary>
        /// 模拟按下键盘上的 H 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task HPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.H, token);

        /// <summary>
        /// 模拟松开键盘上的 H 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task HLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.H, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 I 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task IAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.I, token);

        /// <summary>
        /// 模拟按下键盘上的 I 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task IPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.I, token);

        /// <summary>
        /// 模拟松开键盘上的 I 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ILoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.I, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 J 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task JAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.J, token);

        /// <summary>
        /// 模拟按下键盘上的 J 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task JPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.J, token);

        /// <summary>
        /// 模拟松开键盘上的 J 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task JLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.J, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 K 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task KAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.K, token);

        /// <summary>
        /// 模拟按下键盘上的 K 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task KPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.K, token);

        /// <summary>
        /// 模拟松开键盘上的 K 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task KLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.K, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 L 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task LAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.L, token);

        /// <summary>
        /// 模拟按下键盘上的 L 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task LPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.L, token);

        /// <summary>
        /// 模拟松开键盘上的 L 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task LLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.L, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 M 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task MAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.M, token);

        /// <summary>
        /// 模拟按下键盘上的 M 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task MPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.M, token);

        /// <summary>
        /// 模拟松开键盘上的 M 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task MLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.M, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 N 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.N, token);

        /// <summary>
        /// 模拟按下键盘上的 N 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.N, token);

        /// <summary>
        /// 模拟松开键盘上的 N 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task NLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.N, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 O 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.O, token);

        /// <summary>
        /// 模拟按下键盘上的 O 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.O, token);

        /// <summary>
        /// 模拟松开键盘上的 O 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task OLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.O, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 P 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task PAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.P, token);

        /// <summary>
        /// 模拟按下键盘上的 P 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task PPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.P, token);

        /// <summary>
        /// 模拟松开键盘上的 P 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task PLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.P, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 Q 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task QAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Q, token);

        /// <summary>
        /// 模拟按下键盘上的 Q 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task QPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Q, token);

        /// <summary>
        /// 模拟松开键盘上的 Q 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task QLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Q, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 R 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task RAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.R, token);

        /// <summary>
        /// 模拟按下键盘上的 R 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task RPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.R, token);

        /// <summary>
        /// 模拟松开键盘上的 R 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task RLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.R, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 S 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task SAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.S, token);

        /// <summary>
        /// 模拟按下键盘上的 S 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task SPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.S, token);

        /// <summary>
        /// 模拟松开键盘上的 S 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task SLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.S, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 T 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task TAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.T, token);

        /// <summary>
        /// 模拟按下键盘上的 T 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task TPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.T, token);

        /// <summary>
        /// 模拟松开键盘上的 T 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task TLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.T, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 U 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.U, token);

        /// <summary>
        /// 模拟按下键盘上的 U 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task UPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.U, token);

        /// <summary>
        /// 模拟松开键盘上的 U 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ULoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.U, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 V 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task VAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.V, token);

        /// <summary>
        /// 模拟按下键盘上的 V 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task VPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.V, token);

        /// <summary>
        /// 模拟松开键盘上的 V 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task VLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.V, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 W 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task WAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.W, token);

        /// <summary>
        /// 模拟按下键盘上的 W 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task WPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.W, token);

        /// <summary>
        /// 模拟松开键盘上的 W 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task WLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.W, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 X 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task XAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.X, token);

        /// <summary>
        /// 模拟按下键盘上的 X 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task XPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.X, token);

        /// <summary>
        /// 模拟松开键盘上的 X 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task XLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.X, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 Y 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task YAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Y, token);

        /// <summary>
        /// 模拟按下键盘上的 Y 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task YPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Y, token);

        /// <summary>
        /// 模拟松开键盘上的 Y 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task YLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Y, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 Z 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ZAsync(CancellationToken token = default)
            => await KeyboardPressAsync(Key.Z, token);

        /// <summary>
        /// 模拟按下键盘上的 Z 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ZPressAsync(CancellationToken token = default)
            => await KeyboardPressKeyAsync(Key.Z, token);

        /// <summary>
        /// 模拟松开键盘上的 Z 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消异步操作</param>
        /// <returns>表示异步操作的任务</returns>
        public async Task ZLoosenAsync(CancellationToken token = default)
            => await KeyboardLoosenKeyAsync(Key.Z, token);
        #endregion

        /// <summary>
        /// 按下常见的字母、数字和符号键<br/>
        /// 根据 SymbolMap 自动判断是否需要按下 Shift 键<br/>
        /// 例如输入大写字母或特殊符号时会自动触发 Shift
        /// </summary>
        /// <param name="data">要输入的字符数组<br/>支持字母、数字及常见符号</param>
        /// <param name="token">可选的 CancellationToken，用于取消操作</param>
        /// <returns>一个表示异步操作的 Task</returns>
        public async Task KeyboardPressCommonKeyAsync(char[] data, CancellationToken token = default)
        {
            foreach (var dt in data)
            {
                if (ConfigModel.SymbolMap.TryGetValue(dt, out var info))
                {
                    if (info.shift)
                        await KeyboardPressAsync((byte)KeyInterop.VirtualKeyFromKey(Key.RightShift), info.key, token);
                    else
                        await KeyboardPressAsync(info.key, token);
                }
            }
        }

        /// <summary>
        /// 同时按下两个键（Key 枚举类型），用于组合键操作<br/>
        /// 先按下 key1，再按下 key2，最后松开 key1<br/>
        /// 例如 Ctrl + C，可以传入 Key.LeftCtrl, Key.C
        /// </summary>
        /// <param name="key1">第一个按下的键（修饰键，如 Ctrl/Shift/Alt）</param>
        /// <param name="key2">第二个按下的键（主操作键，如字母或数字）</param>
        /// <param name="token">可选的 CancellationToken，用于取消操作</param>
        /// <returns>一个表示异步操作的 Task</returns>
        private async Task KeyboardPressAsync(Key key1, Key key2, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key1, token);
            await KeyboardPressAsync(key2, token);
            await KeyboardLoosenKeyAsync(key1, token);
        }

        /// <summary>
        /// 同时按下两个键（byte 类型虚拟键码），用于组合键操作<br/>
        /// 先按下 key1，再按下 key2，最后松开 key1<br/>
        /// 适用于使用 Win32 虚拟键码的按键操作
        /// </summary>
        /// <param name="key1">第一个按下的虚拟键码（修饰键，如 Ctrl/Shift/Alt）</param>
        /// <param name="key2">第二个按下的虚拟键码（主操作键，如字母或数字）</param>
        /// <param name="token">可选的 CancellationToken，用于取消操作</param>
        /// <returns>一个表示异步操作的 Task</returns>
        private async Task KeyboardPressAsync(byte key1, byte key2, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key1, token);
            await KeyboardPressAsync(key2, token);
            await KeyboardLoosenKeyAsync(key1, token);
        }

        /// <summary>
        /// 异步模拟按下并释放功能键
        /// </summary>
        /// <param name="key">功能键字符串，例如 "F1"</param>
        /// <param name="token">取消令牌（可选）</param>
        public async Task KeyboardPressAsync(Key key, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key, token);
            await Task.Delay(ConfigModel.RestTime, token);
            await KeyboardLoosenKeyAsync(key, token);
        }

        /// <summary>
        /// 异步模拟按下并释放功能键
        /// </summary>
        /// <param name="key">功能键字符串，例如 "F1"</param>
        /// <param name="token">取消令牌（可选）</param>
        public async Task KeyboardPressAsync(byte key, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key, token);
            await Task.Delay(ConfigModel.RestTime, token);
            await KeyboardLoosenKeyAsync(key, token);
        }

        /// <summary>
        /// 异步模拟按下并释放功能键（F1-F12）
        /// </summary>
        /// <param name="data">功能键字符串，例如 "F1"</param>
        /// <param name="token">取消令牌（可选）</param>
        public async Task KeyboardPressF1_F12Async(string data, CancellationToken token = default)
        {
            if (!data.StartsWith("F") || !int.TryParse(data[1..], out int index) || index < 1 || index > 12)
                return; // 非法输入直接返回

            Key key = Key.F1 + (index - 1); // 计算 Key 枚举值

            await KeyboardPressKeyAsync(key, token);
            await Task.Delay(ConfigModel.RestTime, token);
            await KeyboardLoosenKeyAsync(key, token);
        }

        /// <summary>
        /// 判断字符类型
        /// </summary>
        /// <param name="data">要判断的字符</param>
        /// <param name="token">取消令牌（可选）</param>
        /// <returns>返回字符类型</returns>
        public Task<DataType> DetermineTypeAsync(char data, CancellationToken token = default)
        {
            DataType type = data switch
            {
                >= '0' and <= '9' => DataType.Number,
                >= 'a' and <= 'z' => DataType.LowerCase,
                >= 'A' and <= 'Z' => DataType.UpperCase,
                _ => DataType.Symbol
            };
            return Task.FromResult(type);
        }

        /// <summary>
        /// 获取按键状态
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        /// <returns>按下返回true，未按下返回false</returns>
        public async Task<bool> GetKeyStateAsync(Key key, CancellationToken token = default)
        {
            return await Task.Run(() =>
            {
                byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
                return (Win32.GetKeyState(vk) == 1);
            }, token);
        }

        /// <summary>
        /// 按下
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        public async Task KeyboardPressKeyAsync(byte key, CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                Win32.keybd_event(key, 0, 0, 0);
                await Task.Delay(ConfigModel.RestTime, token);
            }, token);
        }
        /// <summary>
        /// 松开
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        public async Task KeyboardLoosenKeyAsync(byte key, CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                Win32.keybd_event(key, 0, Win32.KEYEVENTF_KEYUP, 0);
                await Task.Delay(ConfigModel.RestTime, token);
            }, token);
        }

        /// <summary>
        /// 按下
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        public async Task KeyboardPressKeyAsync(Key key, CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
                Win32.keybd_event(vk, 0, 0, 0);
                await Task.Delay(ConfigModel.RestTime, token);
            }, token);
        }
        /// <summary>
        /// 松开
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        public async Task KeyboardLoosenKeyAsync(Key key, CancellationToken token = default)
        {
            await Task.Run(async () =>
            {
                byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
                Win32.keybd_event(vk, 0, Win32.KEYEVENTF_KEYUP, 0);
                await Task.Delay(ConfigModel.RestTime, token);
            }, token);
        }
        #endregion
    }
}
