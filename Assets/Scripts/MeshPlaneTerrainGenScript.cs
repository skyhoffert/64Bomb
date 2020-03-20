using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshPlaneTerrainGenScript : MonoBehaviour {

    void Start() {
        GameObject plane = new GameObject("Plane");

        MeshRenderer mr = plane.AddComponent<MeshRenderer>();
        mr.material = Resources.Load<Material>("Green");

        MeshFilter mf = plane.AddComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.Clear(false);
        mf.mesh = mesh;
        mesh.name = "Plane_mesh";

        Vector3[] vertices = new Vector3[4];
        vertices[0] = transform.position + new Vector3(-3f,0.1f,0f);
        vertices[1] = transform.position + new Vector3(3f,0f,3f);
        vertices[2] = transform.position + new Vector3(3f,-0.1f,-3f);
        vertices[3] = transform.position + new Vector3(0f,-3f,0f);
        mesh.vertices = vertices;

        List<int> triangles = new List<int>();
        triangles.Add(0);
        triangles.Add(1);
        triangles.Add(2);

        triangles.Add(1);
        triangles.Add(0);
        triangles.Add(3);
        triangles.Add(2);
        triangles.Add(1);
        triangles.Add(3);
        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(3);
        mesh.triangles = triangles.ToArray();

        List<Vector2> uv = new List<Vector2>();
        uv.Add(new Vector2(0f,1f));
        uv.Add(new Vector2(1f,1f));
        uv.Add(new Vector2(1f,0f));
        uv.Add(new Vector2(0f,0f));
        mesh.uv = uv.ToArray();

        mesh.RecalculateNormals();
        mesh.Optimize();

        MeshCollider mc = plane.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        /*
        GameObject res = Instantiate(plane);
        res.transform.SetParent(this.transform);
        if (!res) {
            Debug.Log("failed");
        }
        */
    }

    void Update() {
        
    }
}
