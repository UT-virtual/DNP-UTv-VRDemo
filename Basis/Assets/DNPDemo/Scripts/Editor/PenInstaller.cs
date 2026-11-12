using DNPDemo.Pen;
using DNPDemo.Players;
using DNPDemo.Roles;
using UnityEditor;
using UnityEngine;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Builds the spatial pens into the DemoSystems prefab: the controller and sync on a <c>Pen</c> object, and one pen
    /// per role placed in front of that role's spawn point. Runs only when <c>Pen</c> is missing; afterwards the prefab
    /// is the source of truth (move a pen in the prefab to change where it starts).
    /// </summary>
    public static class PenInstaller
    {
        public const string PenObjectName = "Pen";

        // Starting place of each pen relative to its role's spawn point (on the floor, facing forward):
        // within reach of the right hand at about waist height, tip pointing forward.
        static readonly Vector3 StartOffset = new(0.2f, 1.0f, 0.35f);
        static readonly Quaternion StartRotation = Quaternion.Euler(0f, -90f, 0f);

        [MenuItem("DNP Demo/Setup/Install Pen Into DemoSystems Prefab")]
        public static void Install()
        {
            string path = ScenarioInstaller.DemoSystemsPrefabPath;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Fail($"{path} がありません。");
                return;
            }
            GameObject penPrefab = PenPrefabBuilder.CreateIfMissing();
            if (penPrefab == null)
            {
                Fail("ペンのプレハブを作成できませんでした。コンソールを確認してください。");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.transform.Find(PenObjectName) != null)
                {
                    Debug.Log($"[DNPDemo] {path} already has the pen installed.");
                    return;
                }
                var roles = root.GetComponentInChildren<DemoRoleService>(true);
                var spawns = root.GetComponentInChildren<DemoSpawnService>(true);
                if (roles == null || spawns == null)
                {
                    Fail($"{path} に DemoRoleService または DemoSpawnService がありません。");
                    return;
                }
                Transform spawnA = GetSpawn(spawns, "_playerASpawn");
                Transform spawnB = GetSpawn(spawns, "_playerBSpawn");
                if (spawnA == null || spawnB == null)
                {
                    Fail("DemoSpawnService にプレイヤーA・B のスポーン位置が設定されていません。");
                    return;
                }

                var penObject = new GameObject(PenObjectName);
                penObject.transform.SetParent(root.transform, false);
                var controller = penObject.AddComponent<SpatialPenController>();
                var sync = penObject.AddComponent<SpatialPenSync>();

                SpatialPenView penA = CreatePen(penPrefab, penObject.transform, "Pen_PlayerA", spawnA);
                SpatialPenView penB = CreatePen(penPrefab, penObject.transform, "Pen_PlayerB", spawnB);

                WorldUiBuilder.Assign(sync, "_roles", roles);
                WorldUiBuilder.Assign(sync, "_controller", controller);
                WorldUiBuilder.Assign(sync, "_playerAPen", penA);
                WorldUiBuilder.Assign(sync, "_playerBPen", penB);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[DNPDemo] Installed the pen into {path}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static SpatialPenView CreatePen(GameObject prefab, Transform parent, string name, Transform spawn)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(
                spawn.position + spawn.rotation * StartOffset,
                spawn.rotation * StartRotation);
            return instance.GetComponent<SpatialPenView>();
        }

        static Transform GetSpawn(DemoSpawnService spawns, string field) =>
            new SerializedObject(spawns).FindProperty(field)?.objectReferenceValue as Transform;

        static void Fail(string message)
        {
            EditorUtility.DisplayDialog("DNP Demo", message, "OK");
        }
    }
}
