# KotamaAcademyCitadel 小键盘绑定 Mod 备忘录（Strategy2 路线）

> 目的：把我们到目前为止“已经做过什么 / 为什么这么做 / 卡在什么地方 / 下一步需要什么工具能力”整理成一份可回溯的备忘，便于后续扩展 MCP/逆向工具链后继续推进。

## 1. 目标与现象

### 1.1 初始目标
- 让游戏的按键绑定系统支持小键盘（Numpad），尤其是 Unity Input System 控制路径（如 `<Keyboard>/numpad5`）。

### 1.2 关键现象（游戏原生）
- UI 会“暗示性”提示小键盘不可绑定（非弹窗，而是 UI 动态反馈/样式变化）。
- 即使 UI 显示“绑定成功”，实际输入仍然可能保持原默认键（需要我们强制落地到 InputAction）。
- 绑定界面存在“输入任意键”闪烁提示；我们的覆盖显示容易与之叠加。

## 2. 环境与版本信息

### 2.1 游戏与运行环境
- 游戏：`KotamaAcademyCitadel`
- Unity：2022.3.20f1（用户口述/先前确认）
- IL2CPP metadata：v29（从 `global-metadata.dat` + 工具日志确认）
- 关键文件：
  - `GameAssembly.dll`
  - `KotamaAcademyCitadel_Data\il2cpp_data\Metadata\global-metadata.dat`

### 2.2 Mod / 插件
- BepInEx：BepInEx 6（IL2CPP + .NET 6）
- 项目目录：`Modding/NumpadProbe/NumpadProbe/`
- 插件源码主文件：`Modding/NumpadProbe/NumpadProbe/NumpadProbePlugin.cs`
- 部署 DLL：
  - 目标：`BepInEx\plugins\Kotama.NumpadRebind.dll`
  - 备份：同目录 `Kotama.NumpadRebind.dll.bak.YYYYMMDD-HHMMSS`
- 日志：`BepInEx\LogOutput.log`

## 3. 总体策略：为什么选择 Strategy2

### 3.1 Strategy1（未采用/或不足）
- “让游戏原生管线接受小键盘”往往需要绕过多个过滤点、表配置、UI 资源（Sprite）映射等。
- 游戏内部对“可绑定键”可能存在硬编码/表驱动限制（不仅仅是 Input System 层面的 excludes）。

### 3.2 Strategy2（当前路线）
核心思想：**当游戏拒绝小键盘绑定（UI/逻辑层返回 false）时，我们在 Postfix 里“补做”它原本应该做的事**：
- 强制将绑定应用到运行时 Input System（InputAction/ActionMap）。
- 强制持久化 override（存储 cnfId → control path）。
- 尽量让 UI 显示合理（纯文本 fallback），并避免游戏 UI 因缺资源崩溃。

这个策略的优势：
- 不依赖游戏是否把小键盘列入“可绑定列表”。
- 不强依赖 UI 是否存在“原生小键盘图标资源”。

## 4. 已实现的 Hook 点（HarmonyPatch 清单）

以下均在 `Modding/NumpadProbe/NumpadProbe/NumpadProbePlugin.cs` 中实现。

### 4.1 让 rebinding 管线不要排除小键盘
- `InputActionRebindingExtensions.RebindingOperation.WithControlsExcluding`
  - 行为：如果 path 是 `<Keyboard>/numpad...`，则忽略该 exclude（返回原 operation，不执行原方法）。
  - 目的：避免 rebinding 操作把小键盘从候选输入里过滤掉。

### 4.2 清理游戏侧“预过滤列表”
- `EscapeGame.Input.InputBindingHelper.SwapBindingPreHookFilterKey`
  - 行为：把过滤列表中与 numpad 相关的条目移除。
  - 目的：减少“游戏先天就认为某些 key 不可用”的入口限制。

### 4.3 放行 composite override 入口
- `EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.PreCheckCompositeCanOverride`
  - 行为：当 compositeKey 是 numpad 时，强制返回 true。
  - 目的：某些 UI/复合绑定会在这里被挡掉。

