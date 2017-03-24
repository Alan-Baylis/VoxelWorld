using System;
using UnityEngine;
using System.Collections;

using Tex = UnityEngine.Texture2D;
using VoxelWorld.Worldgen;

public class Manager : MonoBehaviour {

    World W;
    bool xr = false;
    public string Seed = "";
    public int SmoothingIterations = 0;
    public int FillPercentage = 0;
    public UnityEngine.Vector2 IslandMaxSize;
    public UnityEngine.Vector2 WorldSize;
    public Tex Atlas;
    public int HeightSegments;
    public int IslandSeaPercentage;

    void Start() 
    {
        int ThreadCount = Environment.ProcessorCount;
        ThreadCount *= 4;
        W = new World(new WorldProperties(WorldSize, new VoxelWorld.Worldgen.Vector3(0,0,0), Seed, IslandMaxSize, Atlas,HeightSegments, IslandSeaPercentage), new CellularAutomataProperties(SmoothingIterations, FillPercentage));
        W.GenerateWorld();
        xr = true;
    }

    void OnGUI()
    {
        if(GUI.Button(new Rect(25,25,100,100), "Generate new world"))
        {
            if (GameObject.Find("Islands") && GameObject.Find("SeaPlane"))
            {
                Destroy(GameObject.Find("Islands"));
                Destroy(GameObject.Find("SeaPlane"));
                Destroy(GameObject.Find("Main Camera"));
            }
            Start();
        }
    }

    void OnDrawGizmos()
    {
        if(xr)
        {
            
            foreach(Island S in W.Islands)
            {
                if (S.IslandSize == IslandSize.Enormous)
                    Gizmos.color = UnityEngine.Color.cyan;
                if(S.IslandSize == IslandSize.Huge)
                    Gizmos.color = UnityEngine.Color.red;
                if (S.IslandSize == IslandSize.Large)
                    Gizmos.color = UnityEngine.Color.magenta;
                if (S.IslandSize == IslandSize.Medium)
                    Gizmos.color = UnityEngine.Color.white;
                if (S.IslandSize == IslandSize.Small)
                    Gizmos.color = UnityEngine.Color.yellow;
                if (S.IslandSize == IslandSize.Tiny)
                    Gizmos.color = UnityEngine.Color.green;
                if (S.IslandSize == IslandSize.Null)
                    Gizmos.color = UnityEngine.Color.black;

                Gizmos.DrawCube(new UnityEngine.Vector3(S.LeftBottomCoordinate.x + 0.5f* S.Size.x,10, S.LeftBottomCoordinate.y + 0.5f* S.Size.y), new UnityEngine.Vector3(0.1f*S.Size.x, 10, 0.1f*S.Size.y));
            }
                     
        }

    }   
     
}