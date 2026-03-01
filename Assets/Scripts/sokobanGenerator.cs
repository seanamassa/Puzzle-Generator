using UnityEngine;
using System.Collections.Generic;

public class SokobanGenerator : MonoBehaviour 
{
    public enum TileType { Wall, Floor, Switch, Crate, Player }

    [Header("Grid Settings")]
    public int width = 24;
    public int height = 24;

    [Header("Puzzle Parameters")]
    [Range(1, 5)] public int switchCount = 3; 
    [Range(0, 5)] public int decoyCrates = 2; // The Red Herrings
    [Range(10, 100)] public int complexitySteps = 45;
    [Range(0f, 1f)] public float straightMomentum = 0.6f;
    public bool useRandomSeed = true;
    public int seed = 12345;

    private TileType[,] grid;
    private string difficultyTag = "Easy";

    void Start() { Generate(); CenterCamera(); }

    void OnGUI() {
        if (GUI.Button(new Rect(10, 10, 150, 50), "Regenerate Puzzle")) { Generate(); CenterCamera(); }
        GUI.Box(new Rect(10, 70, 150, 30), "Difficulty: " + difficultyTag);
        GUI.Label(new Rect(10, 110, 200, 20), $"Switches: {switchCount} | Decoys: {decoyCrates}");
    }

    [ContextMenu("Generate New Puzzle")]
    public void Generate() 
    {
        if (useRandomSeed) seed = Random.Range(0, 99999);
        Random.InitState(seed);

        grid = new TileType[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = TileType.Wall;

        List<Vector2Int> switches = new List<Vector2Int>();
        List<Vector2Int> requiredCrates = new List<Vector2Int>();

        // Spaced-Out Goal Generation based on Switch Count
        for (int i = 0; i < switchCount; i++) {
            Vector2Int pos = Vector2Int.zero;
            int attempts = 0;
            bool valid = false;
            
            while (!valid && attempts < 200) {
                pos = new Vector2Int(Random.Range(5, width - 5), Random.Range(5, height - 5));
                valid = true;
                foreach (var s in switches) {
                    if (Vector2Int.Distance(s, pos) < 4f) valid = false;
                }
                attempts++;
            }

            switches.Add(pos);
            requiredCrates.Add(pos);
            SetFloor(pos);
        }

        Vector2Int currentPlayer = requiredCrates[0] + Vector2Int.right;
        SetFloor(currentPlayer);

        int turns = 0;
        Vector2Int lastDir = Vector2Int.zero;

        // Ensuring a valid solution path while maximizing distance and turns
        for (int i = 0; i < complexitySteps; i++) 
        {
            int cIndex = Random.Range(0, requiredCrates.Count);
            Vector2Int currentCrate = requiredCrates[cIndex];
            
            Vector2Int pullDir = GetRandomDirection();
            if (Random.value < straightMomentum && lastDir != Vector2Int.zero) pullDir = lastDir;
            if (pullDir == lastDir * -1 && lastDir != Vector2Int.zero) continue; 
            
            Vector2Int newCratePos = currentCrate + pullDir;
            Vector2Int newPlayerPos = currentCrate + (pullDir * 2);

            if (IsInside(newCratePos, 2) && IsInside(newPlayerPos, 2) && 
                !requiredCrates.Contains(newCratePos) && !requiredCrates.Contains(newPlayerPos)) 
            {
                CarvePlayerPath(currentPlayer, newPlayerPos, requiredCrates);

                SetFloor(newCratePos);
                SetFloor(newPlayerPos);

                if (pullDir != lastDir) turns++;
                lastDir = pullDir;

                requiredCrates[cIndex] = newCratePos;
                currentPlayer = newPlayerPos;
            }
        }

        // Inject Misdirection Architecture
        AddDeadEnds();
        InjectPillars();

        // Place decoy crates before final placement so we don't accidentally overwrite switches
        AddDecoyCrates(requiredCrates, switches, currentPlayer);

        // Final Placement of Required Elements
        foreach (var s in switches) grid[s.x, s.y] = TileType.Switch;
        foreach (var c in requiredCrates) grid[c.x, c.y] = TileType.Crate;
        grid[currentPlayer.x, currentPlayer.y] = TileType.Player;

        float avgDistance = 0;
        for(int i=0; i<requiredCrates.Count; i++) avgDistance += Vector2Int.Distance(requiredCrates[i], switches[i]);
        avgDistance /= requiredCrates.Count;

        int score = turns + (int)(avgDistance * 5) + (switchCount * 10) + (decoyCrates * 15);
        difficultyTag = score > 100 ? "Hard" : (score > 55 ? "Medium" : "Easy");
    }

    void AddDecoyCrates(List<Vector2Int> required, List<Vector2Int> switches, Vector2Int player)
    {
        int placed = 0;
        int attempts = 0;
        while (placed < decoyCrates && attempts < 500)
        {
            Vector2Int pos = new Vector2Int(Random.Range(2, width - 2), Random.Range(2, height - 2));
            
            // Only place decoys on empty floors, avoiding critical items
            if (grid[pos.x, pos.y] == TileType.Floor && 
                !required.Contains(pos) && !switches.Contains(pos) && pos != player)
            {
                // Safety Check: Prevents the decoy from spawning in a 1-tile hallway and hard-locking the game.
                if (CountFloorNeighbors(pos) >= 3)
                {
                    grid[pos.x, pos.y] = TileType.Crate; // Rendered identical to required crates
                    placed++;
                }
            }
            attempts++;
        }
    }

    // Counts how many adjacent tiles are Floor or Switch, used to prevent placing decoys in tight spots
    int CountFloorNeighbors(Vector2Int pos)
    {
        int count = 0;
        if (grid[pos.x + 1, pos.y] == TileType.Floor || grid[pos.x + 1, pos.y] == TileType.Switch) count++;
        if (grid[pos.x - 1, pos.y] == TileType.Floor || grid[pos.x - 1, pos.y] == TileType.Switch) count++;
        if (grid[pos.x, pos.y + 1] == TileType.Floor || grid[pos.x, pos.y + 1] == TileType.Switch) count++;
        if (grid[pos.x, pos.y - 1] == TileType.Floor || grid[pos.x, pos.y - 1] == TileType.Switch) count++;
        return count;
    }

    // Carves a path for the player from start to end, avoiding critical crate positions
    void CarvePlayerPath(Vector2Int start, Vector2Int end, List<Vector2Int> avoidCrates) 
    {
        Vector2Int curr = start;
        int escapes = 0;
        
        while (curr != end && escapes < 50) 
        {
            SetFloor(curr);
            if (curr == end) break;
            
            Vector2Int diff = end - curr;
            List<Vector2Int> possibleMoves = new List<Vector2Int>();
            
            if (diff.x != 0) possibleMoves.Add(new Vector2Int(Mathf.Clamp(diff.x, -1, 1), 0));
            if (diff.y != 0) possibleMoves.Add(new Vector2Int(0, Mathf.Clamp(diff.y, -1, 1)));
            if (possibleMoves.Count == 0) break;

            Vector2Int step = possibleMoves[Random.Range(0, possibleMoves.Count)];
            Vector2Int next = curr + step;
            
            if (avoidCrates.Contains(next)) {
                Vector2Int ortho1 = new Vector2Int(step.y, step.x); 
                Vector2Int ortho2 = new Vector2Int(-step.y, -step.x);
                if (IsInside(curr + ortho1, 1) && !avoidCrates.Contains(curr + ortho1)) next = curr + ortho1;
                else if (IsInside(curr + ortho2, 1) && !avoidCrates.Contains(curr + ortho2)) next = curr + ortho2;
                else break; 
            }

            if (IsInside(next, 1)) curr = next;
            else break;
            escapes++;
        }
        SetFloor(end);
    }

    // Adds random dead-end corridors to increase the visual complexity and mislead players
    void AddDeadEnds() 
    {
        for (int i = 0; i < 20; i++) {
            Vector2Int pos = new Vector2Int(Random.Range(2, width - 2), Random.Range(2, height - 2));
            if (grid[pos.x, pos.y] == TileType.Floor) {
                Vector2Int dir = GetRandomDirection();
                int length = Random.Range(2, 6);
                for (int l = 0; l < length; l++) {
                    pos += dir;
                    if (IsInside(pos, 1)) SetFloor(pos);
                }
            }
        }
    }

    // Injects "pillars" of walls in 3x3 floor areas to create visual noise and false paths, without blocking the solution
    void InjectPillars()
    {
        for (int x = 2; x < width - 2; x++) {
            for (int y = 2; y < height - 2; y++) {
                if (IsAllFloor3x3(x, y)) grid[x, y] = TileType.Wall;
            }
        }
    }

    // Checks if a 3x3 area centered on (cx, cy) is all Floor or Switch, ensuring we don't block critical paths when placing pillars
    bool IsAllFloor3x3(int cx, int cy) {
        for(int x = cx - 1; x <= cx + 1; x++) {
            for(int y = cy - 1; y <= cy + 1; y++) {
                if (grid[x,y] != TileType.Floor && grid[x,y] != TileType.Switch) return false;
            }
        }
        return true;
    }

    // Sets a tile to Floor if it's not already a Switch, ensuring we don't overwrite critical goal positions
    void SetFloor(Vector2Int p) {
        if (grid[p.x, p.y] != TileType.Switch) grid[p.x, p.y] = TileType.Floor;
    }

    bool IsInside(Vector2Int p, int buffer) => p.x >= buffer && p.x < width - buffer && p.y >= buffer && p.y < height - buffer;
    Vector2Int GetRandomDirection() => new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }[Random.Range(0, 4)];

