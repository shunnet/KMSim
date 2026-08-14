<h1 align="center">⌨️ Snet.KMSim</h1>

<p align="center">
  <img width="120" height="120" src="https://api.snet.cn/pic/nuget.png" alt="Snet Logo"/>
</p>

<p align="center">
  <b>A programmable keyboard & mouse simulator for Windows</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-blue?logo=dotnet"/>
  <img src="https://img.shields.io/badge/platform-Windows-success?logo=windows"/>
  <img src="https://img.shields.io/badge/license-MIT-green"/>
  <img src="https://img.shields.io/github/stars/shunnet/KMSim?style=social"/>
</p>

<p align="center">
  <a href="https://snet.cn"><b>🌐 Website</b></a> ·
  <a href="https://github.com/shunnet/KMSim"><b>📦 GitHub</b></a> ·
  <a href="https://Snet.cn/72VRn"><b>🎬 Demo</b></a>
</p>

<p align="center">
  English | 📖 <a href="README.md"><b>简体中文</b></a>
</p>

## ✨ Introduction

**KMSim** is a **programmable keyboard & mouse simulator for Windows**, built for automated testing, repetitive operations and complex scripted control. It is powered by Win32 APIs (`SetCursorPos` / `mouse_event` / `keybd_event`) with no driver dependency.

## ⚙️ Core Features

| Feature | Description |
|---------|-------------|
| ⌨️ **Keyboard Simulation** | Single keys, key combinations (Ctrl+...), delayed input |
| 🖱️ **Mouse Control** | Move, single click (left / middle / right), wheel scrolling |
| 📜 **Scripting** | Custom scripts for complex automation flows |
| 🎨 **Highly Customizable** | Rich configuration options for different scenarios |
| 📋 **Clipboard Operations** | Copy / paste / cut content, Ctrl-key style shortcuts |

## 📜 Script System

Scripts use the **`Command` or `Command = Parameter`** syntax, with loop and nested-call support:

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
CopyContentAsync = Welcome to Snet.cn KMSim
PasteAsync

While = true
PasteAsync
OemCommaAsync
DelayAsync = 3000
EnterAsync
```

> 💡 Nested calls: wrap space-containing arguments in `‹ ›` —
> `MoveWindowsHandleAsync = GetWindowsHandleAsync‹notepad aaa bbb› 0 0 1000 1000`

## 📦 Installation & Usage

### 1️⃣ Clone

```bash
git clone https://github.com/shunnet/KMSim.git
cd KMSim
```

### 2️⃣ Build

Open `Snet.Windows.KMSim.slnx` with **Visual Studio**, select Debug or Release, then build.

### 3️⃣ Run

Launch `Snet.Windows.KMSim.exe` from the output directory and write/execute scripts in the UI.

## 📚 Resources & Community

| Channel | Link |
|---------|------|
| 🎬 **Demo Video** | [Watch](https://Snet.cn/72VRn) |
| 🌐 **Website** | [snet.cn](https://snet.cn) |
| 🐛 **Issues** | [GitHub Issues](https://github.com/shunnet/KMSim/issues) — bug reports & feature requests |
| ⭐ **Star** | If this project helps you, please give it a Star ❤️ |

## 🙏 Acknowledgements

- 🌐 [Snet.cn](https://snet.cn)
- 🎨 [Snet.Windows.Controls](https://github.com/shunnet/WpfMUI) — WPF controls

## 📜 License

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)

This project is licensed under the **MIT** License — free to use, modify and distribute.

📄 See the [LICENSE](LICENSE) file for the full terms.

> ⚠️ The software is provided "as is", without warranty of any kind.

## 📈 Star History

<a href="https://www.star-history.com/?repos=shunnet%2FKMSim&type=date&legend=bottom-right">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=shunnet/KMSim&type=date&theme=dark&legend=bottom-right"/>
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=shunnet/KMSim&type=date&legend=bottom-right"/>
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=shunnet/KMSim&type=date&legend=bottom-right"/>
 </picture>
</a>
