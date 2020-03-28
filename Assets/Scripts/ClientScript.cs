using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System; // for Byte

public class ClientScript : MonoBehaviour {

    public GameObject network;

    public Text txtLog;

    private Queue rxQ;
    private Queue txQ;

    private Connection hostConn;

    private bool permissionToConnect = false;
    private bool hasConnected = false;

    void Start() {
        Init();
    }

    void Init() {
        this.rxQ = network.GetComponent<NetworkScript>().rxQ;
        this.txQ = network.GetComponent<NetworkScript>().txQ;
        
        if (rxQ is null) {
            return;
        }

        Byte[] msg = new Byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x02;
        txQ.Enqueue(msg);

        // Ask now if you can connect.
        msg = new Byte[4];
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
            } else if (buf[2] == 0x0e) { // YOU_CAN_CONNECT
                if (buf[3] == 0x00) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x43;
                    msg[2] = 0x0f; // I_WILL_CONNECT
                    msg[3] = 0x00;
                    txQ.Enqueue(msg);
                } else {
                    txtLog.text += ("You CANNOT connect :(\n");
                }
            } else if (buf[2] == 0x10) { // YOU_WILL_CONNECT
                txtLog.text += ("got a you_will_connect message.\n");
                if (buf[3] == 0x43) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x11; // ACK_I_WILL_CONNECT
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    txtLog.text +=  ("You sent ACK_I_WILL_CONNECT\n");
                    this.permissionToConnect = true;
                } else {
                    txtLog.text += ("bad code. "+buf[3]+"\n");
                }
            }
        }

        if (this.hostConn.rxQ.Count > 0) {
            Byte[] buf = (Byte[]) hostConn.rxQ.Dequeue();

            if (buf[2] == 0x02) { // PING
                Byte[] msg = new Byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = buf[1];
                hostConn.txQ.Enqueue(msg);
            } else if (buf[2] == 0x03) { // PONG
                txtLog.text += ("Host ping: " + hostConn.ping + " ms\n");
            } else if (buf[2] == 0x13) { // ACK_CLIENT_CONNECTION
                txtLog.text += ("ACK_CLIENT_CONNECTION\n");
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                hostConn.txQ.Enqueue(msg);
            } else if (buf[2] == 0x14) { // HERE_HOST_ADDRESS
                String IP = Connection.IPBufToStr(buf, 3);
                txtLog.text += ("Got IP: " + IP + "\n");
                int port = Connection.PortBufToInt(buf, 7);
                txtLog.text += ("Got Port: " + port + "\n");

                hostConn.Connect(IP, port);
                
                // Send 3 pings
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                hostConn.txQ.Enqueue(msg);
                hostConn.txQ.Enqueue(msg);
                hostConn.txQ.Enqueue(msg);
            }
        }

        if (this.permissionToConnect && !this.hasConnected) {
            // ADD_CLIENT_CONNECTION
            Byte[] msg = new Byte[4];
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
    }
}
