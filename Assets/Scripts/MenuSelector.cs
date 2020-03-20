using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSelector : MonoBehaviour {
    public GameObject[] menuItems;

    private int idxSelected;
    private bool justMoved;
    private float deadZone;

    // Start is called before the first frame update
    void Start() {
        this.idxSelected = 0;
        this.deadZone = 0.1f;
        this.justMoved = false;
    }

    // Update is called once per frame
    void Update() {
        if (this.justMoved) {
            if (Mathf.Abs(Input.GetAxis("Horizontal")) < this.deadZone) {
                this.justMoved = false;
            }
        } else {
            if (Input.GetAxis("Horizontal") > this.deadZone) {
                this.justMoved = true;
                this.idxSelected = (this.idxSelected + 1) % this.menuItems.Length;
            } else if (Input.GetAxis("Horizontal") < -this.deadZone) {
                this.justMoved = true;
                this.idxSelected--;
                if (this.idxSelected < 0) {
                    this.idxSelected = this.menuItems.Length - 1;
                }
            }
        }

        if (this.menuItems[this.idxSelected]) {
            this.menuItems[this.idxSelected].transform.Rotate(0f,100f*Time.deltaTime,0f, Space.Self);
        }

        if (Input.GetButtonDown("Submit")) {
            if (idxSelected == 0) {
                idxSelected = -1;
                SceneManager.LoadScene("GameTest");
            } else if (idxSelected == 1) {
            } else if (idxSelected == 2) {
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
        }
    }
}
