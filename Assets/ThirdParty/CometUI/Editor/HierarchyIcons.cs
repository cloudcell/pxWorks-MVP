using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CometUI
{
    [InitializeOnLoad]
    class HierarchyIcons
    {
        static Texture2D logo;
        static List<int> markedObjects;

        static HierarchyIcons()
        {
            logo = Resources.Load<Texture2D>("comet_icon");
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
        }

        static void HierarchyItemCB(int instanceID, Rect selectionRect)
        {
            Rect r = new Rect(selectionRect);
            r.x = 16;
            r.width = 20;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (go && go.GetComponent<BaseView>())
                GUI.Label(r, logo);
        }
    }
}