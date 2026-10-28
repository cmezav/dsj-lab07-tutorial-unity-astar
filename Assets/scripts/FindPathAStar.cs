using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PathMarker
{
    public MapLocation location;
    public float G, H, F;
    public GameObject marker;
    public PathMarker parent;

    public PathMarker(MapLocation location, float g, float h, float f,
        GameObject marker, PathMarker parent)
    {
        this.location = location;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        this.parent = parent;
    }

    public override bool Equals(object obj)
    {
        return obj is PathMarker other && location.Equals(other.location);
    }

    public override int GetHashCode()
    {
        return location.x * 397 ^ location.z;
    }
}

public class FindPathAStar : MonoBehaviour
{
    public Maze maze;
    public Material closedMaterial;
    public Material openMaterial;
    public GameObject start;
    public GameObject end;
    public GameObject pathP;

    private readonly List<PathMarker> open = new List<PathMarker>();
    private readonly List<PathMarker> closed = new List<PathMarker>();
    private PathMarker goalNode;
    private PathMarker startNode;
    private PathMarker lastPos;
    private bool done;

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.pKey.wasPressedThisFrame)
            BeginSearch();
        if (Keyboard.current.cKey.wasPressedThisFrame && !done && lastPos != null)
            Search(lastPos);
        if (Keyboard.current.mKey.wasPressedThisFrame)
            GetPath();
    }

    private void RemoveAllMarkers()
    {
        foreach (GameObject marker in GameObject.FindGameObjectsWithTag("marker"))
            Destroy(marker);
    }

    private void BeginSearch()
    {
        if (maze == null || maze.map == null || start == null || end == null || pathP == null)
        {
            Debug.LogWarning("Asigna Maze, Start, End y Path P y ejecuta el laberinto antes de pulsar P.");
            return;
        }

        done = false;
        RemoveAllMarkers();
        open.Clear();
        closed.Clear();

        var locations = new List<MapLocation>();
        for (int z = 1; z < maze.depth - 1; z++)
            for (int x = 1; x < maze.width - 1; x++)
                if (maze.map[x, z] != 1)
                    locations.Add(new MapLocation(x, z));

        if (locations.Count < 2)
        {
            Debug.LogWarning("El laberinto necesita al menos dos pasillos libres.");
            return;
        }
        locations.Shuffle();

        MapLocation startLocation = locations[0];
        MapLocation goalLocation = locations[1];
        Vector3 startWorld = new Vector3(startLocation.x * maze.scale, 0f, startLocation.z * maze.scale);
        Vector3 goalWorld = new Vector3(goalLocation.x * maze.scale, 0f, goalLocation.z * maze.scale);
        startNode = new PathMarker(startLocation, 0f, 0f, 0f,
            Instantiate(start, startWorld, Quaternion.identity), null);
        goalNode = new PathMarker(goalLocation, 0f, 0f, 0f,
            Instantiate(end, goalWorld, Quaternion.identity), null);
        open.Add(startNode);
        lastPos = startNode;
    }

    // Cada pulsación de C expande un nodo y escoge el siguiente.
    private void Search(PathMarker current)
    {
        if (current == null || goalNode == null) return;
        if (current.Equals(goalNode))
        {
            done = true;
            Debug.Log("A*: se encontró la meta.");
            return;
        }

        foreach (MapLocation direction in maze.directions)
        {
            MapLocation neighbour = current.location + direction;
            // Primero límites; consultar maze.map antes podría lanzar IndexOutOfRangeException.
            if (neighbour.x < 1 || neighbour.x >= maze.width - 1 ||
                neighbour.z < 1 || neighbour.z >= maze.depth - 1)
                continue;
            if (maze.map[neighbour.x, neighbour.z] == 1 || IsClosed(neighbour))
                continue;

            float g = current.G + Vector2.Distance(current.location.ToVector(), neighbour.ToVector());
            float h = Vector2.Distance(neighbour.ToVector(), goalNode.location.ToVector());
            float f = g + h;

            PathMarker existing = FindOpen(neighbour);
            if (existing != null)
            {
                if (g < existing.G)
                {
                    existing.G = g;
                    existing.H = h;
                    existing.F = f;
                    existing.parent = current;
                    SetCosts(existing.marker, g, h, f);
                }
                continue;
            }

            Vector3 world = new Vector3(neighbour.x * maze.scale, 0f, neighbour.z * maze.scale);
            GameObject pathBlock = Instantiate(pathP, world, Quaternion.identity);
            pathBlock.tag = "marker";
            if (openMaterial != null && pathBlock.TryGetComponent<Renderer>(out var renderer))
                renderer.material = openMaterial;
            SetCosts(pathBlock, g, h, f);
            open.Add(new PathMarker(neighbour, g, h, f, pathBlock, current));
        }

        // El primer empate en F se resuelve con menor H: más cerca de la meta.
        open.Sort((a, b) => {
            int byF = a.F.CompareTo(b.F);
            return byF != 0 ? byF : a.H.CompareTo(b.H);
        });
        if (open.Count == 0)
        {
            done = true;
            Debug.LogWarning("A*: no hay una ruta a la meta.");
            return;
        }

        PathMarker next = open[0];
        open.RemoveAt(0);
        closed.Add(next);
        if (closedMaterial != null && next.marker != null &&
            next.marker.TryGetComponent<Renderer>(out var nextRenderer))
            nextRenderer.material = closedMaterial;
        lastPos = next;
    }

    // Sigue los padres desde la meta hasta el inicio y dibuja solo la ruta.
    private void GetPath()
    {
        if (!done || lastPos == null || goalNode == null ||
            !lastPos.Equals(goalNode) || startNode == null)
        {
            Debug.LogWarning("Primero pulsa P y luego C hasta encontrar la meta; después pulsa M.");
            return;
        }

        var route = new List<MapLocation>();
        PathMarker node = lastPos;
        while (node != null)
        {
            route.Add(node.location);
            if (node.Equals(startNode)) break;
            node = node.parent;
        }
        if (node == null)
        {
            Debug.LogWarning("No se pudo reconstruir la ruta hasta el inicio.");
            return;
        }

        RemoveAllMarkers();
        foreach (MapLocation location in route)
        {
            Vector3 world = new Vector3(location.x * maze.scale, 0f, location.z * maze.scale);
            GameObject block = Instantiate(pathP, world, Quaternion.identity);
            block.tag = "marker";
            if (openMaterial != null && block.TryGetComponent<Renderer>(out var renderer))
                renderer.material = openMaterial;
        }
        Debug.Log("A*: ruta final de " + route.Count + " nodos.");
    }

    private static void SetCosts(GameObject pathBlock, float g, float h, float f)
    {
        TextMesh[] values = pathBlock.GetComponentsInChildren<TextMesh>(true);
        if (values.Length < 3) return;
        values[0].text = "G: " + g.ToString("0.0");
        values[1].text = "H: " + h.ToString("0.0");
        values[2].text = "F: " + f.ToString("0.0");
    }

    private bool IsClosed(MapLocation location)
    {
        foreach (PathMarker marker in closed)
            if (marker.location.Equals(location)) return true;
        return false;
    }

    private PathMarker FindOpen(MapLocation location)
    {
        foreach (PathMarker marker in open)
            if (marker.location.Equals(location)) return marker;
        return null;
    }
}
