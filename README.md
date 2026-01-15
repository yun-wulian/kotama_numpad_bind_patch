# KotamaAcademyCitadel 小键盘/鼠标侧键绑定补丁（IL2CPP / BepInEx6）

[English README](README.en.md)

本项目是一个 **BepInEx IL2CPP 插件**，用于修复《KotamaAcademyCitadel / Kotama Academy Citadel》里 **小键盘（Numpad）无法绑定**的问题，并额外支持 **鼠标侧键（MB4/MB5）** 绑定；同时尽量避免按键设置 UI 因资源缺失导致显示异常。

![实测截图](E.G.png)

## 功能

- 支持绑定小键盘：`Numpad 0-9`、`Numpad * / + -`、`Numpad Enter` 等。
- 支持绑定鼠标侧键：`MB4` / `MB5`（InputSystem 的 `<Mouse>/backButton` / `<Mouse>/forwardButton`）。
- 保留游戏原生的冲突/交换逻辑（我们保持 `PressTxt` 为逻辑控制路径，比如 `<Keyboard>/numpad4`）。
- 针对 UI 资源缺失做了“文本兜底显示”，避免出现卡在“输入任意键”等状态。

## 依赖

- 游戏：KotamaAcademyCitadel（Unity 2022.3，IL2CPP）
- **BepInEx 6（IL2CPP 版本）**
  - 官方 Releases（稳定版）：https://github.com/BepInEx/BepInEx/releases
  - BepInEx 构建站（推荐：IL2CPP 最新 build）：https://builds.bepinex.dev/
  - 项目页（bepinex_be）：https://builds.bepinex.dev/projects/bepinex_be
  - 已验证可用（Windows x64 / IL2CPP metadata v31）：
    - `BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752+dd0655f.zip`
    - https://builds.bepinex.dev/projects/bepinex_be/752/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.752%2Bdd0655f.zip
  - 注意：请选择 `Unity.IL2CPP` 的发行包（例如 Windows x64 的 IL2CPP 压缩包）。

## 安装

1. 安装 BepInEx 6（IL2CPP）。
2. 获取插件 DLL（自行编译或使用你本地构建出来的）：`Kotama.NumpadRebind.dll`。
3. 把 DLL 放到游戏目录：`KotamaAcademyCitadel\BepInEx\plugins\Kotama.NumpadRebind.dll`
4. 启动游戏。

## 编译（推荐）

本工程通过相对路径引用游戏目录下的 `BepInEx/core` 与 `BepInEx/interop` 程序集，因此最省事的方式是把仓库放在游戏目录的 `Modding` 下。

示例：

1. 把仓库放到（或克隆到）游戏目录：
   - `...\KotamaAcademyCitadel\Modding\kotama_numpad_bind_patch\`
2. 编译：
   - `dotnet build .\kotama_numpad_bind_patch\KotamaNumpadRebind.csproj -c Release`
3. 产物：
   - `.\kotama_numpad_bind_patch\bin\Release\net6.0\Kotama.NumpadRebind.dll`

## 常见问题（重要）

### 按键设置菜单消失 / 设置存档损坏

如果你在“按键设置界面等待输入（输入任意键）”的时候直接 `Alt+F4` 强退游戏，可能导致 **设置覆盖文件**写入中断，从而出现“按键设置界面消失/无法打开”的现象。

你可以在 **不影响游戏存档进度** 的情况下，单独重置按键/设置覆盖：

- 删除或重命名这个文件：
  - `C:\Users\<username>\AppData\LocalLow\AtomStringCompany\KotamaAcademyCitadel\<steamid>\settings.json`

推荐步骤：

1. 退出游戏。
2. 把 `settings.json` 重命名为 `settings.json.bak`（或直接删除）。
3. 重新启动游戏，文件会自动重建。

## 备注

- 本仓库不提交任何游戏文件 / BepInEx 文件 / 构建产物；已编译 DLL 请到 GitHub Releases 下载。
