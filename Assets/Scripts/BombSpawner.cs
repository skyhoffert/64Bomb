using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombSpawner : MonoBehaviour {

    public GameObject bomb;
    public int width;
    public int depth;
    public float rate;
    public float incrate;

    void Start() {
    }

    void Update() {
        if (Time.timeScale <= 0f) {
            return;
        }
        
        if (Random.value < rate) {
            GameObject newb = Instantiate(bomb);
            float xpos = Random.value * width - width/2;
            float zpos = Random.value * depth - depth/2;
            newb.transform.position = new Vector3(xpos, transform.position.y, zpos);
        }

        rate += incrate * Time.deltaTime;
    }
}
