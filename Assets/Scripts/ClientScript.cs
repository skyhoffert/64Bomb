using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // for Byte

public class ClientScript : MonoBehaviour {

    public GameObject network;

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
                Debug.Log("Server ping: " + ping + " ms");
            } else if (buf[2] == 0x07) { // YOU_CAN_CONNECT
                if (buf[3] == 0x00) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x43; // I_WILL_CONNECT
                    msg[2] = 0x0f;
                    msg[3] = 0x00;
                    txQ.Enqueue(msg);
                } else {
                    Debug.Log("You CANNOT connect :(");
                }
            } else if (buf[2] == 0x10) { // YOU_WILL_CONNECT
                if (buf[3] == 0x43) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x11; // ACK_I_WILL_CONNECT
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    Debug.Log("You sent ACK_I_WILL_CONNECT");
                    this.permissionToConnect = true;
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
                Debug.Log("Host ping: " + hostConn.ping + " ms");
            } else if (buf[2] == 0x0c) { // ACK_CLIENT_CONNECTION
                Debug.Log("ACK_CLIENT_CONNECTION");
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                hostConn.txQ.Enqueue(msg);
            } 

            // TODO: handle HERE_HOST_ADDRESS
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
    }
}
