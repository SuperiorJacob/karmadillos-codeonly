// Note that this must be FollowAnimationCurve.cs

using UnityEngine;
using System.Collections;

public class EditLineRenderer : MonoBehaviour
{
    public AnimationCurve curveX;
    public LineRenderer Renderer;

    public void SetCurves(AnimationCurve xC)
    {
       
        Renderer.widthCurve = curveX; 
    }

   
}