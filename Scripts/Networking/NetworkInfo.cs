using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AberrationGames.Networking
{
    public class NetworkInfo : MonoBehaviour
    {
        public static NetworkInfo main;

        public string ip;

        [HideInInspector()]
        public bool aboutToLaunch = false;

        public GameObject networkingPrefabCollection;

        public void Start()
        {
            main = this;

            DontDestroyOnLoad(gameObject);
        }

        public void Launch(string a_scene)
        {
            SetLaunch(true);

            Base.Players.ChangingScenes = true;

            LoadIntoScene(a_scene);
        }

        public void SetLaunch(bool a_launch)
        {
            aboutToLaunch = a_launch;
        }

        // TODO
        // Setup scene loading
        public void LoadIntoScene(string a_scene)
        {
            GameObject obj = Instantiate(networkingPrefabCollection, gameObject.transform);
            UnityClient client = obj.GetComponent<UnityClient>();
            client.Host = ip;

            client.Connect(ip, 4296, false);

            NetworkSpawner spawner = obj.GetComponent<NetworkSpawner>();
            spawner.scene = a_scene;

            Debug.Log(spawner.scene);

            SetLaunch(false);

            //Destroy(gameObject);
        }
    }
}
