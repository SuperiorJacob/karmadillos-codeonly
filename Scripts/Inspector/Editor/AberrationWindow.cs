using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using System.Diagnostics;

namespace AberrationGames.EditorTools
{
    [AberrationDescription("Inspector Editor", "Jacob Cooper", "15/10/2021")]
    public class AberrationWindow : EditorWindow
    {
        public GUIContent[] categories;
        public Action<Rect>[] categoryFunctions;

        

        #region Inspection Menu
        private GameObject _inspection;
        private Dictionary<string, object> _display;
        private Vector2 _scrollPos;

        private bool _inspected = false;
        private int _toolbar = 0;
        #endregion

        #region Markdown Compiler
        private List<string> _compileDisplay;
        private string _compilerPath;
        private bool _compiled = false;
        #endregion

        #region Multi-Build
        public bool autoBuild = false;
        public bool shouldBuild = false;
        private string _buildPath = @"Z:/Prog_Yr2/Major Builds/Aberration Games";
        private string _buildName = "1.0.0";
        private BuildTarget _buildTarget = BuildTarget.StandaloneWindows64;
        private bool _buildServer = false;
        private bool _autoZip = false;
        #endregion

        #region Asset List Compiler
        private List<string> _compileDisplay1;
        private Dictionary<string, object> _assetList;
        private Dictionary<string, string> _descriptions;
        private string _compilerPath1;
        private long _fileSize = 0;
        private bool _compiled1 = false;
        private char _splitChar = '%';
        private bool _compilerSize = false;
        #endregion


        [MenuItem("Window/Aberration Window")]
        public static void ShowWindow()
        {
            AberrationWindow window = (AberrationWindow)EditorWindow.GetWindow(typeof(AberrationWindow), true, "Aberration Window", true);
            window.Init();
        }

        [InitializeOnLoadMethod]
        public static void CheckBuild()
        {
            // Automatically build on refresh.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                AberrationWindow window = (AberrationWindow)EditorWindow.GetWindow(typeof(AberrationWindow), false, "Aberration Window", false);

                if (window.autoBuild)
                    window.shouldBuild = true;
            };
        }

