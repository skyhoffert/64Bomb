using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using System;

public class HandleSettingsScreen : MonoBehaviour {

    public Slider slMouseSens;

    public Text placeholderForIP;
    public Text placeholderForPort;

    public Text txtPort;
    public Text txtIP;

    void Start() {
        if (slMouseSens) {
            slMouseSens.value = PlayerPrefs.GetFloat("MouseSens",0.5f);
        }

        placeholderForIP.text = PlayerPrefs.GetString("ServerIP", "127.0.0.1");
        placeholderForPort.text = ""+PlayerPrefs.GetInt("ServerPort", 5000);
    }

    public void UpdateServerIP() {
        // TODO: Validate input!
        Debug.Log("Updated Server IP");
        PlayerPrefs.SetString("ServerIP", txtIP.text);
    }

    public void UpdateServerPort() {
        if (Int32.TryParse(txtPort.text, out int port)) {
            if (port < 1024 || port > 65534) {
                Debug.Log("Bad Port Number");
                return;
            }
            Debug.Log("Updated server port to "+port);
            PlayerPrefs.SetInt("ServerPort", port);
        } else {
            Debug.Log("Invalid Port");
        }
    }

    public void ExitPressed() {
        SceneManager.LoadScene("SampleScene");
    }

    public void SliderChange() {
        if (slMouseSens) {
            PlayerPrefs.SetFloat("MouseSens",slMouseSens.value);
        }
    }
}
