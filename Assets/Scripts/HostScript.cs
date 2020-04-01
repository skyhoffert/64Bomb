using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HostScript : MonoBehaviour {

    public GameObject network;

    // Handles network sharing objects.
    private Dictionary<string, GameObject> objMap;

    public Text txtLog;

    private Queue rxQ;
    private Queue txQ;

    private List<Connection> clientConns;

    private const int MAX_CONNS = 1;

    private bool permissionToHost = false;

    private float lastPingSentStopwatch = 0;

    private bool readyToStart = false;
    private float timeWaitingToStart = 4;

    void Start() {
        objMap = new Dictionary<string, GameObject>();

        clientConns = new List<Connection>();

        Init();
    }

    void Update() {
        if (rxQ is null) {
            Init();
            return;
        }

        if (rxQ.Count > 0) {
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
            } else if (buf[2] == 0x07) { // YOU_CAN_HOST
                if (buf[3] == 0x00) {
                    byte[] msg = new byte[4];
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
                    byte[] msg = new byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x0a;
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    Log("You sent ACK_I_WILL_HOST");
                    this.permissionToHost = true;
                }
            } else if (buf[2] == 0x16) { // HERE_CLIENT_ADDRESS
                string IP = Connection.IPBufToStr(buf, 3);
                Log("Got IP: " + IP + "");
                int port = Connection.PortBufToInt(buf, 7);
                Log("Got Port: " + port + "");

                clientConns[0].Connect(IP, port);

                // Send 3 pings
                byte[] msg = new byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
                clientConns[0].txQ.Enqueue(msg);
                clientConns[0].txQ.Enqueue(msg);

                // DEBUG
                readyToStart = true;
            }
        }

        if (clientConns.Count > 0 && clientConns[0].rxQ.Count > 0) {
            byte[] buf = (byte[]) clientConns[0].rxQ.Dequeue();

            if (buf[2] == 0x02) { // PING
                byte[] msg = new byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = buf[1];
                clientConns[0].txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
               Log("Client ping: " + clientConns[0].ping + " ms");
            } else if (buf[2] == 0x0c) { // ACK_HOST_CONNECTION
                Log("ACK_HOST_CONNECTION");
                byte[] msg = new byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            } else {
                Log("Unknown msg type");
            }
        }

        if (this.permissionToHost && this.clientConns.Count < 1) {
            clientConns.Add(new Connection(PlayerPrefs.GetString("ServerIP", "127.0.0.1"), PlayerPrefs.GetInt("ServerPort", 5000)));
            // ADD_HOST_CONNECTION
            byte[] msg = new byte[4];
            msg[0] = 0x01;
            msg[1] = 0x00;
            msg[2] = 0x0b;
            msg[3] = 0x00;
            clientConns[0].txQ.Enqueue(msg);
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            SceneManager.LoadScene("SampleScene");
        }

        if (this.clientConns.Count > 0) {
            if (this.lastPingSentStopwatch < 1) {
                this.lastPingSentStopwatch += Time.deltaTime;
            } else {
                this.lastPingSentStopwatch = 0;
                byte[] msg = new byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            }
        }

        // DEBUG: game starts some time after client connects.
        if (readyToStart) {
            if (timeWaitingToStart > 0) {
                timeWaitingToStart -= Time.deltaTime;
            } else {
                readyToStart = false;
                StartGame();
            }
        }
    }

    void Init() {
        rxQ = network.GetComponent<NetworkScript>().rxQ;
        txQ = network.GetComponent<NetworkScript>().txQ;

        if (rxQ is null) {
            return;
        }
        
        byte[] msg = new byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x02;
        txQ.Enqueue(msg);

        // Ask now if you can host.
        msg = new byte[4];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x06;
        msg[3] = 0x00;
        txQ.Enqueue(msg);
    }

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

        clientConns.ForEach(delegate(Connection c) {
            List<byte> msg = new List<byte>();
            msg.Add(0x01);
            msg.Add(0x00);
            msg.Add(0x81);
            char[] idar = id.ToCharArray();
            for (int i = 0; i < idar.Length; i++) {
                msg.Add((byte)idar[i]);
            }
            msg.Add(0x00); // NULL byte for string
            msg.Add((byte)(type&0xff));
            msg.Add(0x00);
            Connection.EncodeVec3InList(msg, pars[0]);
            msg.Add(0xff);
            c.txQ.Enqueue(msg.ToArray());
        });
    }

    void UpdateObject(string id, Vector3[] pars, int[] parTypes) {
        GameObject obj = objMap[id];

        for (int i = 0; i < parTypes.Length; i++) {
            if (parTypes[i] == 0) {
                obj.transform.position = pars[i];
            } else if (parTypes[i] == 1) {
                obj.transform.rotation = Quaternion.Euler(pars[i].x,pars[i].y,pars[i].z);
            } else if (parTypes[i] == 2) {
                obj.transform.localScale = pars[i];
            }
        }
    }

    void StartGame() {
        AddObject("base", 0, new Vector3[] {new Vector3(0, -3, 0), new Vector3(20, 1, 1) }, new int[] {0, 2});
        
        AddObject("leftwall", 0, new Vector3[] {new Vector3(-10, 0, 0), new Vector3(1, 10, 1)}, new int[] {0, 2});
        
        AddObject("rightwall", 0, new Vector3[] {new Vector3(10, 0, 0), new Vector3(1, 10, 1)}, new int[] {0, 2});
        
        AddObject("ball", 1, new Vector3[] {new Vector3(0, 3, 0)}, new int[] {0});

        // TODO: pass this information to the client.
    }

    void Log(string s) {
        txtLog.text += (s + "\n");
        Debug.Log(s);
    }
}
