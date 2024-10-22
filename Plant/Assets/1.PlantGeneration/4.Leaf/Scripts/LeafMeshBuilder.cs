using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeafMeshBuilder : MonoBehaviour
{
    public void Build(List<Vector2> points)
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();

        //Left side
        for (int i = 0; i < points.Count; i++)
        {
            vertices.Add(new Vector3(points[i].x, points[i].y, 0));
        }

        int countLeftSide = vertices.Count;

        //Mid side
        for (int i = 0; i < points.Count; i++)
        {
            vertices.Add(new Vector3(0, points[i].y, 0));
        }

        int countMidSide = vertices.Count;

        //Right side
        for (int i = 0; i < points.Count; i++)
        {
            vertices.Add(new Vector3(-points[i].x, points[i].y, 0));
        }

        int countRightSide = vertices.Count;

        List<int> triangles = new List<int>();

        //Left sides construction triangles / faces
        int offset = 0;
        for (int i = 0; i < points.Count - 1; i++)
        {
            int p1 = i + offset;
            int p2 = p1 + 1;
            int p3 = p1 + points.Count;
            int p4 = p2 + points.Count;
            triangles.Add(p1); triangles.Add(p2); triangles.Add(p3);
            triangles.Add(p2); triangles.Add(p4); triangles.Add(p3);
        }

        //Right side construction triangles / faces
        offset = countMidSide;
        for (int i = 0; i < points.Count - 1; i++)
        {
            int p1 = i + offset;
            int p2 = p1 + 1;
            int p3 = p1 - points.Count;
            int p4 = p2 - points.Count;
            triangles.Add(p1); triangles.Add(p2); triangles.Add(p3);
            triangles.Add(p2); triangles.Add(p4); triangles.Add(p3);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = mesh.vertices.Select(v => -Vector3.forward).ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        Mesh flippedMesh = new Mesh();
        flippedMesh.vertices = mesh.vertices;
        flippedMesh.triangles = mesh.triangles;
        InvertNormals(flippedMesh);

        CombineInstance[] combine = new CombineInstance[2];
        combine[0].mesh = mesh;
        combine[0].transform = transform.localToWorldMatrix;
        combine[1].mesh = flippedMesh;
        combine[1].transform = transform.localToWorldMatrix;

        GetComponent<MeshFilter>().mesh.CombineMeshes(combine, true, false);
        GetComponent<MeshFilter>().mesh.RecalculateNormals();
    }

    public static void InvertNormals(Mesh mesh)
    {
        Vector3[] normals = mesh.normals;

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -normals[i];
        }
        mesh.normals = normals;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);

            for (int j = 0; j < triangles.Length; j += 3)
            {
                int temp = triangles[j];
                triangles[j] = triangles[j + 1];
                triangles[j + 1] = temp;
            }

            mesh.SetTriangles(triangles, i);
        }
    }
}
