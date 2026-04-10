using Snet.Windows.KMSim.data;
using System.Text;
using System.Windows.Input;

namespace Snet.Windows.KMSim.core
{
    /// <summary>
    /// 键鼠模拟核心类，封装鼠标移动、点击、键盘输入、窗口管理等底层操作，均通过 <see cref="Win32"/> P/Invoke 实现。
    /// <para>所有操作支持异步和取消令牌，适用于自动化脚本执行场景。</para>
    /// </summary>
    public class KMSimCore
    {
        /// <summary>
        /// 获取窗体的宽度
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <param name="token">取消通知</param>
        /// <returns>宽度</returns>
        public Task<int> GetWindowWidthAsync(IntPtr hWnd, CancellationToken token = default)
        {
            (int Width, int Height) data = Win32.GetWindowSize(hWnd);
            return Task.FromResult(data.Width);
        }
        /// <summary>
        /// 获取窗体的高度
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <param name="token">取消通知</param>
        /// <returns></returns>
        public Task<int> GetWindowHeightAsync(IntPtr hWnd, CancellationToken token = default)
        {
            (int Width, int Height) data = Win32.GetWindowSize(hWnd);
            return Task.FromResult(data.Height);
        }

        /// <summary>
        /// 拷贝内容到剪贴板
        /// </summary>
        /// <param name="content">要复制的文本</param>
        /// <param name="token">取消令牌</param>
        public async Task CopyContentAsync(string content, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(content))
                return;

            await Task.Run(() =>
            {
                var thread = new Thread(() => System.Windows.Clipboard.SetDataObject(content, true));
                // STA 模式线程
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join(); // 等待执行完毕
            }, token);
        }

        /// <summary>
        /// 休息指定毫秒数
        /// </summary>
        /// <param name="ms">毫秒</param>
        /// <param name="token">取消通知</param>
        /// <returns></returns>
        public Task DelayAsync(int ms, CancellationToken token = default)
            => Task.Delay(ms, token);


        /// <summary>
        /// 模糊查找窗口句柄
        /// </summary>
        /// <param name="name">窗口标题关键字</param>
        /// <param name="token">取消通知</param>
        /// <returns>匹配到的窗口句柄，未找到则返回 0</returns>
        public async Task<nint> GetWindowsHandleAsync(string name, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            return await Task.Run(() =>
            {
                nint found = 0;

                // 枚举所有顶级窗口
                Win32.EnumWindows((hWnd, lParam) =>
                {
                    if (token.IsCancellationRequested)
                        return false;

                    if (!Win32.IsWindowVisible(hWnd))
                        return true;

                    string title = GetWindowTitleSafe(hWnd);
                    if (string.IsNullOrEmpty(title))
                        return true;

                    // 模糊匹配标题（忽略大小写）
                    if (title.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        found = hWnd;
                        return false; // 找到后立即停止
                    }

                    // 若顶级窗口不匹配，则递归查找子窗口
                    Win32.EnumChildWindows(hWnd, (child, l) =>
                    {
                        string subTitle = GetWindowTitleSafe(child);
                        if (!string.IsNullOrEmpty(subTitle) &&
                            subTitle.Contains(name, StringComparison.OrdinalIgnoreCase))
                        {
                            found = child;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);

                    return found == 0; // 若找到则停止枚举
                }, IntPtr.Zero);

                token.ThrowIfCancellationRequested();
                return found;
            }, token);
        }

        /// <summary>
        /// 安全获取窗口标题（防止访问无效句柄或权限异常）
        /// </summary>
        private static string GetWindowTitleSafe(nint hWnd)
        {
            try
            {
                StringBuilder sb = new(256);
                int len = Win32.GetWindowText(hWnd, sb, sb.Capacity);
                return len > 0 ? sb.ToString() : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 窗体置顶
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <param name="top">是否置顶</param>
        /// <param name="token">取消通知</param>
        public Task WindowsTopMostAsync(IntPtr hWnd, CancellationToken token = default)
            => Task.Run(() => Win32.SetWindowPos(hWnd, Win32.HWND_TOPMOST, 0, 0, 0, 0, Win32.SWP_NOACTIVATE), token);

        /// <summary>
        /// 移动窗口
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        /// <param name="token">取消通知</param>
        public Task MoveWindowsAsync(IntPtr hWnd, int x, int y, int width, int height, CancellationToken token = default)
            => Task.Run(() => Win32.MoveWindow(hWnd, x, y, width, height, true), token);

        #region 鼠标操作

        /// <summary>
        /// 移动鼠标
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <param name="token">取消通知</param>
        /// <returns></returns>
        public async Task MouseMoveAsync(int x, int y, CancellationToken token = default)
        {
            Win32.SetCursorPos(x, y);
            await Task.Delay(ConfigModel.RestTime, token);
        }

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
            Win32.mouse_event(Win32.MOUSEEVENTF_WHEEL, 0, 0, 100, 0);
            await Task.Delay(ConfigModel.RestTime * 20, token);
        }

        /// <summary>
        /// 实现滚轮 向下滚动
        /// </summary>
        public async Task MouseRollerDownAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_WHEEL, 0, 0, -100, 0);
            await Task.Delay(ConfigModel.RestTime * 20, token);
        }

        /// <summary>
        /// 鼠标左键按下
        /// </summary>
        public Task MouseLeftDownAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 鼠标左键松开
        /// </summary>
        public Task MouseLeftUpAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 鼠标右键按下
        /// </summary>
        public Task MouseRightDownAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 鼠标右键松开
        /// </summary>
        public Task MouseRightUpAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 鼠标中键按下
        /// </summary>
        public Task MouseMiddleDownAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 鼠标中键松开
        /// </summary>
        public Task MouseMiddleUpAsync(CancellationToken token = default)
        {
            Win32.mouse_event(Win32.MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
            return Task.CompletedTask;
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
        public async Task ToggleCaseAsync(bool state, CancellationToken token = default)
        {
            if (state != await GetKeyStateAsync(Key.CapsLock, token))
            {
                await KeyboardPressKeyAsync(Key.CapsLock); // 按下 CapsLock
                await Task.Delay(ConfigModel.RestTime, token); // 等待系统响应
                await KeyboardLoosenKeyAsync(Key.CapsLock); // 松开 CapsLock
            }
        }

        /// <summary>
        /// 获取大写锁定状态
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <returns>目标状态：true 表示开启大写锁定，false 表示关闭大写锁定</returns>
        public Task<bool> GetToggleCaseStatusAsync(CancellationToken token = default)
            => GetKeyStateAsync(Key.CapsLock, token);

        /// <summary>
        /// 执行复制操作（Ctrl + C）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 C 键，实现标准复制操作；
        /// 2. 适用于选中文本或文件后自动复制到剪贴板的场景；
        /// 3. 会调用 KeyboardPressAsync 方法处理按下、松开事件，确保操作稳定。
        /// </remarks>
        public Task CopyAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LeftCtrl, Key.C, token);

        /// <summary>
        /// 执行保存操作（Ctrl + S）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 S 键，实现保存当前文档或文件操作；
        /// 2. 适用于自动化保存功能，例如定时保存文档或编辑内容；
        /// 3. 使用 KeyboardPressAsync 方法保证按键顺序和时长正确。
        /// </remarks>
        public Task SaveAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LeftCtrl, Key.S, token);

        /// <summary>
        /// 执行粘贴操作（Ctrl + V）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 V 键，将剪贴板内容粘贴到当前光标位置；
        /// 2. 适用于自动化文本输入、文件路径粘贴等场景；
        /// 3. KeyboardPressAsync 方法会自动处理按下、松开逻辑，确保粘贴成功。
        /// </remarks>
        public Task PasteAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LeftCtrl, Key.V, token);

