using UnityEditor;
using UnityEngine;

namespace NaughtyAttributes.Editor
{
    [PropertyGrouper(typeof(FoldoutAttribute))]
    public class FoldoutPropertyGrouper : PropertyGrouper
    {
        private bool isVisible = false;

        public override bool IsVisible => isVisible;

        public override void BeginGroup(string label)
        {
            isVisible = EditorGUILayout.Foldout(isVisible, label ?? "", EditorStyles.foldoutHeader);
            if (isVisible)
                EditorGUILayout.BeginVertical(GUI.skin.box); //EditorStyles.foldout
        }

        public override void EndGroup()
        {
            if (isVisible)
                EditorGUILayout.EndVertical();
        }
    }
}
