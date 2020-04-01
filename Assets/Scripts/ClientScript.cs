using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Text; // for Encoding

public class ClientScript : MonoBehaviour {

    public GameObject network;
    
    public Dictionary<string, GameObject> objMap;

    public Text txtLog;

    private Queue rxQ;
    private Queue txQ;

    private Connection hostConn;

    private bool permissionToConnect = false;
    private bool hasConnected = false;
    
    private float lastPingSentStopwatch = 0;

    void Start() {
        objMap = new Dictionary<string, GameObject>();
        
        Init();
    }

    void Init() {
        this.rxQ = network.GetComponent<NetworkScript>().rxQ;
        this.txQ = network.GetComponent<NetworkScript>().txQ;
        
        if (rxQ is null) {
            return;
        }

        byte[] msg = new byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x02;
        txQ.Enqueue(msg);

        // Ask now if you can connect.
        msg = new byte[4];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x0d;
        msg[3] = 0x00;
        this.txQ.Enqueue(msg);

        this.hostConn = new Connection(PlayerPrefs.GetString("ServerIP", "127.0.0.1"), PlayerPrefs.GetInt("ServerPort", 5000));
    }

    void Update() {
        if (this.rxQ is null) {
            Init();
            return;
        }

        if (this.rxQ.Count > 0) {
            byte[] buf = (byte[]) rxQ.Dequeue();

            if (buf[2] == 0x02) { // PING
                byte[] msg = new byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = 0x00;
                txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
                // TODO: this is slow!
                float ping = network.GetComponent<NetworkScript>().ping;
                Log("Server ping: " + ping + " ms");
            } else if (buf[2] == 0x0e) { // YOU_CAN_CONNECT
                if (buf[3] == 0x00) {
                    byte[] msg = new byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x43;
                    msg[2] = 0x0f; // I_WILL_CONNECT
                    msg[3] = 0x00;
                    txQ.Enqueue(msg);
                } else {
                    Log("You CANNOT connect :(");
                }
            } else if (buf[2] == 0x10) { // YOU_WILL_CONNECT
                Log("got a you_will_connect message.");
                if (buf[3] == 0x43) {
                    byte[] msg = new byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x11; // ACK_I_WILL_CONNECT
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    Log ("You sent ACK_I_WILL_CONNECT");
                    this.permissionToConnect = true;
                } else {
                    Log("bad code. "+buf[3]+"");
                }
            }
        }

        if (this.hostConn.rxQ.Count > 0) {
            byte[] buf = (byte[]) hostConn.rxQ.Dequeue();

            if (buf[2] == 0x02) { // PING
                byte[] msg = new byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = buf[1];
                hostConn.txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
                Log("Host ping: " + hostConn.ping + " ms");
            } else if (buf[2] == 0x13) { // ACK_CLIENT_CONNECTION
                Log("ACK_CLIENT_CONNECTION");
                byte[] msg = new byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                hostConn.txQ.Enqueue(msg);
            } else if (buf[2] == 0x14) { // HERE_HOST_ADDRESS
                string IP = Connection.IPBufToStr(buf, 3);
                Log("Got IP: " + IP + "");
                int port = Connection.PortBufToInt(buf, 7);
                Log("Got Port: " + port + "");

                hostConn.Connect(IP, port);
                
                // Send 3 pings
                byte[] msg = new byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                hostConn.txQ.Enqueue(msg);
                hostConn.txQ.Enqueue(msg);
                hostConn.txQ.Enqueue(msg);
            } else if (buf[2] == 0x81) {
                List<byte> tmpname = new List<byte>();
                int namelen = 0;
                while(buf[3 + namelen] != 0x00) {
                    tmpname.Add(buf[3 + namelen]);
                    namelen++;
                }
                string objname = Encoding.UTF8.GetString(tmpname.ToArray(), 0, tmpname.Count);
                int type = buf[3+namelen+1];
                Log("Add obj called " + objname + " of type " + type);
                int velidx = 3+namelen+2;
                Log("got x vector val of " + Connection.ByteArrToFloat_64B(buf, velidx));
            }
        }

        if (this.permissionToConnect && !this.hasConnected) {
            // ADD_CLIENT_CONNECTION
            byte[] msg = new byte[4];
            msg[0] = 0x01;
            msg[1] = 0x00;
            msg[2] = 0x12;
            msg[3] = 0x00;
            hostConn.txQ.Enqueue(msg);
            this.hasConnected = true;
        }
        
        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene("SampleScene");
        }

        if (this.hasConnected) {
            if (this.lastPingSentStopwatch < 1) {
                this.lastPingSentStopwatch += Time.deltaTime;
            } else {
                this.lastPingSentStopwatch = 0;
                byte[] msg = new byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                hostConn.txQ.Enqueue(msg);
            }
        }
    }
    
    // TODO: call this method on reception of object!
    void AddObject(string id, int type, Vector3[] pars, int[] parTypes) {
        GameObject newobj = new GameObject();
        // TODO: remove the destroys below
        if (type == 0) {
            Destroy(newobj);
            newobj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        } else if (type == 1) {
            Destroy(newobj);
            newobj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newobj.AddComponent<Rigidbody>();
            newobj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionZ;
        }

        objMap.Add(id, newobj);

        for (int i = 0; i < parTypes.Length; i++) {
            if (parTypes[i] == 0) {
                newobj.transform.position = pars[i];
            } else if (parTypes[i] == 1) {
                newobj.transform.rotation = Quaternion.Euler(pars[i].x,pars[i].y,pars[i].z);
            } else if (parTypes[i] == 2) {
                newobj.transform.localScale = pars[i];
            }
        }
    }
    
    void Log(string s) {
        txtLog.text += (s + "\n");
        Debug.Log(s);
    }
}
