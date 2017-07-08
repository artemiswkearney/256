using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Codes.Linus.IntVectors;

[RequireComponent(typeof(SpriteRenderer))]
public class GarbageTile : MonoBehaviour {

	public Grid g;
	private int value;
	public int val
	{
		get
		{
			return value;
		}
		set
		{
			this.value = value;
			if (value >= -8192 && value <= 256)
				GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(value.ToString());
			else
				GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("UnknownTile");
		}
	}
	private Vector2i position = Vector2i.one * -1;
	public Vector2i pos
	{
		get
		{
			return position;
		}
		set
		{
			if (g)
			{
				if (position != Vector2i.one * -1)
				{
					g.setGarbageAt(position, null);
				}
				g.setGarbageAt(value, this);
			}
			position = value;
			transform.position = g.transform.position + Vector3.up * 0.25f + Vector3.left * 0.25f + Vector3.back + value;
		}
	}
	public void drop()
	{
		if (g.at(pos) && g.at(pos).isNegative)
			return; // Stacking: Don't drop if over a negative tile
		if (!g.at(pos))
		{
			g.addTile(pos, val);
		}
		else if (g.at(pos).val == -val)
		{
			Destroy(g.at(pos).gameObject);
			g.setAt(pos, null);
		}
		else if (g.at(pos).val == -2 * val)
		{
			g.at(pos).val /= 2;
		}
		else if (g.at(pos).val == val / -2)
		{
			g.at(pos).val = val / 2;
		}
		else if (g.at(pos).val < -val)
		{
			g.at(pos).val = val;
		}
		g.setGarbageAt(pos, null);
		Destroy(gameObject);
	}


	// Use this for initialization
	void Start () {
		transform.localScale = Vector3.one * 0.5f;

	}

	// Update is called once per frame
	void Update () {

	}
}
