using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System; // for Byte

public class HostScript : MonoBehaviour {

    public GameObject network;

    private Queue rxQ;
    private Queue txQ;

    void Start() {
        rxQ = network.GetComponent<NetworkScript>().rxQ;
        txQ = network.GetComponent<NetworkScript>().txQ;

        // Ask now if you can host.
        Byte[] msg = new Byte[4];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x06;
        msg[3] = 0x00;
        txQ.Enqueue(msg);
    }

    void Update() {
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
                Debug.Log("Ping: " + ping + " ms");
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
                }
            }
        }
    }
}
