using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Codes.Linus.IntVectors;

public class InputHandler : MonoBehaviour {

	public Grid target;
	public string up, down, left, right, neutral;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonDown(up))
			target.makeMove(Vector2i.up);
		if (Input.GetButtonDown(down))
			target.makeMove(Vector2i.down);
		if (Input.GetButtonDown(left))
			target.makeMove(Vector2i.left);
		if (Input.GetButtonDown(right))
			target.makeMove(Vector2i.right);
		if (Input.GetButtonDown(neutral))
			target.makeMove(Vector2i.zero);
		
	}
}
