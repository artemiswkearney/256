using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using Codes.Linus.IntVectors;

public class Grid : MonoBehaviour {
	public int w, h;
	public int startTiles;
	public float comboTimerLength;
	public float lagLength;
	public float tileTimerLength;
	public bool allowNullMove = true;
	public bool enableGarbage = true;
	public bool addTileAfterMove = true;

	// If enabled, new tiles appear and combos break at up to
	// <comboSpeedupMaxMultiplier>x speed as combo increases.
	public bool doComboSpeedup = true;
	public float comboSpeedupMaxMultiplier = 2.0f;

	public Slider comboMeter, tileMeter;
	public Text comboText;
	
	public Grid opponent;
	public Tile tilePrefab;
	public GarbageTile garbagePrefab;
	Tile[,] contents;
	GarbageTile[,] garbage;
	int combo;
	List<Vector3i> explosionsInCombo;
	int tilesReceived;
	float comboTimer, lagTimer, tileTimer;
	Vector2i moveBuffer;
	public bool hasWon, hasLost;
	public enum WinLossCode
	{
		NONE,
		MADE256,
		GARBAGEFULL,
		OUTOFMOVES
	}
	public WinLossCode winLossCode;

	Random.State tileValueRandomState;
	Random.State tilePositionRandomState;
	Random.State garbagePositionRandomState;
	/* Rules for safe use of randomness:
	When you need to use RNG:
	1. Save the current RNG state in a local variable.
	2. Load the appropriate one of the above states.
	3. Do your code including RNG calls.
	4. Save the current RNG state to the same variable you loaded from.
	5. Restore the RNG state you saved at the beginning.
	Helper function below if you need it.
	*/
	public void doWithRNG(ref Random.State state, System.Action block)
	{
		Random.State oldState = Random.state;
		Random.state = state;
		block();
		state = Random.state;
		Random.state = oldState;
	}
	// To initialize RNGs:
	public void initTileValueRNG(int seed)
	{
		doWithRNG(ref tileValueRandomState, () => {
				Random.InitState(seed);
				});
	}
	public void initTilePositionRNG(int seed)
	{
		doWithRNG(ref tilePositionRandomState, () => {
				Random.InitState(seed);
				});
	}
	public void initGarbagePositionRNG(int seed)
	{
		doWithRNG(ref garbagePositionRandomState, () => {
				Random.InitState(seed);
				});
	}
	// Or just init them all:
	public void InitRNGs(int seed)
	{
		Random.State metaRNG = default(Random.State);
		doWithRNG(ref metaRNG, () =>
				{
				Random.InitState(seed);
				initTileValueRNG(Random.Range(int.MinValue, int.MaxValue));
				initTilePositionRNG(Random.Range(int.MinValue, int.MaxValue));
				initGarbagePositionRNG(Random.Range(int.MinValue, int.MaxValue));
				});
	}

	public float comboSpeedup()
	{
		return 1.0f + ((Mathf.Pow(Mathf.Atan(combo / 2.0f) / (Mathf.PI / 2), 2)) * (comboSpeedupMaxMultiplier - 1));
	}

	public Tile at(Vector2i position)
	{
		return contents[position.x, position.y];
	}
	public Tile at(int x, int y)
	{
		return contents[x,y];
	}
	public void setAt(Vector2i position, Tile t)
	{
		contents[position.x, position.y] = t;
	}
	public void setAt(int x, int y, Tile t)
	{
		contents[x, y] = t;
	}
	public List<Vector2i> getOpenSpaces()
	{
		List<Vector2i> result = new List<Vector2i>();
		for (int x = 0; x < w; x++)
			for (int y = 0; y < h; y++)
				if (at(x,y) == null)
					result.Add(new Vector2i(x,y));
		return result;
	}
	public GarbageTile garbageAt(Vector2i position)
	{
		return garbage[position.x, position.y];
	}
	public GarbageTile garbageAt(int x, int y)
	{
		return garbage[x,y];
	}
	public void setGarbageAt(Vector2i position, GarbageTile t)
	{
		garbage[position.x, position.y] = t;
	}
	public void setGarbageAt(int x, int y, GarbageTile t)
	{
		garbage[x, y] = t;
	}
	public List<Vector2i> getOpenGarbageSpaces()
	{
		List<Vector2i> result = new List<Vector2i>();
		for (int x = 0; x < w; x++)
			for (int y = 0; y < h; y++)
				if (garbageAt(x,y) == null)
					result.Add(new Vector2i(x,y));
		return result;
	}
	public bool hasGarbage()
	{
		for (int x = 0; x < w; x++)
			for (int y = 0; y < h; y++)
				if (garbageAt(x,y))
					return true;
		return false;
	}

