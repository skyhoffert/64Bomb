using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour {

    private float ttl;
    public GameObject explosion;
    
    void Start() {
        ttl = 5f + Random.value*3f;
        transform.eulerAngles = new Vector3(Random.value*360, Random.value*360, Random.value*360);
    }

    void Update() {
        if (ttl > 0) {
            ttl -= Time.deltaTime;
        } else if (transform.position.y < -100) {
            Destroy(gameObject);
        } else {
            GameObject[] terrBlocks = GameObject.FindGameObjectsWithTag("Terrain");
            foreach (GameObject t in terrBlocks) {
                t.GetComponent<MeshPlaneTerrainGenScript>().CheckExplosion(this.transform.position);
            }

            GameObject[] bombs = GameObject.FindGameObjectsWithTag("Bomb");
            foreach (GameObject b in bombs) {
                if (!b || b == this.gameObject) { continue; }
                Vector3 dv = b.transform.position - transform.position;
                float dist = dv.sqrMagnitude;
                if (dist <= 0) { continue; }
                if (dist < 40f) {
                    Vector3 fv = dv * 1000f / dist;
                    b.GetComponent<Rigidbody>().AddForce(fv, ForceMode.Impulse);
                }
            }

            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject p in players) {
                Vector3 dv = p.transform.position - transform.position;
                float dist = dv.sqrMagnitude;
                if (dist < 40f) {
                    Vector3 fv = dv * 1000f / dist;
                    p.GetComponent<Rigidbody>().AddForce(fv, ForceMode.Impulse);
                }
            }

            GameObject expl = Instantiate(explosion);
            expl.transform.position = transform.position;
            Destroy(gameObject);
        }
    }
}
