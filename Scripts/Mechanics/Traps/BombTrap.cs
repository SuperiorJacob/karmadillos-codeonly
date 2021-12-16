using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("An explosive bomb that will move players & destroy traps.", "Jacob Cooper", "14/11/2021")]
    public class BombTrap : MechanicBase, Interfaces.IMechanic
    {
        [SerializeField] private bool _debug;
        [SerializeField, Range(0f, 100f)] private float _distance;
        [SerializeField, Range(0f, 1000f)] private float _explosiveForce;
        [SerializeField] private float _explosionTime;

        [SerializeField] private GameObject _explosionObject;
        [SerializeField] private GameObject _mesh;

        [SerializeField] private ParticleSystem[] _particles;
        [SerializeField] private MeshRenderer[] _meshes;

        private float _explosionTimer;
        private bool _exploded = false;
        private bool _fizzle = false;
        private float _alphaLerp = 1f;

        public void OnAwake(MechanicLoader a_loader)
        {
        }

        public void OnFixedUpdate(MechanicLoader a_loader)
        {
        }

        public void OnStart(MechanicLoader a_loader)
        {
        }

        public void DestroyTraps()
        {
            List<Events.TrapSticker> traps = new List<Events.TrapSticker>();

            if (Events.GameUILoad.Instance != null)
            {
                foreach (var trap in Events.GameUILoad.Instance.stickers)
                {
                    if (trap.mechanic == null || trap.mechanic == gameObject)
                        continue;

                    if (Vector3.Distance(trap.mechanic.transform.position, transform.position) < _distance)
                    {
                        DestructionHeap.PrepareForDestruction(trap.mechanic);

                        traps.Add(trap);
                    }
                }

                foreach (var trap in traps)
                    Events.GameUILoad.Instance.stickers.Remove(trap);
            }

            traps.Clear();
        }

        public void Explode()
        {
            foreach (var player in Players.GetActivePlayers())
            {
                GameObject ply = player.player;

                if (Vector3.Distance(ply.transform.position, transform.position) < _distance)
                    if (player.player.TryGetComponent(out PlayerBase pb))
                    {
                        Vector3 vec = (ply.transform.position - transform.position);
                        vec.y = 0;

                        float mag = (_distance - vec.magnitude);

                        pb.GetRigidBody().AddForce(vec.normalized * _explosiveForce * mag, ForceMode.Impulse);
                    }
            }

            _mesh.SetActive(false);

            _explosionObject.SetActive(true);
            _explosionObject.transform.localScale = Vector3.zero;

            foreach (var particle in _particles)
            {
                particle.Play();
            }

            StartCoroutine(DeleteExplode());
        }

        public IEnumerator DeleteExplode()
        {
            yield return new WaitForSeconds(_explosionTime / 2);

            _fizzle = true;
            DestroyTraps();

            yield return new WaitForSeconds(_explosionTime / 2);

            DestructionHeap.PrepareForDestruction(gameObject);
        }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {
            if (!(Events.Round.IsIngame() || _debug))
                return;

            if (!_exploded)
            {
                _exploded = true;

                _explosionTimer = Time.realtimeSinceStartup + _explosionTime;
            }
            else if (_exploded && _explosionTimer < Time.realtimeSinceStartup)
            {
                _exploded = false;

                Explode();
            }
        }

        public void OnUpdate(MechanicLoader a_loader)
        {
            if (_exploded && !_fizzle)
                _explosionObject.transform.localScale = Vector3.Lerp(_explosionObject.transform.localScale, Vector3.one * (_distance * 2f), TickBase.deltaTime);
            else if (_fizzle)
            {
                _alphaLerp = Mathf.Lerp(_alphaLerp, 0, TickBase.deltaTime * 10f);

                foreach (var mesh in _meshes)
                {
                    mesh.material.SetFloat("_Alpha", _alphaLerp);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(transform.position, _distance);
        }
#endif
    }
}