	public void breakCombo()
	{
		combo = 0;
		if (enableGarbage)
			if (opponent.combo == 0)
				dropGarbage();
		explosionsInCombo.Clear();
	}
	/* no longer a thing
	public bool checkGarbageLoss()
	{
	if (!enableGarbage) return false;
	foreach (GarbageTile t in garbage)
	if (t == null)
	return false;
	return true;
	}
	*/
	public bool checkLoss()
	{
		foreach (Tile t in contents)
			if (t == null)
				return false;
		foreach (Tile t in contents)
			if (t.canMove(Vector2i.up) || t.canMove(Vector2i.down) || t.canMove(Vector2i.left) || t.canMove(Vector2i.right))
				return false;
		return true;
	}
	public void addTile(Vector2i pos, int val)
	{
		Tile t = Instantiate(tilePrefab.gameObject).GetComponent<Tile>();
		t.g = this;
		t.val = val;
		t.pos = pos;
	}
	public void addTile(int x, int y, int val)
	{
		addTile(new Vector2i(x,y), val);
	}
	public void addTile(int val)
	{
		List<Vector2i> spaces = getOpenSpaces();
		if (spaces.Count == 0)
			return; //Not a problem; just can't add a tile
		doWithRNG(ref tilePositionRandomState, () => {
				Vector2i pos = spaces[Random.Range(0, spaces.Count)];
				addTile(pos, val);
				});
	}
	public void addTile()
	{
		doWithRNG(ref tileValueRandomState, () => {
				addTile(Random.value > 0.9 ? 4 : 2);
				});
	}
	public void addGarbage(Vector2i pos, int val) {
		GarbageTile t = Instantiate(garbagePrefab.gameObject).GetComponent<GarbageTile>();
		t.g = this;
		t.val = val;
		t.pos = pos;
	}
	public void addGarbage(int x, int y, int val) {
		addGarbage(new Vector2i(x, y), val);
	}
	public void addGarbage(int val)
	{
		doWithRNG(ref garbagePositionRandomState, () => {
				List<Vector2i> spaces = getOpenGarbageSpaces();
				if (spaces.Count == 0)
				return; // No room for more garbage
				Vector2i pos = spaces[Random.Range(0, spaces.Count)];
				addGarbage(pos, val);
				});
	}
	public void intensifyGarbage()
	{
		foreach (GarbageTile t in garbage)
		{
			if (t && t.val > -8192)
				t.val *= 2;
		}
	}
	public int neutralizeGarbage()
	{
		int garbageNeutralized = 0;
		foreach (GarbageTile t in garbage)
		{
			if (t)
			{
				t.val /= 2;
				if (t.val == -1)
				{
					setGarbageAt(t.pos, null);
					Destroy(t.gameObject);
					garbageNeutralized++;
				}
			}
		}
		return garbageNeutralized;
	}
	public void sendGarbage(int tiles, int intensifies)
	{
		for (int i = 0; i < intensifies; i++)
			intensifyGarbage();
		for (int i = 0; i < tiles; i++)
			addGarbage(-2);
	}
	static Vector3i v3i(int x, int y, int z) {
		return new Vector3i(x, y, z);
	}
	static readonly IList<Vector3i>  explosionPattern = new ReadOnlyCollection<Vector3i>
	( new []{
		v3i(-1, -1, 32), v3i( 0, -1, 16), v3i( 1, -1, 32),
		v3i(-1,  0, 16), v3i( 0,  0,  8), v3i( 1,  0, 16),
		v3i(-1,  1, 32), v3i( 0,  1, 16), v3i( 1,  1, 32),
	});
	/*
	public void resolveExplosions() {
		float[,] garbageValues = new float[w, h];
		for (int x = 0; x < w; x++) {
			for (int y = 0; y < h; y++) {
				Tile t = at(x, y);
				if (!t) continue;
				if (!t.mergedThisMove) continue;
				foreach (Vector3i v in explosionPattern) {
					int px = v.x + x;
					int py = v.y + y;
					if (px < 0 || px >= w || py < 0 || py >= h) continue;
					garbageValues[px, py] += t.val / v.z;
				}
			}
		}
		for (int x = 0; x < w; x++) {
			for (int y = 0; y < h; y++) {
				if (garbageAt(x, y))
					garbageValues[x, y] += garbageAt(x, y).val;
				if (opponent.garbageAt(x, y))
					garbageValues[x, y] -= opponent.garbageAt(x, y).val;
				//TODO @Optimization Find faster algorithm
				float v = garbageValues[x, y];
				if (v >= 0) {
					if (garbageAt(x, y)) Destroy(garbageAt(x, y).gameObject);
					setGarbageAt(x, y, null);
					if (v >= 2) {
						int g;
						for (g = 2; g * 2 <= v; g *= 2) { if (g == 8192) break; }
						if (opponent.garbageAt(x, y)) opponent.garbageAt(x, y).val = -g;
						else opponent.addGarbage(x, y, -g);
					}
				}
				if (v < 2) {
					if (opponent.garbageAt(x, y)) Destroy(opponent.garbageAt(x, y).gameObject);
					opponent.setGarbageAt(x, y, null);
					if (v < 0) {
						int g;
						for (g = -2; g > v; g *= 2) { if (g == -8192) break; }
						if (garbageAt(x, y)) garbageAt(x, y).val = g;
						else addGarbage(x, y, g);
					}
				}
			}
		}
	}
	*/
	public void resolveExplosions(List<Vector3i> matches) {
		float[,] garbageValues = new float[w, h];
		foreach (Vector3i m in matches) {
			//Debug.Log("Explosion at (" + m.x + ", " + m.y + ") with power " + m.z);
			foreach (Vector3i v in explosionPattern) {
				int px = v.x + m.x;
				int py = v.y + m.y;
				if (px < 0 || px >= w || py < 0 || py >= h) continue;
				garbageValues[px, py] += (float)m.z / v.z;
			}
		}
		for (int x = 0; x < w; x++) {
			for (int y = 0; y < h; y++) {
				if (garbageAt(x, y))
					garbageValues[x, y] += garbageAt(x, y).val;
				if (opponent.garbageAt(x, y))
					garbageValues[x, y] -= opponent.garbageAt(x, y).val;
				//TODO @Optimization Find faster algorithm
				float v = garbageValues[x, y];
				if (v >= 0) {
					if (garbageAt(x, y)) Destroy(garbageAt(x, y).gameObject);
					setGarbageAt(x, y, null);
					if (v >= 2) {
						int g;
						for (g = 2; g * 2 <= v; g *= 2) { if (g == 8192) break; }
						if (opponent.garbageAt(x, y)) opponent.garbageAt(x, y).val = -g;
						else opponent.addGarbage(x, y, -g);
					}
				}
				if (v < 2) {
					if (opponent.garbageAt(x, y)) Destroy(opponent.garbageAt(x, y).gameObject);
					opponent.setGarbageAt(x, y, null);
					if (v < 0) {
						int g;
						for (g = -2; g > v; g *= 2) { if (g == -8192) break; }
						if (garbageAt(x, y)) garbageAt(x, y).val = g;
						else addGarbage(x, y, g);
					}
				}
			}
		}
	}

