using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace AberrationGames.Utils
{
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug),
            EditorTools.AberrationDescription("Lightmap storage class.", "Jacob Cooper", "14/11/2021")]
    public class LightmapsStorage : MonoBehaviour
    {
        [EditorTools.AberrationButton(EditorTools.DeclaredButtonTypes.Button, false, "Load Bakery Data", "LoadBakery")]
        // List of baked lightmaps
        public List<Texture2D> maps = new List<Texture2D>();
        public List<Texture2D> masks = new List<Texture2D>();
        public List<Texture2D> dirMaps = new List<Texture2D>();
        public List<Texture2D> rnmMaps0 = new List<Texture2D>();
        public List<Texture2D> rnmMaps1 = new List<Texture2D>();
        public List<Texture2D> rnmMaps2 = new List<Texture2D>();
        public List<int> mapsMode = new List<int>();

        // new props
        public List<Renderer> bakedRenderers = new List<Renderer>();
        public List<int> bakedIDs = new List<int>();
        public List<Vector4> bakedScaleOffset = new List<Vector4>();
        public List<Mesh> bakedVertexColorMesh = new List<Mesh>();

        public List<Renderer> nonBakedRenderers = new List<Renderer>();

        public List<Light> bakedLights = new List<Light>();
        public List<int> bakedLightChannels = new List<int>();

        public List<Terrain> bakedRenderersTerrain = new List<Terrain>();
        public List<int> bakedIDsTerrain = new List<int>();
        public List<Vector4> bakedScaleOffsetTerrain = new List<Vector4>();

        public List<string> assetList = new List<string>();
        public List<int> uvOverlapAssetList = new List<int>(); // -1 = no UV1, 0 = no overlap, 1 = overlap

        public int[] idremap;

        public bool usesRealtimeGI;

        public Texture2D emptyDirectionTex;

        public bool anyVolumes = false;
        public bool compressedVolumes = false;

#if UNITY_EDITOR
        private void SetValue(string a_field, object a_value)
        {
            var typ = GetType();

            var field = typ.GetField(a_field);
            if (field != null)
                field.SetValue(this, a_value);
        }

        private void LoadBakery()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

            bool success = false;
            foreach (var behaviour in behaviours)
            {
                var typ = behaviour.GetType();
                if (typ.Name == "ftLightmapsStorage")
                {
                    foreach (var field in typ.GetFields())
                    {
                        SetValue(field.Name, field.GetValue(behaviour));
                    }

                    success = true;

                    break;
                }
            }

            if (success)
                Debug.Log("Bakery data successfully mounted.");
            else
                Debug.Log("Bakery does not exist.");
        }
#endif

        void Awake()
        {
            Lightmaps.RefreshScene(gameObject.scene, this);
        }

        void Start()
        {
            // Unity can for some reason alter lightmapIndex after the scene is loaded in a multi-scene setup, so fix that
            Lightmaps.RefreshScene2(gameObject.scene, this);//, appendOffset);
        }

        void OnDestroy()
        {
            Lightmaps.UnloadScene(this);
        }
    }
}