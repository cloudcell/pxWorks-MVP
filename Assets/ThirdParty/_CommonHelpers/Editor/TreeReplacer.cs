using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

// Replaces Unity terrain trees with prefab GameObject. 
// http://answers.unity3d.com/questions/723266/converting-all-terrain-trees-to-gameobjects.html
[ExecuteInEditMode]
public class TreeReplacer : EditorWindow
{

    [Header("References")]
    public Terrain _terrain;
    //============================================
    [MenuItem("My/TreeReplacer")]
    static void Init()
    {
        TreeReplacer window = (TreeReplacer)GetWindow(typeof(TreeReplacer));
    }
    void OnGUI()
    {
        _terrain = (Terrain)EditorGUILayout.ObjectField(_terrain, typeof(Terrain), true);
        if (GUILayout.Button("Convert to objects"))
        {
            Convert();
        }
        if (GUILayout.Button("Clear generated trees"))
        {
            Clear();
        }
    }
    //============================================
    public void Convert()
    {
        TerrainData data = _terrain.terrainData;
        float width = data.size.x;
        float height = data.size.z;
        float y = data.size.y;
        // Create parent
        GameObject parent = GameObject.Find("TREES_GENERATED");
        if (parent == null)
        {
            parent = new GameObject("TREES_GENERATED");
        }

        // Create tree objects
        foreach (TreeInstance tree in data.treeInstances)
        {
            Vector3 position = new Vector3(tree.position.x * width, tree.position.y * y, tree.position.z * height);
            var _tree = data.treePrototypes[tree.prototypeIndex].prefab;
            var q = Quaternion.Euler(0, tree.rotation * Mathf.Rad2Deg, 0);
            Instantiate(_tree, position, q, parent.transform);
        }

        parent.transform.parent = _terrain.transform;
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localRotation = Quaternion.identity;
    }
    public void Clear()
    {
        DestroyImmediate(GameObject.Find("TREES_GENERATED"));
    }
}