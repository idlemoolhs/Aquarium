using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 负责绘制工作
/// </summary>
public class VisualGenerator : MonoBehaviour
{
    [Header("Global generation parameters")]
    [SerializeField] private bool _is3D = true;
    [SerializeField] private float _growthDurationStem = 10f;
    private float _2pi = Mathf.PI * 2f;

    [Header("Spline branches")]
    [SerializeField] private List<SplineBranch> _allSplineBranches = new List<SplineBranch>();
    [SerializeField] private AnimationCurve _branchRadiusSize;
    private float _branchRadiusCoeff = 0.9f;

    [Header("Shapes")]
    [SerializeField] private ShapeVisualMeshComponent[] _meshVisualShapes;
    [SerializeField] private ShapeVisualSpriteComponent[] _spriteVisualShapes;

    [Header("Growth process")]
    private bool _isGenerating = false;
    private bool _allBranchesAreSenescent = false;
    private List<float> _timeLastStepBranches = new List<float>();
    private List<float> _timeSteps = new List<float>();

    [Header("Materials")]
    [SerializeField] private Material _materialBranchMesh;
    [SerializeField] private Material _materialBranchLineRenderer;

    void Start()
    {
        if (_is3D)
        {
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();
        }
        else
        {
            gameObject.AddComponent<LineRenderer>();
        }

        _timeLastStepBranches.Add(0f);
    }

    void Update()
    {
        if (_isGenerating)
        {
            _allBranchesAreSenescent = true;

            for (int i = 0; i < _allSplineBranches.Count; i++)
            {
                _allSplineBranches[i].Update();

                if (Math.Floor(_timeLastStepBranches[i] / _timeSteps[i]) >= 1f)
                {
                    _timeLastStepBranches[i] = _timeSteps[i];
                    _allSplineBranches[i].IsGenerated = true;
                    continue;
                }
                else
                {
                    _allBranchesAreSenescent = false;
                }

                if (i > 0 && ((float)_allSplineBranches[i].IndexParentDivergence / _allSplineBranches[i].ParentSplineBranch.BranchesPart.Count) >= _allSplineBranches[i].ParentSplineBranch.ProgressionGrowth) continue;

                if (!_allSplineBranches[i].HasBeganGeneration && _timeLastStepBranches[i] > _allSplineBranches[i].DelayBeforeGrowth)
                {
                    _allSplineBranches[i].HasBeganGeneration = true;
                    _timeLastStepBranches[i] = 0f;
                    continue;
                }

                _timeLastStepBranches[i] += Time.deltaTime;

                if (!_allSplineBranches[i].HasBeganGeneration) continue;

                _allSplineBranches[i].ProgressionGrowth = _timeLastStepBranches[i] / _timeSteps[i];
                _allSplineBranches[i].AnimateGrowth(_timeLastStepBranches[i], _timeSteps[i]);
            }

            if (_allBranchesAreSenescent && !_allSplineBranches[0].IsSenescent)
            {
                for (int i = 0; i < _allSplineBranches.Count; i++)
                {
                    _allSplineBranches[i].IsSenescent = true;
                }
            }

            if (_is3D)
            {
                GeneratePlantMesh();
            }
            else
            {
                GeneratePlant2D();
            }
        }
    }

    public void InitializeVisualGeneration()
    {
        _isGenerating = true;
    }

