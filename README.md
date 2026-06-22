# One-Click-Translation

一个基于 WPF 的 Windows 桌面翻译工具：选中文字后按快捷键，自动调用有道翻译并在选区附近浮层展示结果。

## 功能特性

- 快捷翻译：全局监听快捷键，复制当前选中文本并翻译
- 自定义触发键：支持常用预设 + 自定义录制（含右键）
- 语种切换：内置常用语种，支持一键交换源/目标语言
- 托盘运行：可最小化到托盘，支持从托盘恢复或退出
- 开机启动：可选开启
- API 配置：图形界面配置并测试有道 AppKey / AppSecret

## 技术栈

- .NET 8 (`net8.0-windows`)
- WPF
- Win32 Hook（全局键盘/鼠标监听）
- 有道智云文本翻译 API

## 环境要求

- Windows 10/11
- .NET SDK 8.0+
- 可访问有道翻译 API

## 快速开始

```powershell
dotnet restore ".\CtrlTranslator.App\CtrlTranslator.App.csproj"
dotnet run --project ".\CtrlTranslator.App\CtrlTranslator.App.csproj"
```

首次启动后请点击「配置 API 密钥」，填入有道密钥。

## 使用说明

1. 在主界面开启「自动翻译」
2. 选择触发快捷键（默认 `Ctrl`）
3. 配置源语种与目标语种（默认 `英语 -> 简体中文`）
4. 在任意应用中选中文字，按快捷键触发翻译

## 配置项

配置文件自动保存到：

`%AppData%\CtrlTranslator\settings.json`

主要字段包括：

- `AutoTranslateEnabled`
- `TriggerHotkey`
- `SourceLanguage` / `TargetLanguage`
- `MinimizeToTrayOnClose`
- `YoudaoAppKey` / `YoudaoAppSecret`

## 打包发布

### 自包含单文件（推荐）

```powershell
dotnet publish ".\CtrlTranslator.App\CtrlTranslator.App.csproj" `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:PublishTrimmed=false
```

输出目录：

`.\CtrlTranslator.App\bin\Release\net8.0-windows\win-x64\publish\`

### 依赖本机 .NET Runtime（包更小）

```powershell
dotnet publish ".\CtrlTranslator.App\CtrlTranslator.App.csproj" -c Release --self-contained false
```

## 项目结构

```text
CtrlTranslator.App/
  Api/                 # 有道 API 客户端
  Models/              # 配置与数据模型
  Services/            # 监听、翻译编排、托盘、存储等核心服务
  ViewModels/          # 主界面状态与命令
  Views/               # 弹窗与浮层
  MainWindow.xaml      # 主窗口
  App.xaml.cs          # 应用入口与生命周期
```

## 常见问题

- **点击关闭后窗口消失了？**  
  检查「关闭最小化到托盘」是否开启；关闭该选项后点击关闭会直接退出应用。

- **为什么没有翻译结果？**  
  优先检查：是否已填写 API 密钥、当前窗口是否有可复制的选中文本、网络是否可访问有道 API。