    void CenterCamera() {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.transform.position = new Vector3((width - 1) / 2f, (height - 1) / 2f, -10f);
        cam.orthographicSize = (height / 2f) + 1f;
    }

    private void OnDrawGizmos() 
    {
        if (grid == null) return;
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector3 pos = new Vector3(x, y, 0);
                TileType type = grid[x, y];

                if (type == TileType.Player) {
                    Gizmos.color = GetColor(TileType.Floor);
                    Gizmos.DrawCube(pos, Vector3.one);
                    Gizmos.color = new Color(0, 0, 0, 0.4f); 
                    Gizmos.DrawWireCube(pos, Vector3.one);
                    Gizmos.color = GetColor(TileType.Player);
                    Gizmos.DrawSphere(pos, 0.4f); 
                } else {
                    Gizmos.color = GetColor(type);
                    Gizmos.DrawCube(pos, Vector3.one);
                    Gizmos.color = new Color(0, 0, 0, 0.4f); 
                    Gizmos.DrawWireCube(pos, Vector3.one);
                }
            }
        }
    }

    Color GetColor(TileType t) 
    {
        switch (t) {
            case TileType.Wall: return new Color(0.2f, 0.2f, 0.2f);
            case TileType.Switch: return Color.green;
            case TileType.Crate: return Color.yellow;
            case TileType.Player: return Color.blue;
            default: return Color.white;
        }
    }
}