    void GeneratePlant2D()
    {
        for (int k = 0; k < _allSplineBranches.Count; k++)
        {
            SplineBranch splineBranch = _allSplineBranches[k];
            float coeffCurrentGrowth = splineBranch.HasBeganGeneration ? _timeLastStepBranches[k] / _timeSteps[k] : 0f;

            if (coeffCurrentGrowth > 1f) coeffCurrentGrowth = 1f;
            if (splineBranch.Spline == null || splineBranch.BranchesPart.Count <= 3 || splineBranch.Spline.GetPoints().Length <= 1) continue;

            CatmullRomPoint[] splinePoints = splineBranch.Spline.GetPoints();
            int splinePointsLimit = Convert.ToInt32(Math.Floor(splinePoints.Length * coeffCurrentGrowth));

            if (splinePointsLimit <= 1) continue;

            LineRenderer lineRenderer = splineBranch.BranchesPart[0].ParentObj.GetComponent<LineRenderer>();
            lineRenderer.enabled = true;

            Vector3[] vectorPointList = new Vector3[splinePointsLimit];

            for (int i = 0; i < splinePointsLimit; i++)
            {
                vectorPointList[i] = splinePoints[i].Position;
            }

            float bottomRadius = _branchRadiusSize.Evaluate(0) * splineBranch.RootRadius * coeffCurrentGrowth;
            float topRadius = _branchRadiusSize.Evaluate(1) * splineBranch.RootRadius * coeffCurrentGrowth;
            lineRenderer.startWidth = bottomRadius;
            lineRenderer.endWidth = topRadius;

            for (int i = 0; i < splinePointsLimit; i++)
            {
                lineRenderer.positionCount = splinePointsLimit;
                lineRenderer.SetPositions(vectorPointList);
            }

            for (int j = 0; j < Convert.ToInt32(Math.Floor(_allSplineBranches[k].BranchesPart.Count * coeffCurrentGrowth)); j++)
            {
                foreach (Shape shape in _allSplineBranches[k].BranchesPart[j].Shapes)
                {
                    shape.UpdateSprite();
                }
            }
        }
    }

