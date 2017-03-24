using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UVC3 = UnityEngine.Vector3;
using UVC2 = UnityEngine.Vector2;

namespace VoxelWorld.Worldgen
{
    public partial class Island
    {
        public Vector2 Size;
        public Vector2 LeftBottomCoordinate;
        public World world;
        public IslandSize IslandSize;
        protected MeshData meshData;
        protected MeshData rockMeshData;
        protected Random RND;
        protected List<Mountain> Mountains = new List<Mountain>();

        #region Privates
        Dictionary<int, List<Triangle>> TriangleDictionary = new Dictionary<int, List<Triangle>>();
        List<List<int>> outLines = new List<List<int>>();
        HashSet<int> checkedVertices = new HashSet<int>();
        List<bool> outLineTypes;
        #endregion
        public Island(Vector2 Size, Vector2 LBCoordinate, World W, IslandSize IsleSize)
        {
            this.Size = Size;
            LeftBottomCoordinate = LBCoordinate;
            world = W;
            IslandSize = IsleSize;
        }
        public Island(IslandSize iSize)
        {
            if(iSize == IslandSize.Null)
            IslandSize = IslandSize.Null;
        }
        #region WorldGen

        public void GenerateIslandShape(Random RNDA)
        {

            if (IslandSize == IslandSize.Enormous || IslandSize == IslandSize.Tiny || IslandSize == IslandSize.Small)
            {
                RND = RNDA;
                int InnerRangeX = 0;
                int InnerRangeY = 0;
                int CircleAmount = 0;

                int SizeModifierXMax = 1;
                int SizeModifierYMax = 1;
                int SizeModifierXMin = 1;
                int SizeModifierYMin = 1;


                //IslandSize dependency settings
                if (IslandSize == IslandSize.Enormous)
                {
                    InnerRangeX = Size.x / 10;  // 10%
                    InnerRangeY = Size.y / 10;
                    SizeModifierXMax = InnerRangeX;
                    SizeModifierYMax = InnerRangeY;
                    SizeModifierXMin = InnerRangeX / 2;
                    SizeModifierYMin = InnerRangeY / 2;
                }

                if (IslandSize == IslandSize.Small)
                {
                    InnerRangeX = Size.x / 3; //66%
                    InnerRangeY = Size.y / 3;
                    SizeModifierXMax = InnerRangeX / 4;
                    SizeModifierYMax = InnerRangeY / 4;
                }
                if (IslandSize == IslandSize.Tiny)
                {
                    InnerRangeX = (int)((Size.x / 3) * 1.2f); //80%
                    InnerRangeY = (int)((Size.y / 3) * 1.2f);
                    SizeModifierXMax = InnerRangeX / 5;
                    SizeModifierYMax = InnerRangeY / 5;
                }
                CircleAmount = (Size.x + Size.y) / 2 - (InnerRangeX + InnerRangeY);

                //Minima en maxima berekenen
                int xmin = LeftBottomCoordinate.x + InnerRangeX;
                int xmax = (LeftBottomCoordinate.x + Size.x) - InnerRangeX;
                int ymin = LeftBottomCoordinate.y + InnerRangeY;
                int ymax = (LeftBottomCoordinate.y + Size.y) - InnerRangeY;

                //cirkels tekenen
                while (CircleAmount > 0)
                {
                    int x = RND.Next(xmin, xmax);
                    int y = RND.Next(ymin, ymax);

                    int CircleRadiusX = RND.Next(SizeModifierXMin, SizeModifierXMax);
                    int CircleRadiusY = RND.Next(SizeModifierYMin, SizeModifierYMax);

                    for (int x2 = (x - CircleRadiusX); x2 < (x + CircleRadiusX); x2++)
                    {
                        for (int y2 = (y - CircleRadiusY); y2 < (y + CircleRadiusY); y2++)
                        {
                            world.TileTypeXZ[x2, y2] = (byte)TileType.Land;
                        }
                    }
                    CircleAmount--;
                }


                for (int i = 0; i < world.CellProps.SmoothingIterations; i++)
                {
                    for (int x = LeftBottomCoordinate.x; x < (LeftBottomCoordinate.x + Size.x); x++)
                    {
                        for (int y = LeftBottomCoordinate.y; y < (LeftBottomCoordinate.y + Size.y); y++)
                        {
                            byte NeighBours = GetNeighbours(new Vector2(x, y));
                            if (NeighBours > 4)
                            {
                                world.TileTypeXZ[x, y] = (byte)TileType.Land;
                            }
                            else
                            {
                                if (NeighBours < 4)
                                    world.TileTypeXZ[x, y] = (byte)TileType.Water;
                                else
                                    world.TileTypeXZ[x, y] = (RND.Next(0, 2) == 1 ? (byte)TileType.Land : (byte)TileType.Water);
                            }
                        }
                    }
                }
            }
            else
            {
                GenerateMiddleShapes();
            }
        }
        public byte GetNeighbours(Vector2 pos)
        {
            byte NCount = 0;
            for (sbyte x = -1; x <= 1; x++)
            {
                for (sbyte y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;
                    if ((pos.x + x) >= LeftBottomCoordinate.x && (pos.x + x) < (LeftBottomCoordinate.x + Size.x) && (pos.y + y) >= LeftBottomCoordinate.y && (pos.y + y) < (LeftBottomCoordinate.y + Size.y))
                    {
                        if (world.TileTypeXZ[pos.x + x, pos.y + y] == (byte)TileType.Land)
                        {
                            NCount++;
                        }
                    }
                }
            }
            return NCount;
        }

