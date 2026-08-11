<h1 align="center">⌨️ Snet.KMSim</h1>

<p align="center">
  <img width="120" height="120" src="https://api.snet.cn/pic/nuget.png" alt="Snet Logo"/>
</p>

<p align="center">
  <b>Windows 平台可编程键鼠模拟器</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet"/>
  <img src="https://img.shields.io/badge/platform-Windows-success?logo=windows"/>
  <img src="https://img.shields.io/badge/license-MIT-green"/>
  <img src="https://img.shields.io/github/stars/shunnet/KMSim?style=social"/>
</p>

<p align="center">
  <a href="https://snet.cn"><b>🌐 官方网站</b></a> ·
  <a href="https://github.com/shunnet/KMSim"><b>📦 GitHub</b></a> ·
  <a href="https://Snet.cn/72VRn"><b>🎬 演示视频</b></a>
</p>

<p align="center">
  📖 <a href="README.en.md"><b>English</b></a> | 简体中文
</p>

## ✨ 项目简介

**KMSim** 是一款 **Windows 下可编程键鼠模拟器**，适用于自动化测试、重复性操作与复杂脚本化控制等场景。底层基于 Win32 API（`SetCursorPos` / `mouse_event` / `keybd_event`）实现，无驱动依赖。

## ⚙️ 核心功能

| 功能 | 说明 |
|------|------|
| ⌨️ **键盘模拟** | 单键、组合键（Ctrl+...）、延迟输入等操作 |
| 🖱️ **鼠标控制** | 移动、单击（左 / 中 / 右）、滚轮滚动 |
| 📜 **脚本化控制** | 自定义脚本实现复杂自动化流程 |
| 🎨 **高度可定制** | 丰富的配置选项，适应不同场景 |
| 📋 **剪贴板操作** | 复制 / 粘贴 / 剪切内容，模拟 Ctrl 键快捷输入 |

## 📜 脚本系统

脚本采用 **`指令` 或 `指令 = 参数`** 语法，支持循环与嵌套调用：

```text
CopyContentAsync = 记事本
LWinAsync
DelayAsync = 3000
PasteAsync
DelayAsync = 3000
EnterAsync
DelayAsync = 3000
SelectAllAsync
DelayAsync = 5000
BackAsync
DelayAsync = 5000
CopyContentAsync = 欢迎使用 Snet.cn 键鼠模拟器
PasteAsync

While = true
PasteAsync
OemCommaAsync
DelayAsync = 3000
EnterAsync
```

> 💡 嵌套调用：使用 `‹ ›` 包裹含空格的整体参数 ——
> `MoveWindowsHandleAsync = GetWindowsHandleAsync‹notepad aaa bbb› 0 0 1000 1000`

## 📦 安装与使用

### 1️⃣ 克隆仓库

```bash
git clone https://github.com/shunnet/KMSim.git
cd KMSim
```

### 2️⃣ 编译项目

使用 **Visual Studio** 打开 `Snet.Windows.KMSim.slnx`，选择 Debug 或 Release 构建。

### 3️⃣ 运行程序

构建完成后运行输出目录中的 `Snet.Windows.KMSim.exe`，在界面中编写并执行脚本。

## 📚 资源与社区

| 渠道 | 链接 |
|------|------|
| 🎬 **演示视频** | [点击观看](https://Snet.cn/72VRn) |
| 🌐 **官方网站** | [snet.cn](https://snet.cn) |
| 🐛 **Issues** | [GitHub Issues](https://github.com/shunnet/KMSim/issues) — 反馈 Bug 或功能建议 |
| ⭐ **Star** | 如果这个项目对你有帮助，请点亮 Star 支持我们 ❤️ |

## 🙏 致谢

- 🌐 [Snet.cn](https://snet.cn)
- 🎨 [Snet.Windows.Controls](https://github.com/shunnet/WpfMUI) — WPF 控件库

## 📜 License

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)

本项目基于 **MIT** 开源协议 —— 自由使用、修改、分发。

📄 完整条款请阅读 [LICENSE](LICENSE) 文件。

> ⚠️ 软件按「原样」提供，作者不对使用后果承担责任。

## 📈 Star History

<a href="https://www.star-history.com/?repos=shunnet%2FKMSim&type=date&legend=bottom-right">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=shunnet/KMSim&type=date&theme=dark&legend=bottom-right"/>
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=shunnet/KMSim&type=date&legend=bottom-right"/>
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=shunnet/KMSim&type=date&legend=bottom-right"/>
 </picture>
</a>
