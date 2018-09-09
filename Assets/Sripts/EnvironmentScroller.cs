using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentScroller : MonoBehaviour
{
    public GameObject IslandPrefab;
    public Transform TileFront;
    public Transform TileBack;
    public Transform PlayerTransform;
    public float TerrainLength = 240.0f;
    public float TerrainWidth = 50.0f;

    private List<GameObject> IslandsFront = new List<GameObject>();
    private List<GameObject> IslandsBack = new List<GameObject>();

    private Vector3 TerrainFrontStartPosition;
    private Vector3 TerrainBackStartPosition;
    private Vector3 PlayerStartPosition;
    private Quaternion PlayerStartRotation;

	// Use this for initialization
	void Start ()
    {
		TerrainFrontStartPosition = TileFront.position;
        TerrainBackStartPosition = TileBack.position;
        PlayerStartPosition = PlayerTransform.position;
        PlayerStartRotation = PlayerTransform.rotation;
        SpawnIslands(TileFront, ref IslandsFront);
        SpawnIslands(TileBack, ref IslandsBack);
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
		UpdatePlayerGroundPosition();
	}

    void UpdatePlayerGroundPosition()
    {
        float PlayerZ = PlayerTransform.position.z;
        if (PlayerZ > TileFront.position.z - TerrainLength * 0.2f && PlayerZ < TileFront.position.z + TerrainLength * 0.5f)
        {
            Debug.Log(PlayerZ + " | " + TileFront.position.z);
            Vector3 currentBackPos = TileBack.transform.position;
            currentBackPos.Set(currentBackPos.x, currentBackPos.y, currentBackPos.z + TerrainLength * 2.0f);
            TileBack.transform.position = currentBackPos;
            Transform oldBack = TileBack;
            List<GameObject> oldIslandsBack = IslandsBack;
            TileBack = TileFront;
            TileFront = oldBack;
            IslandsBack = IslandsFront;
            IslandsFront = oldIslandsBack;
            DestroyIslands(ref IslandsFront);
            SpawnIslands(TileFront, ref IslandsFront);
        }
    }

    public void ResetGame()
    {
        TileFront.position = TerrainFrontStartPosition;
        TileBack.position = TerrainBackStartPosition;
        PlayerTransform.position = PlayerStartPosition;
        PlayerTransform.rotation = PlayerStartRotation;
    }

    void SpawnIslands(Transform Tile, ref List<GameObject> IslandsList)
    {
        float islandRowLength = TerrainLength / 10.0f;
        float zPosStart = Tile.position.z - TerrainLength * 0.5f;
        for (int i = 3; i < 10; i += 2)
        {
            float zPos = zPosStart + i * islandRowLength;
            float xPos = Random.Range(-TerrainWidth * 0.5f + islandRowLength, 
                                      TerrainWidth * 0.5f - islandRowLength);
            IslandsList.Add(GameObject.Instantiate(IslandPrefab, new Vector3(xPos, 0.0f, zPos), Quaternion.identity));
        }
    }

    void DestroyIslands(ref List<GameObject> IslandsList)
    {
        for (int i = 0; i < IslandsList.Count; ++i)
            Destroy(IslandsList[i].gameObject);
        IslandsList.Clear();
    }
}
