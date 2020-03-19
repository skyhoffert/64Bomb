using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour {

    private float ttl = 6f;
    private Transform myTrans;
    public GameObject explosion;
    
    void Start() {
        myTrans = transform.GetChild(0);
    }

    void Update() {
        if (ttl > 0) {
            ttl -= Time.deltaTime;
        } else if (myTrans.position.y < -100) {
            Destroy(gameObject);
        } else {
            GameObject[] terrBlocks = GameObject.FindGameObjectsWithTag("Terrain");
            foreach (GameObject t in terrBlocks) {
                float dist = (t.transform.position - myTrans.position).sqrMagnitude;
                if (dist < 10f) {
                    Destroy(t);
                }
            }
            GameObject expl = Instantiate(explosion);
            expl.transform.position = myTrans.position;
            Destroy(gameObject);
        }
    }
}
