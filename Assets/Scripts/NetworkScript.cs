using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System; // for Byte
using System.Text; // for Encoding
using System.Net.Sockets; // for UdpClient
using System.Net; // for IPEndPoint

public class Connection {

    public static String IPBufToStr(Byte[] buf, int idx) {
        String str = "";
        str += ((int)buf[idx+0]).ToString();
        str += ".";
        str += ((int)buf[idx+1]).ToString();
        str += ".";
        str += ((int)buf[idx+2]).ToString();
        str += ".";
        str += ((int)buf[idx+3]).ToString();
        return str;
    }

    public static int PortBufToInt(Byte[] buf, int idx) {
        int p = 0;
        p += ((int)buf[idx+0] << 24);
        p += ((int)buf[idx+1] << 16);
        p += ((int)buf[idx+2] << 8);
        p += ((int)buf[idx+3] << 0);
        return p;
    }

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

    private bool rxChange = false;
    private bool txChange = false;

    public Connection(String ip, int port) {
        serverIP = ip;
        serverPort = port;

        ping = 1000000;

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

    public void Connect(String ip, int port) {
        txChange = true;
        rxChange = true;
        serverIP = ip;
        serverPort = port;
    }
    
    void RxThread() {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        Byte[] buf;

        while (thrActive) {
            if (rxChange) {
                Thread.Sleep(txSleepTime);
                rxChange = false;
                continue;
            }

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
            if (txChange) {
                Thread.Sleep(txSleepTime);
                txChange = false;
                continue;
            }

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
