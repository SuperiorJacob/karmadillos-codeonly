using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.Utils
{
    public static class ExtShapes 
    {

        public static bool HasComponent<T>(this GameObject obj) where T:Component
        {
            return obj.GetComponent<T>() != null;
        }
        public static void DrawCircle(this GameObject a_container, float a_radius, float a_lineWidth, int a_segments=360, Material a_material=null)
        {
            var line = new LineRenderer();
            if (!a_container.HasComponent<LineRenderer>())
            {
                line = a_container.AddComponent<LineRenderer>();
            }
            else
            {
                line = a_container.GetComponent<LineRenderer>();
            }

            if (a_segments <= 2) a_segments = 3; // prevents weirdness and devide by 0
            line.useWorldSpace = false;
            line.material = a_material;
            line.startWidth = a_lineWidth;
            line.endWidth = a_lineWidth;
            line.positionCount = a_segments + 1;

            var pointCount = a_segments + 1; // adds an extra point to make the start & end point the same to close circle
            var points = new Vector3[pointCount];
            int y_a = 0;

            for (int i = 0; i < pointCount; i++)
            {
                
                var rad = Mathf.Deg2Rad * (i * 360f / a_segments);
                points[i] = new Vector3(Mathf.Sin(rad) * a_radius, 2, Mathf.Cos(rad) * a_radius);

            }

            line.SetPositions(points);

        }



    }
}
