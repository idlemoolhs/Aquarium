using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShapeType
{
    Leaf,
    Flower
}

[System.Serializable]
public class ShapeVisualMeshComponent
{
    public GameObject Mesh;
    public ShapeType Type = ShapeType.Leaf;

    public float MaxSize;
    public float MinSize;
    public float InterpolationValue;
}

[System.Serializable]
public class ShapeVisualSpriteComponent
{
    public Sprite Sprite;
    public ShapeType Type = ShapeType.Leaf;

    public float MaxSize;
    public float MinSize;
    public float InterpolationValue;
}
/// <summary>
/// 导入的外部模型（花、叶子等
/// </summary>
public class Shape
{
    [Header("Shape hierarchical components")]
    private BranchPart _parentBranchPart;

    [Header("Shape components")]
    public GameObject ShapeObj;
    public ShapeType Type = ShapeType.Leaf;
    private Vector3 _rotationAroundBranchAxis = Vector3.zero;
    private float _targetSize;
    private float _interpolationValue;

    private float _counter = 0f;
    private float _delayBeforeGrowth;
    private bool _isGrowing = false;

    public Shape(BranchPart parentBranchPart, ShapeType type, Vector3 rotation)
    {
        _parentBranchPart = parentBranchPart;
        _rotationAroundBranchAxis = rotation;
        _delayBeforeGrowth = Random.Range(1f, 10f);

        Type = type;
    }

    public void InitializeShape(bool is3D, float maxSize, float minSize, float interpolationValue, GameObject mesh = null, Sprite sprite = null)
    {
        ShapeObj = new GameObject("Shape");
        ShapeObj.transform.localScale = Vector3.zero;
        ShapeObj.transform.SetParent(_parentBranchPart.SplinePoint.transform);

        if (minSize > maxSize) Debug.LogError("Min size > Max size");

        _targetSize = Random.Range(minSize, maxSize);
        _interpolationValue = interpolationValue;

        if (is3D)
        {
            ShapeObj.AddComponent<MeshFilter>();
            ShapeObj.AddComponent<MeshRenderer>();
            ShapeObj.GetComponent<MeshFilter>().mesh = mesh.GetComponent<MeshFilter>().sharedMesh;
            ShapeObj.GetComponent<MeshRenderer>().material = mesh.GetComponent<MeshRenderer>().sharedMaterial;
            ShapeObj.GetComponent<MeshRenderer>().enabled = false;
            return;
        }

        ShapeObj.AddComponent<SpriteRenderer>();
        ShapeObj.GetComponent<SpriteRenderer>().sprite = sprite;
        ShapeObj.GetComponent<SpriteRenderer>().enabled = false;
        ShapeObj.GetComponent<SpriteRenderer>().sortingOrder = 1;
    }

    public void UpdateMesh()
    {
        _counter += Time.deltaTime;

        if (!_isGrowing && _counter > _delayBeforeGrowth) _isGrowing = true;
        if (!ShapeObj || !_isGrowing) return;

        ShapeObj.transform.localPosition = Vector3.zero;
        ShapeObj.transform.rotation = Quaternion.LookRotation(_rotationAroundBranchAxis);

        if (!ShapeObj.GetComponent<MeshRenderer>().enabled) ShapeObj.GetComponent<MeshRenderer>().enabled = true;

        if (ShapeObj.transform.localScale.x < _targetSize)
        {
            Vector3 currentScale = ShapeObj.transform.localScale;
            currentScale.Set(currentScale.x + _interpolationValue, currentScale.y + _interpolationValue, currentScale.z + _interpolationValue);
            ShapeObj.transform.localScale = currentScale;
        }
    }

    public void UpdateSprite()
    {
        _counter += Time.deltaTime;

        if (!_isGrowing && _counter > _delayBeforeGrowth) _isGrowing = true;
        if (!ShapeObj || !_isGrowing) return;

        ShapeObj.transform.localPosition = Vector3.zero;
        ShapeObj.transform.rotation = Quaternion.LookRotation(_rotationAroundBranchAxis);

        //For Billboard
        ShapeObj.transform.LookAt(Camera.main.transform.position, _rotationAroundBranchAxis);

        if (!ShapeObj.GetComponent<SpriteRenderer>().enabled) ShapeObj.GetComponent<SpriteRenderer>().enabled = true;

        if (ShapeObj.transform.localScale.x < _targetSize)
        {
            Vector3 currentScale = ShapeObj.transform.localScale;
            currentScale.Set(currentScale.x + _interpolationValue, currentScale.y + _interpolationValue, currentScale.z + _interpolationValue);
            ShapeObj.transform.localScale = currentScale;
        }
    }
}
