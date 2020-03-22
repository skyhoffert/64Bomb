using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshPlaneTerrainGenScript : MonoBehaviour {

    private float width = 100f;
    private float depth = 100f;
    private float triSize = 2f;

    private class Quad {
        public int sIdx; // Upper left corner index in vertices.
        public int v1, v2, v3, v4;
        public Quad(int si, int v1, int v2, int v3, int v4) {
            this.sIdx = si;
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
            this.v4 = v4;
        }
    }

    private bool[] destroyedIdxs; // TODO

    private Mesh mesh;
    private MeshCollider meshCollider;
    private Vector3[] vertices;
    private List<int> triangles;

    private int nTriWid;
    private int nTriDep;

    void Start() {
        GameObject plane = new GameObject("Plane");

        MeshRenderer mr = plane.AddComponent<MeshRenderer>();
        mr.material = Resources.Load<Material>("Grass");

        MeshFilter mf = plane.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.Clear(false);
        mf.mesh = mesh;
        mesh.name = "Plane_mesh";

        nTriWid = (int)(width / triSize);
        nTriDep = (int)(depth / triSize);
        int nVerts = nTriWid * nTriDep;
        vertices = new Vector3[nVerts];
        for (int i = 0; i < nVerts; i++) {
            int r = (int)(i / nTriWid);
            int c = i % nTriWid;
            vertices[i] = transform.position + new Vector3(-width/2 + r*triSize, 0, -depth/2 + c*triSize);
        }
        mesh.vertices = vertices;

        destroyedIdxs = new bool[nVerts];
        for (int i = 0; i < nVerts; i++) {
            destroyedIdxs[i] = false;
        }

        triangles = new List<int>();
        for (int i = 0; i < nVerts; i++) {
            int r = (int)(i / nTriWid);
            int c = i % nTriWid;

            if (c >= nTriWid-1){ continue; }
            if (r >= nTriDep-1){ break; }

            triangles.Add(i);
            triangles.Add(i+1);
            triangles.Add(i+nTriWid+1);

            triangles.Add(i+nTriWid);
            triangles.Add(i);
            triangles.Add(i+nTriWid+1);
        }
        mesh.triangles = triangles.ToArray();

        List<Vector2> uv = new List<Vector2>();
        // TODO: fix this
        for (int i = 0; i < nVerts/3+1; i++) {
            uv.Add(new Vector2(0f,0f));
            uv.Add(new Vector2(0f,1f));
            uv.Add(new Vector2(1f,1f));
        }
        if (uv.Count > nVerts) {
            uv.RemoveRange(nVerts,uv.Count-nVerts);
        }
        mesh.uv = uv.ToArray();

        mesh.RecalculateNormals();
        // NOTE: this line ruins the rest of the code
        //mesh.Optimize();

        meshCollider = plane.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }

    public void CheckExplosion(Vector3 pos) {
        bool vchange = false;
        bool trichange = false;
        for (int i = 0; i < vertices.Length; i++) {
            float dist = (vertices[i] - pos).sqrMagnitude;
            if (dist < 10f) {
                vertices[i] += new Vector3(0,-1,0);
                vchange = true;
                if (vertices[i].y < transform.position.y - 3) {
                    trichange = true;
                    destroyedIdxs[i] = true;
                    int idx = i*6 - ((int)(i/(nTriWid-1)))*6;
                    idx = Mathf.Clamp(idx,0,triangles.Count-6);
                    for (int j = 0; j < 6; j++) {
                        triangles[idx+j] = 0;
                    }
                }
            }
        }

        if (vchange) {
            mesh.vertices = vertices;
        }
        if (trichange) {
            mesh.triangles = triangles.ToArray();
        }
        if (vchange || trichange) {
            meshCollider.sharedMesh = mesh;
        }
    }

    void Update() {
        
    }
}
