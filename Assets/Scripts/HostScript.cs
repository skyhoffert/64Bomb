﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // for Byte

public class HostScript : MonoBehaviour {

    public GameObject network;

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
                Debug.Log("Server ping: " + ping + " ms");
            } else if (buf[2] == 0x07) { // YOU_CAN_HOST
                if (buf[3] == 0x00) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x42;
                    msg[2] = 0x08;
                    msg[3] = 0x00;
                    txQ.Enqueue(msg);
                } else {
                    Debug.Log("You CANNOT host :(");
                }
            } else if (buf[2] == 0x09) { // YOU_WILL_HOST
                if (buf[3] == 0x42) {
                    Byte[] msg = new Byte[4];
                    msg[0] = 0x01;
                    msg[1] = 0x00;
                    msg[2] = 0x0a;
                    msg[3] = buf[1];
                    txQ.Enqueue(msg);
                    Debug.Log("You sent ACK_I_WILL_HOST");
                    this.permissionToHost = true;
                }
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
                Debug.Log("Client ping: " + clientConns[0].ping + " ms");
            } else if (buf[2] == 0x0c) { // ACK_HOST_CONNECTION
                Debug.Log("ACK_HOST_CONNECTION");
                Byte[] msg = new Byte[3];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x02;
                clientConns[0].txQ.Enqueue(msg);
            }
            
            // TODO: handle HERE_CLIENT_ADDRESS
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
    }
}