        #region Inspection Menu
        public void GameObjectInfo()
        {
            int objects = _inspection.transform.childCount + 1;

            MonoBehaviour[] scripts = _inspection.GetComponents<MonoBehaviour>();
            MonoBehaviour[] scriptsInChildren = _inspection.GetComponentsInChildren<MonoBehaviour>();

            Light[] lights = _inspection.GetComponents<Light>();
            Light[] lightsInChildren = _inspection.GetComponentsInChildren<Light>();

            Collider[] colliders = _inspection.GetComponents<Collider>();
            Collider[] collidersInChildren = _inspection.GetComponentsInChildren<Collider>();

            Collider[] colliderJoined = new Collider[colliders.Length + collidersInChildren.Length];

            Array.Copy(colliders, colliderJoined, colliders.Length);
            Array.Copy(collidersInChildren, colliderJoined, collidersInChildren.Length);

            Rigidbody[] rigidbodies = _inspection.GetComponents<Rigidbody>();
            Rigidbody[] rigidbodiesInChildren = _inspection.GetComponentsInChildren<Rigidbody>();

            Rigidbody[] rigidbodiesJoined = new Rigidbody[rigidbodies.Length + rigidbodiesInChildren.Length];

            Array.Copy(rigidbodies, rigidbodiesJoined, rigidbodies.Length);
            Array.Copy(rigidbodiesInChildren, rigidbodiesJoined, rigidbodiesInChildren.Length);

            long colliderCount = colliderJoined.Length;
            long physicsMats = 0;
            long triggers = 0;
            long convex = 0;
            long vertexCount = 0;
            long triangleCount = 0;
            long polygonCount = 0;
            long colliderMemory = 0;
            long rigidodyMemory = 0;

            foreach (var rigid in rigidbodies)
            {
                if (rigid != null)
                {
                    rigidodyMemory += Profiler.GetRuntimeMemorySizeLong(rigid);
                }
            }

            foreach (var collider in colliderJoined)
            {
                if (collider == null)
                    continue;

                if (collider.sharedMaterial != null)
                    physicsMats++;

                if (collider.isTrigger)
                    triggers++;

                colliderMemory += Profiler.GetRuntimeMemorySizeLong(collider);

                if (collider is MeshCollider)
                {
                    MeshCollider col = (MeshCollider)collider;
                    if (col.convex)
                        convex++;

                    if (col != null && col.sharedMesh != null)
                    {
                        vertexCount += col.sharedMesh.vertexCount;
                        triangleCount += col.sharedMesh.triangles.Length;
                        polygonCount += col.sharedMesh.triangles.Length / 3;
                    }
                }
                    
            }

            _display.Add("<color=lime><size=18>Game Object Info</size></color>", null);
            _display.Add("<color=lime>Name</color>", _inspection.name);
            _display.Add("<color=lime>Tag</color>", _inspection.tag);
            _display.Add("<color=lime>Game Object Count</color>", objects);
            _display.Add("<color=lime>Component Count</color>", scripts.Length + scriptsInChildren.Length);
            _display.Add("<color=lime>Light Count</color>", lights.Length + lightsInChildren.Length);
            _display.Add("<color=lime>Rigidbody Count</color>", rigidbodies.Length);
            _display.Add("<color=lime>Rigidbody Memory Usage</color>", $"{((float)rigidodyMemory) / 1024 / 1024} MB");
            
            _display.Add("<color=green><size=18>Collider Info</size></color>", null);
            _display.Add("<color=green>Collider Count</color>", colliderCount);
            _display.Add("<color=green>Collider Memory Usage</color>", $"{((float)colliderMemory) / 1024 / 1024} MB");
            _display.Add("<color=green>Collider PhysicMats</color>", physicsMats);
            _display.Add("<color=green>Collider Triggers</color>", triggers);
            _display.Add("<color=green>MeshCollider Convexs</color>", convex);
            _display.Add("<color=green>MeshCollider Vertexs</color>", vertexCount);
            _display.Add("<color=green>MeshCollider Triangles</color>", triangleCount);
            _display.Add("<color=green>MeshCollider Polys</color>", polygonCount);
        }

        public long MeshInfo()
        {
            bool isUser = _inspection.tag == "User";

            MeshFilter[] meshFilters = _inspection.GetComponents<MeshFilter>();
            MeshFilter[] meshFilterInChildren = _inspection.GetComponentsInChildren<MeshFilter>();

            MeshFilter[] joined = new MeshFilter[meshFilters.Length + meshFilterInChildren.Length];

            Array.Copy(meshFilters, joined, meshFilters.Length);
            Array.Copy(meshFilterInChildren, joined, meshFilterInChildren.Length);

            long meshCount = 0;
            long vertexCount = 0;
            long triangleCount = 0;
            long subMeshCount = 0;
            long boneWeightCount = 0;
            foreach (var filter in joined)
            {
                if (filter == null)
                    continue;

                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                meshCount++;
                vertexCount += mesh.vertexCount;
                triangleCount += mesh.triangles.Length;
                subMeshCount += mesh.subMeshCount;
                boneWeightCount += mesh.boneWeights.Length;
            }

            long polygonCount = triangleCount / 3;
            int triLimit = isUser ? 50000 : 100000;

            _display.Add("<color=lightblue><size=18>Mesh Info</size></color>", null);
            _display.Add("<color=lightblue>Mesh Count</color>", meshCount);
            _display.Add("Bar Mesh", (0, isUser ? 5 : 50, meshCount, "Mesh Limit"));
            _display.Add("<color=lightblue>Vertex Count</color>", vertexCount);
            _display.Add("<color=lightblue>Tri Count</color>", triangleCount);
            _display.Add("Bar Tri", (0, triLimit, triangleCount, "Tri Limit"));
            _display.Add("<color=lightblue>Polygon Count</color>", polygonCount);
            _display.Add("<color=lightblue>Sub Mesh Count</color>", subMeshCount);
            _display.Add("<color=lightblue>Bone Weight Count</color>", boneWeightCount);

            return meshCount;
        }