### 4.4 Strategy2 的核心：绑定失败时“补做绑定”
- `EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.SetKeyboardBinding`
  - Prefix：
    - 当 formatKey 是 numpad 时强制 `playUx=false`，避免游戏播放“不可绑定”的 UI 提示动画。
  - Postfix（仅在 `__result == false` 且 formatKey 为 numpad 时触发）：
    1) 获取当前项 `keyboard._tbSettingKeyboard`，拿到 `cnfId`
    2) 冲突处理：调用 `GetAlreadyKeyboard(formatKey)`，若其他行已占用该键，先 `ResetKeyboard(...)`
    3) 应用运行时绑定：
       - `EscapeGame.Input.InputManager.RebindingBtn(inputActionName, originKey, formatKey, ActionMapTypes.PlayerControls, applyAllMaps)`
       - `EscapeGame.Input.InputManager.RebindingBtn(originKey, formatKey)`
    4) 持久化 + 广播：
       - `KeyboardBindingHelper.SaveOverride(cnfId, formatKey)`
       - `KeyboardBindingHelper.FlushRemapKeyboard({cnfId -> formatKey})`
    5) 修复菜单内部映射：
       - `__instance._View?.ResetCurrentKeyboard(formatKey, keyboard)`
    6) 退出 rebinding 状态：
       - `SwitchRebindingListen(false)`
       - `_bindingState = Origin`
    7) UI 刷新（纯文本）：
       - `keyboard.CleanCurrentBtn()`
       - `KeyboardUiSafe.TryApplyBindingTextOnly(keyboard, displayName)`

### 4.5 rebinding 状态跟踪（避免叠字）
- `EscapeGame.UI.Controls.SettingsMenuKeyboardCtrl.SwitchRebindingListen`
  - Postfix：维护 `RebindingUiState.IsRebinding` 与 `RebindingUiState.ActiveCnfId`
  - 当进入 rebinding 时调用 `KeyboardUiSafe.TryPrepareRebindingVisual(current)`：
    - `CleanCurrentBtn()`
    - 清掉我们写入过的 binding 文本，避免与“输入任意键”闪烁叠加

### 4.6 UI 显示数据层：让 numpad 有可显示的 InputDisData
- `EscapeGame.UIGen.KeyboardBindingHelper.ContainsKey`
  - 行为：当 pressTxt 是 numpad 相关时，强制返回 true（避免表查不到就判定不存在）。

- `EscapeGame.UIGen.KeyboardBindingHelper.GetInputDisData`
  - Prefix：若 pressTxt 是 numpad，则映射到一个“非 numpad 的安全查表 key”（例如把 numpad5 映射为 `"5"`）
  - Postfix：克隆 `InputDisData` 并替换 `PressTxt` 为我们自定义的显示文本（如 `Numpad 5`）
  - 目的：
    - 避免游戏查不到 numpad 对应的 sprite/图片资源
    - 避免直接改动共享表对象（克隆后修改）

### 4.7 行级 UI：SetData / SetCurrentBtn 的防崩溃与覆盖显示
- `EscapeGame.UIGen.Keyboard.SetData`
  - Prefix：若 cnfId 有保存的 overrideKey 且是 numpad，则把 `bindingData` 替换为 `NumpadDisplay.GetBindingDisData(overrideKey)`
  - Postfix：如果存在 override，则尝试纯文本覆盖；否则恢复 icon 模式

- `EscapeGame.UIGen.Keyboard.SetCurrentBtn`
  - 背景：日志反复出现崩溃链：
    - `FairyGUI.NTexture..ctor(UnityEngine.Sprite sprite)` -> `EscapeGame.UIGen.Keyboard.SetCurrentBtn(...)`
    - 这会导致 UI 状态链条中断，出现“输入任意键”残留叠加、甚至 WSAD 跳选等副作用。
  - 当前做法：
    - Prefix：若候选键是 numpad，则构造“安全”的 `changeData`（用非 numpad 查表 + PressTxt 替换），并 `return false` 跳过原方法，避免触发 FairyGUI 的 null sprite 分支。
    - Finalizer：如果仍抛出 `NTexture..ctor` 的 NRE，且候选键是 numpad，则尝试用 `TryApplyBindingTextOnly` 做一次 UI fallback，并吞掉异常（返回 null）。

