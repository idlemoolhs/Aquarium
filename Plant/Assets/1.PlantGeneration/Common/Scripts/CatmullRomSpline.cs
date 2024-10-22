using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Spline线条类，为植物生长提供方向和弯曲
/// </summary>
public class CatmullRomSpline
{
    //Main components
    public int Resolution;
    private CatmullRomPoint[] _splinePoints;
    private Vector3[] _controlPoints;

    public CatmullRomSpline(Transform[] controlPoints, int resolution)
    {
        if(controlPoints == null || controlPoints.Length <= 2 || resolution < 2)
        {
            throw new ArgumentException("Not enough control points or too small resolution ");
        }

        Resolution = resolution;
  
        _controlPoints = new Vector3[controlPoints.Length];
        for(int i = 0; i < controlPoints.Length; i++)
        {
            _controlPoints[i] = controlPoints[i].position;
        }

        _splinePoints = new CatmullRomPoint[Resolution * (_controlPoints.Length - 1)];

        CreateAllSplinePoints();
    }

    private void CreateAllSplinePoints()
    {
        Vector3 p0, p1;
        Vector3 m0, m1;

        for(int counterPoint = 0; counterPoint < _controlPoints.Length - 1; counterPoint++)
        {
            p0 = _controlPoints[counterPoint];
            p1 = _controlPoints[counterPoint + 1];

            if(counterPoint == 0)
            {
                m0 = (p1 - p0 + ((-_controlPoints[counterPoint + 2]) + p1)) / 2;
            }
            else
            {
                m0 = (p1 - _controlPoints[counterPoint - 1]) / 2;
            }

            
            if (counterPoint == _controlPoints.Length - 2)
            {
                m1 = (p0 - _controlPoints[counterPoint - 1] + ((-_controlPoints[counterPoint - 1]) + (_controlPoints[counterPoint - 2]))) / 2;
            }
            else
            {
                m1 = (_controlPoints[counterPoint + 2] - p0) / 2;
            }


            float pointStep = 1.0f / Resolution;
            if (counterPoint == _controlPoints.Length - 2)
            {
                pointStep = 1.0f / (Resolution - 1);
            }

            for(int tesselatedPoint = 0; tesselatedPoint < Resolution; tesselatedPoint++)
            {
                float t = tesselatedPoint * pointStep;
                CatmullRomPoint point = Evaluate(p0, p1, m0, m1, t);
                _splinePoints[counterPoint * Resolution + tesselatedPoint] = point;
            }
        }
    }

    public void DrawSpline()
    {
        if (CheckPoints())
        {
            for (int i = 0; i < _splinePoints.Length; i++)
            {
                if (i < _splinePoints.Length - 1 && _splinePoints[i] != null && _splinePoints[i + 1] != null)
                {
                    Debug.DrawLine(_splinePoints[i].Position, _splinePoints[i + 1].Position, Color.white);
                }
            }
        }
    }

    private bool CheckPoints()
    {
        if (_splinePoints == null)
        {
            throw new NullReferenceException("Spline not initialized");
        }

        return _splinePoints != null;
    }

    public CatmullRomPoint[] GetPoints()
    {
        if (!CheckPoints()) throw new ArgumentException("Spline not initialized");

        return _splinePoints;
    }

    public void Update(Transform[] controlPoints)
    {
        if(controlPoints.Length <= 0 || controlPoints == null)
        {
            throw new ArgumentException("Invalid control points");
        }

        _controlPoints = new Vector3[controlPoints.Length];
        for(int i = 0; i < controlPoints.Length; i++)
        {
            _controlPoints[i] = controlPoints[i].position;
        }

        CreateAllSplinePoints();
    }

    public void Update(int resolution)
    {
        if (resolution < 2)
        {
            throw new ArgumentException("Invalid resolution");
        }

        Resolution = resolution;

        CreateAllSplinePoints();
    }

    public static Vector3 CalculatePosition(Vector3 start, Vector3 end, Vector3 tanStart, Vector3 tanEnd, float t)
    {
        Vector3 position = (2.0f * t * t * t - 3.0f * t *t +1.0f) * start + (t * t * t -2.0f * t * t + t) * tanStart
            + (-2.0f * t * t * t + 3.0f * t * t) * end + (t * t * t - t * t) * tanEnd;
        return position;
    }

    public static Vector3 CalculateTangent(Vector3 start, Vector3 end, Vector3 tanStart, Vector3 tanEnd, float t)
    {
        //Tangent is the derived polynom
        Vector3 tangent = (6.0f * t * t - 6.0f * t) * start + (3.0f * t * t - 4.0f * t + 1.0f) * tanStart
            + (-6.0f * t * t + 6.0f * t) * end + (3.0f * t * t - 2.0f * t) * tanEnd;
        return tangent;
    }

    public static CatmullRomPoint Evaluate(Vector3 start, Vector3 end, Vector3 tanStart, Vector3 tanEnd, float t)
    {
        Vector3 position = CalculatePosition(start, end, tanStart, tanEnd, t);
        Vector3 tangent = CalculateTangent(start, end, tanStart, tanEnd, t);
        return new CatmullRomPoint(position, tangent);
    }
}

public class CatmullRomPoint
{
    //Main attributes
    public Vector3 Position;
    public Vector3 Tangent;

    public CatmullRomPoint(Vector3 position, Vector3 tangent)
    {
        Position = position;
        Tangent = tangent;
    }
}
