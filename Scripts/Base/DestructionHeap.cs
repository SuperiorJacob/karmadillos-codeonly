using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames
{
    [EditorTools.AberrationDescription("A memory management system that prevents stack overflows, invalid memory calls and stable memory deallocation.", "Jacob Cooper", "14/11/2021")]
    public class DestructionHeap : MonoBehaviour
    {
        public static DestructionHeap Instance;

        public Stack<Object> heap;

        public static bool ObjectNotOnHeap(Object a_object)
        {
            return !ObjectOnHeap(a_object);
        }

        public static bool ObjectOnHeap(Object a_object)
        {
            return Instance != null && Instance.heap != null && Instance.heap.Contains(a_object);
        }

        public static bool PrepareForDestruction(Object a_object)
        {
            bool preppable = !ObjectOnHeap(a_object);

            if (preppable && Instance != null && Instance.heap != null)
                Instance.heap.Push(a_object);

            return preppable;
        }

        public static void DestructPlayer(int a_id)
        {
            var data = Base.Players.PlayerDictionary[a_id];

            PrepareForDestruction(data.player);

            data.player = null;

            Base.Players.PlayerDictionary[a_id] = data;
        }

        public static void DestructRef(ref object a_reference)
        {
            if (a_reference is Object)
                PrepareForDestruction((Object)a_reference);

            a_reference = null;
        }

        private void CreateHeap()
        {
            heap = new Stack<Object>();

            Instance = this;
        }

        private void Destruct()
        {
            if (heap == null)
                CreateHeap();

            if (heap.Count < 1)
                return;

            Object obj = heap.Pop();
            Destroy(obj);
        }

        private void Awake()
        {
            CreateHeap();

            DontDestroyOnLoad(gameObject);
        }

        private void LateUpdate()
        {
            Destruct();
        }

        private void OnDestroy()
        {
            Instance = null;
            heap = null;
        }
    }
}
