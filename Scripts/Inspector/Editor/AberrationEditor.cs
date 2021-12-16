using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;
using Unity.Profiling;
using System.Reflection.Emit;

#if UNITY_EDITOR
namespace AberrationGames.EditorTools
{
    // >:)
    public static class BackgroundStyle
    {
        private static readonly GUIStyle _style = new GUIStyle();
        private static Texture2D _texture;

        public static GUIStyle Get(Color color)
        {
            _texture = new Texture2D(1, 1);
            _texture.SetPixel(0, 0, color);
            _texture.Apply();
            _style.normal.background = _texture;
            return _style;
        }
    }

    // Probs optimise this later if you actually care about the well being of your computer.
    // Oh and eventually split the code up... bad practice.

    [CustomEditor(typeof(MonoBehaviour), true)]
    [AberrationDescription("Aberration Games Custom Editor and Diagnoser", "Jacob Cooper", "15/09/2021")]
    public class AberrationExtraEditor : Editor
    {
        public static readonly Color DEFAULT_COLOR = new Color(0f, 0f, 0f, 0.3f);
        public static readonly Color DEFAULT_ERROR_COLOR = new Color(0.3f, 0.1f, 0.1f, 0.3f);
        public static readonly Vector2 DEFAULT_LINE_MARGIN = new Vector2(2f, 2f);

        public const float DEFAULT_LINE_HEIGHT = 1f;

        private List<(string error, MemberInfo reflect)> _debugErrors;

        private ProfilerRecorder _reservedMemory;
        private ProfilerRecorder _usedMemory;

        private long _memoryUsedByThis = 0;
        private long _startMemory = 0;
        private long _classReserveMemory = 0;

        private int _toolBarInt = 0;
        private bool _debugging = false;


        private bool _diagnoseUsage = false;
        private bool _diagnoseReferencedClasses = false;
        private bool _showAuthors = true;
        private bool _showDescriptions = true;
        private bool _showErrors = true;

        private bool _profiled = false;
        private bool _debugMode = false;

        private Vector2 _scrollBarPos;
        private Vector2 _scrollBarPos2;

        public void CreateHorizontalLine(Color a_color, float a_height, Vector2 a_margin)
        {
            GUILayout.Space(a_margin.x);

            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, a_height), a_color);

