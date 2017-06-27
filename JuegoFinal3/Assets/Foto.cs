using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Foto : MonoBehaviour {
    public string levelToLoad;
    public float timer = 10f;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Application.LoadLevel("SeleccionarAvatar");
        }
    }
}
