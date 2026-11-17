# Prefab Context Menu

一个轻量的 Unity 编辑器插件，给 **Hierarchy / Project** 的右键菜单增加两个常用操作，方便在 Unity 与 AI 之间传递「到底是哪个 Prefab」这件事。

两个菜单项都固定在右键菜单的**最下方**。

| 菜单 | 位置 | 作用 |
| --- | --- | --- |
| `定位Project位置` | Hierarchy 右键 | 在 Project 窗口选中、滚动并高亮该节点对应的 Prefab 资源（支持场景中的 Prefab 实例、Prefab 编辑模式） |
| `复制信息给AI` | Project 右键 / Hierarchy 右键 | 把精简的 Prefab 信息（资源路径 + GUID，实例额外带场景）复制到剪贴板，直接粘贴给 AI 即可让它定位到具体文件 |

## 安装

### 方式一：Package Manager（推荐）

1. 打开 `Window > Package Manager`
2. 点击左上角 `+`，选择 **Add package from git URL...**
3. 填入：

```
https://github.com/Tunied/unity-prefab-context-menu.git
```

### 方式二：manifest.json

编辑工程内 `Packages/manifest.json`，在 `dependencies` 中加入：

```json
"com.oldking.prefab-context-menu": "https://github.com/Tunied/unity-prefab-context-menu.git"
```

## 使用

### 定位Project位置

在 Hierarchy 里右键任意节点 → 菜单最下方 `定位Project位置`。

- 选中场景中的 Prefab 实例（包括子节点）时，会定位到它所属的最外层 Prefab 资源；
- 在 Prefab 编辑模式下，会定位到当前正在编辑的 Prefab；
- 多选时会同时选中多个 Prefab 资源，并在 Project 窗口定位第一个；
- 如果对象不是 Prefab，菜单项会隐藏。

实现上会先清掉 Project 窗口里残留的搜索/文件夹筛选状态，再通过 `ProjectBrowser.FrameObject` 定位，避免「明明点了却没跳过去」。

### 复制信息给AI

在 Project 窗口右键 Prefab，或在 Hierarchy 右键 Prefab（实例 / 编辑模式均可）→ 菜单最下方 `复制信息给AI`。

复制到剪贴板的内容非常精简：

```text
Unity Prefab: Assets/2D Explosion VFX/Prefabs/已导出_Explosion2.prefab
GUID: e8976d99dfc8e08468e219e00953f276
```

从 Hierarchy 复制场景实例时会多一行场景路径：

```text
Unity Prefab: Assets/2D Explosion VFX/Prefabs/已导出_Explosion2.prefab
GUID: e8976d99dfc8e08468e219e00953f276
场景: Assets/Scenes/Main.unity
```

多选时会按顺序输出多组，组之间空一行。路径 + GUID 已足够让 AI 精确定位文件，不必再把层级、组件列表等冗余信息塞进剪贴板。

## 兼容性

- **Unity 2020.3 及以上**
- 已在 **Unity 6000.3.10f1** 实测通过（Unity 6.3 起 Project 窗口改用 `EntityId`，代码已做反射兼容，同时保留旧版 `instanceID` 分支）
- 纯编辑器脚本，不会进入运行时构建

## 注意事项

- 本插件与工程内同名的 `Assets/Editor/PrefabContextMenu.cs` 脚本**不要同时存在**，否则同一个菜单路径会被注册两次。
- 菜单项通过 Unity 的优先级机制排在最后（`10000 / 10001`），不会打乱原有右键菜单顺序。

## License

MIT，见 [LICENSE](LICENSE)。
