using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace AberrationGames.Utils
{
    [EditorTools.AberrationDescription("Simple tool for UI buttons / scene changing code.", "Jacob Cooper", "15/10/2021")]
    public class ChangeScene : MonoBehaviour
    {
        public static ChangeScene Instance;
        private string _scene;

        public void Awake()
        {
            Instance = this;
        }

        public void Exit()
        {
            Application.Quit();
        }

        public void SetScene(int a_scene)
        {
            SceneManager.LoadScene(a_scene);

            Base.Players.ChangingScenes = true;
        }

        public void SetScene(string a_scene)
        {
            SceneManager.LoadScene(2);

            Events.MapLoader.nextScene = a_scene;

            Base.Players.ChangingScenes = true;
        }

        public void AsyncScene(int a_scene)
        {
            SceneManager.LoadSceneAsync(_scene, LoadSceneMode.Additive);
            Base.Players.ChangingScenes = true;
        }
    }
}
