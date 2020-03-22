using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HandleSettingsScreen : MonoBehaviour {

    public Slider slMouseSens;

    void Start() {
        if (slMouseSens) {
            slMouseSens.value = PlayerPrefs.GetFloat("MouseSens",0.5f);
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