	public void dropGarbage()
	{
		foreach (GarbageTile t in garbage)
		{
			if (t)
				t.drop();
		}
	}
	public void makeMove(Vector2i direction)
	{
		if (lagTimer > 0)
		{
			moveBuffer = direction;
			return;
		}
		moveBuffer = Vector2i.one * -1;
		if (!allowNullMove && direction == Vector2i.zero)
		{
			/*
			if (checkGarbageLoss())
			{
				hasLost = true;
				winLossCode = WinLossCode.GARBAGEFULL;
			}
			*/
			breakCombo();
			if (checkLoss())
			{
				hasLost = true;
				winLossCode = WinLossCode.OUTOFMOVES;
			}
			return;
		}

		//Debug.Log("Making move: " + direction);
		List<Tile> traversal = new List<Tile>();
		bool tileMoved = false;
		if (direction.x == 1)
		{
			for (int x = w-1; x >= 0; x--)
				for (int y = 0; y < h; y++)
					traversal.Add(at(x,y));
		}
		else if (direction.x == -1)
		{
			for (int x = 0; x < w; x++)
				for (int y = 0; y < h; y++)
					traversal.Add(at(x,y));
		}
		else if (direction.y == 1)
		{
			for (int y = h-1; y >= 0; y--)
				for (int x = 0; x < w; x++)
					traversal.Add(at(x,y));
		}
		else if (direction.y == -1)
		{
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
					traversal.Add(at(x,y));
		}
		else // Null move
			tileMoved = true; // Behave as if a move was made
		bool comboBroken = true;
		//int garbageToSend = 0;
		int timesToIntensify = 0;
		List<Vector3i> matchesThisMove = new List<Vector3i>();
		foreach (Tile t in traversal)
		{
			if (!t) continue;
			tileMoved = t.move(direction) || tileMoved;
			if (!t && combo > 0)
				comboBroken = false;
			else if (t.mergedThisMove)
			{
				combo++;
				comboBroken = false;
				matchesThisMove.Add(new Vector3i(t.pos.x, t.pos.y, t.val));
				if (combo % 4 == 0)
					timesToIntensify++;
				// Debug.Log("Combo: " + combo + "; Speedup: " + comboSpeedup() + "; Speedup Max Mult: " + comboSpeedupMaxMultiplier);
				/* old garbage code
				timesToIntensify += Mathf.FloorToInt(Mathf.Log(t.val, 2));
				if (combo >= 3)
					garbageToSend += Mathf.FloorToInt(Mathf.Log(combo - 1, 2));
				*/
				/*
				comboBroken = false;
				combo = true;
				matchesMade++;
				garbageToSend += t.val / 16;
				*/
				if (t.val == 512)
				{
					hasWon = true;
					winLossCode = WinLossCode.MADE256;
				}
			}
		}
		//timesToIntensify /= 4;
		if (enableGarbage && !comboBroken)
		{
			explosionsInCombo.AddRange(matchesThisMove);
			for (int i = 0; i < timesToIntensify; i++) {
				matchesThisMove.AddRange(explosionsInCombo);
				explosionsInCombo.AddRange(explosionsInCombo);
			}
			resolveExplosions(matchesThisMove);

			/*
			if (hasGarbage())
			{
				for (int i = 0; i < garbageToSend + timesToIntensify; i++)
					neutralizeGarbage();
			}
			else
			{
				opponent.sendGarbage(garbageToSend, timesToIntensify);
			}
			*/
			/*
			if (hasGarbage())
			{
			for (int i = 0; i < matchesMade; i++)
			neutralizeGarbage();
			}
			else
			{
			opponent.sendGarbage(garbageToSend, matchesMade);
			}
			*/
		}

		if (!tileMoved) return;
		comboTimer = 0;
		lagTimer = lagLength;
		if (comboBroken)
		{
			/*
			if (checkGarbageLoss())
			{
			hasLost = true;
			winLossCode = WinLossCode.GARBAGEFULL;
			}
			*/
			breakCombo();
		}
		if (addTileAfterMove)
			addTile();
		if (combo == 0)
		{
			if (checkLoss())
			{
				hasLost = true;
				winLossCode = WinLossCode.OUTOFMOVES;
			}
		}
	}
	public void startGame()
	{
		if (contents == null)
		{
			contents = new Tile[w,h];
		}
		if (enableGarbage && garbage == null)
		{
			garbage = new GarbageTile[w,h];
		}
		if (explosionsInCombo == null) {
			explosionsInCombo = new List<Vector3i>();
		}
		for (int x = 0; x < w; x++)
		{
			for (int y = 0; y < h; y++)
			{
				if (at(x,y))
				{
					Destroy(at(x,y).gameObject);
					setAt(x,y,null);
				}
				if (enableGarbage && garbageAt(x,y))
				{
					Destroy(garbageAt(x,y).gameObject);
					setGarbageAt(x,y,null);
				}
			}
		}

		for (int i = 0; i < startTiles; i++)
			addTile();
		combo = 0;
		explosionsInCombo.Clear();
		tilesReceived = 0;
		hasWon = false;
		hasLost = false;
		winLossCode = WinLossCode.NONE;
		comboTimer = 0;
		lagTimer = 0;
		tileTimer = 0;
	}
	void debugPrintGrid()
	{
		string grid = "";
		for (int y = h-1; y >= 0; y--)
		{
			for (int x = 0; x < w; x++)
			{
				if (at(x,y))
					grid += at(x,y).val.ToString().PadLeft(4, ' ') + " ";
				else
					grid += "  ";
			}
			grid += "\n";
		}
		Debug.Log(grid);
	}
	void debugPrintOpenSpaces()
	{
		List<Vector2i> spaces = getOpenSpaces();
		string output = "";
		foreach (Vector2i s in spaces)
		{
			output += "(" + s.x + "," + s.y + ") ";
		}
		Debug.Log(output);
	}
	// Use this for initialization
	void Start () {
	}

