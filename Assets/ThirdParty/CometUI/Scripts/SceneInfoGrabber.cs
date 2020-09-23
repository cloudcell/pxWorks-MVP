using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CometUI
{
    public static class SceneInfoGrabber<TK> where TK : Component
    {
        /// <summary>Build scene model</summary>
        public static IEnumerable<ViewInfo<TK>> GrabInfos(bool onlySpecialNames)
        {
            //get all UniqueId
            foreach (var uid in GetUIComponentsOnScene())
            {
                var vm = new ViewInfo<TK> { Main = uid };
                vm.Name = uid.GetType().Name;
                vm.Members = GrabInfo(uid.transform, onlySpecialNames);
                yield return vm;
            }
        }

        public static Dictionary<string, Component> GrabInfo(Transform tr, bool onlySpecialNames)
        {
            var res = new Dictionary<string, Component>();

            //get all children (exclude other UniqueId)
            foreach (var comp in FindAllChildren(tr))
            {
                //check name
                var name = comp.gameObject.name;
                if (string.IsNullOrEmpty(name)) continue;

                if (onlySpecialNames && !IsSpecialName(name))
                    continue;//ignore Names with first upper symbol

                //prepare name
                name = PrepareName(name, res);

                //get wellknown component
                res[name] = GetWellKnownComponent(comp);
            }

            return res;
        }

        public static bool IsSpecialName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return char.IsLower(name[0]);
        }

        /// <summary>
        /// Finds active and inactive objects
        /// </summary>
        public static IEnumerable<TK> GetUIComponentsOnScene(bool bOnlyRoot = false, Scene scene = default)
        {
            TK[] pAllObjects = (TK[])Resources.FindObjectsOfTypeAll(typeof(TK));

            foreach (TK pObject in pAllObjects)
            {
                if (bOnlyRoot)
                {
                    if (pObject.transform.parent != null)
                    {
                        continue;
                    }
                }

                if (pObject.hideFlags == HideFlags.NotEditable || pObject.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (Application.isEditor)
                {
                    string sAssetPath = UnityEditor.AssetDatabase.GetAssetPath(pObject.transform.root.gameObject);
                    if (!string.IsNullOrEmpty(sAssetPath))
                        continue;//prefab

                    if (pObject.transform.parent == null || pObject.transform.parent.name.Contains("(Environment)"))
                        continue;//opened prefab
                }

#endif
                if (scene != default && pObject.gameObject.scene != scene)
                    continue;

                yield return pObject;
            }
        }

        /// <summary>Returns all child transfroms (exclude TK and its children)</summary>
        static IEnumerable<Transform> FindAllChildren(Transform parent)
        {
            if (parent == null)
                yield break;

            foreach (Transform child in parent)
            {
                yield return child;

                if (child.GetComponent<TK>() == null)
                    foreach (var elem in FindAllChildren(child))
                        yield return elem;
            }
        }

        static string PrepareName(string name, Dictionary<string, Component> dict)
        {
            name = Regex.Replace(name, @"\W", "");

            //add index if there many items with same name
            var newName = name;
            var counter = 1;
            while (dict.ContainsKey(newName))
            {
                newName = name + counter;
                counter++;
            }

            return newName;
        }

        public static Component GetWellKnownComponent(Transform tr)
        {
            //get all types of components
            var components = tr.GetComponents<Component>();
            var dict = new Dictionary<Type, Component>();
            foreach (var c in components)
            if (c != null)
            {
                dict[c.GetType()] = c;
                if (c is TK)
                    return c;
            }

            //select component
            foreach (var t in WellKnownTypes)
            if (dict.TryGetValue(t, out var c))
                return c;

            //
            return tr;
        }

        public static List<Type> WellKnownTypes = new List<Type>
        {
            typeof(Text),
            typeof(TMPro.TextMeshProUGUI),
            typeof(RawImage),
            typeof(Button),
            typeof(Toggle),
            typeof(Slider),
            typeof(Scrollbar),
            typeof(Dropdown),
            typeof(TMPro.TMP_Dropdown),
            typeof(InputField),
            typeof(TMPro.TMP_InputField),
            typeof(ScrollRect),
            typeof(Image),
            //... and last RectTransform:
            typeof(RectTransform)
        };
    }

    public class ViewInfo<TK>
    {
        public string Name;
        public TK Main;
        public Dictionary<string, Component> Members = new Dictionary<string, Component>();
    }
}