            GUILayout.Space(a_margin.y);
        }

        public void CreateHorizontalLine()
        {
            CreateHorizontalLine(DEFAULT_COLOR, DEFAULT_LINE_HEIGHT, DEFAULT_LINE_MARGIN);
        }

        private void GetMethods(System.Type a_typ, out Dictionary<string, MethodInfo> methods)
        {
            methods = new Dictionary<string, MethodInfo>();

            foreach (var method in a_typ.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static))
            {
                methods[method.Name] = method;
            }
        }

        private void GetFields(System.Type a_typ, out Dictionary<string, SerializedProperty> properties,
            out Dictionary<string, SerializedProperty> serializedProperties,
            out Dictionary<string, FieldInfo> fields,
            out Dictionary<FieldInfo, string> fieldToolBar,
            out Dictionary<string, Texture> toolBars,
            out Dictionary<FieldInfo, bool> required,
            out string toolBarName,
            int a_instanceID = 0)
        {
            properties = new Dictionary<string, SerializedProperty>();
            serializedProperties = new Dictionary<string, SerializedProperty>();

            fields = new Dictionary<string, FieldInfo>();
            fieldToolBar = new Dictionary<FieldInfo, string>();
            required = new Dictionary<FieldInfo, bool>();
            toolBars = new Dictionary<string, Texture>();

            toolBarName = "";

            bool errored = false;

            Texture errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;

            foreach (var field in a_typ.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static))
            {
                if (field.Name == "m_Script")
                    continue;

                fields[field.Name] = field;

                AberrationToolBarAttribute toolAttr = field.GetCustomAttribute<AberrationToolBarAttribute>();
                AberrationEndToolBarAttribute tattr = field.GetCustomAttribute<AberrationEndToolBarAttribute>();

                if (toolAttr == null && tattr != null)
                {
                    toolBarName = "";
                }

                if (toolAttr != null)
                {
                    toolBarName = toolAttr.name;
                }

                AberrationRequiredAttribute requiredAttribute = field.GetCustomAttribute<AberrationRequiredAttribute>();
                Texture icon = null;
                bool isRequired = requiredAttribute != null;

                if (isRequired)
                {
                    object requirement = field.GetValue(target);

                    // Null gate is weird cuz reflection :(
                    if (NullByType(requirement) || requiredAttribute.type != null && requirement.GetType().IsInstanceOfType(requiredAttribute.type))
                    {
                        icon = errorIcon;

                        errored = true;

                        if (toolBars.ContainsKey(toolBarName))
                            toolBars[toolBarName] = icon;

                        Debug.LogError($"<color=gray>{target.GetType()}</color> > <color=lightblue><b>{field.Name}</b></color> is missing the requirement of <b>{ParamTypeConversion(requiredAttribute.type != null ? requiredAttribute.type : field.FieldType)}</b>");
                    }
                }

                required[field] = isRequired;

                if (toolAttr != null)
                {
                    toolBars.Add(toolBarName, icon);
                }

                fieldToolBar[field] = toolBarName;
            }

            if (errored && a_instanceID != 0 && HierarchyIcon.Icons.ContainsKey(a_instanceID))
            {
                HierarchyIcon.Icons[a_instanceID][target.GetInstanceID() - 100] = errorIcon;
            }
            else if (!errored && a_instanceID != 0 && HierarchyIcon.Icons.ContainsKey(a_instanceID) && HierarchyIcon.Icons[a_instanceID].ContainsKey(target.GetInstanceID() - 100))
            {
                HierarchyIcon.Icons[a_instanceID].Remove(target.GetInstanceID() - 100);
            }
        }

        private bool NullByType(object a_obj)
        {
            if (a_obj == null || a_obj.ToString() == "null" || string.IsNullOrEmpty(a_obj.ToString()))
                return true;

            if (a_obj.GetType().IsPrimitive)
            {
                if (a_obj is float)
                    if ((float)a_obj == 0f)
                        return true;
                else if (a_obj is int)
                    if ((int)a_obj == 0)
                        return true;
            }

            return false;
        }

        private void OnEnable()
        {
            if (!_debugging || !_debugMode || !_diagnoseUsage) return;

            _profiled = true;
            _reservedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
            _usedMemory = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
        }

        private void OnDisable()
        {
            if (_profiled)
            {
                _profiled = false;
                _reservedMemory.Dispose();
                _usedMemory.Dispose();
            }
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || target == null)
                return;

            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedObject.GetIterator();
            System.Type typ = serializedObject.targetObject.GetType();

            var declaration = typ.GetCustomAttribute<AberrationDeclareAttribute>();
            if (declaration == null)
            {
                base.OnInspectorGUI();

                EditorGUI.EndChangeCheck();

                return;
            }
            else
            {
                _debugMode = (declaration.declaration == DeclarationTypes.Debug);
            }

            if (_usedMemory.Valid && _usedMemory.CurrentValue > 0)
            {
                _startMemory = _usedMemory.CurrentValue;
            }

            if (_debugMode && _debugging && _diagnoseUsage && !_profiled)
                OnEnable();
            else if (_profiled && (!_diagnoseUsage || !_debugMode || !_debugging))
                OnDisable();

            GUIStyle style = new GUIStyle
            {
                richText = true
            };

            GetMethods(typ, out Dictionary<string, MethodInfo> methods);

            GetFields(typ, out Dictionary<string, SerializedProperty> properties,
                out Dictionary<string, SerializedProperty> serializedProperties,
                out Dictionary<string, FieldInfo> fields,
                out Dictionary<FieldInfo, string> fieldToolBar,
                out Dictionary<string, Texture> toolBars,
                out Dictionary<FieldInfo, bool> required,
                out string toolBarName,
                ((MonoBehaviour)target).gameObject.GetInstanceID());

            string[] toolbarArray = new string[0];
            if (toolBars.Count > 0)
            {
                toolbarArray = new string[toolBars.Keys.Count];

                toolBars.Keys.CopyTo(toolbarArray, 0);

                List<GUIContent> contents = new List<GUIContent>();

                foreach (var tool in toolBars)
                {
                    contents.Add(new GUIContent(tool.Key, tool.Value, ""));
                }

                _toolBarInt = GUILayout.Toolbar(_toolBarInt, contents.ToArray());
                GUILayout.Space(10f);
            }

            bool expanded = true;
            List<bool> showing = new List<bool> { true };
            bool showThis = false;
            bool foldout = false;
            bool doneNoCatBar = false;

            int foldOutCount = 0;
            while (property.NextVisible(expanded))
            {
                if (property.propertyPath == "m_Script")
                    continue;

                List<AberrationButtonAttribute> buttons = new List<AberrationButtonAttribute>();

                serializedProperties[property.name] = property;

                bool noCat = false;

                if (fields.TryGetValue(property.name, out FieldInfo field))
                {
                    AberrationRequiredAttribute requiredAttribute = field.GetCustomAttribute<AberrationRequiredAttribute>();
                    Texture icon = null;
                    bool isRequired = required[field] && requiredAttribute != null;

                    if (isRequired)
                    {
                        object requirement = field.GetValue(target);

                        if (NullByType(requirement) || requiredAttribute.type != null && requirement.GetType().IsInstanceOfType(requiredAttribute.type))
                        {
                            icon = EditorGUIUtility.IconContent("console.erroricon.sml").image;
                        }
                    }

                    if (fieldToolBar.TryGetValue(field, out string barName))
                    {
                        if (!string.IsNullOrEmpty(barName) && barName != toolbarArray[_toolBarInt])
                        {
                            continue;
                        }
                        else if (string.IsNullOrEmpty(barName))
                            noCat = true;
                    }

                    properties[property.name] = property;

                    IEnumerable<AberrationButtonAttribute> battr = field.GetCustomAttributes<AberrationButtonAttribute>();

                    bool foundButton = false;
                    foreach (var buttonAttribute in battr)
                    {
                        switch (buttonAttribute.declaredType)
                        {
                            case DeclaredButtonTypes.Button:
                                if (!string.IsNullOrEmpty(buttonAttribute.reflect) && methods.ContainsKey(buttonAttribute.reflect))
                                {
                                    buttons.Add(buttonAttribute);
                                    foundButton = true;
                                }

                                break;
                            case DeclaredButtonTypes.Hidden:
                                if (field.FieldType == typeof(bool) && buttonAttribute != null)
                                {
                                    buttonAttribute.enabled = property.boolValue;
                                    foldout = true;
                                    showThis = true;
                                    foundButton = true;

                                    foldOutCount++;

                                    showing.Insert(foldOutCount, buttonAttribute.enabled);
                                }

                                break;
                            default:
                                break;
                        }
                    }

                    {
                        AberrationFinishButtonAttribute fattr = field.GetCustomAttribute<AberrationFinishButtonAttribute>();

                        if (fattr != null)
                        {
                            foldOutCount--;
                            if (foundButton)
                            {
                                foldout = false;
                                showing.RemoveAt(foldOutCount);
                            }
                        }
                    }

                    if (showing[foldOutCount] || showThis && showing[foldOutCount - 1])
                    {
                        if (noCat && !doneNoCatBar)
                        {
                            doneNoCatBar = true;
                            GUILayout.Space(10f);
                            CreateHorizontalLine();
                        }

                        GUILayout.BeginHorizontal();
                        if (foldout && !showThis)
                            GUILayout.Space(20 * foldOutCount);

                        foreach (var button in buttons)
                        {
                            if (GUILayout.Button(button.name))
                            {
                                MethodInfo method = methods[button.reflect];
                                method.Invoke(target, button.parse);
                            }
                        }
                        GUILayout.EndHorizontal();

                        using (new EditorGUI.DisabledScope("m_Script" == property.propertyPath))
                        {
                            int spacing = foldOutCount > 0 ? (20 * (foldOutCount - 1)) : 0;
                            if (foldout && !showThis)
                                spacing = 20 * foldOutCount;

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(spacing);

                            EditorGUILayout.PropertyField(property, GetGUIContent(field, property, isRequired, icon), true);

                            GUILayout.EndHorizontal();
                        }

                        showThis = false;
                    }
                }

                expanded = false;
            }

            if (_debugMode)
            {
                GUILayout.Space(10f);
                CreateHorizontalLine();

                GUILayout.BeginHorizontal();

                if (_debugErrors != null)
                    _debugErrors.Clear();
                else
                    _debugErrors = new List<(string, MemberInfo)>();

                if (GUILayout.Button("Debug Information"))
                {
                    _debugging = !_debugging;
                }

                GUILayout.EndHorizontal();

                if (_debugging)
                {
                    _showErrors = GUILayout.Toggle(_showErrors, "Show Errors");
                    _diagnoseUsage = GUILayout.Toggle(_diagnoseUsage, "Diagnose Code (Expensive)");
                    if (_diagnoseUsage)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20f);
                        _diagnoseReferencedClasses = GUILayout.Toggle(_diagnoseReferencedClasses, "Diagnose Referenced Classes (Expensive)");
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20f);
                        long reservedMemory = _reservedMemory.Valid ? _reservedMemory.CurrentValue : 0;
                        long usedMemory = _usedMemory.Valid ? _usedMemory.CurrentValue : 0;

                        if (usedMemory > 0)
                        {
                            float usedMemoryAsKB = usedMemory / 1024;
                            float usedByThisAsKB = _memoryUsedByThis / 1024;
                            float classMemoryAsKB = (float)_classReserveMemory / 1024;

                            float usagePercentile = (usedByThisAsKB / usedMemoryAsKB) * 100;
                            float classReservePercentile = (classMemoryAsKB / usedMemory) * 100;

                            if (classReservePercentile < 0.00001) classReservePercentile = 0;

                            GUILayout.BeginVertical(BackgroundStyle.Get(DEFAULT_COLOR));
                            GUILayout.Label($"<color=gray>Reserved Memory: <color=aqua>{reservedMemory / 1024 / 1024} MB</color></color>", style);
                            GUILayout.Label($"<color=gray>Used Memory: <color=aqua>{usedMemoryAsKB / 1024} MB</color></color>", style);
                            GUILayout.Label($"<color=white>Inspector Memory: <color=aqua>{usedByThisAsKB} KB</color> (<color=lime>{usagePercentile}%</color> of used memory)</color>", style);
                            GUILayout.Label("", style);
                            GUILayout.Label($"<color=white>Reserved Class Memory: <color=aqua>{classMemoryAsKB} KB</color> (<color=lime>{classReservePercentile}%</color> of used memory)</color>", style);
                            GUILayout.EndVertical();
                        }

                        GUILayout.EndHorizontal();
                    }

                    _showDescriptions = GUILayout.Toggle(_showDescriptions, "Show Descriptors");
                    if (_showDescriptions)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20f);
                        _showAuthors = GUILayout.Toggle(_showAuthors, "Show Authors");
                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(20f);

                    GUILayout.BeginHorizontal(BackgroundStyle.Get(DEFAULT_COLOR));
                    _scrollBarPos = EditorGUILayout.BeginScrollView(_scrollBarPos, GUILayout.Height(200));

                    System.Type classType = target.GetType();
                    DisplayDescriptionClass(classType, style);

                    GUILayout.Label($"");
                    GUILayout.Label($"<size=20><color=white>Fields</color></size>", style);

                    List<(MemberInfo name, string modifier, bool isField)> modifierPositions = new List<(MemberInfo name, string modifier, bool isField)>();

                    _classReserveMemory = 0;

                    foreach (var field in fields)
                    {
                        FieldInfo fieldInfo = field.Value;

                        object fieldValue = fieldInfo.GetValue(target);

                        if (_diagnoseUsage)
                        {
                            _classReserveMemory += GetBytes(fieldInfo.FieldType, fieldValue);
                        }

                        UnityEngine.Object fieldValueObject = fieldValue as UnityEngine.Object;

                        string val = $"<b>{ (fieldValue != null ? (fieldInfo.FieldType.IsPrimitive ? fieldValue.ToString().ToLower() : $"{(fieldValueObject != null ? fieldValueObject.name : fieldValue.ToString())}") : "null")}</b>";

                        string modifier = FieldGetModifier(fieldInfo);

                        modifierPositions.Add((fieldInfo, modifier, true));

                        string serialized = serializedProperties.ContainsKey(field.Key) ? "[<color=lime>Serialized</color>] " : "";

                        if (_showDescriptions)
                            DisplayDescription(fieldInfo, style, _showAuthors);
                        GUILayout.Label($"<color=lightblue>{serialized}{modifier} {ParamTypeConversion(fieldInfo.FieldType)} {FieldNameConversion(fieldInfo, methods)} = {val ?? "null"}</color>", style);
                    }

                    GUILayout.Label($"");
                    GUILayout.Label($"<size=20><color=white>Methods</color></size>", style);

                    foreach (var method in methods)
                    {
                        MethodInfo methodInfo = method.Value;

                        if (methodInfo.GetBaseDefinition().ReflectedType != target.GetType() 
                            || methodInfo.Name[0] == '<'
                            || methodInfo.Name.StartsWith("get_") 
                            || methodInfo.Name.StartsWith("set_"))
                            continue;

                        string modifier = MethodGetModifier(methodInfo);

                        modifierPositions.Add((methodInfo, modifier, false));

                        string par = "";

                        var arr = methodInfo.GetParameters();
                        int count = 0;
                        foreach (var val in arr)
                        {
                            if (val.IsOut || val.IsIn)
                                par += val.IsOut ? "out " : "in ";

                            string parName = val.Name;
                            if (!methodInfo.IsSpecialName && IsInValidParam(parName))
                            {
                                parName = $"<color=red>{parName}</color>";

                                _debugErrors.Add(($"Incorrect naming conventions, should be <color=lime>a_</color> and not <color=red>{parName}</color>", methodInfo));
                            }
                            else
                                parName = $"<color=aqua>{parName}</color>";

                            par += $"{ParamTypeConversion(val.ParameterType)} {parName}";

                            if (val.IsOptional)
                                par += $" = {(val.DefaultValue == null ? "null" : val.DefaultValue.ToString())}";

                            count++;
                            if (count < arr.Length)
                                par += ", ";
                        }

                        if (_showDescriptions)
                            DisplayDescription(methodInfo, style, _showAuthors);
                        GUILayout.Label($"<color=lightblue>{modifier} {ParamTypeConversion(methodInfo.ReturnType)} {MethodNameConversion(methodInfo)}({par})</color>", style);
                    }

                    for (int i = 0; i < modifierPositions.Count; i++)
                    {
                        if (i < 1) continue;

                        (MemberInfo typ, string modifier, bool isField) modifier = modifierPositions[i];
                        (MemberInfo typ, string modifier, bool isField) prevModifier = modifierPositions[i - 1];

                        if (!modifier.isField && !prevModifier.isField)
                        {
                            MethodInfo modifierMethod = modifier.typ as MethodInfo;
                            MethodInfo prevModifierMethod = prevModifier.typ as MethodInfo;

                            if (((modifier.modifier.Contains("public") && prevModifier.modifier.Contains("public"))
                            ||
                            (modifier.modifier.Contains("private") && prevModifier.modifier.Contains("private"))) 
                            &&
                            modifierMethod.ReturnType != prevModifierMethod.ReturnType && (prevModifierMethod.ReturnType.IsPrimitive || prevModifierMethod.ReturnType.Name == "Void"))
                            {
                                _debugErrors.Add(($"Incorrect code structuring, <color=lightblue>{(modifier.isField ? "FIELD" : "METHOD")} {modifier.modifier} {modifier.typ.Name}</color> should not be below primitives <color=lightblue>{(prevModifier.isField ? "FIELD" : "METHOD")} {prevModifier.modifier} {prevModifier.typ.Name}</color>!", modifier.typ));
                            }
                            if (modifierMethod.IsSpecialName || prevModifierMethod.IsSpecialName)
                                continue;
                        }

                        if (modifier.isField == prevModifier.isField && modifier.modifier != prevModifier.modifier)
                        {
                            if (modifier.modifier.Contains("public"))
                            {
                                if (modifier.modifier.Contains("static") | modifier.modifier.Contains("readonly"))
                                    _debugErrors.Add(($"Incorrect code structuring, <color=lightblue>{(modifier.isField ? "FIELD" : "METHOD")} {modifier.modifier} {modifier.typ.Name}</color> should be at the very top and not below <color=lightblue>{(prevModifier.isField ? "FIELD" : "METHOD")} {prevModifier.modifier} {prevModifier.typ.Name}</color>!", modifier.typ));
                                else if (!(prevModifier.modifier.Contains("static") | prevModifier.modifier.Contains("readonly")))
                                    _debugErrors.Add(($"Incorrect code structuring, <color=lightblue>{(modifier.isField ? "FIELD" : "METHOD")} {modifier.modifier} {modifier.typ.Name}</color> should not be below <color=lightblue>{(prevModifier.isField ? "FIELD" : "METHOD")} {prevModifier.modifier} {prevModifier.typ.Name}</color>!", modifier.typ));
                            }

                            if ((modifier.modifier.Contains("public") && prevModifier.modifier.Contains("public"))
                                ||
                                (modifier.modifier.Contains("private") && prevModifier.modifier.Contains("private")))
                            {
                                if (modifier.isField)
                                {
                                    FieldInfo modifierField = modifier.typ as FieldInfo;
                                    FieldInfo prevModifierField = prevModifier.typ as FieldInfo;

                                    if (!modifierField.FieldType.IsPrimitive && prevModifierField.FieldType.IsPrimitive)
                                    {
                                        _debugErrors.Add(($"Incorrect code structuring, <color=lightblue>{(modifier.isField ? "FIELD" : "METHOD")} {modifier.modifier} {modifier.typ.Name}</color> should not be below primitives <color=lightblue>{(prevModifier.isField ? "FIELD" : "METHOD")} {prevModifier.modifier} {prevModifier.typ.Name}</color>!", modifier.typ));
                                    }
                                }
                            }
                        }
                        else if (modifier.isField && !prevModifier.isField)
                            _debugErrors.Add(($"Incorrect code structuring, the field <color=lightblue>{modifier.modifier} {modifier.typ.Name}</color> should not be below a method (<color=lightblue>{prevModifier.modifier} {prevModifier.typ.Name}</color>)!", modifier.typ));
                        
                    }

                    GUILayout.EndScrollView();
                    GUILayout.EndHorizontal();

                    if (_debugErrors.Count > 0 && _showErrors)
                    {
                        GUILayout.Space(10f);
                        GUILayout.Label($"<color=white>Convention Error</color>", style);
                        GUILayout.Space(5f);
                        GUILayout.BeginHorizontal(BackgroundStyle.Get(DEFAULT_ERROR_COLOR));

                        _scrollBarPos2 = EditorGUILayout.BeginScrollView(_scrollBarPos2, GUILayout.Height(200));

                        Dictionary<MemberInfo, List<string>> debugReflection = new Dictionary<MemberInfo, List<string>>();

                        foreach (var e in _debugErrors)
                        {
                            if (!debugReflection.ContainsKey(e.reflect))
                            {
                                debugReflection[e.reflect] = new List<string>();
                            }

                            if (!debugReflection[e.reflect].Contains(e.error))
                                debugReflection[e.reflect].Add(e.error);
                        }

                        foreach (var e in debugReflection)
                        {
                            GUILayout.Space(5f);

                            MethodInfo reflectMethod = e.Key as MethodInfo;
                            FieldInfo reflectField = e.Key as FieldInfo;

                            string name = "";
                            string type = "";
                            if (reflectMethod != null)
                            {
                                name = reflectMethod.Name;
                                type = "<color=yellow>METHOD</color>";
                            }
                            else if (reflectField != null)
                            {
                                name = reflectField.Name;
                                type = "<color=green>FIELD</color>";
                            }

                            string reply = $"<size=16><color=white>{type} {name}</color></size>";
                            GUILayout.Label(reply, style);

                            foreach (var debugError in debugReflection[e.Key])
                            {
                                GUILayout.Label($"<color=white>{debugError}</color>", style);
                            }
                        }


                        GUILayout.EndScrollView();
                        GUILayout.EndHorizontal();
                    }

                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();

            if (_diagnoseUsage && _debugging && _usedMemory.Valid)
            {
                _memoryUsedByThis = _usedMemory.CurrentValue - _startMemory;
            }
        }

        private int GetBytes(Type a_type, object a_value)
        {
            int size = 0;
            int count = 0;

            if (a_type.IsPrimitive || a_type.IsLayoutSequential)
            {
                size = System.Runtime.InteropServices.Marshal.SizeOf(a_type);
            }
            else
            {
                string lowerName = a_type.Name.ToLower();
                if (a_type.IsGenericType)
                {
                    foreach (var generic in a_type.GetGenericArguments())
                        size += GetBytes(generic, null);

                    if (lowerName == "dictionary`2" || lowerName == "list`1")
                    {
                        if (a_value != null)
                        {
                            IDictionary dict = a_value as IDictionary;

                            if (dict != null)
                                count = dict.Count;

                            IList list = a_value as IList;

                            if (list != null)
                                count = list.Count;
                        }
                    }
                }
                else if (a_type.IsClass && _diagnoseReferencedClasses) // This is honestly terrifying.
                {
                    foreach (var field in a_type.GetFields())
                    {
                        // This is extremely badly performant so use at will.
                        System.Type typ = field.FieldType;

                        if (typ.IsPrimitive || typ.IsValueType && typ.IsLayoutSequential)
                        {
                            size = System.Runtime.InteropServices.Marshal.SizeOf(typ);
                        }
                        else if (typ.IsGenericType)
                        {
                            foreach (var generic in typ.GetGenericArguments())
                                size += GetBytes(generic, null);
                        }
                    }
                }
            }

            if (a_value != null && a_type.IsArray)
            {
                count = (a_value as Array).Length;
            }

            if (count > 0)
                size *= count;

            return size;
        }

        private GUIContent GetGUIContent(MemberInfo a_info, SerializedProperty a_prop, bool a_isRequired = false, Texture a_icon = null)
        {
            GUIContent content = new GUIContent($"{a_prop.displayName}{(a_isRequired ? "*" : "")}", "");

            AberrationDescriptionAttribute description = a_info.GetCustomAttribute<AberrationDescriptionAttribute>();
            if (description != null)
                content.tooltip = description.description;

            if (a_icon != null)
                content.image = a_icon;

            return content;
        }

        private void DisplayDescriptionClass(Type a_classType, GUIStyle a_style)
        {
            if (_showDescriptions)
                DisplayDescription(a_classType, a_style, _showAuthors);

            string classInterfaces = "";

            foreach (var inter in a_classType.GetInterfaces())
            {
                classInterfaces += $", <color=teal>{inter.Name}</color>";
            }

            GUILayout.Label($"<color=lightblue>{TypeGetModifier(a_classType)} class <color=silver>{a_classType.Namespace}</color>.<color=lime>{a_classType.Name}</color> : <color=lime>{a_classType.BaseType.Name}</color>{classInterfaces}</color>", a_style);
        }

        private void DisplayDescription(MemberInfo a_obj, GUIStyle a_style, bool a_showAuthors = false)
        {
            AberrationDescriptionAttribute description = a_obj.GetCustomAttribute<AberrationDescriptionAttribute>();
            if (description != null)
            {
                if (a_showAuthors && (!string.IsNullOrEmpty(description.author) || !string.IsNullOrEmpty(description.lastEdit) || !string.IsNullOrEmpty(description.id)))
                {
                    string author = $"Author: <color=orange>{description.author}</color> ";
                    string identification = $"ID: <color=orange>{description.id}</color> ";
                    string lastEdit = $"Last Edit: <color=orange>{description.lastEdit}</color> ";

                    string display = "";
                    if (!string.IsNullOrEmpty(description.author))
                        display += author;
                    if (!string.IsNullOrEmpty(description.id))
                        display += identification;
                    if (!string.IsNullOrEmpty(description.lastEdit))
                        display += lastEdit;

                    GUILayout.Label($"<color=grey><size=12>// {display}</size></color>", a_style);
                }

                GUILayout.Label($"<color=grey><size=12>// {description.description}</size></color>", a_style);
            }
        }

        private void DisplayDescription(System.Type a_type, GUIStyle a_style, bool a_showAuthors = false)
        {
            AberrationDescriptionAttribute description = a_type.GetCustomAttribute<AberrationDescriptionAttribute>();
            if (description != null) 
            {
                if (a_showAuthors && (!string.IsNullOrEmpty(description.author) || !string.IsNullOrEmpty(description.lastEdit) || !string.IsNullOrEmpty(description.id)))
                {
                    string author = $"Author: <color=orange>{description.author}</color> ";
                    string identification = $"ID: <color=orange>{description.id}</color> ";
                    string lastEdit = $"Last Edit: <color=orange>{description.lastEdit}</color> ";

                    string display = "";
                    if (!string.IsNullOrEmpty(description.author))
                        display += author;
                    if (!string.IsNullOrEmpty(description.id))
                        display += identification;
                    if (!string.IsNullOrEmpty(description.lastEdit))
                        display += lastEdit;

                    GUILayout.Label($"<color=grey><size=12>// {display}</size></color>", a_style);
                }
                    
                GUILayout.Label($"<color=grey><size=12>// {description.description}</size></color>", a_style);
            }
        }

        public static string MethodGetModifier(MethodInfo a_method)
        {
            string modifier = "private";

            if (a_method.IsFamilyAndAssembly)
                modifier = "protected internal";
            else if (a_method.IsFamily)
                modifier = "protected";
            else if (a_method.IsFamilyOrAssembly)
                modifier = "protected internal";
            else if (a_method.IsAssembly)
                modifier = "internal";
            else if (a_method.IsPublic)
                modifier = "public";

            if (a_method.IsStatic)
                modifier += " static";

            return modifier;
        }

        public static string TypeGetModifier(Type a_type)
        {
            string modifier = "private";

            if (a_type.IsNestedFamANDAssem)
                modifier = "protected internal";
            else if (a_type.IsNestedFamily)
                modifier = "protected";
            else if (a_type.IsNestedFamORAssem)
                modifier = "protected internal";
            else if (a_type.IsNestedAssembly)
                modifier = "internal";
            else if (a_type.IsPublic)
                modifier = "public";

            if (a_type.IsAbstract && a_type.IsSealed)
                modifier += " static ";

            return modifier;
        }

        public static string FieldGetModifier(FieldInfo a_field)
        {
            string modifier = "private";

            if (a_field.IsFamilyAndAssembly)
                modifier = "protected internal";
            else if (a_field.IsFamily)
                modifier = "protected";
            else if (a_field.IsFamilyOrAssembly)
                modifier = "protected internal";
            else if (a_field.IsAssembly)
                modifier = "internal";
            else if (a_field.IsPublic)
                modifier = "public";

            if (a_field.IsStatic)
                modifier += " static ";

            if (a_field.IsInitOnly)
                modifier += " readonly ";

            return modifier;
        }

        private string FieldNameConversion(FieldInfo a_fieldInfo, Dictionary<string, MethodInfo> a_methods = null)
        {
            string name = a_fieldInfo.Name;
            bool usingPrefix = name[0] == '_';

            string checkName = usingPrefix ? name.Substring(1) : name;
            bool nameIsTooLong = checkName.Length > 32;
            bool checkNameLower = char.IsLower(checkName[0]);
            bool nameIsFucked = false;

            foreach (char l in checkName)
            {
                if (l == '-' || l == '_' || char.IsDigit(l))
                {
                    nameIsFucked = true;
                }
            }

            string color = "aqua";

            string propertyName = "";
            if (name[0] == '<')
            {
                foreach (char l in checkName)
                {
                    if (l != '<')
                    {
                        if (l == '>')
                        {
                            break;
                        }
                        else
                        {
                            propertyName += l;
                        }
                    }
                }

                string getText = "<color=yellow>get</color>";
                string setText = "<color=yellow>set</color>";

                if (a_methods.TryGetValue("get_" + propertyName, out MethodInfo getter) && getter.ReturnType != a_fieldInfo.FieldType)
                    getText = $"{MethodGetModifier(getter)} {getText}";

                if (a_methods.TryGetValue("set_" + propertyName, out MethodInfo setter) && setter.ReturnType != a_fieldInfo.FieldType)
                    setText = $"{MethodGetModifier(setter)} {setText}";

                name = $"<color=silver>Property</color> {propertyName} <color=lightblue>{'{'} {getText}; {setText}; {'}'}</color>";
            }

            bool error = (a_fieldInfo.IsPublic && usingPrefix || !a_fieldInfo.IsPublic && !usingPrefix) || !checkNameLower || nameIsFucked;
            if (error && string.IsNullOrEmpty(propertyName))
            {
                color = "red";

                string camelLower = $"{char.ToLower(checkName[0])}{checkName.Substring(1)}";
                string pascalUpper = $"{char.ToUpper(checkName[0])}{checkName.Substring(1)}";

                if (a_fieldInfo.IsPublic && usingPrefix)
                    _debugErrors.Add(($"Incorrect naming conventions, should not use <color=red>_</color>", a_fieldInfo));
                
                if (!a_fieldInfo.IsPublic && !usingPrefix)
                    _debugErrors.Add(($"Incorrect naming conventions, should use <color=lime>_</color> instead of <color=red>{name[0]}</color>.", a_fieldInfo));
                
                if (!checkNameLower && !a_fieldInfo.IsStatic)
                    _debugErrors.Add(($"Incorrect naming conventions, fields should always be camelCase (<color=lime>{camelLower}</color>) instead of PascalCase (<color=red>{checkName}</color>).", a_fieldInfo));
                else if (checkNameLower && a_fieldInfo.IsStatic)
                    _debugErrors.Add(($"Incorrect naming conventions, static fields should always be PascalCase (<color=lime>{pascalUpper}</color>) instead of PascalCase (<color=red>{checkName}</color>).", a_fieldInfo));

                if (nameIsTooLong)
                    _debugErrors.Add(($"Incorrect naming conventions, field names should never be longer then 32 characters!", a_fieldInfo));
                
                if (nameIsFucked)
                    _debugErrors.Add(($"Incorrect naming conventions, fields should not contain <color=red>digits</color> or <color=red>_</color> or <color=red>-</color> outside of prefixes.", a_fieldInfo));
            }
            else if (!string.IsNullOrEmpty(propertyName) && checkNameLower)
            {
                string pascalUpper = $"{char.ToUpper(checkName[0])}{checkName.Substring(1)}";

                _debugErrors.Add(($"Incorrect naming conventions, properties should always be PascalCase (<color=lime>{pascalUpper}</color>) instead of casmelCase (<color=red>{checkName}</color>).", a_fieldInfo));
            }

            return $"<color={color}>{name}</color>";
        }

        private string MethodNameConversion(MethodInfo a_methodInfo)
        {
            string name = a_methodInfo.Name;
            bool usingCaps = char.IsUpper(name[0]);
            bool nameIsTooLong = name.Length > 32;
            bool nameIsFucked = name.Contains("_") || name.Contains("-");

            foreach (char l in name)
            {
                if (l == '-' || l == '_' || char.IsDigit(l))
                {
                    nameIsFucked = true;
                }
            }

            bool error = !usingCaps || nameIsTooLong || nameIsFucked;
            if (!error || a_methodInfo.IsSpecialName)
                name = $"<color=yellow>{name}</color>";
            else if (error)
            {
                string pascalUpper = $"{char.ToUpper(name[0])}{name.Substring(1)}";
                name = $"<color=red>{name}</color>";

                if (!usingCaps)
                    _debugErrors.Add(($"Incorrect naming conventions, methods should always be PascalCase (<color=lime>{pascalUpper}</color>) instead of camelCase (<color=red>{name}</color>).", a_methodInfo));

                if (nameIsTooLong)
                    _debugErrors.Add(($"Incorrect naming conventions, method names should never be longer then 32 characters!", a_methodInfo));

                if (nameIsFucked)
                    _debugErrors.Add(($"Incorrect naming conventions, methods must not contain <color=red>digits</color> or <color=red>_</color> or <color=red>-</color>.", a_methodInfo));
            }

            return name;
        }

        public bool IsInValidParam(string a_param)
        {
            return a_param[0] != 'a' || a_param[1] != '_';
        }

        public string ParamTypeConversion(System.Type a_type)
        {
            string name = $"<b>{a_type.Name.Replace("`", "").Replace("=", "").Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")}</b>";
            string col = "lightblue";

            if (a_type.Name == "Void")
                return $"<color={col}>{name.ToLower()}</color>";

            if (a_type.IsGenericType)
            {
                col = "lime";
                string pars = "";
                int count = 0;
                foreach (var generic in a_type.GetGenericArguments())
                {
                    if (count > 0)
                        pars += ", ";

                    count++;
                    pars += ParamTypeConversion(generic);
                }

                name = $"{name}< {pars} >";
            }
            else if (a_type.IsPrimitive)
                name = name.ToLower();
            else if (a_type.IsEnum)
                col = "orange";
            else if (a_type.IsInterface)
                col = "orange";
            else if (a_type.IsValueType)
                col = "teal";
            else if (a_type.IsClass)
                col = "green";

            if (a_type.IsArray)
                name += "<color=white>[]</color>";
            
            return $"<color={col}>{name}</color>";
        }
    }


    // This is horrible practice. Don't be like me, make lazy code that works with new stuff...
    // This was a quick hotfix copy paste so it works on scriptable objects, hopefully rework later.
    [CustomEditor(typeof(ScriptableObject), true)]
    [AberrationDescription("Aberration Games Custom Editor and Diagnoser", "Jacob Cooper", "27/10/2021")]
    public class AberrationExtraEditorScriptableObjects : Editor
    {
        public static readonly Color DEFAULT_COLOR = new Color(0f, 0f, 0f, 0.3f);
        public static readonly Color DEFAULT_ERROR_COLOR = new Color(0.3f, 0.1f, 0.1f, 0.3f);
        public static readonly Vector2 DEFAULT_LINE_MARGIN = new Vector2(2f, 2f);

        public const float DEFAULT_LINE_HEIGHT = 1f;

        private int _toolBarInt = 0;

        private bool _profiled = false;

        private Vector2 _scrollBarPos;
        private Vector2 _scrollBarPos2;

        public void CreateHorizontalLine(Color a_color, float a_height, Vector2 a_margin)
        {
            GUILayout.Space(a_margin.x);

            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, a_height), a_color);

            GUILayout.Space(a_margin.y);
        }

        public void CreateHorizontalLine()
        {
            CreateHorizontalLine(DEFAULT_COLOR, DEFAULT_LINE_HEIGHT, DEFAULT_LINE_MARGIN);
        }

        private void GetMethods(System.Type a_typ, out Dictionary<string, MethodInfo> methods)
        {
            methods = new Dictionary<string, MethodInfo>();

            foreach (var method in a_typ.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static))
            {
                methods[method.Name] = method;
            }
        }

        private void GetFields(System.Type a_typ, out Dictionary<string, SerializedProperty> properties,
            out Dictionary<string, SerializedProperty> serializedProperties,
            out Dictionary<string, FieldInfo> fields,
            out Dictionary<FieldInfo, string> fieldToolBar,
            out Dictionary<string, Texture> toolBars,
            out Dictionary<FieldInfo, bool> required,
            out string toolBarName,
            int a_instanceID = 0)
        {
            properties = new Dictionary<string, SerializedProperty>();
            serializedProperties = new Dictionary<string, SerializedProperty>();

            fields = new Dictionary<string, FieldInfo>();
            fieldToolBar = new Dictionary<FieldInfo, string>();
            required = new Dictionary<FieldInfo, bool>();
            toolBars = new Dictionary<string, Texture>();

            toolBarName = "";

            bool errored = false;

            Texture errorIcon = EditorGUIUtility.IconContent("console.erroricon.sml").image;

            foreach (var field in a_typ.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static))
            {
                if (field.Name == "m_Script")
                    continue;

                fields[field.Name] = field;

                AberrationToolBarAttribute toolAttr = field.GetCustomAttribute<AberrationToolBarAttribute>();
                AberrationEndToolBarAttribute tattr = field.GetCustomAttribute<AberrationEndToolBarAttribute>();

                if (toolAttr == null && tattr != null)
                {
                    toolBarName = "";
                }

                if (toolAttr != null)
                {
                    toolBarName = toolAttr.name;
                }

                AberrationRequiredAttribute requiredAttribute = field.GetCustomAttribute<AberrationRequiredAttribute>();
                Texture icon = null;
                bool isRequired = requiredAttribute != null;

                if (isRequired)
                {
                    object requirement = field.GetValue(target);

                    // Null gate is weird cuz reflection :(
                    if (NullByType(requirement) || requiredAttribute.type != null && requirement.GetType().IsInstanceOfType(requiredAttribute.type))
                    {
                        icon = errorIcon;

                        errored = true;

                        if (toolBars.ContainsKey(toolBarName))
                            toolBars[toolBarName] = icon;

                        Debug.LogError($"<color=gray>{target.GetType()}</color> > <color=lightblue><b>{field.Name}</b></color> is missing the requirement of <b>{ParamTypeConversion(requiredAttribute.type != null ? requiredAttribute.type : field.FieldType)}</b>");
                    }
                }

                required[field] = isRequired;

                if (toolAttr != null)
                {
                    toolBars.Add(toolBarName, icon);
                }

                fieldToolBar[field] = toolBarName;
            }

            if (errored && a_instanceID != 0 && HierarchyIcon.Icons.ContainsKey(a_instanceID))
            {
                HierarchyIcon.Icons[a_instanceID][target.GetInstanceID() - 100] = errorIcon;
            }
            else if (!errored && a_instanceID != 0 && HierarchyIcon.Icons.ContainsKey(a_instanceID) && HierarchyIcon.Icons[a_instanceID].ContainsKey(target.GetInstanceID() - 100))
            {
                HierarchyIcon.Icons[a_instanceID].Remove(target.GetInstanceID() - 100);
            }
        }

        private bool NullByType(object a_obj)
        {
            if (a_obj == null || a_obj.ToString() == "null" || string.IsNullOrEmpty(a_obj.ToString()))
                return true;

            if (a_obj.GetType().IsPrimitive)
            {
                if (a_obj is float)
                    if ((float)a_obj == 0f)
                        return true;
                    else if (a_obj is int)
                        if ((int)a_obj == 0)
                            return true;
            }

            return false;
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || target == null)
                return;

            EditorGUI.BeginChangeCheck();
            serializedObject.UpdateIfRequiredOrScript();

            SerializedProperty property = serializedObject.GetIterator();
            System.Type typ = serializedObject.targetObject.GetType();

            var declaration = typ.GetCustomAttribute<AberrationDeclareAttribute>();
            if (declaration == null)
            {
                base.OnInspectorGUI();

                EditorGUI.EndChangeCheck();

                return;
            }

            GUIStyle style = new GUIStyle
            {
                richText = true
            };

            GetMethods(typ, out Dictionary<string, MethodInfo> methods);

            GetFields(typ, out Dictionary<string, SerializedProperty> properties,
                out Dictionary<string, SerializedProperty> serializedProperties,
                out Dictionary<string, FieldInfo> fields,
                out Dictionary<FieldInfo, string> fieldToolBar,
                out Dictionary<string, Texture> toolBars,
                out Dictionary<FieldInfo, bool> required,
                out string toolBarName,
                ((ScriptableObject)target).GetInstanceID());

            string[] toolbarArray = new string[0];
            if (toolBars.Count > 0)
            {
                toolbarArray = new string[toolBars.Keys.Count];

                toolBars.Keys.CopyTo(toolbarArray, 0);

                List<GUIContent> contents = new List<GUIContent>();

                foreach (var tool in toolBars)
                {
                    contents.Add(new GUIContent(tool.Key, tool.Value, ""));
                }

                _toolBarInt = GUILayout.Toolbar(_toolBarInt, contents.ToArray());
                GUILayout.Space(10f);
            }

            bool expanded = true;
            List<bool> showing = new List<bool> { true };
            bool showThis = false;
            bool foldout = false;
            bool doneNoCatBar = false;

            int foldOutCount = 0;
            while (property.NextVisible(expanded))
            {
                if (property.propertyPath == "m_Script")
                    continue;

                List<AberrationButtonAttribute> buttons = new List<AberrationButtonAttribute>();

                serializedProperties[property.name] = property;

                bool noCat = false;

                if (fields.TryGetValue(property.name, out FieldInfo field))
                {
                    AberrationRequiredAttribute requiredAttribute = field.GetCustomAttribute<AberrationRequiredAttribute>();
                    Texture icon = null;
                    bool isRequired = required[field] && requiredAttribute != null;

                    if (isRequired)
                    {
                        object requirement = field.GetValue(target);

                        if (NullByType(requirement) || requiredAttribute.type != null && requirement.GetType().IsInstanceOfType(requiredAttribute.type))
                        {
                            icon = EditorGUIUtility.IconContent("console.erroricon.sml").image;
                        }
                    }

                    if (fieldToolBar.TryGetValue(field, out string barName))
                    {
                        if (!string.IsNullOrEmpty(barName) && barName != toolbarArray[_toolBarInt])
                        {
                            continue;
                        }
                        else if (string.IsNullOrEmpty(barName))
                            noCat = true;
                    }

                    properties[property.name] = property;

                    IEnumerable<AberrationButtonAttribute> battr = field.GetCustomAttributes<AberrationButtonAttribute>();

                    bool foundButton = false;
                    foreach (var buttonAttribute in battr)
                    {
                        switch (buttonAttribute.declaredType)
                        {
                            case DeclaredButtonTypes.Button:
                                if (!string.IsNullOrEmpty(buttonAttribute.reflect) && methods.ContainsKey(buttonAttribute.reflect))
                                {
                                    buttons.Add(buttonAttribute);
                                    foundButton = true;
                                }

                                break;
                            case DeclaredButtonTypes.Hidden:
                                if (field.FieldType == typeof(bool) && buttonAttribute != null)
                                {
                                    buttonAttribute.enabled = property.boolValue;
                                    foldout = true;
                                    showThis = true;
                                    foundButton = true;

                                    foldOutCount++;

                                    showing.Insert(foldOutCount, buttonAttribute.enabled);
                                }

                                break;
                            default:
                                break;
                        }
                    }

                    if (foundButton)
                    {
                        AberrationFinishButtonAttribute fattr = field.GetCustomAttribute<AberrationFinishButtonAttribute>();

                        if (fattr != null)
                        {
                            foldOutCount--;
                            if (foundButton)
                            {
                                foldout = false;
                                showing.RemoveAt(foldOutCount);
                            }
                        }
                    }

                    if (showing[foldOutCount] || showThis && showing[foldOutCount - 1])
                    {
                        if (noCat && !doneNoCatBar)
                        {
                            doneNoCatBar = true;
                            GUILayout.Space(10f);
                            CreateHorizontalLine();
                        }

                        GUILayout.BeginHorizontal();
                        if (foldout && !showThis)
                            GUILayout.Space(20 * foldOutCount);

                        foreach (var button in buttons)
                        {
                            if (GUILayout.Button(button.name))
                            {
                                MethodInfo method = methods[button.reflect];
                                method.Invoke(target, button.parse);
                            }
                        }
                        GUILayout.EndHorizontal();

                        using (new EditorGUI.DisabledScope("m_Script" == property.propertyPath))
                        {
                            int spacing = foldOutCount > 0 ? (20 * (foldOutCount - 1)) : 0;
                            if (foldout && !showThis)
                                spacing = 20 * foldOutCount;

                            GUILayout.BeginHorizontal();
                            GUILayout.Space(spacing);

                            EditorGUILayout.PropertyField(property, GetGUIContent(field, property, isRequired, icon), true);
                            GUILayout.EndHorizontal();
                        }

                        showThis = false;
                    }
                }

                expanded = false;
            }

            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();
        }

        public string ParamTypeConversion(System.Type a_type)
        {
            string name = $"<b>{a_type.Name.Replace("`", "").Replace("=", "").Replace("1", "").Replace("2", "").Replace("3", "").Replace("4", "")}</b>";
            string col = "lightblue";

            if (a_type.Name == "Void")
                return $"<color={col}>{name.ToLower()}</color>";

            if (a_type.IsGenericType)
            {
                col = "lime";
                string pars = "";
                int count = 0;
                foreach (var generic in a_type.GetGenericArguments())
                {
                    if (count > 0)
                        pars += ", ";

                    count++;
                    pars += ParamTypeConversion(generic);
                }

                name = $"{name}< {pars} >";
            }
            else if (a_type.IsPrimitive)
                name = name.ToLower();
            else if (a_type.IsEnum)
                col = "orange";
            else if (a_type.IsInterface)
                col = "orange";
            else if (a_type.IsValueType)
                col = "teal";
            else if (a_type.IsClass)
                col = "green";

            if (a_type.IsArray)
                name += "<color=white>[]</color>";

            return $"<color={col}>{name}</color>";
        }

        private GUIContent GetGUIContent(MemberInfo a_info, SerializedProperty a_prop, bool a_isRequired = false, Texture a_icon = null)
        {
            GUIContent content = new GUIContent($"{a_prop.displayName}{(a_isRequired ? "*" : "")}", "");

            AberrationDescriptionAttribute description = a_info.GetCustomAttribute<AberrationDescriptionAttribute>();
            if (description != null)
                content.tooltip = description.description;

            if (a_icon != null)
                content.image = a_icon;

            return content;
        }
    }
}
#endif