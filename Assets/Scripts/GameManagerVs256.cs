using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagerVs256 : MonoBehaviour {

	public Grid p1, p2;
	public InputHandler i1, i2;
	public string resetInput;
	public Text p1win, p2win; 
	public float timerLength;
	public float tileTimerLength;
	public bool gameOver;
	//float tileTimer;
	// Use this for initialization
	void Start () {
		p1.comboTimerLength = 0;
		p2.comboTimerLength = 0;
		p1.tileTimerLength = 0;
		p2.tileTimerLength = 0;
		i1.enabled = false;
		i2.enabled = false;
		gameOver = true;
		//tileTimer = 0;
	}


	// Update is called once per frame
	void Update () {
		/*
		if (tileTimerLength != 0 && !gameOver)
		{
			tileTimer += Time.deltaTime;
			if (tileTimer > tileTimerLength)
			{
				int value = Random.value > 0.9f ? 4 : 2;
				p1.addTile(value);
				p2.addTile(value);
				tileTimer = 0;
			}
		}
		*/
		if (Input.GetButtonDown(resetInput))
		{
			gameOver = false;
			//tileTimer = 0;
			i1.enabled = true;
			i2.enabled = true;
			p1.comboTimerLength = timerLength;
			p2.comboTimerLength = timerLength;
			p1.tileTimerLength = tileTimerLength;
			p2.tileTimerLength = tileTimerLength;
			p1win.text = "";
			p2win.text = "";
			int seed = Random.Range(int.MinValue, int.MaxValue);
			p1.InitRNGs(seed);
			p2.InitRNGs(seed);
			p1.startGame();
			p2.startGame();
		}
		if (!gameOver)
		{
			if (p1.winLossCode != Grid.WinLossCode.NONE || p2.winLossCode != Grid.WinLossCode.NONE)
			{
				p1.comboTimerLength = 0;
				p2.comboTimerLength = 0;
				p1.tileTimerLength = 0;
				p2.tileTimerLength = 0;
				i1.enabled = false;
				i2.enabled = false;
				gameOver = true;
				if (p1.winLossCode == p2.winLossCode)
				{
					p1win.text = "DRAW";
					p2win.text = "DRAW";
				}
				else if (p1.winLossCode == Grid.WinLossCode.MADE256)
				{
					p1win.text = "WIN";
					p2win.text = "LOSE";
				}
				else if (p2.winLossCode == Grid.WinLossCode.MADE256)
				{
					p1win.text = "LOSE";
					p2win.text = "WIN";
				}
				else if (p1.winLossCode == Grid.WinLossCode.OUTOFMOVES)
				{
					p1win.text = "LOSE";
					p2win.text = "WIN";
				}
				else if (p2.winLossCode == Grid.WinLossCode.OUTOFMOVES)
				{
					p1win.text = "WIN";
					p2win.text = "LOSE";
				}
				else if (p1.winLossCode == Grid.WinLossCode.GARBAGEFULL)
				{
					p1win.text = "LOSE";
					p2win.text = "WIN";
				}
				else if (p2.winLossCode == Grid.WinLossCode.GARBAGEFULL)
				{
					p1win.text = "WIN";
					p2win.text = "LOSE";
				}
			}
		}
	}
}
