using UnityEngine;
using System.Collections;

public class deleteMe : MonoBehaviour {

    public MovieTexture mov;

    public UnityEngine.UI.Text fps;

	// Use this for initialization
	void Start () {
	mov.Play();
	}
	
	// Update is called once per frame
	void Update () {
	fps.text = (1f/Time.deltaTime).ToString();
	}
}
