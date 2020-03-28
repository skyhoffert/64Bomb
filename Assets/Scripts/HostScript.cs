using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System; // for Byte

public class HostScript : MonoBehaviour {

    public GameObject network;

    public Text txtLog;

    private Queue rxQ;
    private Queue txQ;

    private Connection[] clientConns;

    private const int MAX_CONNS = 1;

    private bool permissionToHost = false;
    private int clientConnsAdded = 0;

    void Start() {
        Init();
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
                txtLog.text += ("Server ping: " + ping + " ms\n");
            } else if (buf[2] == 0x07) { // YOU_CAN_HOST
                if (buf[3] == 0x00) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x42;
                    msg[2] = 0x08;
                    msg[3] = 0x00;
                    txQ.Enqueue(msg);
                } else {
                    txtLog.text += ("You CANNOT host :(\n");
                }
            } else if (buf[2] == 0x09) { // YOU_WILL_HOST
                if (buf[3] == 0x42) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x0a;
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    txtLog.text += ("You sent ACK_I_WILL_HOST\n");
                    this.permissionToHost = true;
                }
            } else if (buf[2] == 0x16) { // HERE_CLIENT_ADDRESS
                String IP = Connection.IPBufToStr(buf, 3);
                txtLog.text += ("Got IP: " + IP + "\n");
                int port = Connection.PortBufToInt(buf, 7);
                txtLog.text += ("Got Port: " + port + "\n");

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
                msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
               txtLog.text += ("Client ping: " + clientConns[0].ping + " ms\n");
            } else if (buf[2] == 0x0c) { // ACK_HOST_CONNECTION
                txtLog.text += ("ACK_HOST_CONNECTION\n");
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            } else {
                txtLog.text += ("Unknown msg type\n");
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
    }
}
