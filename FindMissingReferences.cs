using UnityEngine;
using UnityEditor;
using StoryGiant.Extensions;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

//public class Entry
//{
//    public GameObject obj;
//    public int count;
//    public string name;
//    public string scene;
//    public string parent;
//    public string root;
//}

public class FindMissingReferences : EditorWindow
{
    int m_GameObjectCount = 0, m_ComponentsCount = 0, m_MissingCount = 0;
    bool m_IsActive = false;
    //List<Entry> m_Items = new List<Entry>();
    List<Found> m_Items = new List<Found>();
    Vector2 m_ScrollPos;
    int m_FrameSkipper;

    [MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References")]
    public static void ShowWindow()
    {
        var window = GetWindow(typeof(FindMissingReferences));
        window.titleContent = new GUIContent("Find Missing References");
    }

    // Copies, deletes, and recreates all instances of a component in the scene to resolve annoying (but apparently harmless) "unsupported type" errors.
    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/Re-attach Problematic Components")]
    //static void CleanUpUnsupportedTypeErrorsInScene() { CleanUpUnsupportedTypeErrorsInScene<LevelVariationController>(); }
    //static void CleanUpUnsupportedTypeErrorsInScene<T>() where T: MonoBehaviour
    //{
    //    // Find and replace "Bus" to change the component being worked on
    //    var components = Resources.FindObjectsOfTypeAll<T>();
    //    // We go up to the GameObject level because it's not a good idea to destroy components as we iterate over them.
    //    GameObject[] objs = components.Select(component => component.gameObject).ToArray();
    //    foreach (GameObject obj in objs)
    //    {
    //        PrefabType pType = PrefabUtility.GetPrefabType(obj);
    //        bool pInstance = pType == PrefabType.PrefabInstance;
    //        bool pNone = pType == PrefabType.None;
    //        // Don't change prefabs themselves, as that will result in duplicate copies of the component and other weirdness
    //        if (pInstance || pNone)
    //        {
    //            var component = obj.GetComponent<T>();
    //            UnityEditorInternal.ComponentUtility.CopyComponent(component);
    //            if (pInstance)
    //            {
    //                PrefabUtility.DisconnectPrefabInstance(obj);
    //            }
    //            DestroyImmediate(component, true);
    //            if (pInstance)
    //            {
    //                // This will bring back the destroyed component
    //                PrefabUtility.ReconnectToLastPrefab(obj);
    //                component = obj.GetComponent<T>();
    //                UnityEditorInternal.ComponentUtility.PasteComponentValues(component);
    //            }
    //            else
    //            {
    //                // must be pNone
    //                UnityEditorInternal.ComponentUtility.PasteComponentAsNew(obj);
    //            }
    //        }
    //    }
    //}

    void Update()
    {
        if (HasOpenInstances<FindMissingReferences>() && m_IsActive && ++m_FrameSkipper % 10 == 0)
        {
            FindInSelected();
            Repaint();
        }
    }

    static readonly GUILayoutOption[] LayoutWidth200 = new[] { GUILayout.Width(200) };
    static readonly GUILayoutOption[] LayoutWidth100 = new[] { GUILayout.Width(100) };

    void OnGUI()
    {
        //m_IsActive = GUILayout.Toggle(m_IsActive, "Find Missing Scripts in selected GameObjects");
        if (!m_IsActive && GUILayout.Button("Update Now"))
        {
            FindInSelected();
            SGDebug.Log(LogTag.Editor, string.Format("Searched {0} GameObjects, {1} components, found {2} missing", m_GameObjectCount, m_ComponentsCount, m_MissingCount));
        }

        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos, false, true);
        foreach (var a in m_Items)
        {
            GUILayout.BeginHorizontal();
            if (a.component)
            {
                EditorGUILayout.ObjectField("", a.component, typeof(Component), true, LayoutWidth200);
            }
            else if (a.scene == null)
            {
                EditorGUILayout.ObjectField("", a.obj, typeof(GameObject), true, LayoutWidth200);
            }
            else
            {
                EditorGUILayout.ObjectField("", a.scene, typeof(SceneAsset), true, LayoutWidth200);
            }
            //EditorGUILayout.ObjectField("", a.container, typeof(GameObject), true);
            //EditorGUILayout.LabelField($"\"{item.name}\" Amount: {item.count} Scene: {item.scene} Root: {item.root} Parent: {item.parent}");
            var info = $"{a.type} [{a.containerName}]";
            if (!string.IsNullOrEmpty(a.path))
            {
                info += $" \"{a.path}\"";
            }
            if (!string.IsNullOrEmpty(a.property))
            {
                info += " " + a.property;
            }
            //info += $"parent: {a.parent}";
            EditorGUILayout.LabelField(info);
            if (!string.IsNullOrEmpty(a.guid))
            {
                EditorGUILayout.TextField(a.guid, LayoutWidth100);
            }
            if (a.fileId != 0)
            {
                EditorGUILayout.LongField(a.fileId, LayoutWidth100);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    private void FindInSelected()
    {
        //m_GameObjectCount = 0;
        //m_ComponentsCount = 0;
        //m_MissingCount = 0;
        //
        //m_Items.Clear();
        //var selectedObjects = Selection.gameObjects.ToList();
        //if (selectedObjects.Empty())
        //{
        //    selectedObjects = DependenciesBrowser.GetAllObjectsInScene();
        //}
        //foreach (GameObject g in selectedObjects)
        //{
        //    FindInGO(g);
        //}
        //
        //var x = FindMissingComponentsInScenesAndPrefabs();
        //while (x.MoveNext())
        //{
        //    if (EditorUtility.DisplayCancelableProgressBar("Searching missing scripts in assets.", $"{x.Current?.name}", 0))
        //    {
        //        break;
        //    }
        //}
        //EditorUtility.ClearProgressBar();

        m_Items = FindMissingReferencesEverywhere();
    }

    //private static int GetMissingComponentCount(GameObject g)
    //{
    //    return g.GetComponents<Component>().Count(comp =>
    //    {
    //        // if-else for debugging.
    //        if (comp == null)
    //        {
    //            return true;
    //        }
    //        else
    //        {
    //            return false;
    //        }
    //    });
    //}

    //private void FindInGO(GameObject g)
    //{
    //    AddIfHasMissing(g);
    //    foreach (Transform t in g.transform)
    //    {
    //        FindInGO(t.gameObject);
    //    }
    //}

    //private void AddIfHasMissing(GameObject g)
    //{
    //    int missingCount = GetMissingComponentCount(g);
    //    if (missingCount > 0)
    //    {
    //        m_Items.Add(new Entry{
    //            obj = g,
    //            count = missingCount,
    //            name = g.name,
    //            parent = g.transform.parent?.name,
    //            root = g.transform.root.name,
    //            scene = g.scene.name,
    //        });
    //    }
    //    ++m_GameObjectCount;
    //}

    //private IEnumerator<GameObject> FindMissingComponentsInScenesAndPrefabs()
    //{
    //    yield return null;
    //    foreach (var g in ForAllObjectsInScenesAndPrefabs("TextExpress"))
    //    {
    //        AddIfHasMissing(g);
    //        yield return g;
    //    }
    //}

    public static string SaveScenes()
    {
        var currentScenePath = SceneManager.GetActiveScene().path;

        if (string.IsNullOrWhiteSpace(currentScenePath))
        {
            switch (EditorUtility.DisplayDialogComplex("WARNING", "You must save the current scene before starting to find missing references in the project.", "Save", "Discard", "Cancel"))
            {
                case 2:
                    return null;

                case 1:
                    if (EditorSceneManager.SaveOpenScenes())
                    {
                        currentScenePath = SceneManager.GetActiveScene().path;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("WARNING",
                            "Could not start finding missing references in the project because the current scene is not saved.",
                            "Ok");
                        {
                            return null;
                        }
                    }
                    break;
            }
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return null;
        }

        return currentScenePath;
    }

    private static IEnumerable<GameObject> ForAllObjectsInScenesAndPrefabs(string projectName)
    {
        var currentScenePath = FindMissingReferences.SaveScenes();
        if (currentScenePath == null)
        {
            yield break;
        }

        foreach (var scene in ForAllScenes(projectName))
        {
            foreach (var c in scene.EnumerateAllChildren())
            {
                yield return c.gameObject;
            }
        }

        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath);
        }

        foreach (var prefab in ForAllPrefabs(projectName))
        {
            foreach (var c in prefab.transform.EnumerateAllChildren())
            {
                yield return c.gameObject;
            }
        }
    }

    private static IEnumerable<string> DirSearch(string sDir)
    {
        string[] files = null;
        try
        {
            files = Directory.GetFiles(sDir);
        }
        catch (Exception e)
        {
            SGDebug.Log(LogTag.System, e.Message);
        }

        if (files != null)
        {
            foreach (var f in files)
            {
                yield return f;
            }
        }

        string[] dirs = null;
        try
        {
            dirs = Directory.GetDirectories(sDir);
        }
        catch (Exception e)
        {
            SGDebug.Log(LogTag.System, e.Message);
        }

        if (dirs != null)
        {
            foreach (var d in dirs)
            {
                foreach (var r in DirSearch(d))
                {
                    yield return r;
                }
            }
        }
    }

    private static IEnumerable<Scene> ForAllScenes(string projectName)
    {
        var fileNames = DirSearch($"Assets/{projectName}").Where(f => f.EndsWith(".unity")).ToList();
        foreach (var fileName in fileNames)
        {
            var fullPath = fileName.Replace(@"\", "/");
            string assetPath = fullPath.Replace(Application.dataPath, "Assets");
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(assetPath);
            }
            catch (Exception e) { }
            if (scene.IsValid())
            {
                SGDebug.Log(LogTag.System, $"Loaded scene \"{assetPath}\"");
                yield return scene;
            }
            else
            {
                SGDebug.LogWarning(LogTag.System, $"Could not load scene \"{assetPath}\"");
            }
            //var op = SceneManager.UnloadSceneAsync(scene);
            //var counter = 0;
            //while (op != null && !op.isDone && counter < 100)
            //{
            //    ++counter;
            //}
        }
    }

    private static IEnumerable<GameObject> ForAllPrefabs(string projectName)
    {
        var fileNames = DirSearch($"Assets/{projectName}").Where(f => f.EndsWith(".prefab")).ToList();
        foreach (var fileName in fileNames)
        {
            var fullPath = fileName.Replace(@"\", "/");
            string assetPath = fullPath.Replace(Application.dataPath, "Assets");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                yield return prefab;
            }
        }
    }

    private class ObjectData
    {
        public float ExpectedProgress;
        public GameObject GameObject;
    }

    public class Found
    {
        public GameObject obj;
        public SceneAsset scene;
        public GameObject container;
        //public string name;
        public string type;
        public Component component;
        public string property;
        public string guid;
        public long fileId;
        //public string parent;
        public string path;
        public string containerName;
    }

    private static List<Found> m_Entries = new List<Found>();

    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References")] // Everywhere
    public static List<Found> FindMissingReferencesEverywhere()
    {
        var currentScenePath = SaveScenes();
        if (currentScenePath == null)
        {
            return null;
        }

        var scenes = EditorBuildSettings.scenes;
        var progressWeight = 1 / (float)(scenes.Length + 1);

        //clearConsole();
        m_Entries.Clear();

        var count = 0;
        var wasCancelled = true;
        var currentProgress = 0f;
        foreach (var scene in scenes)
        {
            Scene openScene;
            try
            {
                openScene = EditorSceneManager.OpenScene(scene.path);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    $"Could not open scene at path \"{scene.path}\". This scene was added to the build, and it's possible that it has been deleted: Error: {ex.Message}");
                continue;
            }

            count += FindMissingReferencesInScene(openScene, progressWeight, () => wasCancelled = false, () => wasCancelled = true, currentProgress);
            currentProgress += progressWeight;
            if (wasCancelled)
            {
                break;
            }
        }

        if (!wasCancelled)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var objs = allAssetPaths.Where(IsProjectAsset).ToArray();

            try
            {
                count += FindMissingReferencesInPaths(objs, () => wasCancelled = false, () => wasCancelled = true, currentProgress, progressWeight);
            }
            catch (Exception e)
            {
                wasCancelled = true;
            }
        }

