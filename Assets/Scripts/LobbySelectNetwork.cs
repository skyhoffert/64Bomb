using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System.Threading;
using System; // for Byte
using System.Text; // for Encoding
using System.Net.Sockets; // for UdpClient
using System.Net; // for IPEndPoint

public class LobbySelectNetwork : MonoBehaviour {

    // TODO: this script has too much in it, move things to LobbySelectScript

    public Text txt_serverStatus;
    public Text txt_hostStatus;
    public Text txt_clientStatus;
    
    private const float pingTimeMax = 1;
    private float pingTime;
    private float ping;
    private float pingSentTime;
    
    private UdpClient udpClient;
    
    private bool thrActive;
    private Thread thrRx;
    private int rxTimeout = 100; // ms
    private int rxBufferSize = 4096; // Bytes

    private String IP;
    private int PORT;

    private bool couldHost = false;
    private bool canHost = false;

    private bool couldJoin = false;
    private bool canJoin = false;

    void Start() {
        this.IP = PlayerPrefs.GetString("ServerIP", "127.0.0.1");
        this.PORT = PlayerPrefs.GetInt("ServerPort", 5000);

        // TODO: something is wrong here.
        Debug.Log("Using "+this.IP+":"+this.PORT);

        this.pingTime = pingTimeMax;
        this.ping = -1;

        this.udpClient = new UdpClient();
        udpClient.Client.ReceiveTimeout = rxTimeout;
        udpClient.Client.ReceiveBufferSize = rxBufferSize;
        
        thrActive = true;
        thrRx = new Thread(new ThreadStart(RxThread));
        thrRx.Start();
    }

    void Update() {
        if (this.pingTime > 0) {
            this.pingTime -= Time.deltaTime;
        } else {
            this.pingTime = pingTimeMax;
            
            Byte[] msg = new Byte[3];
            msg[0] = 0x01;
            msg[1] = 0x00;
            msg[2] = 0x02;
            udpClient.Send(msg, msg.Length, IP, PORT);
            pingSentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            if (ping >= 0) {
                txt_serverStatus.text = ""+ping+" ms";
            }

            if (!couldHost) {
                if (canHost) {
                    couldHost = true;
                    txt_hostStatus.text = "OK: Host Game";
                }
            }

            if (!couldJoin) {
                if (canJoin) {
                    couldJoin = true;
                    txt_clientStatus.text = "OK: Join Game";
                }
            }
        }
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

            if (buf[2] == 0x02) {
                Byte[] msg = new Byte[4];
                msg[0] = 0x01;
                msg[1] = 0x00;
                msg[2] = 0x03;
                msg[3] = 0x00;
                udpClient.Send(msg, msg.Length, IP, PORT);
            } else if (buf[2] == 0x03) {
                float now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                ping = now - pingSentTime;
            } else if (buf[2] == 0x07) {
                Debug.Log("You can host status code: " + buf[3]);
                if (buf[3] == 0x00) {
                    canHost = true;
                }
            } else if (buf[2] == 0x0e) {
                Debug.Log("You can connect status code: " + buf[3]);
                if (buf[3] == 0x00) {
                    canJoin = true;
                }
            }
        }
    }

    public void HostGameClicked() {
        if (!canHost) {
            Byte[] msg = new Byte[4];
            msg[0] = 0x01;
            msg[1] = 0x00;
            msg[2] = 0x06;
            msg[3] = 0x00;
            udpClient.Send(msg, msg.Length, IP, PORT);
        } else {
            SceneManager.LoadScene("GameAsHost");
        }
    }

    public void JoinGameClicked() {
        if (!canJoin) {
            Byte[] msg = new Byte[4];
            msg[0] = 0x01;
            msg[1] = 0x00;
            msg[2] = 0x0d;
            msg[3] = 0x00;
            udpClient.Send(msg, msg.Length, IP, PORT);
        } else {
            SceneManager.LoadScene("GameAsClient");
        }
    }
    
    void OnDestroy() {
        Byte[] msg = new Byte[3];
        msg[0] = 0x01;
        msg[1] = 0x00;
        msg[2] = 0xff;
        udpClient.Send(msg, msg.Length, IP, PORT);

        thrActive = false;
        thrRx.Join();

        udpClient.Close();
    }
}