	// Update is called once per frame
	void Update () {
		if (lagTimer > 0)
		{
			lagTimer -= Time.deltaTime;
			if (lagTimer <= 0 && moveBuffer != Vector2i.one * -1)
			{
				makeMove(moveBuffer);
			}
		}
		float increment = Time.deltaTime;
		if (doComboSpeedup)
		{
			increment *= comboSpeedup();
		}
		if (comboTimerLength != 0)
		{
			comboTimer += increment;
			if (comboTimer > comboTimerLength)
				makeMove(Vector2i.zero);
			if (combo > 0)
				comboMeter.value = 1.0f - (comboTimer / comboTimerLength);
			else
				comboMeter.value = 0;
		}
		if (tileTimerLength != 0)
		{
			tileTimer += increment;
			if (tileTimer > tileTimerLength)
			{
				addTile();
				tilesReceived++;
				//Debug.Log("Tiles Received for " + this.name + ": " + tilesReceived);
				tileTimer -= tileTimerLength;
			}
			tileMeter.value = tileTimer / tileTimerLength;
		}
		if (comboText)
		{
			if (combo > 0)
				comboText.text = combo.ToString();
			else
				comboText.text = "";
		}
		/*
		if (Input.anyKeyDown)
		{
		debugPrintGrid();
		}
		*/
	}
}
