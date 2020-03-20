using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour {

    private float ttl = 8f;
    public GameObject explosion;
    
    void Start() {
    }

    void Update() {
        if (ttl > 0) {
            ttl -= Time.deltaTime;
        } else if (transform.position.y < -100) {
            Destroy(gameObject);
        } else {
            GameObject[] terrBlocks = GameObject.FindGameObjectsWithTag("Terrain");
            foreach (GameObject t in terrBlocks) {
                float dist = (t.transform.position - transform.position).sqrMagnitude;
                if (dist < 10f) {
                    Destroy(t);
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