        public long MaterialInfo(long a_meshes = 0)
        {
            Renderer[] renders = _inspection.GetComponents<Renderer>();
            Renderer[] rendersInChildren = _inspection.GetComponentsInChildren<Renderer>();

            Renderer[] joined = new Renderer[renders.Length + rendersInChildren.Length];

            Array.Copy(renders, joined, renders.Length);
            Array.Copy(rendersInChildren, joined, rendersInChildren.Length);

            long rendererCount = 0;
            long referencedMaterialCount = 0;
            long textureCount = 0;
            long referencedTextureCount = 0;
            long shaderCount = 0;
            long textureMemoryUsage = 0;

            HideFlags hideFlagMask = HideFlags.HideInInspector | HideFlags.HideAndDontSave;
            HideFlags hideFlagMask1 = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy | HideFlags.NotEditable | HideFlags.DontUnloadUnusedAsset;

            List<Material> dontReuseMats = new List<Material>();
            List<Texture> dontReuseTexs = new List<Texture>();
            foreach (var render in joined)
            {
                if (render == null)
                    continue;

                rendererCount++;

                foreach (var mat in render.sharedMaterials)
                {
                    if (mat == null)
                        continue;

                    if (dontReuseMats.Contains(mat))
                        continue;

                    dontReuseMats.Add(mat);
                    referencedMaterialCount++;
                    shaderCount++;
                }

                foreach (UnityEngine.Object obj in EditorUtility.CollectDependencies(new UnityEngine.Object[] { render }))
                {
                    Texture tex = obj as Texture;
                    if (tex != null)
                    {
                        textureCount++;

                        if (dontReuseTexs.Contains(tex) || tex.hideFlags == HideFlags.HideAndDontSave || tex.hideFlags == hideFlagMask || tex.hideFlags == hideFlagMask1)
                            continue;

                        dontReuseTexs.Add(tex);

                        referencedTextureCount++;

                        textureMemoryUsage += Profiler.GetRuntimeMemorySizeLong(tex);
                    }
                }
            }

            float mem = ((float)textureMemoryUsage) / 1024 / 1024;
            long drawCalls = a_meshes + referencedMaterialCount;
            _display.Add("<color=yellow><size=18>Render Info</size></color>", null);
            _display.Add("<color=yellow>Renderer Count</color>", referencedMaterialCount);
            _display.Add("<color=yellow>Material Count</color>", referencedMaterialCount);
            _display.Add("<color=yellow>Draw Calls</color>", drawCalls);
            _display.Add("Bar Draw Calls", (0, 100, drawCalls, "Draw Call Limit"));
            _display.Add("<color=yellow>Texture Count</color>", referencedTextureCount);
            _display.Add("<color=yellow>Texture Memory Usage</color>", $"{mem} MB");
            _display.Add("Bar Texture Memory", (0, 115, (long)mem, "Texture Memory MB Limit"));

            return referencedMaterialCount;
        }

