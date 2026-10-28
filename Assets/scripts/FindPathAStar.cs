using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

public class PathMarker {

    public MapLocation location;

    public float G;
    public float H;
    public float F;

    public GameObject marker;
    public GameObject parent;

    public PathMarker(MapLocation l, float g, float h, float f, GameObject marker, GameObject p) {

        location = l;
        G = g;
        H = h;
        F = f;
        this.marker = marker;
        parent = p;
    }

    public override bool Equals(object obj) {

        if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            return false;
        else
            return location.Equals(((PathMarker)obj).location);
    }

    public override int GetHashCode() {

        return 0;
    }
}

public class FindPathAStar : MonoBehaviour {

    public Maze maze;
    public Material closedMaterial;
    public Material openMaterial;

    List<PathMarker> open = new List<PathMarker>();
    List<PathMarker> closed = new List<PathMarker>();

    public GameObject start;
    public GameObject end;
    public GameObject pathP;

    PathMarker goalNode;
    PathMarker startNode;
    PathMarker lastPos;

    bool done = false;

    void RemoveAllMarkers() {

        GameObject[] markers = GameObject.FindGameObjectsWithTag("marker");

        foreach (GameObject m in markers) {

            Destroy(m);
        }
    }

    void BeginSearch() {

        done = false;
        RemoveAllMarkers();

        List<MapLocation> locations = new List<MapLocation>();

        for (int z = 1; z < maze.depth; ++z) {
            for (int x = 1; x < maze.width; ++x) {

                if (maze.map[x, z] != 1) {

                    locations.Add(new MapLocation(x, z));
                }
            }
        }

        locations.Shuffle();

        Vector3 startLocation = new Vector3(locations[0].x * maze.scale, 0.0f, locations[0].z * maze.scale);
        startNode = new PathMarker(new MapLocation(locations[0].x, locations[0].z), 0.0f, 0.0f, 0.0f,
            Instantiate(start, startLocation, Quaternion.identity), null);

        Vector3 goalLocation = new Vector3(locations[1].x * maze.scale, 0.0f, locations[1].z * maze.scale);
        goalNode = new PathMarker(new MapLocation(locations[1].x, locations[1].z), 0.0f, 0.0f, 0.0f,
            Instantiate(end, goalLocation, Quaternion.identity), null);
    }

    void Start() {

    }

    void Update() {

        if(Keyboard.current.pKey.wasPressedThisFrame) BeginSearch();
    }
}