using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerControllerLocal : MonoBehaviour {

    public Camera cam;
    public GameObject target;
    private Rigidbody rigidBody;
    private float deadZoneH = 0.05f;
    private float deadZoneV = 0.05f;
    public float height = 5;
    public float distance = 10;
    private float angle;
    private float fAmount = 10f;
    private float rotAmount = 10f;
    private float jumpForce = 120f;
    private float fallAdd = -0.03f;

    private int velState = 0; // 0:falling, 1:rising, 2:grounded
    private bool hasJump = false;
    private float jcd = 0;
    private float jcdMax = 0.5f;
    private float jVM = 0.1f;

    void Start() {
        rigidBody = target.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update() {
        if (velState == 0) {
            if (rigidBody.velocity.y >= 0) {
                velState = 2;
                hasJump = true;
            } else {
                rigidBody.velocity += new Vector3(0,fallAdd,0);
            }
        } else if (velState == 1) {
            if (rigidBody.velocity.y <= 0) {
                velState = 0;
            }
        } else if (velState == 2) {
            if (rigidBody.velocity.y > 0) {
                velState = 1;
            } else if (rigidBody.velocity.y < 0) {
                velState = 0;
            }
        }

        // Keep the camera at the correct height.
        Vector3 dP = new Vector3(0,(cam.transform.position.y - this.height) - target.transform.position.y, 0);
        cam.transform.position -= dP;

        // Rotate the camera around the player.
        float rotHor = -Input.GetAxis("Mouse X");
        if (Mathf.Abs(rotHor) > deadZoneH) {
            cam.transform.LookAt(target.transform);
            this.angle += rotHor * rotAmount * Time.deltaTime;
        }

        // Move the cam to the correct position.
        float dZ = -Mathf.Cos(this.angle) * distance;
        float dX = Mathf.Sin(this.angle) * distance;
        cam.transform.position = new Vector3(target.transform.position.x + dX, target.transform.position.y + height, target.transform.position.z + dZ);

        // Movement on the XZ plane.
        float axHor = Input.GetAxis("Horizontal");
        float axVert = Input.GetAxis("Vertical");
        if (Mathf.Abs(axVert) > deadZoneV) {
            Vector3 dir = target.transform.position - cam.transform.position;
            dir.y = 0;
            rigidBody.AddForceAtPosition(dir * fAmount * Mathf.Sign(axVert), target.transform.position);
        }
        if (Mathf.Abs(axHor) > deadZoneH) {
            Quaternion turnToHor = Quaternion.Euler(0,90,0);
            Vector3 dir = target.transform.position - cam.transform.position;
            dir.y = 0;
            dir = turnToHor * dir;
            rigidBody.AddForceAtPosition(dir * fAmount * Mathf.Sign(axHor), target.transform.position);
        }

        if (jcd > 0) {
            jcd -= Time.deltaTime;
        } else if (hasJump && Input.GetButton("Jump")) {
            hasJump = false;
            jcd = jcdMax;
            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            target.transform.position += new Vector3(0,jVM,0);
        }

        if (Input.GetButtonDown("Cancel")) {
            if (Cursor.lockState == CursorLockMode.Locked) {
                Cursor.lockState = CursorLockMode.None;
                Time.timeScale = 1f;
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                Time.timeScale = 0f;
            }
        }

        if (target.transform.position.y < -100) {
            rigidBody.velocity = new Vector3(0,0,0);
            target.transform.position = new Vector3(0,0,0);
        }
    }

    public void ExitPress() {
        SceneManager.LoadScene("SampleScene");
    }
}
