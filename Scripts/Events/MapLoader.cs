using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AberrationGames.Events
{
    [EditorTools.AberrationDescription("Loads prefabed maps asynchronously into the scene with a loading screen.", "Jacob Cooper", "15/10/2021")]
    public class MapLoader : MonoBehaviour
    {
        public static string nextScene = "";

        public Canvas canvas;
        public Image banner;
        public Slider progress;
        public Text percentage;

        private GameObject _obj;

        public void Start()
        {
            if (Networking.NetworkSpawner.main != null)
                nextScene = Networking.NetworkSpawner.main.scene;

            StartCoroutine(LoadLevel());
            //StartCoroutine(LoadScene());
        }

        IEnumerator LoadLevel()
        {
            yield return null;

            SceneManager.LoadScene(3, LoadSceneMode.Additive);

            if (nextScene == "Lobby")
            {
                var hand = Addressables.LoadSceneAsync(nextScene);

                bool isDone = false;
                hand.Completed += handle =>
                {
                    isDone = true;

                    if (Networking.Shared.NetworkInformation.IsNetworking && Networking.Shared.NetworkInformation.Realm == Networking.Shared.NetworkRealm.Local)
                    {
                        using (DarkRift.Message message = DarkRift.Message.CreateEmpty((ushort)Networking.Shared.NetworkTags.LobbyJoinSuccessful))
                            Networking.Client.ConnectionManager.Instance.Client.SendMessage(message, DarkRift.SendMode.Reliable);

                        if (Base.PlayerLoader.Instance.gameObject.TryGetComponent(out UnityEngine.InputSystem.PlayerInputManager input))
                        {
                            input.DisableJoining();
                        }
                    }
                };

                while (!isDone)
                {
                    //Output the current progress
                    float prog = hand.PercentComplete;

                    progress.value = prog;
                    percentage.text = (prog * 100) + "%";

                    // Check if the load has finished
                    if (prog >= 1f)
                    {
                        percentage.text = "Completed";
                    }

                    yield return null;
                }
            }
            else
            {
                var hand = Addressables.InstantiateAsync(nextScene, Vector3.zero, Quaternion.identity, null, true);

                bool isDone = false;
                hand.Completed += handle =>
                {
                    isDone = true;

                    _obj = handle.Result;
                };

                while (!isDone)
                {
                    //Output the current progress
                    float prog = hand.PercentComplete;

                    progress.value = prog;
                    percentage.text = (prog * 100) + "%";

                    // Check if the load has finished
                    if (prog >= 1f)
                    {
                        percentage.text = "Completed";
                    }

                    yield return null;
                }

                yield return new WaitForSeconds(Settings.Instance.settingsReference.sceneFakeLoadDelay);

                Scene scene = SceneManager.GetSceneAt(0);
                Scene main = SceneManager.GetSceneAt(1);
                SceneManager.MoveGameObjectToScene(_obj, main);

                SceneManager.UnloadSceneAsync(scene.buildIndex);

                Round.Instance.StartGame();
            }
        }

        IEnumerator LoadScene()
        {
            yield return null;// new WaitForSeconds(0.1f);

            AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(nextScene);

            asyncOperation.allowSceneActivation = false;

            while (!asyncOperation.isDone)
            {
                //Output the current progress
                float prog = (asyncOperation.progress + 0.1f);

                progress.value = prog;
                percentage.text = (prog * 100) + "%";

                // Check if the load has finished
                if (prog >= 1f)
                {
                    percentage.text = "Completed";
                    asyncOperation.allowSceneActivation = true;

                    //if (Networking.NetworkInfo.main != null && Networking.NetworkInfo.main.aboutToLaunch)
                    //{
                    //    Networking.NetworkInfo.main.LoadIntoScene();
                    //}
                }

                yield return null;
            }
        }
    }
}
