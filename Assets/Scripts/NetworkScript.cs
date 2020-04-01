using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Threading;
using System; // for byte
using System.Text; // for Encoding
using System.Net.Sockets; // for UdpClient
using System.Net; // for IPEndPoint

public class Connection {

    public static String IPBufToStr(byte[] buf, int idx) {
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

    public static int PortBufToInt(byte[] buf, int idx) {
        int p = 0;
        p += ((int)buf[idx+0] << 24);
        p += ((int)buf[idx+1] << 16);
        p += ((int)buf[idx+2] << 8);
        p += ((int)buf[idx+3] << 0);
        return p;
    }

    // TODO: this may not be correct...
    //       perhaps byte is being treated as a signed integer in conversion
    public static byte[] FloatToByteArr_64B(float f) {
        float adjustedf = f*1000;
        int intval = (int)adjustedf;
        byte[] encoded = new byte[4];
        encoded[0] = (byte) ((intval >> 24) & 0xff);
        encoded[1] = (byte) ((intval >> 16) & 0xff);
        encoded[2] = (byte) ((intval >>  8) & 0xff);
        encoded[3] = (byte) ((intval >>  0) & 0xff);
        string t = "val: ";
        for (int i = 0; i < 4; i++) {
            t += encoded[i] + ", ";
        }
        Debug.Log(t);
        return encoded;
    }
    
    // TODO: comment. return float that was decoded
    // TODO: this is not correct...
    public static float ByteArrToFloat_64B(byte[] buf, int sidx) {
        int intval = 0;
        intval |= (int) (((uint) (buf[sidx+0])) << 24);
        intval |= (int) (((uint) (buf[sidx+1])) << 16);
        intval |= (int) (((uint) (buf[sidx+2])) <<  8);
        intval |= (int) (((uint) (buf[sidx+3])) <<  0);
        float decval = ((float)intval) / 1000f;
        return decval;
    }

    // TODO: comment. returns 0 on success
    public static int EncodeVec3InList(List<byte> buf, Vector3 vector) {
        byte[] tmp = FloatToByteArr_64B(vector.x);
        for (int i = 0; i < 4; i++) {
            buf.Add(tmp[i]);
        }
        tmp = FloatToByteArr_64B(vector.y);
        for (int i = 0; i < 4; i++) {
            buf.Add(tmp[i]);
        }
        tmp = FloatToByteArr_64B(vector.z);
        for (int i = 0; i < 4; i++) {
            buf.Add(tmp[i]);
        }
        return 0;
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

    // TODO: it would be nice if there was a variable here to determine if a connection is active.
    //       also, if pings were sent in the background to check this, that might be okay.

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
        byte[] msg = new byte[3];
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
        byte[] buf;

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
                byte[] dq = (byte[])txQ.Dequeue();
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
