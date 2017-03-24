using System;
using System.Threading;
using System.Collections.Generic;
using UVC3 = UnityEngine.Vector3;

namespace VoxelWorld.Worldgen
{
    public class World
    {
        public byte[,] TileTypeXZ;
        public WorldProperties WorldProps;
        public CellularAutomataProperties CellProps;
        public List<Island> Islands;

        public World(WorldProperties WorldProps, CellularAutomataProperties CAProps)
        {
            this.WorldProps = WorldProps;
            CellProps = CAProps;
        }

        public void GenerateWorld()
        {
            TileTypeXZ = new byte[WorldProps.Size.x, WorldProps.Size.y];
            GenerateIslandsAlternative();
            new Thread(CheckWorld).Start();
            CreateSea();
            CreateCamera();
        }
        void CheckWorld()
        {
            for (int x = 0; x < WorldProps.Size.x; x++)
            {
                for (int y = 0; y < WorldProps.Size.y; y++)
                {
                    if (TileTypeXZ[x, y] == (byte)TileType.NullByte)
                        TileTypeXZ[x, y] = (byte)TileType.Water;
                }
            }
        }

        #region OLDIslandGen
        /*
        public void GenerateIslands()
        {
            Islands = new List<Island>();
            if (IslandProps.IslandAmount % 2 == 0)
            {
                int IslandsXY = (int)Math.Round(Math.Sqrt(IslandProps.IslandAmount), 0, MidpointRounding.AwayFromZero);
                int IsleRegSizeX = WorldProps.Size.x / IslandsXY;
                int IsleRegSizeY = WorldProps.Size.x / IslandsXY;

                int IsleSizeX = IsleRegSizeX - 2 * IslandProps.IslandOuterRange;
                int IsleSizeY = IsleRegSizeY - 2 * IslandProps.IslandOuterRange;

                for (int x = 0; x < IslandsXY; x++ )
                {
                    for (int y = 0; y < IslandsXY; y++ )
                    {
                        int ShiftHorizontal = 0;
                        int ShiftVertical = 0;

                        if (x != 0 || y != 0 || x != (IslandsXY - 1) || y != (IslandsXY - 1))
                        {
                            ShiftHorizontal = WorldProps.RandomGen.Next(-IslandProps.IslandOuterRange, IslandProps.IslandOuterRange + 1);
                            ShiftVertical = WorldProps.RandomGen.Next(-IslandProps.IslandOuterRange, IslandProps.IslandOuterRange + 1);
                        }
                        else 
                        {
                            if (x == 0 || y == 0)
                            {
                                ShiftHorizontal = WorldProps.RandomGen.Next(0, IslandProps.IslandOuterRange + 1);
                                ShiftVertical = WorldProps.RandomGen.Next(0, IslandProps.IslandOuterRange + 1);
                            }
                            else 
                            {
                                ShiftHorizontal = WorldProps.RandomGen.Next(-IslandProps.IslandOuterRange, 1);
                                ShiftVertical = WorldProps.RandomGen.Next(-IslandProps.IslandOuterRange,1);
                            }
                        }

                        Vector2 LeftBottomCoordinate = new Vector2((x*IsleRegSizeX + IslandProps.IslandOuterRange)+ShiftHorizontal, (y*IsleRegSizeY + IslandProps.IslandOuterRange)+ShiftVertical);
                        Islands.Add(new Island(new Vector2(IsleSizeX, IsleSizeY), LeftBottomCoordinate, WorldProps, this));
                    }
                }

            }
            int ThreadCount = Islands.Count;

            Thread[] Threads = new Thread[ThreadCount];

            for (int T = 0; T < ThreadCount; T++)
            {
                int j = T;
                Random G = new Random(WorldProps.RandomGen.Next().ToString().GetHashCode());
                Threads[j] = new Thread(() =>
                {
                    Islands[j].CellularAutomata(G);
                });
            }
            for (int p = 0; p < ThreadCount; p++)
            {
                Threads[p].Start();
            }
            for (int p = 0; p < ThreadCount; p++)
            {
                Threads[p].Join();
            }
            for (int i = 0; i < Islands.Count; i++ )
            {
                Islands[i].CreateIsland();
            }
        }*/
        #endregion
        public void GenerateIslandsAlternative()
        {
            Islands = new List<Island>();

            int IslandsX = WorldProps.Size.x / (int)WorldProps.IslandMaxSize.x;
            int IslandsY = WorldProps.Size.y / (int)WorldProps.IslandMaxSize.y;

            if(IslandsX * WorldProps.IslandMaxSize.x > WorldProps.Size.x || IslandsY * WorldProps.IslandMaxSize.y > WorldProps.Size.y)
            {
                UnityEngine.Debug.Log("Error, invalid amount");
                return;
            }

            for (int x = 0; x < IslandsX; x++ )
            {
                for (int y = 0; y < IslandsY; y++ )
                {
                    IslandSize isSize;
                    //EilandBestaan bepalen
                    int j = WorldProps.RandomGen.Next(0,101);
                    if (j < WorldProps.WorldSeaPercentage)
                    {
                        //Island is non existent
                        Islands.Add(new Island(IslandSize.Null));
                    }
                    else
                    {
                        //Eilandgrootte bepalen
                        int i = WorldProps.RandomGen.Next(0, 101);
                        if (i >= 90)
                            isSize = IslandSize.Enormous;
                        else if (i >= 80)
                            isSize = IslandSize.Huge;
                        else if (i >= 60)
                            isSize = IslandSize.Large;
                        else if (i >= 45)
                            isSize = IslandSize.Medium;
                        else if (i >= 30)
                            isSize = IslandSize.Small;
                        else
                            isSize = IslandSize.Tiny;
                        Islands.Add(new Island(new Vector2((int)WorldProps.IslandMaxSize.x, (int)WorldProps.IslandMaxSize.y), new Vector2(x * (int)WorldProps.IslandMaxSize.x, y * (int)WorldProps.IslandMaxSize.y), this, isSize));
                    }
                }
            }
 
            int ThreadCount = Islands.Count;
            int WorkBlockSize = 8;
            int WorkGroups = ThreadCount / WorkBlockSize;
            
            for (int W = 0; W < WorkGroups; W++)
            {
                Thread[] Threads = new Thread[WorkBlockSize];

                for (int T = 0; T < WorkBlockSize; T++)
                {
                    int j = T;
                    Random G = new Random(WorldProps.RandomGen.Next().ToString().GetHashCode());
                    Threads[j] = new Thread(() =>
                    {
                        if (Islands[W * WorkBlockSize + j].IslandSize != IslandSize.Null)
                        {
                            Islands[W * WorkBlockSize + j].GenerateIslandShape(G);
                            Islands[W * WorkBlockSize + j].GenerateBaseMeshData();
                            Islands[W * WorkBlockSize + j].GenerateExtendedMeshdata();
                            Islands[W * WorkBlockSize + j].GenerateMountains();
                        }
                    });
                }
                for (int i = 0; i < WorkBlockSize; i++)
                {
                    Threads[i].Start();
                }
                for (int i = 0; i < WorkBlockSize; i++)
                {
                    Threads[i].Join();
                }
                
                //Weer op de main thread
                for (int i = 0; i < WorkBlockSize; i++)
                {
                    if(Islands[W * WorkBlockSize + i].IslandSize != IslandSize.Null)
                    Islands[W * WorkBlockSize + i].CreateIsland();
                }
                
                for (int i = 0; i < WorkBlockSize; i++)
                {
                    Island Old = Islands[W * WorkBlockSize + i];
                    Island New = new Island(Old.Size, Old.LeftBottomCoordinate, Old.world, Old.IslandSize);
                    Old = null;
                    Islands[W * WorkBlockSize + i] = null;
                    Islands[W * WorkBlockSize + i] = New;                
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            for (int i = 0; i < Islands.Count; i++)
            {
                if (Islands[i].IslandSize == IslandSize.Null)
                {
                    Islands.RemoveAt(i);
                }
            }

            //Extra garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

        }

        void CreateSea()
        {
            UVC3 LeftBottom = new UVC3(0,0,0);
            UVC3 RightBottom = new UVC3(3*WorldProps.Size.x, 0, 0);
            UVC3 LeftTop = new UVC3(0,0, 3*WorldProps.Size.y);
            UVC3 RightTop = new UVC3(3*WorldProps.Size.x,0,3*WorldProps.Size.y);

            UVC3[] Vertices = new UVC3[4];
            Vertices[0] = LeftTop;
            Vertices[1] = RightTop;
            Vertices[2] = LeftBottom;
            Vertices[3] = RightBottom;

            int[] Triangles = new int[6] { 0,1,2,2,1,3 };

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.vertices = Vertices;
            mesh.triangles = Triangles;
            mesh.RecalculateNormals();
            UnityEngine.GameObject SPO = UnityEngine.GameObject.Find("SeaPlane");

            if (SPO == null)
            {
                SPO = new UnityEngine.GameObject();
                SPO.name = "SeaPlane";
            }
            SPO.transform.position = new UVC3(-WorldProps.Size.x, 0, -WorldProps.Size.x);
            SPO.AddComponent<UnityEngine.MeshRenderer>().material.color = new UnityEngine.Color32(30, 97, 255, 255);
            SPO.AddComponent<UnityEngine.MeshFilter>().sharedMesh = mesh;
        }
        void CreateCamera()
        {
            UnityEngine.GameObject CamerObject = new UnityEngine.GameObject();
            CamerObject.name = "Main Camera";
            CamerObject.transform.position = new UVC3(0, 75, 0);
            CamerObject.transform.rotation = UnityEngine.Quaternion.Euler(45,0,0);
            CamerObject.AddComponent<UnityEngine.Camera>();
            CamerObject.AddComponent<ViewDrag>();
            CamerObject.GetComponent<UnityEngine.Camera>().nearClipPlane = 0.1f;
        }
    }   
}
