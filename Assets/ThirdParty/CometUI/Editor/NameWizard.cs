using System;
using UnityEditor;
using UnityEngine;

namespace CometUI
{
    public class NameWizard : ScriptableWizard
    {
        string Name;
        Action<string> OnCreated;

        public static void CreateWizard(string name, Action<string> onCreated)
        {
            var wizard = ScriptableWizard.DisplayWizard<NameWizard>("Name", "OK");
            wizard.Name = name;
            wizard.OnCreated = onCreated;
        }

        void OnWizardCreate()
        {
            OnCreated?.Invoke(Name);
        }

        void OnWizardUpdate()
        {
            helpString = "Name";
        }

        protected override bool DrawWizardGUI()
        {
            Name = GUILayout.TextField(Name);
            return false;
        }
    }
}