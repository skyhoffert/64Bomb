using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainScript : MonoBehaviour {

    public int width;
    public int depth;
    public float blocksize;
    public GameObject terrainBlock;

    void Start() {
        float sPosX = this.transform.position.x - width/2*blocksize;
        float sPosD = this.transform.position.z - depth/2*blocksize;
        for (int w = 0; w < width; w++) {
            for (int d = 0; d < depth; d++) {
                GameObject go = Instantiate(terrainBlock);
                go.transform.SetParent(this.transform);
                go.transform.localScale = new Vector3(blocksize, 1, blocksize);
                go.transform.position = new Vector3(sPosX + w*blocksize, this.transform.position.y, sPosD + d*blocksize);
            }
        }
    }

    void Update() {
        
    }
}
