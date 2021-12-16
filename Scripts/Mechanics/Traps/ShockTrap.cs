using AberrationGames.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Mechanics.Traps
{
    [EditorTools.AberrationDescription("Shock trap that stuns a player - Single use per round", "Duncan Sykes", "18/08/2021")]
    [EditorTools.AberrationDeclare(EditorTools.DeclarationTypes.Debug)]
    public class ShockTrap : MechanicBase, Interfaces.IMechanic, Interfaces.ITrigger
    {
        [EditorTools.AberrationToolBar("Trap Modifers")]
        public bool debug;
        public bool triggered;
        public int shockTime; 

        [EditorTools.AberrationEndToolBar]

        [EditorTools.AberrationToolBar("Lightning")]
        [EditorTools.AberrationRequired] public Material lightningMaterial;
        [EditorTools.AberrationRequired] public Transform lightningEmissionPoint;
        [EditorTools.AberrationRequired] public int arcAmount;
        public int minArcs;
        public int maxArcs;
        public float minArcHeight;
        public float maxArcHeight;
        public Transform testTarget;


        [EditorTools.AberrationEndToolBar]
        private LineRenderer[] _lineRenderer;
        private GameObject[] _LightningObjects;
        private bool _shocked = false;

        public void OnAwake(MechanicLoader a_loader) 
        {

        }

        public void OnFixedUpdate(MechanicLoader a_loader) 
        {
            
        }

        public void ResetState()
        {
            triggered = false;
            _shocked = false;

            foreach(var a in _LightningObjects)
            {
                if (a != null)
                    a.SetActive(true);
            }
        }

        public void OnStart(MechanicLoader a_loader) 
        {
            _LightningObjects = new GameObject[arcAmount];
            _lineRenderer = new LineRenderer[arcAmount];
            for(int i = 0; i < arcAmount-1; i++)
            {
                GameObject LightningObject = new GameObject("LightningObject:" + i.ToString());
                LightningObject.transform.parent = this.gameObject.transform;
                //LightningObject.SetActive(false);
                
                var Lr = LightningObject.AddComponent<LineRenderer>();
                Lr.startWidth = 0.05f;
                Lr.endWidth = 0.05f;
                Lr.material = lightningMaterial;
                _lineRenderer[i] = Lr;
                _LightningObjects[i] = LightningObject;
            }
            Debug.Log(_LightningObjects.Length);
            if (minArcs <=0) minArcs = 1;
            if (maxArcs <=0) maxArcs = 1;

            // Jacob hot fix
            if (Events.Round.Instance != null)
                Events.Round.Instance.postEndRound.AddListener(ResetState);
        }

        public void OnUpdate(MechanicLoader a_loader) 
        {
            
        }

        public void TriggerEnter(MechanicLoader a_loader, Collider a_collider)
        {
            if (triggered) return;

            // Moved to trigger stay
            //if (a_collider.TryGetComponent(out PlayerBase ply)
            //{

            //    ply.FreezePlayer(shockTime);
            //}
        }

        public void TriggerExit(MechanicLoader a_loader, Collider a_collider)
        {
            if (!triggered) return;

            foreach(var a in _LightningObjects) 
            {
                if (a != null)
                    a.SetActive(false);
            }
        }

        public void TriggerStay(MechanicLoader a_loader, Collider a_collider) 
        {
            // Changed to represent if the game is running or not to trigger the trap.
            if (triggered ||
                !debug && !Events.Round.IsIngame()) return;

            Debug.Log(_shocked + " is shocked");

            // Also please find the shock target, and switch from TryGetComponent to PlayerBase target;
            if (!_shocked && a_collider.TryGetComponent(out PlayerBase shockPly))
            {
                _shocked = true;
                triggered = true;

                // shockTarget = shockPly;
                shockPly.FreezePlayer(shockTime);
            }

            // if (shockTarget != null) 
            if (a_collider.TryGetComponent(out PlayerBase ply))
            {
                foreach(var lines in _lineRenderer)
                {
                    if (lines == null)
                        continue;

                    int arcs = Random.Range(minArcs,maxArcs);
                    Vector3[] arcPositions = new Vector3[arcs + 2];
                    lines.positionCount = arcPositions.Length;
                    arcPositions[0] = lightningEmissionPoint.position;
                    arcPositions[arcPositions.Length - 1] = ply.transform.position;

                    Vector3 positionDifference = ply.transform.position - lightningEmissionPoint.position;


                    Vector3 directionToArc = positionDifference.normalized;

                    float length = positionDifference.magnitude;
                    positionDifference.y = 0;

                    float averageDistance = length / arcs;
                    float randomDistanceToAdd = averageDistance * 0.3f;

                    for(int i=1; i< arcPositions.Length - 1; i++)
                    {
                        if (i == arcPositions.Length - 2)
                        {
                            arcPositions[i] = lightningEmissionPoint.position + 
                                              directionToArc * (averageDistance * i) + 
                                              (Random.Range(-randomDistanceToAdd, 0) *
                                              directionToArc);
                        }
                        else
                        {
                            arcPositions[i] = lightningEmissionPoint.position + 
                                              directionToArc * (averageDistance * i) + 
                                              (Random.Range(-randomDistanceToAdd, randomDistanceToAdd) *
                                              directionToArc);
                        }
                        arcPositions[i].y = Random.Range(minArcHeight,maxArcHeight); 
                    }
                    lines.SetPositions(arcPositions);
                }
                
            }
        }

        public void OnTickUpdate(MechanicLoader a_loader, float a_tickDelta)
        {

        }
    }
}
