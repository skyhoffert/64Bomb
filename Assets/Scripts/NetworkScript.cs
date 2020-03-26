using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System; // for Byte
using System.Text; // for Encoding
using System.Net.Sockets; // for UdpClient
using System.Net; // for IPEndPoint

public class NetworkScript : MonoBehaviour {

    private Thread thrRx;
    private Thread thrTx;
    private bool thrActive;

    public Queue rxQ;
    public Queue txQ;

    private UdpClient udpClient;
    private int rxTimeout = 100; // ms
    private int rxBufferSize = 4096; // Bytes

    void RxThread() {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        Byte[] buf;

        while (thrActive) {
            try {
                buf = udpClient.Receive(ref endPoint);
                //Debug.Log("rx from " + endPoint.Address.ToString() + ":" + endPoint.Port.ToString());
            } catch (SocketException) {
                continue;
            }

            if (buf[2] == 0x02) {
                Debug.Log("Got pinged.");
                Byte[] msg = new Byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = 0x00;
                udpClient.Send(msg, msg.Length, "127.0.0.1", 5000);
            }
        }
    }

    void TxThread() {
        Byte[] msg = new Byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0x02;
        udpClient.Send(msg, msg.Length, "127.0.0.1", 5000);

        while (thrActive) {
            if (txQ.Count > 0) {
                Debug.Log("found item in tx queue.");
                String dq = txQ.Dequeue().ToString();
            }
        }
    }

    void OnDestroy() {
        thrActive = false;

        thrRx.Join();
        thrTx.Join();

        udpClient.Close();
    }

    void Start() {
        // Create thread-safe tx and rx queues.
        rxQ = Queue.Synchronized(new Queue());
        txQ = Queue.Synchronized(new Queue());

        // Set up udp client.
        udpClient = new UdpClient();
        udpClient.Client.ReceiveTimeout = rxTimeout;
        udpClient.Client.ReceiveBufferSize = rxBufferSize;

        // Set up threads.
        thrActive = true;

        thrRx = new Thread(new ThreadStart(RxThread));
        thrTx = new Thread(new ThreadStart(TxThread));

        thrRx.Start();
        thrTx.Start();
    }

    void Update() { /* empty */ }
}
