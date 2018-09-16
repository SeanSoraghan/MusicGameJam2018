using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentScroller : MonoBehaviour
{
    public GameObject IslandPrefab;
    public Transform TileFront;
    public Transform TileBack;
    public GameObject WaterGridFront;
    public GameObject WaterGridBack;
    private MassSpawner WaterSpawnerFront;
    private MassSpawner WaterSpawnerBack;
    private MassSpringSystem WaterSystemFront;
    private MassSpringSystem WaterSystemBack;
    public PlayerController Player;
    private WaterForceController PlayerForceController;
    public float TerrainLength = 240.0f;
    public float TerrainWidth = 50.0f;

    private List<GameObject> IslandsFront = new List<GameObject>();
    private List<GameObject> IslandsBack = new List<GameObject>();

    private Vector3 TerrainFrontStartPosition;
    private Vector3 TerrainBackStartPosition;
    private Vector3 PlayerStartPosition;
    private Quaternion PlayerStartRotation;

    private bool TerrainSwapRequired = false;
    private float TerrainSwapSyncLimit = 0.4f;
	// Use this for initialization
	void Start ()
    {
		TerrainFrontStartPosition = TileFront.position;
        TerrainBackStartPosition = TileBack.position;
        PlayerStartPosition = Player.transform.position;
        PlayerStartRotation = Player.transform.rotation;
        SpawnIslands(TileFront, ref IslandsFront);
        SpawnIslands(TileBack, ref IslandsBack);
        if (WaterGridBack != null)
        {
            WaterSpawnerBack = WaterGridBack.GetComponent<MassSpawner>();
            WaterSystemBack = WaterGridBack.GetComponent<MassSpringSystem>();
        }
        if (WaterGridFront != null)
        {
            WaterSpawnerFront = WaterGridFront.GetComponent<MassSpawner>();
            WaterSystemFront = WaterGridFront.GetComponent<MassSpringSystem>();
        }
        PlayerForceController = Player.GetComponent<WaterForceController>();
	}
	
	// Update is called once per frame
	void FixedUpdate ()
    {
		UpdatePlayerGroundPosition();
	}

    void UpdatePlayerGroundPosition()
    {
        float PlayerZ = Player.transform.position.z;
        if (PlayerForceController != null && 
            PlayerZ > TileBack.position.z + TerrainLength * 0.4f && PlayerZ < TileFront.position.z - TerrainLength * 0.4f &&
            PlayerForceController.WaterGrid != WaterSystemFront)
        {
            PlayerForceController.WaterGrid = WaterSystemFront;
        }
        if (PlayerZ > TileFront.position.z - TerrainLength * 0.1f)
        {
            if (PlayerZ < TileFront.position.z + TerrainLength * TerrainSwapSyncLimit)
            {
                if (!TerrainSwapRequired)
                { 
                    TerrainSwapRequired = true;
                }
            }
            else
            {
                // We're too close to the edge of the terrain to wait for a beat to sync to.
                SwapTerrain();
            }
            
        }
    }

    public void SwapTerrain()
    {
        Vector3 currentBackPos = TileBack.transform.position;
        currentBackPos.Set(currentBackPos.x, currentBackPos.y, currentBackPos.z + TerrainLength * 2.0f);
        TileBack.transform.position = currentBackPos;

        Transform oldBack = TileBack;
        TileBack = TileFront;
        TileFront = oldBack;

        List<GameObject> oldIslandsBack = IslandsBack;
        IslandsBack = IslandsFront;
        IslandsFront = oldIslandsBack;
        DestroyIslands(ref IslandsFront);
        SpawnIslands(TileFront, ref IslandsFront);

        Player.SwapWaterPlanes();
        TerrainSwapRequired = false;
    }

    public void ResetGame()
    {
        TileFront.position = TerrainFrontStartPosition;
        TileBack.position = TerrainBackStartPosition;
        Player.transform.position = PlayerStartPosition;
        Player.transform.rotation = PlayerStartRotation;
    }

    void SpawnIslands(Transform Tile, ref List<GameObject> IslandsList)
    {
        //float islandRowLength = TerrainLength / 10.0f;
        //float zPosStart = Tile.position.z - TerrainLength * 0.5f;
        //for (int i = 3; i < 10; i += 2)
        //{
        //    float zPos = zPosStart + i * islandRowLength;
        //    float xPos = Random.Range (-TerrainWidth * 0.5f + islandRowLength,
        //                              TerrainWidth * 0.5f - islandRowLength);
        //    IslandsList.Add (GameObject.Instantiate (IslandPrefab, new Vector3 (xPos, 10.0f, zPos), Quaternion.identity));
        //}
    }

    void DestroyIslands(ref List<GameObject> IslandsList)
    {
        for (int i = 0; i < IslandsList.Count; ++i)
            Destroy(IslandsList[i].gameObject);
        IslandsList.Clear();
    }

    public void OnBeat()
    {
        if (TerrainSwapRequired)
            SwapTerrain();
    }
}