        ShowFinishDialog(wasCancelled, count);

        // Restore the scene that was originally open when the tool was started.
        if (!string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath);
        }

        return m_Entries;
    }

    private static bool IsProjectAsset(string path)
    {
#if UNITY_EDITOR_OSX
        return !path.StartsWith("/");
#else
        return path.Substring(1, 2) != ":/";
#endif
    }

    private static int FindMissingReferencesInPaths(string[] paths, Action onFinished, Action onCanceled, float initialProgress, float progressWeight)
    {
        var count = 0;
        var wasCancelled = false;
        for (var i = 0; i < paths.Length; i++)
        {
            var obj = AssetDatabase.LoadAssetAtPath(paths[i], typeof(GameObject)) as GameObject;
            if (obj == null || !obj)
            {
                continue;
            }

            if (wasCancelled || EditorUtility.DisplayCancelableProgressBar(
                "Searching missing references in assets.", $"{paths[i]}", initialProgress + ((i / (float)paths.Length) * progressWeight)))
            {
                onCanceled.Invoke();
                return count;
            }

            count += FindMissingReferencesInTree(RmAssets(paths[i]), obj);
        }

        onFinished.Invoke();
        return count;
    }

    private static int FindMissingReferencesInTree(string context, GameObject go)
    {
        var count = 0;
        count += CheckForMissingPrefab(context, go, false);

        var components = go.GetComponents<Component>();

        for (var j = 0; j < components.Length; j++)
        {
            count += DoCheck(context, go, components[j], false);
        }

        foreach (Transform child in go.transform)
        {
            count += FindMissingReferencesInTree(context, child.gameObject);
        }

        return count;
    }

    private static int FindMissingReferencesInScene(Scene scene, float progressWeightByScene, Action onFinished, Action onCanceled, float currentProgress)
    {
        var rootObjects = scene.GetRootGameObjects();

        var queue = new Queue<ObjectData>();
        foreach (var rootObject in rootObjects)
        {
            queue.Enqueue(new ObjectData { ExpectedProgress = progressWeightByScene / rootObjects.Length, GameObject = rootObject });
        }

        string context = RmAssets(scene.path);

        var count = 0;
        while (queue.Any())
        {
            var data = queue.Dequeue();
            var go = data.GameObject;

            count += CheckForMissingPrefab(context, go, true);

            var components = go.GetComponents<Component>();
            float progressEachComponent = (data.ExpectedProgress) / (components.Length + go.transform.childCount);

            for (var j = 0; j < components.Length; j++)
            {
                currentProgress += progressEachComponent;
                if (EditorUtility.DisplayCancelableProgressBar($"Searching missing references in {context}", go.name, currentProgress))
                {
                    onCanceled.Invoke();
                    return count;
                }

                count += DoCheck(context, go, components[j], true);
            }

            foreach (Transform child in go.transform)
            {
                if (child.gameObject == go) continue;
                queue.Enqueue(new ObjectData { ExpectedProgress = progressEachComponent, GameObject = child.gameObject });
            }
        }

        onFinished.Invoke();
        return count;
    }

    private static string RmAssets(string context) => context.StartsWith("Assets/") ? context.Substring(7) : context;

    private static int CheckForMissingPrefab(string context, GameObject go, bool isInScene)
    {
        if (go.name.ToLower().Contains("missing prefab") ||
            PrefabUtility.IsPrefabAssetMissing(go) ||
            PrefabUtility.IsDisconnectedFromPrefabAsset(go))
        {
            m_Entries.Add(new Found
            {
                obj = go,
                scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(go.scene.path),
                type = "PREFAB",
                //container = ,
                //name = go.name,
                //guid = ,
                //fileId = ,
                //parent = go.transform.parent?.name,
                path = FullPath(go, isInScene),
                containerName = context,
            });

            Debug.LogError($"Missing PREFAB: [{context}] {FullPath(go, isInScene)} has missing prefab \"{go.name}\"");
            return 1;
        }
        return 0;
    }

    private static int DoCheck(string context, GameObject go, Component objectsComponent, bool isInScene)
    {
        var count = 0;
        if (!objectsComponent)
        {
            m_Entries.Add(new Found
            {
                obj = go,
                scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(go.scene.path),
                type = "COMPONENT",
                //container = ,
                //name = go.name,
                //guid = guid,
                //fileId = fileId,
                //parent = go.transform.parent?.name,
                path = FullPath(go, isInScene),
                containerName = context,
            });
            Debug.LogError($"Missing COMPONENT: [{context}] {FullPath(go, isInScene)}", go);
            count++;
        }
        else
        {
            var so = new SerializedObject(objectsComponent);
            var sp = so.GetIterator();
            while (sp.NextVisible(true))
            {
                if (sp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sp.objectReferenceInstanceIDValue, out string guid, out long fileId);

                        var componentName = objectsComponent.GetType().Name;
                        var variable = sp.name;
                        var property = ObjectNames.NicifyVariableName(variable);
                        m_Entries.Add(new Found
                        {
                            obj = go,
                            scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(go.scene.path),
                            type = "REFERENCE",
                            property = $"{property} ({variable})",
                            component = objectsComponent,
                            //container = ,
                            //name = go.name,
                            guid = guid,
                            fileId = fileId,
                            //parent = go.transform.parent?.name,
                            path = FullPath(go, isInScene),
                            containerName = context,
                        });
                        Debug.LogError($"Missing REFERENCE: [{RmAssets(context)}] {FullPath(go, isInScene)} GUID: {guid}, fileID: {fileId}, component: {componentName}, property: {property} ({variable})", go);

                        count++;
                    }
                }
            }
        }
        return count;
    }

    //private static void showInitialProgressBar(string searchContext, bool clearConsole = true)
    //{
    //    if (clearConsole)
    //    {
    //        Debug.ClearDeveloperConsole();
    //    }
    //    EditorUtility.DisplayProgressBar("Missing References Finder", $"Preparing search in {searchContext}", 0f);
    //}

    private static void ShowFinishDialog(bool wasCancelled, int count)
    {
        EditorUtility.ClearProgressBar();
        //EditorUtility.DisplayDialog("Missing References Finder",
        //                            wasCancelled ?
        //                                $"Process cancelled.\n{count} missing references were found.\n Current results are shown as errors in the console." :
        //                                $"Finished finding missing references.\n{count} missing references were found.\n Results are shown as errors in the console.",
        //                            "Ok");
    }

    private static string FullPath(GameObject go, bool isInScene)
    {
        var parent = go.transform.parent;
        if (parent)
        {
            var prepend = FullPath(parent.gameObject, isInScene);
            if (prepend.Length > 0) { prepend += "/"; }
            return prepend + go.name;
        }

        return isInScene ? go.name : string.Empty;
    }

    //private static void clearConsole()
    //{
    //    var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
    //    if (logEntries == null) return;
    //
    //    var clearMethod = logEntries.GetMethod("Clear",
    //        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
    //    if (clearMethod == null) return;
    //
    //    clearMethod.Invoke(null, null);
    //}

    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing Prefabs")]
    //static void Init()
    //{
    //    var allPrefabs = AssetDatabase.GetAllAssetPaths().Where(s => s.Contains(".prefab")).ToList();
    //    var count = 0;
    //    EditorUtility.DisplayProgressBar("Processing...", "Begin Job", 0);
    //
    //    foreach (var prefab in allPrefabs)
    //    {
    //        var o = AssetDatabase.LoadMainAssetAtPath(prefab);
    //
    //        if (o == null)
    //        {
    //            Debug.Log("prefab " + prefab + " null?");
    //            continue;
    //        }
    //
    //        GameObject go;
    //        try
    //        {
    //            go = (GameObject)PrefabUtility.InstantiatePrefab(o);
    //            EditorUtility.DisplayProgressBar("Processing...", go.name, ++count / (float)allPrefabs.Count);
    //            FindMissingPrefabInGO(go, prefab, true);
    //
    //            GameObject.DestroyImmediate(go);
    //        }
    //        catch
    //        {
    //            Debug.Log("For some reason, prefab " + prefab + " won't cast to GameObject");
    //
    //        }
    //    }
    //
    //    EditorUtility.ClearProgressBar();
    //}
    //
    //
    //static void FindMissingPrefabInGO(GameObject g, string prefabName, bool isRoot)
    //{
    //    if (g.name.Contains("Missing Prefab") ||
    //        PrefabUtility.IsPrefabAssetMissing(g) ||
    //        PrefabUtility.IsDisconnectedFromPrefabAsset(g))
    //    {
    //        Debug.LogError($"{prefabName} has missing prefab \"{g.name}\"");
    //        return;
    //    }
    //
    //    if (!isRoot)
    //    {
    //        if (PrefabUtility.IsAnyPrefabInstanceRoot(g))
    //        {
    //            return;
    //        }
    //
    //        GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(g);
    //        if (root == g)
    //        {
    //            return;
    //        }
    //    }
    //
    //    // Now recurse through each child GO (if there are any):
    //    foreach (Transform childT in g.transform)
    //    {
    //        //Debug.Log("Searching " + childT.name  + " " );
    //        FindMissingPrefabInGO(childT.gameObject, prefabName, false);
    //    }
    //}

    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References In current scene", false)]
    //public static void FindMissingReferencesInCurrentScene()
    //{
    //    var scene = SceneManager.GetActiveScene();
    //    showInitialProgressBar(scene.path);
    //
    //    clearConsole();
    //
    //    var wasCancelled = false;
    //    var count = findMissingReferencesInScene(scene, 1, () => wasCancelled = false, () => wasCancelled = true);
    //    showFinishDialog(wasCancelled, count);
    //}
    //
    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References In current prefab", false)]
    //public static void FindMissingReferencesInCurrentPrefab()
    //{
    //    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
    //
    //    var assetPath = prefabStage.assetPath;
    //    showInitialProgressBar(assetPath);
    //    clearConsole();
    //
    //    var count = findMissingReferences(assetPath, prefabStage.prefabContentsRoot, true);
    //    showFinishDialog(false, count);
    //}
    //
    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References In current prefab", true)]
    //public static bool FindMissingReferencesInCurrentPrefabValidate() => PrefabStageUtility.GetCurrentPrefabStage() != null;
    //
    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References In all scenes in build", false)]
    //public static void FindMissingReferencesInAllScenesInBuild()
    //{
    //    var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToList();
    //
    //    var count = 0;
    //    var wasCancelled = true;
    //    foreach (var scene in scenes)
    //    {
    //        Scene openScene;
    //        try
    //        {
    //            openScene = EditorSceneManager.OpenScene(scene.path);
    //        }
    //        catch (Exception ex)
    //        {
    //            Debug.LogError($"Could not open scene at path \"{scene.path}\". This scene was added to the build, and it's possible that it has been deleted: Error: {ex.Message}");
    //            continue;
    //        }
    //
    //        count += findMissingReferencesInScene(openScene, 1 / (float)scenes.Count(), () => wasCancelled = false, () => wasCancelled = true);
    //        if (wasCancelled) break;
    //    }
    //    showFinishDialog(wasCancelled, count);
    //}

    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References In all scenes in project", false)]
    //public static void FindMissingReferencesInAllScenes() {
    //    var scenes = EditorBuildSettings.scenes;
    //
    //    var finished = true;
    //    foreach (var scene in scenes) {
    //        var s = EditorSceneManager.OpenScene(scene.path);
    //        finished = findMissingReferencesInScene(s, 1 /(float)scenes.Count());
    //        if (!finished) break;
    //    }
    //    showFinishDialog(!finished);
    //}

    //[MenuItem("𝓢𝓽𝓸𝓻𝔂 𝓖𝓲𝓪𝓷𝓽/🛠 Tools/🔍 Find Missing References In assets", false)]
    //public static void FindMissingReferencesInAssets()
    //{
    //    showInitialProgressBar("all assets");
    //    var allAssetPaths = AssetDatabase.GetAllAssetPaths();
    //    var objs = allAssetPaths
    //               .Where(isProjectAsset)
    //               .ToArray();
    //
    //    var wasCancelled = false;
    //    var count = findMissingReferences("Project", objs, () => wasCancelled = false, () => wasCancelled = true);
    //    showFinishDialog(wasCancelled, count);
    //}
}
