using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerLocal : MonoBehaviour {

    public Camera camera;
    public GameObject target;
    public Rigidbody rigidBody;
    private float deadZoneH = 0.1f;
    private float deadZoneV = 0.1f;
    public float height = 5;
    public float distance = 10;
    private float angle;
    private float fAmount = 1;
    private float rotAmount = 20f;
    private float jumpForce = 120f;
    private float fallAdd = -0.03f;

    private int velState = 0; // 0:falling, 1:rising, 2:grounded
    private bool hasJump = false;

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
        Vector3 dP = new Vector3(0,(camera.transform.position.y - this.height) - target.transform.position.y, 0);
        camera.transform.position -= dP;

        // Rotate the camera around the player.
        float rotHor = -Input.GetAxis("Mouse X");
        if (Mathf.Abs(rotHor) > deadZoneH) {
            camera.transform.LookAt(target.transform);
            this.angle += rotHor * rotAmount * Time.deltaTime;
        }

        // Move the camera to the correct position.
        float dZ = -Mathf.Cos(this.angle) * distance;
        float dX = Mathf.Sin(this.angle) * distance;
        camera.transform.position = new Vector3(target.transform.position.x + dX, target.transform.position.y + height, target.transform.position.z + dZ);

        // Movement on the XZ plane.
        float axHor = Input.GetAxis("Horizontal");
        float axVert = Input.GetAxis("Vertical");
        if (Mathf.Abs(axVert) > deadZoneV) {
            Vector3 dir = target.transform.position - camera.transform.position;
            dir.y = 0;
            rigidBody.AddForceAtPosition(dir * fAmount * Mathf.Sign(axVert), target.transform.position);
        }
        if (Mathf.Abs(axHor) > deadZoneH) {
            Quaternion turnToHor = Quaternion.Euler(0,90,0);
            Vector3 dir = target.transform.position - camera.transform.position;
            dir.y = 0;
            dir = turnToHor * dir;
            rigidBody.AddForceAtPosition(dir * fAmount * Mathf.Sign(axHor), target.transform.position);
        }

        if (hasJump && Input.GetButton("Jump")) {
            hasJump = false;
            rigidBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (Input.GetButton("Cancel")) {
            if (Cursor.lockState == CursorLockMode.Locked) {
                Cursor.lockState = CursorLockMode.None;
            } else {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}
