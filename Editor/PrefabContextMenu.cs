using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OldKing.EditorTools
{
    /// <summary>
    /// Prefab 右键菜单扩展（两个菜单项均排在右键菜单最下方）：
    /// 1. Hierarchy 右键「定位Project位置」：在 Project 窗口中定位并高亮该对象对应的 Prefab 资源。
    /// 2. Project / Hierarchy 右键「复制信息给AI」：把精简的 Prefab 信息（资源路径 + GUID，必要时带场景）写入剪贴板。
    /// </summary>
    internal static class PrefabContextMenu
    {
        /// <summary>Unity 中 priority 越大越靠后，这里固定压在菜单最下方（定位在上、复制在下）。</summary>
        private const int MenuPriorityLocate = 10000;
        private const int MenuPriorityCopy = 10001;

        private const string MenuLocateInProject = "GameObject/定位Project位置";
        private const string MenuCopyInHierarchy = "GameObject/复制信息给AI";
        private const string MenuCopyInProject = "Assets/复制信息给AI";

        #region 定位 Project 位置（Hierarchy 右键）

        [MenuItem(MenuLocateInProject, false, MenuPriorityLocate)]
        private static void LocateSelectedPrefab()
        {
            var assets = CollectPrefabAssets(Selection.gameObjects);
            if (assets.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "定位Project位置",
                    "当前选中的对象不是 Prefab（也不是 Prefab 实例）。\n请在 Hierarchy 中选择 Prefab 节点后再试。",
                    "确定");
                return;
            }

            RevealInProject(assets);
        }

        [MenuItem(MenuLocateInProject, true)]
        private static bool ValidateLocateSelectedPrefab()
        {
            return CollectPrefabAssets(Selection.gameObjects).Count > 0;
        }

        /// <summary>把选中的 GameObject 解析成对应的 Prefab 资源（去重、保持选择顺序）。</summary>
        private static List<Object> CollectPrefabAssets(GameObject[] gameObjects)
        {
            var result = new List<Object>();
            if (gameObjects == null || gameObjects.Length == 0) return result;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();

            foreach (var go in gameObjects)
            {
                if (go == null) continue;

                string path = GetPrefabAssetPath(go, stage);
                if (string.IsNullOrEmpty(path)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;

                if (!result.Contains(asset)) result.Add(asset);
            }

            return result;
        }

        /// <summary>取对象所属的 Prefab 资源路径：Prefab 编辑模式取当前编辑资源，场景实例取最近实例根。</summary>
        private static string GetPrefabAssetPath(GameObject go, PrefabStage stage)
        {
            if (go == null) return null;

            if (stage != null && stage.IsPartOfPrefabContents(go))
                return stage.assetPath;

            string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
            if (string.IsNullOrEmpty(path))
                path = AssetDatabase.GetAssetPath(go);

            return !string.IsNullOrEmpty(path) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)
                ? path
                : null;
        }

        /// <summary>在 Project 窗口中选中并定位资源。</summary>
        private static void RevealInProject(List<Object> assets)
        {
            if (assets == null || assets.Count == 0) return;

            Object primary = assets[0];
            Selection.objects = assets.ToArray();

            if (TryFrameProjectBrowser(primary)) return;

            // 兜底：拿不到 Project 窗口的内部方法时，至少把资源 Ping 出来。
            EditorGUIUtility.PingObject(primary);
        }

        /// <summary>
        /// Unity 没有公开“在 Project 窗口定位资源”的 API，这里反射调用 ProjectBrowser.FrameObject，
        /// 并兼容 EntityId（Unity 6.3+）与 int instanceID（旧版本）两种签名。
        /// </summary>
        private static bool TryFrameProjectBrowser(Object asset)
        {
            if (asset == null) return false;

            try
            {
                Type browserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
                if (browserType == null) return false;

                MethodInfo getAllBrowsers = browserType.GetMethod(
                    "GetAllProjectBrowsers",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo frameObject = browserType.GetMethod(
                    "FrameObject",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (getAllBrowsers == null || frameObject == null) return false;

                IList browsers = getAllBrowsers.Invoke(null, null) as IList;
                if (browsers == null || browsers.Count == 0) return false;

                object[] args = BuildFrameArguments(asset, frameObject);
                if (args == null) return false;

                for (int i = 0; i < browsers.Count; i++)
                {
                    EditorWindow window = browsers[i] as EditorWindow;
                    if (window == null) continue;

                    frameObject.Invoke(browsers[i], args);
                    window.Focus();
                    window.Repaint();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[定位Project位置] 无法在 Project 窗口定位资源：" + e.Message);
            }

            return false;
        }

        /// <summary>按 FrameObject(..., bool ping) 的实参类型构造参数（EntityId / int / long）。</summary>
        private static object[] BuildFrameArguments(Object asset, MethodInfo frameObject)
        {
            ParameterInfo[] parameters = frameObject.GetParameters();
            object[] args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;

                if (type == typeof(bool))
                {
                    args[i] = true;
                    continue;
                }

                if (type == typeof(int))
                {
                    args[i] = asset.GetInstanceID();
                    continue;
                }

                if (type == typeof(long))
                {
                    args[i] = (long)asset.GetInstanceID();
                    continue;
                }

                if (type.Name == "EntityId")
                {
                    MethodInfo getEntityId = typeof(Object).GetMethod(
                        "GetEntityId",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (getEntityId == null) return null;

                    args[i] = getEntityId.Invoke(asset, null);
                    continue;
                }

                return null;
            }

            return args;
        }

        #endregion

        #region 复制信息给AI（Project / Hierarchy 右键）

        [MenuItem(MenuCopyInProject, false, MenuPriorityCopy)]
        private static void CopyInfoFromProject()
        {
            CopyPrefabInfo(Selection.objects, false);
        }

        [MenuItem(MenuCopyInProject, true)]
        private static bool ValidateCopyInfoFromProject()
        {
            return CollectCopyEntries(Selection.objects, false).Count > 0;
        }

        [MenuItem(MenuCopyInHierarchy, false, MenuPriorityCopy)]
        private static void CopyInfoFromHierarchy()
        {
            CopyPrefabInfo(Selection.objects, true);
        }

        [MenuItem(MenuCopyInHierarchy, true)]
        private static bool ValidateCopyInfoFromHierarchy()
        {
            return CollectCopyEntries(Selection.objects, true).Count > 0;
        }

        private class PrefabEntry
        {
            public Object Asset;
            public string ScenePath;
        }

        private static List<PrefabEntry> CollectCopyEntries(Object[] objects, bool fromHierarchy)
        {
            var entries = new List<PrefabEntry>();
            if (objects == null || objects.Length == 0) return entries;

            var stage = PrefabStageUtility.GetCurrentPrefabStage();

            foreach (var obj in objects)
            {
                string path;
                string scenePath = null;

                if (fromHierarchy)
                {
                    GameObject go = obj as GameObject;
                    if (go == null) continue;

                    path = GetPrefabAssetPath(go, stage);
                    if (string.IsNullOrEmpty(path)) continue;

                    if (stage != null && stage.IsPartOfPrefabContents(go))
                        scenePath = "Prefab 编辑模式：" + stage.assetPath;
                    else if (go.scene != null && !string.IsNullOrEmpty(go.scene.path))
                        scenePath = go.scene.path;
                }
                else
                {
                    path = AssetDatabase.GetAssetPath(obj);
                    if (string.IsNullOrEmpty(path)) continue;
                    if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null) continue;

                if (entries.Exists(e => e.Asset == asset)) continue;

                entries.Add(new PrefabEntry { Asset = asset, ScenePath = scenePath });
            }

            return entries;
        }

        private static void CopyPrefabInfo(Object[] objects, bool fromHierarchy)
        {
            var entries = CollectCopyEntries(objects, fromHierarchy);
            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("复制信息给AI", "当前选中的对象不是 Prefab。", "确定");
                return;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) builder.AppendLine();

                var entry = entries[i];
                string assetPath = AssetDatabase.GetAssetPath(entry.Asset);
                string guid = AssetDatabase.AssetPathToGUID(assetPath);

                builder.Append("Unity Prefab: ").AppendLine(assetPath);
                builder.Append("GUID: ").AppendLine(guid);

                if (!string.IsNullOrEmpty(entry.ScenePath))
                    builder.Append("场景: ").AppendLine(entry.ScenePath);
            }

            string text = builder.ToString().TrimEnd();
            EditorGUIUtility.systemCopyBuffer = text;

            Debug.Log("[复制信息给AI] 已复制到剪贴板：\n" + text);
        }

        #endregion
    }
}