## 5. UI 纯文本覆盖的实现要点（KeyboardUiSafe）

核心函数：`KeyboardUiSafe.TryApplyBindingTextOnly(EscapeGame.UIGen.Keyboard keyboard, string display)`
- 在写入文本前调用 `keyboard.CleanCurrentBtn()`，尽可能清掉“输入任意键”/旧状态残留。
- 覆盖的是 `keyboard.list`（右侧按键显示按钮）的 `title`，避免误改左侧功能名文本。
- 额外清理：
  - `keyboard.list.icon = ""`、`keyboard.list.selectedIcon = ""`
  - `keyboard.Btn.visible = false`，并尝试 `keyboard.Btn.url = ""`

备注：这条路线最终是“纯文本 UI”，即使游戏有原生图标资源也不强依赖它。

## 6. 已知问题与证据（为什么说“信息不足”）

### 6.1 关键崩溃证据（日志）
`BepInEx\LogOutput.log` 中反复出现：
- `System.NullReferenceException` at `FairyGUI.NTexture..ctor (UnityEngine.Sprite sprite)`
- 由 `EscapeGame.UIGen.Keyboard.SetCurrentBtn` 调用触发

推断：
- 游戏的 InputDisData / sprite 表对某些键（尤其是 numpad 相关映射）存在缺项或返回 null sprite。
- UI 刷新链条一旦异常，后续“清理闪烁提示 / 恢复选择状态”可能没执行，从而出现叠字、跳选等 UI 层级问题。

### 6.2 我们无法“反编译内部实现”的根因
- 游戏是 IL2CPP：不存在可直接反编译的 C# method body（Assembly-CSharp.dll 只剩壳或不存在）。
- 现阶段 MCP 工具只能做到：
  - 获取 metadata 版本、生成 dummy dll（类型/字段/方法签名）
  - 但无法直接得到 `SetCurrentBtn` 等函数的 C# 逻辑实现（只能走 native 反汇编/IDA/Ghidra 路线）。

### 6.3 现有 dnSpy MCP 的阻塞点（工具链层）
我们尝试过 `mcp__dnspy__il2cpp_dump`：
- 能识别：
  - `Metadata Version: 29`
  - `Change il2cpp version to: 29.1`
  - `Dumping... Done! Generate dummy dll... Done!`
- 但工具进程最后会因 `Console.ReadKey` 抛异常（无控制台环境），导致输出工件未能稳定落盘或无法定位输出目录。

这导致：
- 我们无法持续、可重复地使用“dummy dll + 类型系统”来做更深层的静态分析与自动化检索。

## 7. 代码与工件入口（便于快速接手）

- 源码主入口：`Modding/NumpadProbe/NumpadProbe/NumpadProbePlugin.cs`
- 关键 Patch 类：
  - `Patch_SettingsMenuKeyboardCtrl_SetKeyboardBinding`（Strategy2 核心）
  - `Patch_KeyboardBindingHelper_GetInputDisData`（显示数据兜底）
  - `Patch_Keyboard_SetData`（列表刷新）
  - `Patch_Keyboard_SetCurrentBtn`（防崩溃/当前项刷新）
- 部署 DLL：`BepInEx\plugins\Kotama.NumpadRebind.dll`
- 运行日志：`BepInEx\LogOutput.log`

## 8. 下一步（为“扩展工具”做准备）

为了真正做到“健壮、完美”，我们需要能回答这些问题（需要工具支持，不靠猜）：
- `EscapeGame.UIGen.Keyboard.SetCurrentBtn` 内部到底如何从 `InputDisData` 构造 sprite/url？
- `cfg.TbCfg.InputDisData` 的字段含义、UI 使用路径、缺省值策略是什么？
- 游戏对“可绑定键”还有哪些隐式过滤点（表、硬编码、黑名单、控制器映射层）？

因此后续要做的“工具扩展方向”大概率是：
- 可重复产出并落盘的 IL2CPP dump（dummy dll / dump.cs / method address）
- 可用的 native 反汇编/符号定位（配合 method address）
- 或者在游戏运行时做更强的 hook/trace（记录具体调用参数与 UI 子节点状态）

