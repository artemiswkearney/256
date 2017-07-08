using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Codes.Linus.IntVectors;

public class UnitTestManager : MonoBehaviour {

	public Grid g, o;

	public void doMergeOrderTest()
	{
		g.startTiles = 0;
		g.w = 4;
		g.h = 4;
		o.startTiles = 0;
		o.w = 4;
		o.h = 4;
		foreach (Vector2i v in new Vector2i[] {Vector2i.up, Vector2i.down, Vector2i.left, Vector2i.right})
		{
			g.startGame();
			o.startGame();
			Vector2i position = Vector2i.zero;
			if (v.x == 1)
				position.x = 3;
			if (v.y == 1)
				position.y = 3;
			g.addTile(position, 2);
			g.addTile(position - v, 2);
			g.addTile(position - v - v, 2);
			g.makeMove(v);
			Debug.Assert(g.at(position) && g.at(position).val == 4 && g.at(position - v) && g.at(position - v).val == 2,
					"MergeOrderTest failed! Direction: " + v);
		}
		Debug.Log("MergeOrderTest passed.");

	}
	public void doMergeOrderTest2()
	{
		g.startTiles = 0;
		g.w = 4;
		g.h = 4;
		o.startTiles = 0;
		o.w = 4;
		o.h = 4;
		foreach (Vector2i v in new Vector2i[] {Vector2i.up, Vector2i.down, Vector2i.left, Vector2i.right})
		{
			g.startGame();
			o.startGame();
			Vector2i position = Vector2i.zero;
			if (v.x == 1)
				position.x = 3;
			if (v.y == 1)
				position.y = 3;
			g.addTile(position, 2);
			g.addTile(position - v, -2);
			g.addTile(position - v - v, 2);
			g.makeMove(v);
			Debug.Assert(g.at(position) && g.at(position).val == 2 && !g.at(position - v),
					"MergeOrderTest2 failed! Direction: " + v);
		}
		Debug.Log("MergeOrderTest2 passed.");
	}


	// Use this for initialization
	void Start () {
		doMergeOrderTest();	
		doMergeOrderTest2();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
