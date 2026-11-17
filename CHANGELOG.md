# Changelog

## [1.0.0] - 2026-11-17

### Added
- Hierarchy 右键菜单「定位Project位置」：在 Project 窗口定位并高亮 Prefab 资源，兼容场景实例与 Prefab 编辑模式。
- Project / Hierarchy 右键菜单「复制信息给AI」：复制精简的 Prefab 信息（资源路径 + GUID，实例附带场景路径）到剪贴板。
- 两个菜单项通过 `MenuItem` 优先级固定在右键菜单最下方。
- 兼容 Unity 6.3 的 `EntityId` 与旧版 `instanceID` 的 `ProjectBrowser.FrameObject` 签名。
- 定位前自动清除 Project 窗口残留的搜索 / 文件夹筛选状态。
