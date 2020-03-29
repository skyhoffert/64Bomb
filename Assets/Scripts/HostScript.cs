using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System; // for Byte

public class HostScript : MonoBehaviour {

    public GameObject network;

    private List<GameObject> staticObjs;
    private List<GameObject> dynamicObjs;

    public Text txtLog;

    private Queue rxQ;
    private Queue txQ;

    private Connection[] clientConns;

    private const int MAX_CONNS = 1;

    private bool permissionToHost = false;
    private int clientConnsAdded = 0;

    private float lastPingSentStopwatch = 0;

    void Start() {
        staticObjs = new List<GameObject>();
        dynamicObjs = new List<GameObject>();

        StartGame();

        Init();
    }

    void Update() {
        if (rxQ is null) {
            Init();
            return;
        }

        if (rxQ.Count > 0) {
            Byte[] buf = (Byte[]) rxQ.Dequeue();

            if (buf[2] == 0x02) { // PING
                Byte[] msg = new Byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = 0x00;
                txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
                // TODO: this is slow!
                float ping = network.GetComponent<NetworkScript>().ping;
                Log("Server ping: " + ping + " ms");
            } else if (buf[2] == 0x07) { // YOU_CAN_HOST
                if (buf[3] == 0x00) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x42;
                    msg[2] = 0x08;
                    msg[3] = 0x00;
                    txQ.Enqueue(msg);
                } else {
                    Log("You CANNOT host :(");
                }
            } else if (buf[2] == 0x09) { // YOU_WILL_HOST
                if (buf[3] == 0x42) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x0a;
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    Log("You sent ACK_I_WILL_HOST");
                    this.permissionToHost = true;
                }
            } else if (buf[2] == 0x16) { // HERE_CLIENT_ADDRESS
                String IP = Connection.IPBufToStr(buf, 3);
                Log("Got IP: " + IP + "");
                int port = Connection.PortBufToInt(buf, 7);
                Log("Got Port: " + port + "");

                clientConns[0].Connect(IP, port);

                // Send 3 pings
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
                clientConns[0].txQ.Enqueue(msg);
                clientConns[0].txQ.Enqueue(msg);
            }
        }

        if (clientConns[0].rxQ.Count > 0) {
            Byte[] buf = (Byte[]) clientConns[0].rxQ.Dequeue();

            if (buf[2] == 0x02) { // PING
                Byte[] msg = new Byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = buf[1];
                clientConns[0].txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
               Log("Client ping: " + clientConns[0].ping + " ms");
            } else if (buf[2] == 0x0c) { // ACK_HOST_CONNECTION
                Log("ACK_HOST_CONNECTION");
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            } else {
                Log("Unknown msg type");
            }
        }

        if (this.permissionToHost && this.clientConnsAdded == 0) {
            // ADD_HOST_CONNECTION
            Byte[] msg = new Byte[4];
            msg[0] = 0x01;
            msg[1] = 0x00;
            msg[2] = 0x0b;
            msg[3] = 0x00;
            clientConns[0].txQ.Enqueue(msg);
            this.clientConnsAdded++;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene("SampleScene");
        }

        if (this.clientConnsAdded > 0) {
            if (this.lastPingSentStopwatch < 1) {
                this.lastPingSentStopwatch += Time.deltaTime;
            } else {
                this.lastPingSentStopwatch = 0;
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            }
        }
    }

    void Init() {
        rxQ = network.GetComponent<NetworkScript>().rxQ;
        txQ = network.GetComponent<NetworkScript>().txQ;

        if (rxQ is null) {
            return;
        }
        
        Byte[] msg = new Byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x02;
        txQ.Enqueue(msg);

        // Ask now if you can host.
        msg = new Byte[4];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x06;
        msg[3] = 0x00;
        txQ.Enqueue(msg);

        clientConns = new Connection[MAX_CONNS];
        clientConns[0] = new Connection(PlayerPrefs.GetString("ServerIP", "127.0.0.1"), PlayerPrefs.GetInt("ServerPort", 5000));
    }

    void AddObject(int whicharr, int type, Vector3 pos) {
        GameObject newobj = new GameObject();
        // TODO: remove the destroys below
        if (type == 0) {
            Destroy(newobj);
            newobj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        } else if (type == 1) {
            Destroy(newobj);
            newobj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newobj.AddComponent<Rigidbody>();
        }

        if (whicharr == 0) {
            staticObjs.Add(newobj);
        } else if (whicharr == 1) {
            dynamicObjs.Add(newobj);
        }

        newobj.transform.position = pos;
    }

    void UpdateObject(int whicharr, int idx, int updatetype, Vector3 par) {
        GameObject obj = new GameObject();
        if (whicharr == 0) {
            Destroy(obj);
            obj = staticObjs[idx];
        } else if (whicharr == 1) {
            Destroy(obj);
            obj = dynamicObjs[idx];
        }

        if (updatetype == 0) {
            obj.transform.position = par;
        } else if (updatetype == 1) {
            obj.transform.rotation = Quaternion.Euler(par.x,par.y,par.z);
        } else if (updatetype == 2) {
            obj.transform.localScale = par;
        }
    }

    void StartGame() {
        AddObject(0, 0, new Vector3(0, -3, 0));
        UpdateObject(0, 0, 2, new Vector3(20, 1, 1));
        
        AddObject(0, 0, new Vector3(-10, 0, 0));
        UpdateObject(0, 1, 2, new Vector3(1, 10, 1));
        
        AddObject(0, 0, new Vector3(10, 0, 0));
        UpdateObject(0, 2, 2, new Vector3(1, 10, 1));
        
        AddObject(1, 1, new Vector3(0, 3, 0));

        // TODO: pass this information to the client.
    }

    void Log(String s) {
        txtLog.text += (s + "\n");
        Debug.Log(s);
    }
}
