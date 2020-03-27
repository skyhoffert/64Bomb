using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System; // for Byte
using System.Text; // for Encoding
using System.Net.Sockets; // for UdpClient
using System.Net; // for IPEndPoint

public class Connection {

    private Thread thrRx;
    private Thread thrTx;
    private bool thrActive;

    public Queue rxQ;
    public Queue txQ;

    private UdpClient udpClient;
    private int rxTimeout = 100; // ms
    private int rxBufferSize = 4096; // Bytes
    private int txSleepTime = 100; // ms
    
    private String serverIP;
    private int serverPort;
    
    public float ping; // In this file to provide accurate time measurement between ping and pong.
    private float pingSentTime; // TODO: only works for a single ping and doesn't check ID.

    public Connection(String ip, int port) {
        serverIP = ip;
        serverPort = port;

        ping = -1;

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

    ~Connection() {
        // Kill the connection.
        Byte[] msg = new Byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0xff;
        udpClient.Send(msg, msg.Length, serverIP, serverPort);

        thrActive = false;

        thrRx.Join();
        thrTx.Join();

        udpClient.Close();
    }
    
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

            if (buf[2] == 0x03) {
                float now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                ping = now - pingSentTime;
            }

            rxQ.Enqueue(buf);
        }
    }

    void TxThread() {
        while (thrActive) {
            if (txQ.Count > 0) {
                Byte[] dq = (Byte[])txQ.Dequeue();
                if (dq[0] == 0x00) {
                    Debug.Log("Bad TX dequeue");
                } else {
                    udpClient.Send(dq, dq.Length, serverIP, serverPort);
                }

                if (dq[2] == 0x02) { // PING
                    pingSentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;;
                }
            } else {
                Thread.Sleep(txSleepTime);
            }
        }
    }
}

public class NetworkScript : MonoBehaviour {

    private Connection serverConn;

    public Queue rxQ;
    public Queue txQ;

    public float ping;

    void Start() {
        serverConn = new Connection(PlayerPrefs.GetString("ServerIP", "127.0.0.1"), PlayerPrefs.GetInt("ServerPort", 5000));

        this.rxQ = serverConn.rxQ;
        this.txQ = serverConn.txQ;
    }

    void Update() {
        this.ping = serverConn.ping;
    }
}