    void GeneratePlantMesh()
    {
        int nbSides = 10;
        int nbVerticesForAnExtremity = nbSides + 1;

        for (int k = 0; k < _allSplineBranches.Count; k++)
        {
            SplineBranch splineBranch = _allSplineBranches[k];
            float coeffCurrentGrowth = splineBranch.HasBeganGeneration ? _timeLastStepBranches[k] / _timeSteps[k] : 0f;

            if (coeffCurrentGrowth > 1f) coeffCurrentGrowth = 1f;
            if (splineBranch.Spline == null || splineBranch.BranchesPart.Count <= 3 || splineBranch.Spline.GetPoints().Length <= 1) continue;

            CatmullRomPoint[] splinePoints = splineBranch.Spline.GetPoints();

            int splinePointsLimit = Convert.ToInt32(Math.Floor(splinePoints.Length * coeffCurrentGrowth));

            if (splinePointsLimit <= 1) continue;

            GameObject parentObj = splineBranch.BranchesPart[0].ParentObj;
            float coeffGrowthParentRadius = splineBranch.ParentSplineBranch != null ? splineBranch.ParentSplineBranch.ProgressionGrowth : 1f;
            float coeffGrowthRadius = coeffCurrentGrowth * coeffGrowthParentRadius;

            int nbVerticesBottom = nbSides + 1;
            int nbVerticesTop = nbSides + 1;
            int nbVerticesSides = nbSides * 2 + 2;

            int nbTrianglesIndexesBottom = (nbVerticesBottom - 1) * 3; //sides * 3 = triangles
            int nbTrianglesIndexesTop = (nbVerticesTop - 1) * 3; //sides * 3 = triangles
            int nbTrianglesIndexesSides = (nbVerticesSides - 2) * 3 * 2 * 2; //two triangles for one face with 2 different set of points

            int nbBranchesParts = splinePointsLimit;
            int nbTotalVertices = nbVerticesSides * nbBranchesParts + nbVerticesBottom + nbVerticesTop;
            int nbTriangleIndex = nbTrianglesIndexesSides * nbBranchesParts + nbTrianglesIndexesBottom + nbTrianglesIndexesTop;

            Vector3[] vertices = new Vector3[nbTotalVertices];
            int[] triangleindexes = new int[nbTriangleIndex];

            int previousVerticeNumber = 0;

            for (int j = 0; j < splinePointsLimit; j++)
            {
                float bottomRadius = _branchRadiusSize.Evaluate((float)j / splinePointsLimit) * splineBranch.RootRadius * coeffGrowthRadius;
                float topRadius = j == splinePointsLimit - 1
                    ? _branchRadiusSize.Evaluate((float)(j) / splinePointsLimit) * splineBranch.RootRadius * coeffGrowthRadius
                    : _branchRadiusSize.Evaluate((float)(j + 1) / splinePointsLimit) * splineBranch.RootRadius * coeffGrowthRadius;
                float branchLength = 1f;

                if (j == splinePointsLimit - 1 && !_allBranchesAreSenescent)
                {
                    splineBranch.ProgressionBranchPartGrowth = _timeLastStepBranches[k] % splineBranch.GrowthBranchPartDuration;
                    float currentProgressionBranchPart = splineBranch.ProgressionBranchPartGrowth / splineBranch.GrowthBranchPartDuration;

                    branchLength *= currentProgressionBranchPart;
                }

                Vector3 initialBasePointPosition = splinePoints[j].Position;
                int initialVerticeNumber = j == 0 ? 0 : nbVerticesSides * j + nbVerticesBottom;
                int initialTriangleNumber = j == 0 ? 0 : nbTrianglesIndexesSides * j + nbTrianglesIndexesBottom;

                Vector3 direction = j == splinePoints.Length - 1 ? splinePoints[j].Position - splinePoints[j - 1].Position : splinePoints[j + 1].Position - splinePoints[j].Position;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);

                if (j == 0)
                {
                    //Bottom vertices construction
                    vertices[initialVerticeNumber] = initialBasePointPosition;
                    for (int i = initialVerticeNumber + 1; i <= initialVerticeNumber + nbSides; i++)
                    {
                        float rad = (float)i / nbSides * _2pi;
                        Vector3 vertex = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
                        vertex = rotation * vertex;
                        vertices[i] = initialBasePointPosition + vertex;
                    }
                }

                //Sides vertices construction
                int beginning = initialVerticeNumber;
                int ending = initialVerticeNumber + nbVerticesSides;

                if (j == 0)
                {
                    beginning = initialVerticeNumber + nbVerticesBottom;
                    ending = initialVerticeNumber + nbVerticesBottom + nbVerticesSides;
                }

                int v = 0;
                for (int i = beginning; i <= ending - 4; i += 2)
                {
                    float rad = (float)v / nbSides * _2pi;
                    Vector3 vertex1 = new Vector3(Mathf.Cos(rad) * topRadius, 0f, Mathf.Sin(rad) * topRadius);
                    Vector3 vertex2 = new Vector3(Mathf.Cos(rad) * bottomRadius, 0f, Mathf.Sin(rad) * bottomRadius);
                    vertex1 = rotation * vertex1;
                    vertex2 = rotation * vertex2;
                    vertices[i] = initialBasePointPosition + direction * branchLength + vertex1;
                    vertices[i + 1] = initialBasePointPosition + vertex2;
                    v++;
                }
                vertices[ending - 2] = vertices[beginning];
                vertices[ending - 1] = vertices[beginning + 1];

                if (j == splinePointsLimit - 1)
                {
                    //Top vertices construction
                    vertices[ending] = initialBasePointPosition + direction * branchLength;
                    for (int i = ending + 1; i <= ending + nbSides; i++)
                    {
                        float rad = (float)(i - nbSides - 1) / nbSides * _2pi;
                        Vector3 vertex = new Vector3(Mathf.Cos(rad) * topRadius, 0f, Mathf.Sin(rad) * topRadius);
                        vertex = rotation * vertex;
                        vertices[i] = initialBasePointPosition + direction * branchLength + vertex;
                    }
                }

                //Bottom triangles construction
                int tri = 0;
                int incrementTriangle = initialTriangleNumber;

                if (j == 0)
                {
                    while (tri < nbSides - 1) //We generate nbSides -1 because of the last triangle with specific indexes
                    {
                        triangleindexes[incrementTriangle] = initialVerticeNumber + 0;
                        triangleindexes[incrementTriangle + 1] = initialVerticeNumber + tri + 1;
                        triangleindexes[incrementTriangle + 2] = initialVerticeNumber + tri + 2;
                        tri++;
                        incrementTriangle += 3;
                    }

                    //Last triangle linking our center point with the last generated vertices
                    triangleindexes[incrementTriangle] = initialVerticeNumber;
                    triangleindexes[incrementTriangle + 1] = initialVerticeNumber + tri + 1;
                    triangleindexes[incrementTriangle + 2] = initialVerticeNumber + 1;
                    tri++;
                    tri++;
                    incrementTriangle += 3;
                }

                if (j == 0)
                {
                    while (tri < nbSides + nbSides * 2)
                    {
                        triangleindexes[incrementTriangle] = initialVerticeNumber + tri + 2;
                        triangleindexes[incrementTriangle + 1] = initialVerticeNumber + tri + 1;
                        triangleindexes[incrementTriangle + 2] = initialVerticeNumber + tri;
                        tri++;
                        incrementTriangle += 3;

                        triangleindexes[incrementTriangle] = initialVerticeNumber + tri + 1;
                        triangleindexes[incrementTriangle + 1] = initialVerticeNumber + tri + 2;
                        triangleindexes[incrementTriangle + 2] = initialVerticeNumber + tri;
                        tri++;
                        incrementTriangle += 3;
                    }
                }
                else
                {
                    while (tri < nbSides * 2)
                    {
                        triangleindexes[incrementTriangle] = initialVerticeNumber + tri + 2;
                        triangleindexes[incrementTriangle + 1] = (previousVerticeNumber - 1) + tri + 1;
                        triangleindexes[incrementTriangle + 2] = initialVerticeNumber + tri;
                        tri++;
                        incrementTriangle += 3;

                        triangleindexes[incrementTriangle] = initialVerticeNumber + tri + 1;
                        triangleindexes[incrementTriangle + 1] = (previousVerticeNumber - 1) + tri + 2;
                        triangleindexes[incrementTriangle + 2] = (previousVerticeNumber - 1) + tri;
                        tri++;
                        incrementTriangle += 3;
                    }
                }

                previousVerticeNumber = j == 0 ? beginning : initialVerticeNumber;

                if (j == splinePointsLimit - 1)
                {
                    //Top triangles construction
                    while (tri < nbSides * 2 + nbSides + 1)
                    {
                        triangleindexes[incrementTriangle] = initialVerticeNumber + tri + 2;
                        triangleindexes[incrementTriangle + 1] = initialVerticeNumber + tri + 1;
                        triangleindexes[incrementTriangle + 2] = initialVerticeNumber + nbVerticesSides;
                        tri++;
                        incrementTriangle += 3;
                    }

                    //Last triangle linking our center point with the last generated vertices of top extremity
                    triangleindexes[incrementTriangle] = initialVerticeNumber + nbVerticesSides + 1;
                    triangleindexes[incrementTriangle + 1] = initialVerticeNumber + tri + 1;
                    triangleindexes[incrementTriangle + 2] = initialVerticeNumber + nbVerticesSides;
                    tri++;
                    tri++;
                    incrementTriangle += 3;
                }
            }

            //Assign new constructed mesh to our GameObject
            parentObj.GetComponent<MeshFilter>().mesh.vertices = vertices;
            parentObj.GetComponent<MeshFilter>().mesh.triangles = triangleindexes;
            parentObj.GetComponent<MeshFilter>().mesh.RecalculateNormals();
            parentObj.GetComponent<MeshRenderer>().material = _materialBranchMesh;

            //Basic calculation of uvs
            Vector2[] uvs = new Vector2[vertices.Length];

            for (int i = 0; i < uvs.Length; i++)
            {
                uvs[i] = new Vector2(vertices[i].x, vertices[i].y);
            }

            parentObj.GetComponent<MeshFilter>().mesh.uv = uvs;

            for (int j = 0; j < Convert.ToInt32(Math.Floor(_allSplineBranches[k].BranchesPart.Count * coeffCurrentGrowth)); j++)
            {
                foreach (Shape shape in _allSplineBranches[k].BranchesPart[j].Shapes)
                {
                    shape.UpdateMesh();
                }
            }
        }
    }
    /// <summary>
    /// 格式化为Spline分支
    /// </summary>
    /// <param name="_branchesParts"></param>
    public void FormatIntoSplineBranches(List<BranchPart> _branchesParts)
    {
        _allSplineBranches = FormatBranchesPartFrom(_branchesParts[0]);

        for (int i = 0; i < _allSplineBranches.Count; i++)
        {
            SplineBranch branch = _allSplineBranches[i];
            GameObject branchObj = branch.BranchesPart[0].ParentObj;

            if (_is3D)
            {
                branchObj.AddComponent<MeshFilter>();
                branchObj.AddComponent<MeshRenderer>();
            }
            else
            {
                branchObj.AddComponent<LineRenderer>();
                LineRenderer lineRenderer = branchObj.GetComponent<LineRenderer>();

                lineRenderer.textureMode = LineTextureMode.Tile;
                lineRenderer.material = _materialBranchLineRenderer;
                lineRenderer.enabled = false;
            }

            for (int j = 0; j < _allSplineBranches[i].BranchesPart.Count; j++)
            {
                foreach (Shape shape in _allSplineBranches[i].BranchesPart[j].Shapes)
                {
                    switch (shape.Type) //Hard coded => To define inside _meshVisualShapes & _spriteVisualShapes variables
                    {
                        case ShapeType.Flower:
                            shape.InitializeShape(_is3D,
                                _is3D ? _meshVisualShapes[0].MaxSize : _spriteVisualShapes[0].MaxSize,
                                _is3D ? _meshVisualShapes[0].MinSize : _spriteVisualShapes[0].MinSize,
                                _is3D ? _meshVisualShapes[0].InterpolationValue : _spriteVisualShapes[0].InterpolationValue,
                                _meshVisualShapes[0].Mesh,
                                _spriteVisualShapes[0].Sprite
                                );
                            break;

                        case ShapeType.Leaf:
                            shape.InitializeShape(_is3D,
                                _is3D ? _meshVisualShapes[1].MaxSize : _spriteVisualShapes[1].MaxSize,
                                _is3D ? _meshVisualShapes[1].MinSize : _spriteVisualShapes[1].MinSize,
                                _is3D ? _meshVisualShapes[1].InterpolationValue : _spriteVisualShapes[1].InterpolationValue,
                                _meshVisualShapes[1].Mesh,
                                _spriteVisualShapes[1].Sprite
                                );
                            break;
                    }
                }
            }

            if (_timeLastStepBranches.Count <= i) _timeLastStepBranches.Add(0f);
            if (_timeSteps.Count <= i) _timeSteps.Add(0f);

            float growthDuration = i == 0 ? _growthDurationStem : UnityEngine.Random.Range(_growthDurationStem / 2, _growthDurationStem);
            float delayBeforeGrowth = i == 0 ? 0f : UnityEngine.Random.Range(0f, 2f);

            branch.DelayBeforeGrowth = delayBeforeGrowth;
            _timeSteps[i] = growthDuration;
            branch.GrowthBranchPartDuration = growthDuration / ((branch.BranchesPart.Count - 1) * branch.Resolution);

            if (branch.ParentSplineBranch == null)
            {
                branch.SetRootRadius(1f);
                continue;
            }

            float coeffFromParent = (1 - ((float)branch.IndexParentDivergence / branch.ParentSplineBranch.BranchesPart.Count));
            branch.SetRootRadius(coeffFromParent * branch.ParentSplineBranch.RootRadius * _branchRadiusCoeff);
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="branchPart"></param>
    /// <param name="splineBranches"></param>
    /// <param name="parentSplineBranch"></param>
    /// <param name="indexList"></param>
    /// <param name="oldIndex"></param>
    /// <returns></returns>
    private List<SplineBranch> FormatBranchesPartFrom(BranchPart branchPart, List<SplineBranch> splineBranches = null, SplineBranch parentSplineBranch = null, int indexList = -1, int oldIndex = -1)
    {
        if (splineBranches == null)
        {
            splineBranches = new List<SplineBranch>();
        }

        if (indexList == -1)
        {
            splineBranches.Add(new SplineBranch(splineBranches.Count));
            indexList = splineBranches.Count - 1;

            if (parentSplineBranch != null)
            {
                splineBranches[indexList].ParentSplineBranch = parentSplineBranch;
                splineBranches[indexList].IndexParentDivergence = oldIndex;
            }
        }

        splineBranches[indexList].AddBranchPart(branchPart);

        for (int i = branchPart.ChildrenBranches.Count - 1; i >= 0; i--)
        {//Normally for space colonization, first branch in order (index = 0) is the direct child but not for LSystem
            if (branchPart.ParentObj != branchPart.ChildrenBranches[i].ParentObj)
            {
                FormatBranchesPartFrom(branchPart.ChildrenBranches[i], splineBranches, splineBranches[indexList], -1, splineBranches[indexList].BranchesPart.Count);
            }
            else
            {
                FormatBranchesPartFrom(branchPart.ChildrenBranches[i], splineBranches, null, indexList);
            }
        }

        return splineBranches;
    }
}