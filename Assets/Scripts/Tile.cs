using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Codes.Linus.IntVectors;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class Tile : MonoBehaviour 
{ 
	public Grid g;
	private Animator animator;
	private Vector2i position = Vector2i.one * -1;
	public Vector2i pos
	{
		get
		{
			return position;
		}
		set
		{
			if (lastPosition != Vector2i.one * -1)
			{
				if (g)
					g.setAt(position, null);
				slideStart = Time.time;
				slideEnd = (slideStart + (2.0f / 60) * (value - lastPosition).magnitude) / g.comboSpeedup();
				sliding = true;
				if (!animator)
					animator = GetComponent<Animator>();
				animator.SetBool("Ready", false);
			}
			else
				transform.position = g.transform.position + value;
			if (g)
				g.setAt(value, this);
			position = value;
			/*
			if (g)
			{
				if (position != Vector2i.one * -1)
					g.setAt(position, null);
				g.setAt(value, this);
			}
			position = value;
			transform.position = g.transform.position + position;
			*/
		}
	}
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
			if (!sliding)
			{
				if (value >= -8192 && value <= 256)
					GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(value.ToString());
				else
					GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("UnknownTile");
			}
		}
	}
	public bool isNegative
	{
		get
		{
			return val < 0;
		}
	}
	public bool mergedThisMove;
	private Vector2i lastPosition = Vector2i.one * -1;
	private float slideStart, slideEnd;
	public bool sliding;
	public bool popAnimFinished; // Used by the animation to signal when it finishes to the tile.
	private Tile mergeTarget; // The tile we're sliding into; don't destroy it until we get there

	public bool move(Vector2i direction, bool recursed = false)
	{
		if (!recursed)
		{
			if (sliding)
				endSlide();
			lastPosition = position;
		}
		mergedThisMove = false;
		Vector2i next = position + direction;
		if (!next.IsWithinBounds(Vector2i.zero, new Vector2i(g.w, g.h)))
			return false;
		if (g.at(next) == null)
		{
			pos = next;
			move(direction, true);
			return true;
		}
		if (!isNegative && g.at(next).isNegative && g.at(next).val >= (val / -4)) {
			Tile other = g.at(next);
			pos = next; // Sets our position, but removes other from grid
			other.pos = next - direction; // Moves other into our old position, but removes us from grid
			g.setAt(pos, this); // Adds us back to grid
			move(direction, false); // Pretend we're not recursing for now until there's an animation for this
			other.move(direction); // In case we merge or negate; don't want to leave empty space.
			return true;
		}
		if (g.at(next).val == val && !g.at(next).mergedThisMove && !isNegative)
		{
			//Destroy(g.at(next).gameObject);
			mergeTarget = g.at(next);
			g.at(next).transform.position += Vector3.forward * -0.125f; // Nudge the other tile toward the camera so we slide behind it.
			pos = next;
			val *= 2;
			mergedThisMove = true;
			animator.SetTrigger("Merge");
			return true;
		}
		if (g.at(next).val == -val && !g.at(next).mergedThisMove)
		{
			//Destroy(g.at(next).gameObject);
			mergeTarget = g.at(next);
			pos = next;
			if (!isNegative)
			{
				value = -value; // Show the negative tile disappearing. Since we're already sliding, this won't show until we get there.
				mergeTarget.transform.position += Vector3.forward * -0.125f; // The other tile is negative, so we want to slide behind it.
			}
			else
				mergeTarget.transform.position += Vector3.forward * 0.125f; // The other tile is positive, so we want to slide in front of it.

			g.setAt(next, null);
			//Destroy(gameObject);
			animator.SetTrigger("Pop");
			return true;
		}
		return false;
	}
	public bool canMove(Vector2i direction)
	{
		Vector2i next = position + direction;
		if (!next.IsWithinBounds(Vector2i.zero, new Vector2i(g.w, g.h)))
			return false;
		return 
		(
		 	g.at(next) == null ||
			(g.at(next).val == val && !isNegative) ||
			g.at(next).val == -val
		);
	}

	void endSlide()
	{
		if (mergeTarget)
			Destroy(mergeTarget.gameObject);
		transform.position = g.transform.position + position;
		sliding = false;
		animator.SetBool("Ready", true);
		if (value >= -8192 && value <= 256)
			GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(value.ToString());
		else
			GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("UnknownTile");
	}

	// Use this for initialization
	void Start () {
		animator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
		if (sliding)
		{
			if (Time.time >= slideEnd)
			{
				endSlide();
			}
			else
			{
				transform.position = Vector3.Lerp(
						g.transform.position + lastPosition,
					   	g.transform.position + position,
					   	Mathf.InverseLerp(slideStart, slideEnd, Time.time));
			}
		}
		if (popAnimFinished)
			Destroy(gameObject);
	}
}