        public void InspectionMenu(Rect a_area)
        {
            EditorGUILayout.BeginHorizontal();
            _inspection = (GameObject)EditorGUILayout.ObjectField(_inspection, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Inspect"))
            {
                if (_inspection == null)
                    ShowNotification(new GUIContent("No object selected for inspection."));
                else
                {
                    _display = new Dictionary<string, object>();

                    GameObjectInfo();
                    MaterialInfo(MeshInfo());

                    _inspected = true;
                }
            }

            if (_inspected && _display != null)
            {
                GUIStyle style = new GUIStyle { richText = true };

                GUILayout.Label("<size=20><color=white>Inspection</color></size>", style);

                _scrollPos = GUILayout.BeginScrollView(_scrollPos);
                GUILayout.BeginVertical();

                foreach (var display in _display)
                {
                    if (display.Value == null)
                    {
                        GUILayout.Space(10f);
                        GUILayout.Label($"<size=15><color=white><b>{display.Key}</b></color></size>", style);
                    }
                    else if (display.Key.Contains("Bar"))
                    {
                        (int min, int max, long val, string name) val = ((int min, int max, long val, string name))display.Value;

                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"<size=10><color=white><b>{val.min}</b></color></size>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, richText = true }, GUILayout.ExpandWidth(true));
                        GUILayout.Label($"<size=10><color=white><b>{val.max / 2}</b></color></size>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true }, GUILayout.ExpandWidth(true));
                        GUILayout.Label($"<size=10><color=white><b>{val.max}</b></color></size>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight, richText = true }, GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();
                        EditorGUI.ProgressBar(GUILayoutUtility.GetRect(a_area.width, 20), (Mathf.Clamp(val.val, val.min, val.max) / val.max), val.name);

                        //GUILayout.Space(20f);
                    }
                    else
                        GUILayout.Label($"<size=15><color=white><b>{display.Key}</b> = {display.Value}</color></size>", style);
                }

                GUILayout.EndVertical();
                GUILayout.EndScrollView();
            }
        }
        #endregion

        #region Markdown Compiler
        public string ParamTypeConversion(System.Type a_type)
        {
            // This is fucked ..
            string newChar = "";
            string name = a_type.Name.Replace("`", newChar).Replace("=", newChar).Replace("1", newChar).Replace("2", newChar).Replace("3", newChar).Replace("4", newChar);

            if (name == "Void")
                return name.ToLower();

            if (a_type.IsGenericType)
            {
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

            if (a_type.IsArray)
                name += "[]";

            return name;
        }

        private string DisplayDescriptionClass(Type a_classType)
        {
            string classInterfaces = "";

            foreach (var inter in a_classType.GetInterfaces())
            {
                classInterfaces += $", {inter.Name}";
            }

            return $@"{AberrationExtraEditor.TypeGetModifier(a_classType)} class *{a_classType.Namespace}*.**{a_classType.Name}** {(a_classType.BaseType != null ? @": " + a_classType.BaseType.Name.Replace("ValueType", "Struct") : "")}{classInterfaces}";
        }

        private void MarkdownWrite(System.Type a_type)
        {
            if (a_type.Name.StartsWith("<"))
                return;

            string prevDir = $@"{_compilerPath}";
            string[] namespaces = a_type.Namespace.Split('.');

            foreach (string nameSpace in namespaces)
            {
                string newDir = $@"{prevDir}\{nameSpace}";

                System.IO.Directory.CreateDirectory($@"{newDir}\");

                prevDir = newDir;
            }

            string dir = @$"{prevDir}\{a_type.Name}.md";
            //string n = namespaces[namespaces.Length - 1];

            StringBuilder builder = new StringBuilder()
                .Append($@"## {a_type.Name}")
                .AppendLine()
                .Append($@"{DisplayDescriptionClass(a_type)}")
                .AppendLine()
                .AppendLine();

            string description = "";
            var declaration = a_type.GetCustomAttribute<AberrationDescriptionAttribute>();
            if (declaration != null)
            {
                description = declaration.description;
                builder.Append($@"> **Author** {declaration.author}")
                    .AppendLine()
                    .Append($@"> **Last Edit** {declaration.lastEdit}");
            }

            builder.AppendLine()
                .Append("### Description")
                .AppendLine()
                .Append($@"{description}")
                .AppendLine();

            bool doneFields = false;
            foreach (var field in a_type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static))
            {
                if (field.IsSpecialName)
                    continue;

                if (!doneFields)
                {
                    doneFields = true;
                    builder.AppendLine().Append($@"### Fields");
                }

                var fieldDescriptor = field.GetCustomAttribute<AberrationDescriptionAttribute>();
                builder.AppendLine()
                    .Append($@" - **{AberrationExtraEditor.FieldGetModifier(field)} {ParamTypeConversion(field.FieldType)} {field.Name}** > *{(fieldDescriptor != null ? fieldDescriptor.description : "")}*");
            }

            bool doneMethods = false;
            foreach (var method in a_type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static))
            {
                if (method.GetBaseDefinition().ReflectedType != a_type
                    || method.Name[0] == '<'
                    || method.Name.StartsWith("get_")
                    || method.Name.StartsWith("set_"))
                                    continue;
                if (!doneMethods)
                {
                    doneMethods = true;
                    builder.AppendLine().AppendLine().Append($@"### Methods");
                }

                string par = "";

                var arr = method.GetParameters();
                int count = 0;
                foreach (var val in arr)
                {
                    if (val.IsOut || val.IsIn)
                        par += val.IsOut ? "out " : "in ";

                    par += $"{ParamTypeConversion(val.ParameterType)} {val.Name}";

                    if (val.IsOptional)
                        par += $" = {(val.DefaultValue == null ? "null" : val.DefaultValue.ToString())}";

                    count++;
                    if (count < arr.Length)
                        par += ", ";
                }

                var methodDescriptor = method.GetCustomAttribute<AberrationDescriptionAttribute>();
                builder.AppendLine()
                    .Append($@" - **{AberrationExtraEditor.MethodGetModifier(method)} {ParamTypeConversion(method.ReturnType)} {method.Name}({par})** > *{(methodDescriptor != null ? methodDescriptor.description : "")}*");
            }

            System.IO.File.WriteAllText(dir, builder.ToString());
            _compileDisplay.Add(dir);
        }

        public void MarkdownCompiler(Rect a_area)
        {
            EditorGUILayout.BeginHorizontal();
            _compilerPath = GUILayout.TextField(_compilerPath);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Compile To"))
            {
                if (string.IsNullOrEmpty(_compilerPath))
                    ShowNotification(new GUIContent("No path for the compiler..."));
                else
                {
                    string targetNamespace = "AberrationGames";

                    _compileDisplay = new List<string>();

                    IEnumerable<System.Type> GetAll()
                    {
                        return AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(assembly => assembly.GetTypes())
                            .Where(type => type.FullName.Contains(targetNamespace))
                            .Select(type => type);
                    }
                    GetAll().ToList().ForEach(x => MarkdownWrite(x));

                    _compiled = true;
                }
            }

            if (_compiled)
            {
                GUILayout.Label($"Compiled to {_compilerPath}");
                GUILayout.Label($"Scripts Compiled = {_compileDisplay.Count}");

                _scrollPos = GUILayout.BeginScrollView(_scrollPos, BackgroundStyle.Get(new Color(0.1f, 0.1f, 0.1f)));
                for (int i = 0; i < _compileDisplay.Count; i++)
                {
                    string comp = _compileDisplay[i];
                    GUILayout.Label($"[{i}] {comp}");
                }
                GUILayout.EndScrollView();
            }
        }
        #endregion

        #region Multi-Build
        public void MultiBuild(Rect a_area)
        {
            EditorGUILayout.BeginHorizontal();
            _buildPath = GUILayout.TextField(_buildPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _buildName = GUILayout.TextField(_buildName);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10f);

            _buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("Build Target", _buildTarget);

            _buildServer = GUILayout.Toggle(_buildServer, new GUIContent("Build Server"));
            _autoZip = GUILayout.Toggle(_autoZip, new GUIContent("7 Zip Finished Build"));
            autoBuild = GUILayout.Toggle(autoBuild, new GUIContent("Auto Build on Refresh"));

            GUILayout.Space(10f);

            if (shouldBuild || GUILayout.Button("Build"))
            {
                shouldBuild = false;

                if (string.IsNullOrEmpty(_buildPath) || string.IsNullOrEmpty(_buildName) || !System.IO.Directory.Exists(_buildPath))
                    ShowNotification(new GUIContent("Missing settings please update!"));
                else
                {
                    string applicationName = @"Karmadillo";

                    string loc = _buildPath + @"/" + _buildName;
                    string client = loc + @"/Client";
                    string server = loc + @"/Server";

                    // Create/Renew folders
                    if (System.IO.Directory.Exists(loc))
                        System.IO.Directory.Delete(loc, true);

                    System.IO.Directory.CreateDirectory(loc);
                    System.IO.Directory.CreateDirectory(client);
                    System.IO.Directory.CreateDirectory(server);

                    AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);
                    AddressableAssetSettings.BuildPlayerContent();

                    // Client
                    BuildPlayerOptions clientBuildOptions = new BuildPlayerOptions()
                    {
                        scenes = new[] {
                            "Assets/Game/Scenes/Main/IntroCredits.unity",
                            "Assets/Game/Scenes/Main/MainMenuNEW.unity",
                            "Assets/Game/Scenes/Main/LoadingScreen.unity",
                            "Assets/Game/Scenes/Main/CompletelyEmpty.unity"
                        },
                        locationPathName = client + @$"/{applicationName}.exe",
                        target = _buildTarget,
                        options = BuildOptions.None //| BuildOptions.Development
                    };

                    BuildPipeline.BuildPlayer(clientBuildOptions);

                    if (_buildServer)
                    {
                        // Server
                        BuildPlayerOptions serverBuildOptions = new BuildPlayerOptions()
                        {
                            scenes = new[] { "Assets/Game/Scenes/Networking/Server.unity", "Assets/Game/Scenes/Prototype/LoadingScreen.unity" },
                            locationPathName = server + @$"/{applicationName} Server.exe",
                            target = _buildTarget,
                            options = BuildOptions.None | BuildOptions.EnableHeadlessMode //BuildOptions.Development
                        };

                        BuildPipeline.BuildPlayer(serverBuildOptions);
                    }

                    if (_autoZip)
                    {
                        ProcessStartInfo p = new ProcessStartInfo
                        {
                            FileName = @"C:\Program Files\7-Zip\7z.exe",
                            Arguments = "a -t7z \"" + (loc + @"/Karmadillos.7z") + "\" \"" + client + "\" -mx=9",
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process x = Process.Start(p);
                        x.WaitForExit();
                    }
                }
            }
        }
        #endregion

        #region Asset List Compiler
        public Dictionary<string, object> RetrieveAssets(DirectoryInfo a_dirInfo, Dictionary<string, object> a_assetList)
        {
            foreach (var f in a_dirInfo.GetFiles())
            {
                if (!f.Attributes.HasFlag(FileAttributes.Hidden)
                    && !f.Name.Contains("meta"))
                {
                    long size = f.Length / 1000;
                    string ex = "KB";

                    if (size > 1000)
                    {
                        size /= 1000;
                        ex = "MB";

                        if (size > 1000)
                        {
                            size /= 1000;
                            ex = "GB";
                        }
                    }

                    a_assetList[f.Name] = size + " " + ex;

                    _fileSize += f.Length;
                    _compileDisplay1.Add(f.FullName + " | " + a_assetList[f.Name]);
                }
            }

            foreach (var d in a_dirInfo.GetDirectories())
            {
                if (!d.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    Dictionary<string, object> list = new Dictionary<string, object>();

                    a_assetList[d.Name] = RetrieveAssets(d, list);
                }
            }

            return a_assetList;
        }

        public string GetValueFromExtension(string a_fileName)
        {
            string fileName = "";
            string extension = a_fileName.Split('.')[1].ToLower();

            if (extension == "anim")
                fileName = $"Animation{_splitChar}Dev";
            else if (extension == "controller")
                fileName = $"Animator{_splitChar}Dev";
            else if (extension == "wav" || extension == "mp3" || extension == "ogg")
                fileName = $"Sound{_splitChar}Dev";
            else if (extension == "mat")
                fileName = $"Material{_splitChar}Art";
            else if (extension == "tga" || extension == "ttf" || extension == "tif" || extension == "png")
                fileName = $"Texture{_splitChar}Art";
            else if (extension == "rendertexture")
                fileName = $"RenderTexture{_splitChar}Dev";
            else if (extension == "shadergraph" || extension == "shadersubgraph" || extension == "hlsl" || extension == "shader")
                fileName = $"Shader{_splitChar}Dev";
            else if (extension == "fbx")
                fileName = $"Model{_splitChar}Art";
            else if (extension == "prefab")
                fileName = $"Prefab{_splitChar}Dev";
            else if (extension == "unity")
                fileName = $"Scene{_splitChar}Dev";
            else if (extension == "cs")
                fileName = $"Script{_splitChar}Dev";
            else if (extension == "exr" || extension == "hdr" || extension == "lighting")
                fileName = $"Lighting{_splitChar}Dev";
            else if (extension == "asset")
                fileName = $"Asset{_splitChar}Dev";
            else if (extension == "physicmaterial")
                fileName = $"PhysicMaterial{_splitChar}Dev";
            else if (extension == "inputactions")
                fileName = $"Input{_splitChar}Dev";

            return fileName;
        }

        public Dictionary<string, string> GetDescriptions()
        {
            Dictionary<string, string> descriptions = new Dictionary<string, string>();

            foreach (var typ in AppDomain.CurrentDomain.GetAssemblies()
                       .SelectMany(t => t.GetTypes())
                       .Where(t => t.IsClass && t.Namespace != null && t.Namespace.Contains("AberrationGames")))
            {
                UnityEngine.Debug.Log(typ.Name);

                AberrationDescriptionAttribute desc = typ.GetCustomAttribute<EditorTools.AberrationDescriptionAttribute>();

                if (desc != null)
                {
                    descriptions[typ.Name] = desc.description;
                }
            }

            return descriptions;
        }

        public void BuildStringFromAssets(string a_dir, Dictionary<string, object> a_asset, StringBuilder a_builder)
        {
            long size = 0;

            foreach (var asset in a_asset)
            {
                if (asset.Value.GetType() == typeof(string))
                {
                    string g = (string)asset.Value;

                    string[] splits = g.Split(' ');

                    long s = Int32.Parse(splits[0]);

                    if (splits[1] == "MB")
                        s *= 1000;
                    else if (splits[1] == "GB")
                        s *= 1000000;

                    size += s;
                }
            }

            string ex = "KB";
            if (size > 1000)
            {
                ex = "MB";
                size /= 1000;

                if (size > 1000)
                {
                    ex = "GB";
                    size /= 1000;
                }
            }

            a_builder.Append(a_dir + $"/{_splitChar}{_splitChar}{_splitChar}{size} {ex}").AppendLine();

            foreach (var asset in a_asset)
            {
                if (asset.Value.GetType() == typeof(string))
                {
                    string vex = GetValueFromExtension(asset.Key);
                    a_builder.Append(@$"{asset.Key}{_splitChar}{vex}{_splitChar}{asset.Value}");

                    if (vex.Contains("Script"))
                    {
                        string[] split = asset.Key.Split('.');

                        //UnityEngine.Debug.Log(split[0] + " " + _descriptions.ContainsKey(split[0]));

                        if (_descriptions.ContainsKey(split[0]))
                            a_builder.Append($@"{_splitChar}{_descriptions[split[0]]}");
                    }

                    a_builder.AppendLine();
                }
                else
                {
                    BuildStringFromAssets(asset.Key, (Dictionary<string, object>)asset.Value, a_builder);
                }
            }
        }

        public void BuildStringSizeFromAssets(string a_dir, Dictionary<string, object> a_asset, StringBuilder a_builder)
        {
            long size = 0;

            foreach (var asset in a_asset)
            {
                if (asset.Value.GetType() == typeof(string))
                {
                    string g = (string)asset.Value;

                    string[] splits = g.Split(' ');

                    long s = Int32.Parse(splits[0]);

                    if (splits[1] == "MB")
                        s *= 1000;
                    else if (splits[1] == "GB")
                        s *= 1000000;

                    size += s;
                }
            }

            a_builder.Append(a_dir + $"/{_splitChar}{size}").AppendLine();

            foreach (var asset in a_asset)
            {
                if (asset.Value.GetType() == typeof(string))
                {
                }
                else
                {
                    BuildStringSizeFromAssets(asset.Key, (Dictionary<string, object>)asset.Value, a_builder);
                }
            }
        }

        public void AssetListCompiler(Rect a_area)
        {
            EditorGUILayout.BeginHorizontal();
            _compilerPath1 = GUILayout.TextField(_compilerPath1);
            EditorGUILayout.EndHorizontal();
            _compilerSize = GUILayout.Toggle(_compilerSize, "Toggle send size.");

            if (GUILayout.Button("Compile To"))
            {
                if (string.IsNullOrEmpty(_compilerPath1))
                    ShowNotification(new GUIContent("No path for the compiler..."));
                else
                {
                    _compileDisplay1 = new List<string>();
                    _descriptions = GetDescriptions();

                    _fileSize = 0;
                    DirectoryInfo d = new DirectoryInfo(@Application.dataPath + "/Game");
                    // _compilerPath1

                    _assetList = RetrieveAssets(d, new Dictionary<string, object>());

                    StringBuilder builder = new StringBuilder();
                    builder.Append($"{_splitChar}{_splitChar}{_splitChar}{(float)_fileSize / 1000 / 1000 / 1000} GB").AppendLine();
                    builder.Append($"Name{_splitChar}Type{_splitChar}Group{_splitChar}Size{_splitChar}Description").AppendLine();

                    BuildStringFromAssets("", _assetList, builder);

                    System.IO.File.WriteAllText(_compilerPath1, builder.ToString());

                    if (_compilerSize)
                    {
                        StringBuilder builder1 = new StringBuilder();
                        builder1.Append($"{_splitChar}{(float)_fileSize / 1000 / 1000 / 1000} GB").AppendLine();
                        builder1.Append($"Name{_splitChar}Size").AppendLine();

                        BuildStringSizeFromAssets("", _assetList, builder1);

                        System.IO.File.WriteAllText(_compilerPath1.Replace(".txt", "_size.txt"), builder1.ToString());
                    }

                    _assetList = null;
                    _descriptions = null;
                    _compiled1 = true;
                }
            }

            if (_compiled1)
            {
                GUILayout.Label($"Compiled to {_compilerPath1}");
                GUILayout.Label($"Files Compiled = {_compileDisplay1.Count}");
                GUILayout.Label($"Asset Size = {(float)_fileSize / 1000 / 1000 / 1000} GB");

                _scrollPos = GUILayout.BeginScrollView(_scrollPos, BackgroundStyle.Get(new Color(0.1f, 0.1f, 0.1f)));
                for (int i = 0; i < _compileDisplay1.Count; i++)
                {
                    string comp = _compileDisplay1[i];
                    GUILayout.Label($"[{i}] {comp}");
                }
                GUILayout.EndScrollView();
            }
        }
        #endregion

        public void Init()
        {
            categories = new GUIContent[]
            {
                new GUIContent("Inspection Menu", EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image, ""),
                new GUIContent("Markdown Compiler", EditorGUIUtility.IconContent("AssetLabelIcon").image, ""),
                new GUIContent("Asset List Compiler", EditorGUIUtility.IconContent("AssetLabelIcon").image, ""),
                new GUIContent("Multi Build", EditorGUIUtility.IconContent("d_BuildSettings.SelectedIcon").image, "")
            };

            categoryFunctions = new Action<Rect>[]
            {
                InspectionMenu,
                MarkdownCompiler,
                AssetListCompiler,
                MultiBuild,
            };
        }

        private void OnGUI()
        {
            if (categories == null || categories.Length < 1 || categoryFunctions == null || categoryFunctions[0] == null)
                Init();

            Rect area = new Rect(0, 0, Mathf.Clamp(position.width / 4, 0, 230), position.height);
            GUILayout.BeginArea(area);
            _toolbar = GUILayout.SelectionGrid(_toolbar, categories, 1);
            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(area.width, 0, position.width - area.width, area.height));
            GUILayout.Label($"<color=white><b><size=15>{categories[_toolbar].text}</size></b></color>", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, richText = true });
            categoryFunctions[_toolbar].Invoke(area);
            GUILayout.EndArea();
        }
    }
}
