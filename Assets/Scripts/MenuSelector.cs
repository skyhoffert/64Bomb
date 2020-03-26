using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSelector : MonoBehaviour {

    public GameObject[] menuItems;
    public Camera cam;

    private int idxSelected;
    private bool justMoved;
    private float deadZone;

    // Start is called before the first frame update
    void Start() {
        this.idxSelected = 0;
        this.deadZone = 0.1f;
        this.justMoved = false;

        // Update user prefs every session.
        PlayerPrefs.SetString("ServerIP", "127.0.0.1");
        PlayerPrefs.SetInt("ServerPort", 5000);
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
                EnterGame();
            } else if (idxSelected == 1) {
                EnterSettings();
            } else if (idxSelected == 2) {
                Quit();
            }
        } else if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit)) {
                GameObject objectHit = hit.transform.gameObject;
                if (objectHit == menuItems[0]) {
                    EnterGame();
                } else if (objectHit == menuItems[1]) {
                    EnterSettings();
                } else if (objectHit == menuItems[2]) {
                    Quit();
                }
            }
        } else {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit)) {
                GameObject objectHit = hit.transform.gameObject;
                if (objectHit == menuItems[0]) {
                    idxSelected = 0;
                } else if (objectHit == menuItems[1]) {
                    idxSelected = 1;
                } else if (objectHit == menuItems[2]) {
                    idxSelected = 2;
                }
            }
        }
    }

    private void EnterGame() {
        SceneManager.LoadScene("LobbySelect");
    }

    private void EnterSettings() {
        SceneManager.LoadScene("Settings");
    }

    private void Quit() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
