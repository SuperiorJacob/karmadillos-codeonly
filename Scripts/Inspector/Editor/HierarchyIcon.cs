using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace AberrationGames.EditorTools
{
    /// <summary>
    /// Draws a Comment Icon on GameObjects in the Hierarchy that contain the Comment component.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyIcon
    {
        public static Dictionary<int, Dictionary<int, Texture>> Icons = new Dictionary<int, Dictionary<int, Texture>>();

        static HierarchyIcon()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
        }

        static Dictionary<int, Texture> CheckForIcons(int a_instance)
        {
            Icons.TryGetValue(a_instance, out Dictionary<int, Texture> value);

            // Custom icon support without having to open the script :)
            GameObject obj = (GameObject)EditorUtility.InstanceIDToObject(a_instance);

            if (obj != null)
            {
                Dictionary<int, Texture> newDict = new Dictionary<int, Texture>();

                foreach (var o in obj.GetComponents<MonoBehaviour>())
                {
                    if (o == null)
                        continue;

                    var attr = o.GetType().GetCustomAttribute<AberrationDeclareAttribute>();
                    if (attr != null)
                    {
                        int instanceID = o.GetInstanceID();
                        if (!string.IsNullOrEmpty(attr.icon) && (!newDict.ContainsKey(instanceID) || newDict[instanceID] == null))
                            newDict[instanceID] = EditorGUIUtility.IconContent(attr.icon).image;

                        if (value != null)
                        {
                            if (!newDict.ContainsKey(instanceID) && value.ContainsKey(instanceID))
                                newDict[instanceID] = value[instanceID];

                            if (value.TryGetValue(instanceID - 100, out Texture v))
                            {
                                newDict[instanceID - 100] = v;
                            }
                        }
                    }
                }

                if (newDict.Count > 0)
                {
                    value = newDict;
                    Icons[a_instance] = value;
                }
            }

            return value;
        }

        static void HandleHierarchyWindowItemOnGUI(int a_instanceID, Rect a_selectionRect)
        {
            if (Application.isPlaying)
                return;

            Dictionary<int, Texture> icons = CheckForIcons(a_instanceID);

            if (icons != null)
            {
                int count = 0;
                foreach (var icon in icons)
                {
                    count++;
                    GUI.DrawTexture(new Rect(a_selectionRect.xMax - (16 * icons.Count) + (16 * count), a_selectionRect.yMin, 16, 16), icon.Value);
                }
            }
        }
    }
}
#endif
