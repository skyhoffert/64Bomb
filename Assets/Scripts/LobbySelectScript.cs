using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbySelectScript : MonoBehaviour {
    void Start() { }

    void Update() { }
    
    public void ExitClicked() {
        SceneManager.LoadScene("SampleScene");
    }

    public void PlayLocalClicked() {
        SceneManager.LoadScene("GameTest");
    }
}