        public void GenerateBaseMeshData()
        {
            Vector2 Dimensions = new Vector2(Size.x+1, Size.y+1);
            UVC3[] Vertices = new UVC3[Dimensions.x * Dimensions.y];
            UVC2[] UVs = new UVC2[Dimensions.x * Dimensions.y];
            List<int> Triangles = new List<int>();
            byte[,] CheckedOnes = new byte[Dimensions.x,Dimensions.y];

                for (int xv = 0; xv < Dimensions.x; xv++)
                {
                    for (int yv = 0; yv < Dimensions.y; yv++)
                    {
                        if (((xv > 0 && xv < Dimensions.x - 1) && (yv > 0 && yv < Dimensions.y - 1)) && (world.TileTypeXZ[LeftBottomCoordinate.x + xv, LeftBottomCoordinate.y + yv] == (byte)TileType.Land))
                        {
                            Vertices[Dimensions.x * xv + yv] = new UVC3(LeftBottomCoordinate.x + xv - 0.5f, world.WorldProps.HeightSegments, LeftBottomCoordinate.y + yv - 0.5f);
                            CheckedOnes[xv, yv] = 1;
                        }
                        else
                        {
                            Vertices[Dimensions.x * xv + yv] = new UVC3(LeftBottomCoordinate.x + xv - 0.5f, world.WorldProps.HeightSegments, LeftBottomCoordinate.y + yv - 0.5f);
                        }
                    }
                }

                for (int x = 0; x < Dimensions.x - 1; x++)
                {
                    for (int y = 0; y < Dimensions.y - 1; y++)
                    {
                        if (CheckedOnes[x,y] == 1)
                        {
                            Triangles.Add(Dimensions.x * x + 1 + y);
                            Triangles.Add(Dimensions.x * x + Dimensions.x + 1 + y);
                            Triangles.Add(Dimensions.x * x + y);

                            Triangles.Add(Dimensions.x * x + y);
                            Triangles.Add(Dimensions.x * x + Dimensions.x + 1 + y);
                            Triangles.Add(Dimensions.x * x + Dimensions.x + y);
                        }
                    }
                }
            meshData = new MeshData(Dimensions, Vertices, Triangles.ToArray(), UVs);
        }
        public void GenerateExtendedMeshdata()
        {
            outLines.Clear();
            checkedVertices.Clear();
            outLineTypes = new List<bool>();

            int TriangleCount = meshData.Triangles.Length / 3;
            List<Triangle> TrianglesInMesh = new List<Triangle>();
            #region TriangleDetection
            for (int Tc = 0; Tc < TriangleCount; Tc++)
            {
                Triangle T = new Triangle(meshData.Triangles[3*Tc], meshData.Triangles[3*Tc+1], meshData.Triangles[3*Tc+2]);
                TrianglesInMesh.Add(T);
            }

            #endregion
            #region TriangleInsertion
            for (int i = 0; i < TriangleCount; i++)
            {
                AddToTriangleDictionary(TrianglesInMesh[i].VertexIndexA, TrianglesInMesh[i]);
                AddToTriangleDictionary(TrianglesInMesh[i].VertexIndexB, TrianglesInMesh[i]);
                AddToTriangleDictionary(TrianglesInMesh[i].VertexIndexC, TrianglesInMesh[i]);
            }
            #endregion
            GetMeshOutlines(meshData);
            GetOutlineTypes(meshData);
            List<UVC3> wallVertices = new List<UVC3>();
            List<int> wallTriangles = new List<int>();
            List<UVC2> wallUVs = new List<UVC2>();

            int hs = world.WorldProps.HeightSegments;
            int Ctor = 0;
            foreach (List<int> outline in outLines)
            {              
                for (int i = 0; i < outline.Count-1; i++)
                {
                    int StartIndex = wallVertices.Count;
                    wallVertices.Add(new UVC3(meshData.Vertices[outline[i]].x, meshData.Vertices[outline[i]].y, meshData.Vertices[outline[i]].z));
                    wallVertices.Add(new UVC3(meshData.Vertices[outline[i+1]].x, meshData.Vertices[outline[i+1]].y, meshData.Vertices[outline[i + 1]].z));
                    wallVertices.Add(new UVC3(meshData.Vertices[outline[i]].x, meshData.Vertices[outline[i]].y-hs, meshData.Vertices[outline[i]].z));
                    wallVertices.Add(new UVC3(meshData.Vertices[outline[i+1]].x, meshData.Vertices[outline[i+1]].y-hs, meshData.Vertices[outline[i + 1]].z));

                    wallUVs.Add(new UVC2(0.5f, 0.5f));
                    wallUVs.Add(new UVC2(1.0f, 0.5f));
                    wallUVs.Add(new UVC2(1.0f, 0.0f));
                    wallUVs.Add(new UVC2(0.5f, 0.0f));
                        
                    if (outLineTypes[Ctor])
                    {
                        wallTriangles.Add(StartIndex + 3);
                        wallTriangles.Add(StartIndex + 2);
                        wallTriangles.Add(StartIndex);

                        wallTriangles.Add(StartIndex);
                        wallTriangles.Add(StartIndex + 1);
                        wallTriangles.Add(StartIndex + 3);
                    }
                    else
                    {
                        wallTriangles.Add(StartIndex);
                        wallTriangles.Add(StartIndex + 2);
                        wallTriangles.Add(StartIndex + 3);

                        wallTriangles.Add(StartIndex + 3);
                        wallTriangles.Add(StartIndex + 1);
                        wallTriangles.Add(StartIndex);
                    }                
                }
                Ctor++;
            }
            rockMeshData = new MeshData(new Vector2(0, 0), wallVertices.ToArray(), wallTriangles.ToArray(), wallUVs.ToArray());
        }
        #region ExtendedMeshCode
        void GetMeshOutlines(MeshData meshData)
        {
            for (int vertexIndex = 0; vertexIndex < meshData.Vertices.Length; vertexIndex++)
            {
                if (!checkedVertices.Contains(vertexIndex))
                {
                    int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex != -1)
                    {
                        checkedVertices.Add(vertexIndex);
                        List<int> newOutline = new List<int>();
                        newOutline.Add(vertexIndex);
                        outLines.Add(newOutline);
                        FollowOutline(newOutlineVertex, outLines.Count - 1);
                        outLines[outLines.Count - 1].Add(vertexIndex);
                    }
                }
            }
        }
        void FollowOutline(int vertexIndex, int outlineIndex)
        {
            outLines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);
            if (nextVertexIndex != -1)
            {
                FollowOutline(nextVertexIndex, outlineIndex);
            }
        }
        int GetConnectedOutlineVertex(int VertexIndex)
        {
            if (!TriangleDictionary.ContainsKey(VertexIndex))
            {
                return -1;
            }
            List<Triangle> trianglesContainingVertex = TriangleDictionary[VertexIndex];

            for (int i = 0; i < trianglesContainingVertex.Count; i++)
            {
                Triangle triangle = trianglesContainingVertex[i];
                for (int j = 0; j < 3; j++)
                {
                    int vertexB = triangle[j];
                    if (vertexB != VertexIndex && !checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(VertexIndex, vertexB))
                            return vertexB;
                    }
                }
            }
            return -1;
        }
        struct Triangle
        {
            public int VertexIndexA;
            public int VertexIndexB;
            public int VertexIndexC;
            public int[] vertices;
            public Triangle(int a, int b, int c)
            {
                VertexIndexA = a;
                VertexIndexB = b;
                VertexIndexC = c;

                vertices = new int[3];
                vertices[0] = VertexIndexA;
                vertices[1] = VertexIndexB;
                vertices[2] = VertexIndexC;
            }

            public int this[int i]
            {
                get
                {
                    return vertices[i];
                }
            }

            public bool Contains(int VertexIndex)
            {
                return VertexIndex == VertexIndexA || VertexIndex == VertexIndexB || VertexIndex == VertexIndexC;
            }

        }
        void AddToTriangleDictionary(int VertexIndexKey, Triangle triangle)
        {
            if (TriangleDictionary.ContainsKey(VertexIndexKey))
            {
                TriangleDictionary[VertexIndexKey].Add(triangle);
            }
            else
            {
                List<Triangle> TList = new List<Triangle>();
                TList.Add(triangle);
                TriangleDictionary.Add(VertexIndexKey, TList);
            }
        }
        bool IsOutlineEdge(int VertexA, int VertexB)
        {
            List<Triangle> TrianglesA = TriangleDictionary[VertexA];
            int sharedTriangleCount = 0;
            for (int i = 0; i < TrianglesA.Count; i++ )
            {
                if (TrianglesA[i].Contains(VertexB))
                    sharedTriangleCount++;
                if (sharedTriangleCount > 1)
                    break;
            }
            return sharedTriangleCount == 1;
        }
        void GetOutlineTypes(MeshData meshData)
        {
            for (int i = 0; i < outLines.Count; i++)
            {
                bool LargestOutline = true;
                bool Check = false;

                BeforeChecks:

                if (Check)
                {
                    continue;
                }


                for (int j = 0; j < outLines.Count; j++)
                {
                    if (outLines[j].Count > outLines[i].Count)
                    {
                        LargestOutline = false;
                    }
                }
                if (LargestOutline)
                {
                    //Kan nu geen inneroutline zijn, het is de grootste
                    outLineTypes.Add(false);
                    continue;
                }
                //Kan nog een inneroutline zijn....
                List<UVC3> Vertices = new List<UVC3>();
                for (int h = 0; h < outLines[i].Count; h++)
                {
                    Vertices.Add(meshData.Vertices[outLines[i][h]]);
                }
                for (int x = 0; x < outLines[i].Count; x++)
                {
                    float ZMin = -1;
                    int YIteration = -1;

                    for (int y = 0; y < outLines[i].Count; y++)
                    {
                        if (Vertices[x].x == Vertices[y].x && x != y && Vertices[x].z > Vertices[y].z && (UVC3.Distance(Vertices[x], Vertices[y]) < ZMin || ZMin == -1))
                        {
                            ZMin = UVC3.Distance(Vertices[x], Vertices[y]);
                            YIteration = y;
                        }
                    }
                    if (YIteration != -1)
                    {
                        bool a = CheckCoords(meshData.Vertices[outLines[i][x]], meshData.Vertices[outLines[i][YIteration]]);
                        if (a)
                        {
                            Check = true;
                            outLineTypes.Add(true);
                            goto BeforeChecks;
                        }
                    }
                }
                //Nu niet meer
                outLineTypes.Add(false);
            }
        }
        bool CheckCoords(UVC3 A, UVC3 B)
        {
            int Ya = (int)(A.z - 0.5f);
            int Yb = (int)(B.z - 0.5f);
            int Xa = (int)(A.x - 0.5f);
            int Xb = (int)(B.x - 0.5f);

            Vector2 Coords = new Vector2(Xa, Ya - ((Ya - Yb) / 2));
            //x coords gelijk
            if (world.TileTypeXZ[Coords.x, Coords.y] == (byte)TileType.Water && world.TileTypeXZ[Xa,Ya+1] == (byte)TileType.Land && world.TileTypeXZ[Xb,Yb-1] == (byte)TileType.Land)
            {
                //Pathfind!!!
                List<Vector2> Tiles = FindPath(new Vector2(Coords.x - LeftBottomCoordinate.x, Coords.y - LeftBottomCoordinate.y), new Vector2(0,0), TileType.Water);
                if (Tiles.Count == 0)
                {
                    return true;
                }
                //Is an OUTLINE
                //UnityEngine.Debug.Log(Coords.x + "-" + Coords.y + "<" + Xa + "-" + Ya + ">" + "<" + Xb + "-" + Yb + ">" + BaseWaterTiles.Count);
                return false;
            }
            //ISNOT OUTLINE
            return false;
        }

        #endregion
        #region MountainGeneration
        public void GenerateMountains()
        {
            List<Vector2> PossibleMountainMidpoints = new List<Vector2>();
            //get possible mountain locations
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    if (world.TileTypeXZ[LeftBottomCoordinate.x + x, LeftBottomCoordinate.y + y] == (byte)TileType.Land)
                    {
                        int WCount = GetWaterTileCount(LeftBottomCoordinate.x + x, LeftBottomCoordinate.y + y);
                        if (WCount > 1)
                        {
                            //Can be a mountain range
                            PossibleMountainMidpoints.Add(new Vector2(LeftBottomCoordinate.x + x, LeftBottomCoordinate.y + y));
                        }
                    }
                }
            }

            //Afhankelijk van grootte van eiland maken
            int MountainRangeAmount = 8;
            for (int i = 0; i < MountainRangeAmount; i++)
            {
                int Pos = RND.Next(0,PossibleMountainMidpoints.Count);
                int Radius = RND.Next(5,23);
                Vector3[,] FullTiles = new Vector3[Radius, Radius];

                for (int x = -1*(Radius/2); x < Radius/2; x++)
                {
                    for (int y = -1 * (Radius / 2); y < Radius / 2; y++)
                    {
                            if (PossibleMountainMidpoints[Pos].x + x > 0 && PossibleMountainMidpoints[Pos].x <= world.WorldProps.Size.x && PossibleMountainMidpoints[Pos].y + y > 0 && PossibleMountainMidpoints[Pos].y <= world.WorldProps.Size.y)
                            {
                            float height = -1;
                                if (x * x + y * y <= Radius*Radius)
                                {
                                //Hoogte berekenen
                                float height2 = Radius/2 - UVC3.Distance(new UVC3(PossibleMountainMidpoints[Pos].x, 5, PossibleMountainMidpoints[Pos].y), new UVC3(PossibleMountainMidpoints[Pos].x + x, 5, PossibleMountainMidpoints[Pos].y + y));
                                height = (height2 + world.WorldProps.HeightSegments);
                                }
                                FullTiles[x + Radius/2,y + Radius/2] = (new Vector3(PossibleMountainMidpoints[Pos].x + x, (int)height ,PossibleMountainMidpoints[Pos].y + y));
                            }
                    }
                }
                Mountains.Add(new Mountain(FullTiles,PossibleMountainMidpoints[Pos], Radius, world));
            }
            foreach (Mountain M in Mountains)
            {
                try
                {
                    M.GenerateMeshData();
                }
                catch(Exception e) { UnityEngine.Debug.Log(e); }
            }

        }
        public class Mountain
        {
            public Vector3[,] MountainTiles;
            public Vector2 Midpoint;
            public int Radius;
            public World world;
            public MeshData MountainMesh;
            public Mountain(Vector3[,] MountainTiles, Vector2 Midpoint, int Radius, World world)
            {
                this.MountainTiles = MountainTiles;
                this.Midpoint = Midpoint;
                this.Radius = Radius;
                this.world = world;
                MountainMesh = new MeshData();
            }
            public void GenerateMeshData()
            {
                List<UVC3> Vertices = new List<UVC3>();
                List<int> Triangles = new List<int>();

                foreach (UVC3 V in MountainTiles)
                {
                    if (V.y == -1)
                        continue;

                    UVC3[] verts = new UVC3[4];
                    int[] tris = new int[6] { 0, 1, 2, 2, 1, 3};     

                    verts[0] = new UVC3(V.x - 0.5f, V.y, V.z + 0.5f);
                    verts[1] = new UVC3(V.x + 0.5f, V.y, V.z + 0.5f);
                    verts[2] = new UVC3(V.x - 0.5f, V.y, V.z - 0.5f);
                    verts[3] = new UVC3(V.x + 0.5f, V.y, V.z - 0.5f);
                    
                    int CurrentVertPos = Vertices.Count;

                    for (int i = 0; i < verts.Length; i++)
                    {
                        Vertices.Add(verts[i]);
                    }
                    for (int o = 0; o < tris.Length; o++)
                    {
                        Triangles.Add(CurrentVertPos + tris[o]);
                    }

                    //-------------------------------------------------
                    // Vertex layout
                    // 0 ------1
                    // |     / |
                    // |   /*  |		
                    // | /     |
                    // 2 ------ 3    

                    UVC3[] verts2 = new UVC3[4];
                    int[] tris2 = new int[6] { 2, 1, 0, 3, 1, 2 };       

                    verts[0] = new UVC3(V.x - 0.5f, V.y, V.z + 0.5f);
                    verts[1] = new UVC3(V.x + 0.5f, V.y, V.z + 0.5f);
                    verts[2] = new UVC3(V.x - 0.5f, 0, V.z + 0.5f);
                    verts[3] = new UVC3(V.x + 0.5f, 0, V.z + 0.5f);

                    int CurrentVertPos2 = Vertices.Count;

                    for (int i = 0; i < verts2.Length; i++)
                    {
                        Vertices.Add(verts[i]);
                    }
                    for (int o = 0; o < tris2.Length; o++)
                    {
                        Triangles.Add(CurrentVertPos2 + tris2[o]);
                    }

                    //-------------------------------------------------

                    UVC3[] verts3 = new UVC3[4];
                    int[] tris3 = new int[6] { 0, 1, 2, 2, 1, 3 };

                    verts[0] = new UVC3(V.x - 0.5f, V.y, V.z - 0.5f);
                    verts[1] = new UVC3(V.x + 0.5f, V.y, V.z - 0.5f);
                    verts[2] = new UVC3(V.x - 0.5f, 0, V.z - 0.5f);
                    verts[3] = new UVC3(V.x + 0.5f, 0, V.z - 0.5f);

                    int CurrentVertPos3 = Vertices.Count;

                    for (int i = 0; i < verts3.Length; i++)
                    {
                        Vertices.Add(verts[i]);
                    }
                    for (int o = 0; o < tris3.Length; o++)
                    {
                        Triangles.Add(CurrentVertPos3 + tris3[o]);
                    }

                    //-------------------------------------------------

                    UVC3[] verts4 = new UVC3[4];
                    int[] tris4 = new int[6] { 2, 1, 0, 3, 1, 2 };

                    verts[0] = new UVC3(V.x + 0.5f, V.y, V.z + 0.5f);
                    verts[1] = new UVC3(V.x + 0.5f, V.y, V.z - 0.5f);
                    verts[2] = new UVC3(V.x + 0.5f, 0, V.z + 0.5f);
                    verts[3] = new UVC3(V.x + 0.5f, 0, V.z - 0.5f);

                    int CurrentVertPos4 = Vertices.Count;

                    for (int i = 0; i < verts4.Length; i++)
                    {
                        Vertices.Add(verts[i]);
                    }
                    for (int o = 0; o < tris4.Length; o++)
                    {
                        Triangles.Add(CurrentVertPos4 + tris4[o]);
                    }

                    //-------------------------------------------------

                    UVC3[] verts5 = new UVC3[4];
                    int[] tris5 = new int[6] { 0, 1, 2, 2, 1, 3 };

                    verts[0] = new UVC3(V.x - 0.5f, V.y, V.z + 0.5f);
                    verts[1] = new UVC3(V.x - 0.5f, V.y, V.z - 0.5f);
                    verts[2] = new UVC3(V.x - 0.5f, 0, V.z + 0.5f);
                    verts[3] = new UVC3(V.x - 0.5f, 0, V.z - 0.5f);

                    int CurrentVertPos5 = Vertices.Count;

                    for (int i = 0; i < verts5.Length; i++)
                    {
                        Vertices.Add(verts[i]);
                    }
                    for (int o = 0; o < tris5.Length; o++)
                    {
                        Triangles.Add(CurrentVertPos5 + tris5[o]);
                    }
                }
                MountainMesh.Vertices = Vertices.ToArray();
                MountainMesh.Triangles = Triangles.ToArray();
            }      
        }
        int GetWaterTileCount(int x, int y)
        {
            int WTCount = 0;
            for (int xt = -1; xt < 2; xt++)
            {
                for (int yt = -1; yt < 2; yt++)
                {
                    if (xt == 0 && yt == 0)
                    {
                        continue;
                    }
                    else
                    {
                        if (world.TileTypeXZ[x + xt, y + yt] == (byte)TileType.Water)
                        {
                            WTCount++;
                        }
                    }
                }
            }
            return WTCount;
        }

        #endregion
        #endregion
        #region AStar
        List<Vector2> FindPath(Vector2 StartPosition, Vector2 EndPosition, TileType T)
        {
            AstarObject[,] Set = new AstarObject[Size.x,Size.y];
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    Set[x, y] = new AstarObject(LeftBottomCoordinate.x+x,LeftBottomCoordinate.y+y, world);
                }
            }

            //List<AstarObject> OpenSet = new List<AstarObject>();
            Heap<AstarObject> OpenSet = new Heap<AstarObject>(Size.x * Size.y);
            HashSet<AstarObject> ClosedSet = new HashSet<AstarObject>();
            AstarObject Start = Set[StartPosition.x, StartPosition.y];
            AstarObject End = Set[EndPosition.x,EndPosition.y];
            OpenSet.Add(Start);

            while (OpenSet.Count > 0)
            {
                /*
                AstarObject CurrentLocation = OpenSet[0];
                for (int i = 1; i < OpenSet.Count; i++)
                {
                    if (OpenSet[i].fCost < CurrentLocation.fCost || OpenSet[i].fCost == CurrentLocation.fCost && OpenSet[i].hCost < CurrentLocation.hCost)
                    {
                        CurrentLocation = OpenSet[i];
                    }
                }
                OpenSet.Remove(CurrentLocation);
                */
                AstarObject CurrentLocation = OpenSet.RemoveFirst();

                ClosedSet.Add(CurrentLocation);

                if (CurrentLocation == End)
                {
                    return RetracePath(Start,End);
                    //Retracepath and stuff.
                }
                List<AstarObject> Neighbours = GetNeighbours(CurrentLocation, ref Set);
                foreach (AstarObject neighbour in Neighbours)
                {
                    if (neighbour.TType != T || ClosedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbour = CurrentLocation.gCost + GetDistance(CurrentLocation, neighbour);
                    if (newMovementCostToNeighbour < neighbour.gCost || !OpenSet.Contains(neighbour))
                    {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, End);
                        neighbour.parent = CurrentLocation;

                        if (!OpenSet.Contains(neighbour))
                        {
                            OpenSet.Add(neighbour);
                        }
                        else
                        {
                            OpenSet.UpdateItem(neighbour);
                        }

                    }

                }

            }
            return new List<Vector2>();
        }
        List<AstarObject> GetNeighbours(AstarObject A, ref AstarObject[,] Set)
        {
            List<AstarObject> Neighbours = new List<AstarObject>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y =-1; y <=1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int CheckX = A.x + x;
                    int CheckY = A.y + y;

                    if (CheckX >= LeftBottomCoordinate.x && CheckX < LeftBottomCoordinate.x + Size.x && CheckY >= LeftBottomCoordinate.y && CheckY < LeftBottomCoordinate.y + Size.y)
                    {
                        Neighbours.Add(Set[CheckX - LeftBottomCoordinate.x,CheckY - LeftBottomCoordinate.y]);
                    }
                }
            }
            return Neighbours;
        }
        int GetDistance(AstarObject A, AstarObject B)
        {
            int dstX = UnityEngine.Mathf.Abs(A.x - B.x);
            int dstY = UnityEngine.Mathf.Abs(A.y - B.y);

            if (dstX > dstY)
                return 14 * dstY + 10 * dstX;
            return 14 * dstX + 10 * (dstY - dstX);

        }
        List<Vector2> RetracePath(AstarObject Start, AstarObject End)
        {
            List<Vector2> path = new List<Vector2>();

            AstarObject CurrentA = End;
            while (CurrentA != Start)
            {
                path.Add(CurrentA.ToVector2());
                CurrentA = CurrentA.parent;
            }
            path.Reverse();
            return path;
        }
        #endregion
        public bool SaveMeshToDisk()
        {
            UnityEngine.GameObject T = UnityEngine.GameObject.Find("Islands/Island " + LeftBottomCoordinate);
            if (T == null)
                return false;
            //BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream("Island " + LeftBottomCoordinate + ".bin", FileMode.Create, FileAccess.Write, FileShare.None);
            //formatter.Serialize(stream, new SpecialConversionClasses.SerializableMeshData(meshData));
            stream.Close();
            //meshData = new MeshData();
            UnityEngine.GameObject.Destroy(T.GetComponent<UnityEngine.MeshFilter>().sharedMesh);
            T.GetComponent<UnityEngine.MeshFilter>().sharedMesh = null;
            return true;
        }
        public bool LoadMeshFromDisk()
        {
            try
            {
                //BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream("Island " + LeftBottomCoordinate + ".bin", FileMode.Open, FileAccess.Read, FileShare.None);
                //SpecialConversionClasses.SerializableMeshData M = (SpecialConversionClasses.SerializableMeshData)formatter.Deserialize(stream);
                stream.Close();
                //meshData = M.ToMeshData();
                //MeshStuff
                return true;
            }
            catch
            {
                return false;
            }
        }
        public void CreateIsland() 
        {

            UnityEngine.GameObject IslandGameObject = new UnityEngine.GameObject("Island " + LeftBottomCoordinate);
            UnityEngine.GameObject IslandRockObject = new UnityEngine.GameObject("RockMesh");

            UnityEngine.GameObject Islands = UnityEngine.GameObject.Find("Islands");

            if (Islands == null)
            {
                Islands = new UnityEngine.GameObject("Islands");
            }
            IslandGameObject.transform.parent = Islands.transform;
            IslandRockObject.transform.parent = IslandGameObject.transform;

            #region BaseMesh
            IslandGameObject.AddComponent<UnityEngine.MeshRenderer>();
            IslandGameObject.AddComponent<UnityEngine.MeshFilter>();
            //IslandGameObject.AddComponent<UnityEngine.MeshCollider>();
            
            //MeshFilter base
            IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh = new UnityEngine.Mesh();
            IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices = meshData.Vertices;
            IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.triangles = meshData.Triangles;
            IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.uv = meshData.UVs;
            IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.RecalculateNormals();
            var o_865_12_636258850854118809 = IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
            IslandGameObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.UploadMeshData(true);
            /*
            //MeshCollider base
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh = new UnityEngine.Mesh();
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.vertices = meshData.Vertices;
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.triangles = meshData.Triangles;
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.uv = meshData.UVs;
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.RecalculateNormals();
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.Optimize();
            IslandGameObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.UploadMeshData(true);
            */
            IslandGameObject.GetComponent<UnityEngine.MeshRenderer>().material.color = new UnityEngine.Color32(51,204,51,255);
            #endregion
            #region RockMesh
            IslandRockObject.AddComponent<UnityEngine.MeshRenderer>();
            IslandRockObject.AddComponent<UnityEngine.MeshFilter>();
            //IslandRockObject.AddComponent<UnityEngine.MeshCollider>();

            //MeshFilter rock
            IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh = new UnityEngine.Mesh();
            IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices = rockMeshData.Vertices;
            IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.triangles = rockMeshData.Triangles;
            IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.uv = rockMeshData.UVs;
            IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.RecalculateNormals();
            var o_890_12_636258850854413999 = IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh;
            IslandRockObject.GetComponent<UnityEngine.MeshFilter>().sharedMesh.UploadMeshData(true);
            /*
            //MeshCollider rock 
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh = new UnityEngine.Mesh();
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.vertices = rockMeshData.Vertices;
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.triangles = rockMeshData.Triangles;
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.uv = rockMeshData.UVs;
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.RecalculateNormals();
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.Optimize();
            IslandRockObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.UploadMeshData(true);
            */
            IslandRockObject.GetComponent<UnityEngine.MeshRenderer>().material.mainTexture = world.WorldProps.TexAtlas;
            #endregion
            
            for (int i = 0; i < Mountains.Count; i++)
            {
                Mountain M = Mountains[i];
                UnityEngine.GameObject MountainObject = new UnityEngine.GameObject();
                MountainObject.name = "Mountain " + M.Midpoint.x + "-" + M.Midpoint.y;
                MountainObject.AddComponent<UnityEngine.MeshRenderer>();
                MountainObject.AddComponent<UnityEngine.MeshFilter>().mesh = new UnityEngine.Mesh();
                MountainObject.AddComponent<UnityEngine.MeshCollider>().sharedMesh = new UnityEngine.Mesh();

                MountainObject.GetComponent<UnityEngine.MeshFilter>().mesh.vertices = M.MountainMesh.Vertices;
                MountainObject.GetComponent<UnityEngine.MeshFilter>().mesh.triangles = M.MountainMesh.Triangles;
                MountainObject.GetComponent<UnityEngine.MeshFilter>().mesh.uv = M.MountainMesh.UVs;
                MountainObject.GetComponent<UnityEngine.MeshFilter>().mesh.RecalculateNormals();
                var o_918_16_636258850854419003 = MountainObject.GetComponent<UnityEngine.MeshFilter>().mesh;
                MountainObject.GetComponent<UnityEngine.MeshFilter>().mesh.UploadMeshData(true);

                MountainObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.vertices = M.MountainMesh.Vertices;
                MountainObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.triangles = M.MountainMesh.Triangles;
                MountainObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.uv = M.MountainMesh.UVs;
                MountainObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.RecalculateNormals();
                var o_925_16_636258850854424007 = MountainObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh;
                MountainObject.GetComponent<UnityEngine.MeshCollider>().sharedMesh.UploadMeshData(true);

                MountainObject.GetComponent<UnityEngine.MeshRenderer>().material.color = new UnityEngine.Color(0.5f,0.5f,0.5f,1f);
                MountainObject.GetComponent<UnityEngine.MeshRenderer>().material.SetFloat("_Glossiness", 0.0f);
                MountainObject.transform.parent = IslandGameObject.transform;
            }   
            meshData.ResetData();
            rockMeshData.ResetData();
        }
        
    }
}