        /// <summary>
        /// 执行全选操作（Ctrl + A）
        /// </summary>
        /// <param name="token">可选的取消令牌，用于取消操作</param>
        /// <remarks>
        /// 1. 模拟同时按下 LeftCtrl 和 A 键，选中当前窗口或文本框中的全部内容；
        /// 2. 适用于自动化文本编辑、批量操作或数据处理场景；
        /// 3. KeyboardPressAsync 方法会自动处理按下、松开事件，确保全选动作生效。
        /// </remarks>
        public Task SelectAllAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LeftCtrl, Key.A, token);

        #region 键盘其余按键
        /// <summary>
        /// 按下并松开回车<br/>
        /// 该方法会模拟按下并松开回车键，通常用于触发提交或确认操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EnterAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Enter, token);

        /// <summary>
        /// 按下回车<br/>
        /// 该方法会模拟按下回车键，通常用于触发提交或确认操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EnterPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Enter, token);

        /// <summary>
        /// 松开回车<br/>
        /// 该方法会模拟松开回车键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EnterLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Enter, token);

        /// <summary>
        /// 按下并松开Tab<br/>
        /// 该方法会模拟按下并松开Tab键，通常用于在控件之间切换焦点。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task TabAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Tab, token);

        /// <summary>
        /// 按下Tab<br/>
        /// 该方法会模拟按下Tab键，通常用于在控件之间切换焦点。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task TabPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Tab, token);

        /// <summary>
        /// 松开Tab<br/>
        /// 该方法会模拟松开Tab键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task TabLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Tab, token);

        /// <summary>
        /// 按下并松开CapsLock<br/>
        /// 该方法会模拟按下并松开CapsLock键，通常用于开启或关闭大写字母锁定。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task CapsLockAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.CapsLock, token);

        /// <summary>
        /// 按下CapsLock<br/>
        /// 该方法会模拟按下CapsLock键，通常用于开启或关闭大写字母锁定。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task CapsLockPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.CapsLock, token);

        /// <summary>
        /// 松开CapsLock<br/>
        /// 该方法会模拟松开CapsLock键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task CapsLockLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.CapsLock, token);

        /// <summary>
        /// 按下并松开回退键<br/>
        /// 该方法会模拟按下并松开回退键，通常用于删除光标前的一个字符。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task BackAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Back, token);

        /// <summary>
        /// 按下回退键<br/>
        /// 该方法会模拟按下回退键，通常用于删除光标前的一个字符。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task BackPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Back, token);

        /// <summary>
        /// 松开回退键<br/>
        /// 该方法会模拟松开回退键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task BackLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Back, token);

        /// <summary>
        /// 按下并松开空格<br/>
        /// 该方法会模拟按下并松开空格键，通常用于在输入框中插入空格。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task SpaceAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Space, token);

        /// <summary>
        /// 按下空格<br/>
        /// 该方法会模拟按下空格键，通常用于在输入框中插入空格。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task SpacePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Space, token);

        /// <summary>
        /// 松开空格<br/>
        /// 该方法会模拟松开空格键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task SpaceLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Space, token);

        /// <summary>
        /// 按下并松开左Ctrl<br/>
        /// 该方法会模拟按下并松开左侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LeftCtrlAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LeftCtrl, token);

        /// <summary>
        /// 按下左Ctrl<br/>
        /// 该方法会模拟按下左侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LeftCtrlPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.LeftCtrl, token);

        /// <summary>
        /// 松开左Ctrl<br/>
        /// 该方法会模拟松开左侧Ctrl键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LeftCtrlLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.LeftCtrl, token);

        /// <summary>
        /// 按下并松开右Ctrl<br/>
        /// 该方法会模拟按下并松开右侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RightCtrlAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.RightCtrl, token);

        /// <summary>
        /// 按下右Ctrl<br/>
        /// 该方法会模拟按下右侧Ctrl键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RightCtrlPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.RightCtrl, token);

        /// <summary>
        /// 松开右Ctrl<br/>
        /// 该方法会模拟松开右侧Ctrl键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RightCtrlLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.RightCtrl, token);

        /// <summary>
        /// 按下并松开左Win<br/>
        /// 该方法会模拟按下并松开左侧Win键，通常用于打开开始菜单或触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LWinAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LWin, token);

        /// <summary>
        /// 按下左Win<br/>
        /// 该方法会模拟按下左侧Win键，通常用于打开开始菜单或触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LWinPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.LWin, token);

        /// <summary>
        /// 松开左Win<br/>
        /// 该方法会模拟松开左侧Win键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LWinLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.LWin, token);

        /// <summary>
        /// 按下并松开右Win<br/>
        /// 该方法会模拟按下并松开右侧Win键，通常用于触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RWinAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.RWin, token);

        /// <summary>
        /// 按下右Win<br/>
        /// 该方法会模拟按下右侧Win键，通常用于触发Win键组合操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RWinPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.RWin, token);

        /// <summary>
        /// 松开右Win<br/>
        /// 该方法会模拟松开右侧Win键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RWinLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.RWin, token);

        /// <summary>
        /// 按下并松开左Alt<br/>
        /// 该方法会模拟按下并松开左侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LeftAltAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.LeftAlt, token);

        /// <summary>
        /// 按下左Alt<br/>
        /// 该方法会模拟按下左侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LeftAltPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.LeftAlt, token);

        /// <summary>
        /// 松开左Alt<br/>
        /// 该方法会模拟松开左侧Alt键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task LeftAltLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.LeftAlt, token);

        /// <summary>
        /// 按下并松开右Alt<br/>
        /// 该方法会模拟按下并松开右侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RightAltAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.RightAlt, token);

        /// <summary>
        /// 按下右Alt<br/>
        /// 该方法会模拟按下右侧Alt键，通常用于与其他键组合触发操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RightAltPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.RightAlt, token);

        /// <summary>
        /// 松开右Alt<br/>
        /// 该方法会模拟松开右侧Alt键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task RightAltLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.RightAlt, token);

        /// <summary>
        /// 按下并松开Delete<br/>
        /// 该方法会模拟按下并松开Delete键，通常用于删除光标后的字符或选中内容。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task DeleteAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Delete, token);

        /// <summary>
        /// 按下Delete<br/>
        /// 该方法会模拟按下Delete键，通常用于删除光标后的字符或选中内容。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task DeletePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Delete, token);

        /// <summary>
        /// 松开Delete<br/>
        /// 该方法会模拟松开Delete键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task DeleteLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Delete, token);

        /// <summary>
        /// 按下并松开Apps（菜单键）<br/>
        /// 该方法会模拟按下并松开Apps键，通常用于打开上下文菜单。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task AppsAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Apps, token);

        /// <summary>
        /// 按下Apps（菜单键）<br/>
        /// 该方法会模拟按下Apps键，通常用于打开上下文菜单。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task AppsPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Apps, token);

        /// <summary>
        /// 松开Apps（菜单键）<br/>
        /// 该方法会模拟松开Apps键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task AppsLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Apps, token);

        /// <summary>
        /// 按下并松开PageUp<br/>
        /// 该方法会模拟按下并松开PageUp键，通常用于向上翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PageUpAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.PageUp, token);

        /// <summary>
        /// 按下PageUp<br/>
        /// 该方法会模拟按下PageUp键，通常用于向上翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PageUpPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.PageUp, token);

        /// <summary>
        /// 松开PageUp<br/>
        /// 该方法会模拟松开PageUp键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PageUpLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.PageUp, token);

        /// <summary>
        /// 按下并松开PageDown<br/>
        /// 该方法会模拟按下并松开PageDown键，通常用于向下翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PageDownAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.PageDown, token);

        /// <summary>
        /// 按下PageDown<br/>
        /// 该方法会模拟按下PageDown键，通常用于向下翻页操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PageDownPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.PageDown, token);

        /// <summary>
        /// 松开PageDown<br/>
        /// 该方法会模拟松开PageDown键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PageDownLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.PageDown, token);

        /// <summary>
        /// 按下并松开Insert<br/>
        /// 该方法会模拟按下并松开Insert键，通常用于切换插入/覆盖模式。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task InsertAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Insert, token);

        /// <summary>
        /// 按下Insert<br/>
        /// 该方法会模拟按下Insert键，通常用于切换插入/覆盖模式。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task InsertPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Insert, token);

        /// <summary>
        /// 松开Insert<br/>
        /// 该方法会模拟松开Insert键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task InsertLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Insert, token);

        /// <summary>
        /// 按下并松开Home<br/>
        /// 该方法会模拟按下并松开Home键，通常用于将光标移动到行首或页面顶部。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task HomeAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Home, token);

        /// <summary>
        /// 按下Home<br/>
        /// 该方法会模拟按下Home键，通常用于将光标移动到行首或页面顶部。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task HomePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Home, token);

        /// <summary>
        /// 松开Home<br/>
        /// 该方法会模拟松开Home键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task HomeLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Home, token);

        /// <summary>
        /// 按下并松开End<br/>
        /// 该方法会模拟按下并松开End键，通常用于将光标移动到行尾或页面底部。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EndAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.End, token);

        /// <summary>
        /// 按下End<br/>
        /// 该方法会模拟按下End键，通常用于将光标移动到行尾或页面底部。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EndPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.End, token);

        /// <summary>
        /// 松开End<br/>
        /// 该方法会模拟松开End键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EndLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.End, token);

        /// <summary>
        /// 按下并松开小键盘Enter<br/>
        /// 该方法会模拟按下并松开小键盘Enter键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumEnterAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Enter, token);

        /// <summary>
        /// 按下小键盘Enter<br/>
        /// 该方法会模拟按下小键盘Enter键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumEnterPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Enter, token);

        /// <summary>
        /// 松开小键盘Enter<br/>
        /// 该方法会模拟松开小键盘Enter键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumEnterLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Enter, token);

        /// <summary>
        /// 按下并松开Pause/Break<br/>
        /// 该方法会模拟按下并松开Pause/Break键，通常用于暂停程序或调试。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PauseAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Pause, token);

        /// <summary>
        /// 按下Pause/Break<br/>
        /// 该方法会模拟按下Pause/Break键，通常用于暂停程序或调试。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PausePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Pause, token);

        /// <summary>
        /// 松开Pause/Break<br/>
        /// 该方法会模拟松开Pause/Break键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PauseLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Pause, token);

        /// <summary>
        /// 按下并松开PrintScreen<br/>
        /// 该方法会模拟按下并松开PrintScreen键，通常用于截屏操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PrintScreenAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.PrintScreen, token);

        /// <summary>
        /// 按下PrintScreen<br/>
        /// 该方法会模拟按下PrintScreen键，通常用于截屏操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PrintScreenPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.PrintScreen, token);

        /// <summary>
        /// 松开PrintScreen<br/>
        /// 该方法会模拟松开PrintScreen键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task PrintScreenLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.PrintScreen, token);

        /// <summary>
        /// 按下并松开ScrollLock<br/>
        /// 该方法会模拟按下并松开ScrollLock键，通常用于滚动锁定操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task ScrollLockAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Scroll, token);

        /// <summary>
        /// 按下ScrollLock<br/>
        /// 该方法会模拟按下ScrollLock键，通常用于滚动锁定操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task ScrollLockPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Scroll, token);

        /// <summary>
        /// 松开ScrollLock<br/>
        /// 该方法会模拟松开ScrollLock键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task ScrollLockLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Scroll, token);

        /// <summary>
        /// 按下并松开NumLock<br/>
        /// 该方法会模拟按下并松开NumLock键，通常用于切换小键盘输入模式。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumLockAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumLock, token);

        /// <summary>
        /// 按下NumLock<br/>
        /// 该方法会模拟按下NumLock键，通常用于切换小键盘输入模式。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumLockPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumLock, token);

        /// <summary>
        /// 松开NumLock<br/>
        /// 该方法会模拟松开NumLock键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumLockLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumLock, token);

        /// <summary>
        /// 按下并松开Escape<br/>
        /// 该方法会模拟按下并松开Escape键，通常用于取消操作或退出界面。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EscapeAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Escape, token);

        /// <summary>
        /// 按下Escape<br/>
        /// 该方法会模拟按下Escape键，通常用于取消操作或退出界面。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EscapePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Escape, token);

        /// <summary>
        /// 松开Escape<br/>
        /// 该方法会模拟松开Escape键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task EscapeLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Escape, token);


        #endregion

        #region 方向
        /// <summary>
        /// 模拟按下并松开 ↑ 键<br/>
        /// 该方法会模拟按下并松开 ↑ 键，通常用于模拟按键动作，例如在键盘事件或自动化测试中使用。</summary>
        /// <param name="token">取消令牌，用于取消操作。调用者可以通过传递 CancellationToken 来取消该任务。</param>
        /// <returns>表示操作的任务。该任务在按键操作完成后返回。</returns>
        public Task UpAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Up, token);

        /// <summary>
        /// 模拟按下 ↑ 键<br/>
        /// 该方法会模拟按下 ↑ 键，但不会松开它，通常用于需要持续按住某个键的场景。</summary>
        /// <param name="token">取消令牌，用于取消操作。可以在操作中途取消按键事件。</param>
        /// <returns>表示操作的任务。该任务在按键按下操作完成后返回。</returns>
        public Task UpPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Up, token);

        /// <summary>
        /// 模拟松开 ↑ 键<br/>
        /// 该方法会模拟松开 ↑ 键，通常用于在按键操作后释放键盘按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。可以在操作中途取消松开操作。</param>
        /// <returns>表示操作的任务。该任务在松开操作完成后返回。</returns>
        public Task UpLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Up, token);

        /// <summary>
        /// 模拟按下并松开 ↓ 键<br/>
        /// 该方法会模拟按下并松开 ↓ 键，通常用于模拟按键动作，类似于按键事件的触发。</summary>
        /// <param name="token">取消令牌，用于取消操作。如果需要取消操作，可以传递一个 CancellationToken。</param>
        /// <returns>表示操作的任务。任务会在按键动作完成后返回。</returns>
        public Task DownAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Down, token);

        /// <summary>
        /// 模拟按下 ↓ 键<br/>
        /// 该方法会模拟按下 ↓ 键并保持按下状态，通常用于需要持续按下某个键的场景。</summary>
        /// <param name="token">取消令牌，用于取消操作。可以在按键按下时通过取消令牌中断操作。</param>
        /// <returns>表示操作的任务。任务会在按下操作完成后返回。</returns>
        public Task DownPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Down, token);

        /// <summary>
        /// 模拟松开 ↓ 键<br/>
        /// 该方法会模拟松开 ↓ 键，通常用于按键释放后事件的触发。</summary>
        /// <param name="token">取消令牌，用于取消操作。通过传递取消令牌，调用者可以取消该操作。</param>
        /// <returns>表示操作的任务。任务会在松开操作完成后返回。</returns>
        public Task DownLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Down, token);

        /// <summary>
        /// 模拟按下并松开 ← 键<br/>
        /// 该方法会模拟按下并松开 ← 键，用于模拟标准的键盘按下并松开事件。</summary>
        /// <param name="token">取消令牌，用于取消操作。可以通过传递取消令牌来取消该任务。</param>
        /// <returns>表示操作的任务。任务在键盘按下并松开后完成。</returns>
        public Task LeftAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Left, token);

        /// <summary>
        /// 模拟按下 ← 键<br/>
        /// 该方法会模拟按下 ← 键，并保持按下状态，适用于需要长时间按住某个键的操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。通过传递取消令牌，可以中断操作。</param>
        /// <returns>表示操作的任务。该任务在按键按下操作完成后返回。</returns>
        public Task LeftPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Left, token);

        /// <summary>
        /// 模拟松开 ← 键<br/>
        /// 该方法会模拟松开 ← 键，适用于释放键盘按键的操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。通过传递取消令牌，调用者可以取消该任务。</param>
        /// <returns>表示操作的任务。任务会在松开操作完成后返回。</returns>
        public Task LeftLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Left, token);

        /// <summary>
        /// 模拟按下并松开 → 键<br/>
        /// 该方法会模拟按下并松开 → 键，通常用于触发键盘按下松开事件。</summary>
        /// <param name="token">取消令牌，用于取消操作。调用者可以通过传递 CancellationToken 来取消任务。</param>
        /// <returns>表示操作的任务。任务会在键盘按下并松开后完成。</returns>
        public Task RightAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Right, token);

        /// <summary>
        /// 模拟按下 → 键<br/>
        /// 该方法会模拟按下 → 键并保持按下状态，适用于长时间按住某个键的操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。调用者可以传递取消令牌来中断操作。</param>
        /// <returns>表示操作的任务。任务会在按下操作完成后返回。</returns>
        public Task RightPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Right, token);

        /// <summary>
        /// 模拟松开 → 键<br/>
        /// 该方法会模拟松开 → 键，通常用于按键操作的结束或释放事件。</summary>
        /// <param name="token">取消令牌，用于取消操作。可以在任务执行过程中通过传递取消令牌来终止操作。</param>
        /// <returns>表示操作的任务。任务会在松开操作完成后返回。</returns>
        public Task RightLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Right, token);

        #endregion

        #region F1~F12
        /// <summary>
        /// 模拟按下并松开 F1 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F1Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F1, token);

        /// <summary>
        /// 模拟按下 F1 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F1PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F1, token);

        /// <summary>
        /// 模拟松开 F1 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F1LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F1, token);

        /// <summary>
        /// 模拟按下并松开 F2 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F2Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F2, token);

        /// <summary>
        /// 模拟按下 F2 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F2PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F2, token);

        /// <summary>
        /// 模拟松开 F2 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F2LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F2, token);

        /// <summary>
        /// 模拟按下并松开 F3 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F3Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F3, token);

        /// <summary>
        /// 模拟按下 F3 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F3PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F3, token);

        /// <summary>
        /// 模拟松开 F3 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F3LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F3, token);

        /// <summary>
        /// 模拟按下并松开 F4 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F4Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F4, token);

        /// <summary>
        /// 模拟按下 F4 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F4PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F4, token);

        /// <summary>
        /// 模拟松开 F4 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F4LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F4, token);

        /// <summary>
        /// 模拟按下并松开 F5 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F5Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F5, token);

        /// <summary>
        /// 模拟按下 F5 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F5PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F5, token);

        /// <summary>
        /// 模拟松开 F5 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F5LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F5, token);

        /// <summary>
        /// 模拟按下并松开 F6 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F6Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F6, token);

        /// <summary>
        /// 模拟按下 F6 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F6PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F6, token);

        /// <summary>
        /// 模拟松开 F6 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F6LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F6, token);

        /// <summary>
        /// 模拟按下并松开 F7 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F7Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F7, token);

        /// <summary>
        /// 模拟按下 F7 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F7PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F7, token);

        /// <summary>
        /// 模拟松开 F7 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F7LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F7, token);

        /// <summary>
        /// 模拟按下并松开 F8 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F8Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F8, token);

        /// <summary>
        /// 模拟按下 F8 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F8PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F8, token);

        /// <summary>
        /// 模拟松开 F8 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F8LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F8, token);

        /// <summary>
        /// 模拟按下并松开 F9 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F9Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F9, token);

        /// <summary>
        /// 模拟按下 F9 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F9PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F9, token);

        /// <summary>
        /// 模拟松开 F9 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F9LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F9, token);

        /// <summary>
        /// 模拟按下并松开 F10 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F10Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F10, token);

        /// <summary>
        /// 模拟按下 F10 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F10PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F10, token);

        /// <summary>
        /// 模拟松开 F10 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F10LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F10, token);

        /// <summary>
        /// 模拟按下并松开 F11 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F11Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F11, token);

        /// <summary>
        /// 模拟按下 F11 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F11PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F11, token);

        /// <summary>
        /// 模拟松开 F11 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F11LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F11, token);

        /// <summary>
        /// 模拟按下并松开 F12 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F12Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.F12, token);

        /// <summary>
        /// 模拟按下 F12 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F12PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F12, token);

        /// <summary>
        /// 模拟松开 F12 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task F12LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F12, token);
        #endregion

        #region 数字

        /// <summary>
        /// 模拟按下并松开小键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad0Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad0, token);

        /// <summary>
        /// 模拟按下小键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad0PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad0, token);

        /// <summary>
        /// 模拟松开小键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad0LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad0, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad1Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad1, token);

        /// <summary>
        /// 模拟按下小键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad1PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad1, token);

        /// <summary>
        /// 模拟松开小键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad1LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad1, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad2Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad2, token);

        /// <summary>
        /// 模拟按下小键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad2PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad2, token);

        /// <summary>
        /// 模拟松开小键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad2LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad2, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad3Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad3, token);

        /// <summary>
        /// 模拟按下小键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad3PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad3, token);

        /// <summary>
        /// 模拟松开小键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad3LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad3, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad4Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad4, token);

        /// <summary>
        /// 模拟按下小键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad4PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad4, token);

        /// <summary>
        /// 模拟松开小键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad4LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad4, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad5Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad5, token);

        /// <summary>
        /// 模拟按下小键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad5PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad5, token);

        /// <summary>
        /// 模拟松开小键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad5LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad5, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad6Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad6, token);

        /// <summary>
        /// 模拟按下小键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad6PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad6, token);

        /// <summary>
        /// 模拟松开小键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad6LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad6, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad7Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad7, token);

        /// <summary>
        /// 模拟按下小键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad7PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad7, token);

        /// <summary>
        /// 模拟松开小键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad7LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad7, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad8Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad8, token);

        /// <summary>
        /// 模拟按下小键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad8PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad8, token);

        /// <summary>
        /// 模拟松开小键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad8LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad8, token);

        /// <summary>
        /// 模拟按下并松开小键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad9Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.NumPad9, token);

        /// <summary>
        /// 模拟按下小键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad9PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.NumPad9, token);

        /// <summary>
        /// 模拟松开小键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NumPad9LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.NumPad9, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D0Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D0, token);

        /// <summary>
        /// 模拟按下主键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D0PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D0, token);

        /// <summary>
        /// 模拟松开主键盘数字键 0
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D0LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D0, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D1Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D1, token);

        /// <summary>
        /// 模拟按下主键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D1PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D1, token);

        /// <summary>
        /// 模拟松开主键盘数字键 1
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D1LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D1, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D2Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D2, token);

        /// <summary>
        /// 模拟按下主键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D2PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D2, token);

        /// <summary>
        /// 模拟松开主键盘数字键 2
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D2LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D2, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D3Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D3, token);

        /// <summary>
        /// 模拟按下主键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D3PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D3, token);

        /// <summary>
        /// 模拟松开主键盘数字键 3
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D3LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D3, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D4Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D4, token);

        /// <summary>
        /// 模拟按下主键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D4PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D4, token);

        /// <summary>
        /// 模拟松开主键盘数字键 4
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D4LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D4, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D5Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D5, token);

        /// <summary>
        /// 模拟按下主键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D5PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D5, token);

        /// <summary>
        /// 模拟松开主键盘数字键 5
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D5LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D5, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D6Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D6, token);

        /// <summary>
        /// 模拟按下主键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D6PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D6, token);

        /// <summary>
        /// 模拟松开主键盘数字键 6
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D6LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D6, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D7Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D7, token);

        /// <summary>
        /// 模拟按下主键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D7PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D7, token);

        /// <summary>
        /// 模拟松开主键盘数字键 7
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D7LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D7, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D8Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D8, token);

        /// <summary>
        /// 模拟按下主键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D8PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D8, token);

        /// <summary>
        /// 模拟松开主键盘数字键 8
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D8LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D8, token);

        /// <summary>
        /// 模拟按下并松开主键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D9Async(CancellationToken token = default)
            => KeyboardPressAsync(Key.D9, token);

        /// <summary>
        /// 模拟按下主键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D9PressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D9, token);

        /// <summary>
        /// 模拟松开主键盘数字键 9
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task D9LoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D9, token);

        #endregion

        #region 符号

        /// <summary>
        /// 按下并松开小键盘加号<br/>
        /// 该方法会模拟按下并松开小键盘加号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumAddAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Add, token);

        /// <summary>
        /// 按下小键盘加号<br/>
        /// 该方法会模拟按下小键盘加号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumAddPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Add, token);

        /// <summary>
        /// 松开小键盘加号<br/>
        /// 该方法会模拟松开小键盘加号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumAddLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Add, token);

        /// <summary>
        /// 按下并松开小键盘减号<br/>
        /// 该方法会模拟按下并松开小键盘减号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumSubtractAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Subtract, token);

        /// <summary>
        /// 按下小键盘减号<br/>
        /// 该方法会模拟按下小键盘减号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumSubtractPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Subtract, token);

        /// <summary>
        /// 松开小键盘减号<br/>
        /// 该方法会模拟松开小键盘减号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumSubtractLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Subtract, token);

        /// <summary>
        /// 按下并松开小键盘乘号<br/>
        /// 该方法会模拟按下并松开小键盘乘号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumMultiplyAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Multiply, token);

        /// <summary>
        /// 按下小键盘乘号<br/>
        /// 该方法会模拟按下小键盘乘号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumMultiplyPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Multiply, token);

        /// <summary>
        /// 松开小键盘乘号<br/>
        /// 该方法会模拟松开小键盘乘号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumMultiplyLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Multiply, token);

        /// <summary>
        /// 按下并松开小键盘除号<br/>
        /// 该方法会模拟按下并松开小键盘除号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumDivideAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Divide, token);

        /// <summary>
        /// 按下小键盘除号<br/>
        /// 该方法会模拟按下小键盘除号键，通常用于数字运算或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumDividePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Divide, token);

        /// <summary>
        /// 松开小键盘除号<br/>
        /// 该方法会模拟松开小键盘除号键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumDivideLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Divide, token);

        /// <summary>
        /// 按下并松开小键盘小数点<br/>
        /// 该方法会模拟按下并松开小键盘小数点键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumDecimalAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Decimal, token);

        /// <summary>
        /// 按下小键盘小数点<br/>
        /// 该方法会模拟按下小键盘小数点键，通常用于数字输入或小键盘操作。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumDecimalPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Decimal, token);

        /// <summary>
        /// 松开小键盘小数点<br/>
        /// 该方法会模拟松开小键盘小数点键，通常用于释放按键。</summary>
        /// <param name="token">取消令牌，用于取消操作。</param>
        /// <returns>表示操作的任务。</returns>
        public Task NumDecimalLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Decimal, token);

        /// <summary>
        /// 模拟按下并松开 ` ~ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemTildeAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemTilde, token);

        /// <summary>
        /// 模拟按下 ` ~ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemTildePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemTilde, token);

        /// <summary>
        /// 模拟松开 ` ~ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemTildeLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemTilde, token);

        /// <summary>
        /// 模拟按下并松开 - _ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemMinusAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemMinus, token);

        /// <summary>
        /// 模拟按下 - _ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemMinusPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemMinus, token);

        /// <summary>
        /// 模拟松开 - _ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemMinusLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemMinus, token);

        /// <summary>
        /// 模拟按下并松开 = + 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPlusAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemPlus, token);

        /// <summary>
        /// 模拟按下 = + 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPlusPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemPlus, token);

        /// <summary>
        /// 模拟松开 = + 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPlusLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemPlus, token);

        /// <summary>
        /// 模拟按下并松开 [ { 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemOpenBracketsAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemOpenBrackets, token);

        /// <summary>
        /// 模拟按下 [ { 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemOpenBracketsPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemOpenBrackets, token);

        /// <summary>
        /// 模拟松开 [ { 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemOpenBracketsLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemOpenBrackets, token);

        /// <summary>
        /// 模拟按下并松开 ] } 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemCloseBracketsAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemCloseBrackets, token);

        /// <summary>
        /// 模拟按下 ] } 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemCloseBracketsPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemCloseBrackets, token);

        /// <summary>
        /// 模拟松开 ] } 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemCloseBracketsLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemCloseBrackets, token);

        /// <summary>
        /// 模拟按下并松开 \ | 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPipeAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemPipe, token);

        /// <summary>
        /// 模拟按下 \ | 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPipePressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemPipe, token);

        /// <summary>
        /// 模拟松开 \ | 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPipeLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemPipe, token);

        /// <summary>
        /// 模拟按下并松开 ; : 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemSemicolonAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemSemicolon, token);

        /// <summary>
        /// 模拟按下 ; : 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemSemicolonPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemSemicolon, token);

        /// <summary>
        /// 模拟松开 ; : 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemSemicolonLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemSemicolon, token);

        /// <summary>
        /// 模拟按下并松开 ' " 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemQuotesAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemQuotes, token);

        /// <summary>
        /// 模拟按下 ' " 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemQuotesPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemQuotes, token);

        /// <summary>
        /// 模拟松开 ' " 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemQuotesLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemQuotes, token);

        /// <summary>
        /// 模拟按下并松开 , ＜ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemCommaAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemComma, token);

        /// <summary>
        /// 模拟按下 , ＜ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemCommaPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemComma, token);

        /// <summary>
        /// 模拟松开 , ＜ 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemCommaLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemComma, token);

        /// <summary>
        /// 模拟按下并松开 . > 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPeriodAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemPeriod, token);

        /// <summary>
        /// 模拟按下 . > 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPeriodPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemPeriod, token);

        /// <summary>
        /// 模拟松开 . > 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemPeriodLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemPeriod, token);

        /// <summary>
        /// 模拟按下并松开 / ? 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemQuestionAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.OemQuestion, token);

        /// <summary>
        /// 模拟按下 / ? 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemQuestionPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.OemQuestion, token);

        /// <summary>
        /// 模拟松开 / ? 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OemQuestionLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.OemQuestion, token);

        #endregion

        #region 字母
        /// <summary>
        /// 模拟按下并松开键盘上的 A 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task AAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.A, token);

        /// <summary>
        /// 模拟按下键盘上的 A 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task APressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.A, token);

        /// <summary>
        /// 模拟松开键盘上的 A 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ALoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.A, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 B 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task BAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.B, token);

        /// <summary>
        /// 模拟按下键盘上的 B 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task BPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.B, token);

        /// <summary>
        /// 模拟松开键盘上的 B 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task BLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.B, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 C 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task CAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.C, token);

        /// <summary>
        /// 模拟按下键盘上的 C 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task CPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.C, token);

        /// <summary>
        /// 模拟松开键盘上的 C 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task CLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.C, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 D 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task DAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.D, token);

        /// <summary>
        /// 模拟按下键盘上的 D 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task DPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.D, token);

        /// <summary>
        /// 模拟松开键盘上的 D 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task DLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.D, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 E 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task EAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.E, token);

        /// <summary>
        /// 模拟按下键盘上的 E 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task EPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.E, token);

        /// <summary>
        /// 模拟松开键盘上的 E 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ELoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.E, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 F 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task FAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.F, token);

        /// <summary>
        /// 模拟按下键盘上的 F 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task FPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.F, token);

        /// <summary>
        /// 模拟松开键盘上的 F 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task FLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.F, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 G 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task GAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.G, token);

        /// <summary>
        /// 模拟按下键盘上的 G 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task GPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.G, token);

        /// <summary>
        /// 模拟松开键盘上的 G 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task GLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.G, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 H 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task HAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.H, token);

        /// <summary>
        /// 模拟按下键盘上的 H 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task HPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.H, token);

        /// <summary>
        /// 模拟松开键盘上的 H 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task HLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.H, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 I 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task IAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.I, token);

        /// <summary>
        /// 模拟按下键盘上的 I 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task IPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.I, token);

        /// <summary>
        /// 模拟松开键盘上的 I 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ILoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.I, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 J 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task JAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.J, token);

        /// <summary>
        /// 模拟按下键盘上的 J 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task JPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.J, token);

        /// <summary>
        /// 模拟松开键盘上的 J 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task JLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.J, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 K 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task KAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.K, token);

        /// <summary>
        /// 模拟按下键盘上的 K 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task KPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.K, token);

        /// <summary>
        /// 模拟松开键盘上的 K 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task KLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.K, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 L 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task LAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.L, token);

        /// <summary>
        /// 模拟按下键盘上的 L 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task LPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.L, token);

        /// <summary>
        /// 模拟松开键盘上的 L 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task LLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.L, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 M 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task MAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.M, token);

        /// <summary>
        /// 模拟按下键盘上的 M 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task MPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.M, token);

        /// <summary>
        /// 模拟松开键盘上的 M 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task MLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.M, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 N 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.N, token);

        /// <summary>
        /// 模拟按下键盘上的 N 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.N, token);

        /// <summary>
        /// 模拟松开键盘上的 N 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task NLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.N, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 O 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.O, token);

        /// <summary>
        /// 模拟按下键盘上的 O 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.O, token);

        /// <summary>
        /// 模拟松开键盘上的 O 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task OLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.O, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 P 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task PAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.P, token);

        /// <summary>
        /// 模拟按下键盘上的 P 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task PPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.P, token);

        /// <summary>
        /// 模拟松开键盘上的 P 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task PLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.P, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 Q 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task QAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Q, token);

        /// <summary>
        /// 模拟按下键盘上的 Q 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task QPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Q, token);

        /// <summary>
        /// 模拟松开键盘上的 Q 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task QLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Q, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 R 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task RAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.R, token);

        /// <summary>
        /// 模拟按下键盘上的 R 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task RPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.R, token);

        /// <summary>
        /// 模拟松开键盘上的 R 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task RLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.R, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 S 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task SAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.S, token);

        /// <summary>
        /// 模拟按下键盘上的 S 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task SPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.S, token);

        /// <summary>
        /// 模拟松开键盘上的 S 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task SLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.S, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 T 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task TAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.T, token);

        /// <summary>
        /// 模拟按下键盘上的 T 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task TPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.T, token);

        /// <summary>
        /// 模拟松开键盘上的 T 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task TLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.T, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 U 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task UAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.U, token);

        /// <summary>
        /// 模拟按下键盘上的 U 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task UPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.U, token);

        /// <summary>
        /// 模拟松开键盘上的 U 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ULoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.U, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 V 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task VAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.V, token);

        /// <summary>
        /// 模拟按下键盘上的 V 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task VPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.V, token);

        /// <summary>
        /// 模拟松开键盘上的 V 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task VLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.V, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 W 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task WAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.W, token);

        /// <summary>
        /// 模拟按下键盘上的 W 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task WPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.W, token);

        /// <summary>
        /// 模拟松开键盘上的 W 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task WLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.W, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 X 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task XAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.X, token);

        /// <summary>
        /// 模拟按下键盘上的 X 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task XPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.X, token);

        /// <summary>
        /// 模拟松开键盘上的 X 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task XLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.X, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 Y 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task YAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Y, token);

        /// <summary>
        /// 模拟按下键盘上的 Y 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task YPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Y, token);

        /// <summary>
        /// 模拟松开键盘上的 Y 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task YLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Y, token);

        /// <summary>
        /// 模拟按下并松开键盘上的 Z 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ZAsync(CancellationToken token = default)
            => KeyboardPressAsync(Key.Z, token);

        /// <summary>
        /// 模拟按下键盘上的 Z 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ZPressAsync(CancellationToken token = default)
            => KeyboardPressKeyAsync(Key.Z, token);

        /// <summary>
        /// 模拟松开键盘上的 Z 键
        /// </summary>
        /// <param name="token">取消令牌，用于取消操作</param>
        /// <returns>表示操作的任务</returns>
        public Task ZLoosenAsync(CancellationToken token = default)
            => KeyboardLoosenKeyAsync(Key.Z, token);


        /// <summary>
        /// 模拟按下并释放功能键（F1-F12）
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
        /// 按下常见的字母、数字和符号键
        /// </summary>
        /// <param name="data">要输入的字母、数字及常见符号</param>
        /// <param name="token">可选的 CancellationToken，用于取消操作</param>
        /// <returns>一个表示操作的 Task</returns>
        public async Task KeyboardPressCommonKeyAsync(string data, CancellationToken token = default)
        {
            foreach (var dt in data)
            {
                if (token.IsCancellationRequested)
                    return;

                if (ConfigModel.SymbolMap.TryGetValue(dt, out var info))
                {
                    await ToggleCaseAsync(info.toggleCase);

                    if (info.key2 == null)
                    {
                        await KeyboardPressAsync(info.key1, token);
                    }
                    else if (info.key2 != null)
                    {
                        await KeyboardPressAsync(info.key2.GetValueOrDefault(), info.key1, token);
                    }
                }
                await Task.Delay(ConfigModel.RestTime, token);
            }
        }
        #endregion



        #region 私有
        /// <summary>
        /// 同时按下两个键（Key 枚举类型），用于组合键操作<br/>
        /// 先按下 key1，再按下 key2，最后松开 key1<br/>
        /// 例如 Ctrl + C，可以传入 Key.LeftCtrl, Key.C
        /// </summary>
        /// <param name="key1">第一个按下的键（修饰键，如 Ctrl/Shift/Alt）</param>
        /// <param name="key2">第二个按下的键（主操作键，如字母或数字）</param>
        /// <param name="token">可选的 CancellationToken，用于取消操作</param>
        /// <returns>一个表示操作的 Task</returns>
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
        /// <returns>一个表示操作的 Task</returns>
        private async Task KeyboardPressAsync(byte key1, byte key2, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key1, token);
            await KeyboardPressAsync(key2, token);
            await KeyboardLoosenKeyAsync(key1, token);
        }

        /// <summary>
        /// 模拟按下并释放功能键
        /// </summary>
        /// <param name="key">功能键字符串，例如 "F1"</param>
        /// <param name="token">取消令牌（可选）</param>
        private async Task KeyboardPressAsync(Key key, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key, token);
            await Task.Delay(ConfigModel.RestTime, token);
            await KeyboardLoosenKeyAsync(key, token);
        }

        /// <summary>
        /// 模拟按下并释放功能键
        /// </summary>
        /// <param name="key">功能键字符串，例如 "F1"</param>
        /// <param name="token">取消令牌（可选）</param>
        private async Task KeyboardPressAsync(byte key, CancellationToken token = default)
        {
            await KeyboardPressKeyAsync(key, token);
            await Task.Delay(ConfigModel.RestTime, token);
            await KeyboardLoosenKeyAsync(key, token);
        }

        /// <summary>
        /// 获取按键状态
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        /// <returns>按下返回true，未按下返回false</returns>
        private Task<bool> GetKeyStateAsync(Key key, CancellationToken token = default)
        {
            byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
            return Task.FromResult(Win32.GetKeyState(vk) == 1);
        }

        /// <summary>
        /// 按下
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        private async Task KeyboardPressKeyAsync(byte key, CancellationToken token = default)
        {
            Win32.keybd_event(key, 0, 0, 0);
            await Task.Delay(ConfigModel.RestTime, token);
        }
        /// <summary>
        /// 松开
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        private async Task KeyboardLoosenKeyAsync(byte key, CancellationToken token = default)
        {
            Win32.keybd_event(key, 0, Win32.KEYEVENTF_KEYUP, 0);
            await Task.Delay(ConfigModel.RestTime, token);
        }

        /// <summary>
        /// 按下
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        private async Task KeyboardPressKeyAsync(Key key, CancellationToken token = default)
        {
            byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
            Win32.keybd_event(vk, 0, 0, 0);
            await Task.Delay(ConfigModel.RestTime, token);
        }
        /// <summary>
        /// 松开
        /// </summary>
        /// <param name="key">键值</param>
        /// <param name="token">取消通知</param>
        private async Task KeyboardLoosenKeyAsync(Key key, CancellationToken token = default)
        {
            byte vk = (byte)KeyInterop.VirtualKeyFromKey(key);
            Win32.keybd_event(vk, 0, Win32.KEYEVENTF_KEYUP, 0);
            await Task.Delay(ConfigModel.RestTime, token);
        }
        #endregion
        #endregion
    }
}
