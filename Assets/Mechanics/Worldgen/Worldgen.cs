using System;
using Tex = UnityEngine.Texture2D;

namespace VoxelWorld.Worldgen
{
    [Serializable]
    public struct Vector3
    {
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
        public Vector3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        static public implicit operator Vector3(UnityEngine.Vector3 UnityVector3)
        {
            return new Vector3((int)UnityVector3.x, (int)UnityVector3.y, (int)UnityVector3.z);
        }
        static public explicit operator UnityEngine.Vector3(Vector3 VoxelWorldVector3)
        {
            return new UnityEngine.Vector3(VoxelWorldVector3.x, VoxelWorldVector3.y, VoxelWorldVector3.z);
        }
        public UnityEngine.Vector3 ToUV3()
        {
            return new UnityEngine.Vector3(x, y, z);
        }
        public override string ToString()
        {
            return "(" + x + "," + y + "," + z + ")";
        }
    }

    [Serializable]
    public struct Vector2
    {
        public int x { get; set; }
        public int y { get; set; }
        public Vector2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        static public implicit operator Vector2(UnityEngine.Vector2 UnityVector2)
        {
            return new Vector2((int)UnityVector2.x, (int)UnityVector2.y);
        }
        static public explicit operator UnityEngine.Vector2(Vector2 VoxelWorldVector2)
        {
            return new UnityEngine.Vector2(VoxelWorldVector2.x, VoxelWorldVector2.y);
        }

        public UnityEngine.Vector2 ToUV2()
        {
            return new UnityEngine.Vector2(x, y);
        }

        public UnityEngine.Vector3 ToUV3()
        {
            return new UnityEngine.Vector3(x, 0, y);
        }
        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
    }

    public struct WorldProperties
    {
        public Vector2 Size { get; set; }
        public Vector3 StartPosition { get; set; }
        public string Seed { get; set; }
        public Random RandomGen { get; set; }
        public UnityEngine.Vector2 IslandMaxSize { get; set; }
        public Tex TexAtlas { get; set; }
        public int HeightSegments { get; set; }
        public int WorldSeaPercentage { get; set; }

        public WorldProperties(Vector2 WorldSize, Vector3 StartPosition, string seed, Vector2 IslandMaxSize,Tex TexAtlas, int HeightSegments, int WorldSeaPerc)
        {
            Size = WorldSize;
            this.StartPosition = StartPosition;
            Seed = seed;
            this.IslandMaxSize = IslandMaxSize.ToUV2();
            this.TexAtlas = TexAtlas;
            this.HeightSegments = HeightSegments;
            WorldSeaPercentage = WorldSeaPerc;

            if (Seed != "")
            {
                RandomGen = new Random(Seed.GetHashCode());
            }
            else
            {
                RandomGen = new Random(DateTime.Now.Date.GetHashCode() + UnityEngine.Time.time.GetHashCode());
            }
        }
    }

    public struct CellularAutomataProperties
    {
        public int SmoothingIterations { get; set; }
        public int FillPercentage { get; set; }
        public CellularAutomataProperties(int SmoothingIterations, int FillPercentage)
        {
            this.SmoothingIterations = SmoothingIterations;
            this.FillPercentage = FillPercentage;
        }
    }
    public struct MeshData 
    {
        public Vector2 Dimensions;
        public UnityEngine.Vector3[] Vertices;
        public int[] Triangles;
        public UnityEngine.Vector2[] UVs;
        public MeshData(Vector2 Dimensions, UnityEngine.Vector3[] Vertices, int[] Triangles, UnityEngine.Vector2[] UVs)
        {
            this.Dimensions = Dimensions;
            this.Vertices = Vertices;
            this.Triangles = Triangles;
            this.UVs = UVs;
        }

        public void ResetData()
        {
            Dimensions = new Vector2();
            Vertices = new UnityEngine.Vector3[0];
            Triangles = new int[0];
            UVs = new UnityEngine.Vector2[0];
        }
    }

    public enum IslandSize
    {
        Null,
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Enormous
    }

    [Serializable]
    class AstarObject : IHeapItem<AstarObject>
    {
        public int x;
        public int y;
        public World WorldRef;
        public TileType TType {
            get { return (TileType)WorldRef.TileTypeXZ[x, y]; }
        }
        public int gCost { get; set; }
        public int hCost { get; set; }
        public int fCost
        {
            get { return gCost + hCost; }
        }
        public AstarObject parent { get; set; }
        
        public AstarObject(int x, int y, World wRef)
        {
            this.x = x;
            this.y = y;
            WorldRef = wRef;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x,y);
        }
        public int CompareTo(AstarObject AObjectToCompare)
        {
            int compare = fCost.CompareTo(AObjectToCompare.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(AObjectToCompare.hCost);
            }
            return -compare;
        }

        public int HeapIndex
        {
            get; set;
        }
    }
}

namespace VoxelWorld.Worldgen.SpecialConversionClasses
{
    [Serializable]
    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public UnityEngine.Vector2 ToUVC2()
        {
            return new UnityEngine.Vector2(x,y);
        }
    }
    [Serializable]
    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;    
        }
        public UnityEngine.Vector3 ToUVC3()
        {
            return new UnityEngine.Vector3(x,y,z);
        }
    }
    [Serializable]
    public struct SerializableMeshData
    {
        public Vector2 Dimensions;
        public Vector3[] Vertices;
        public int[] Triangles;
        public Vector2[] UVs;
        public SerializableMeshData(Worldgen.MeshData M)
        {
            Dimensions = new Vector2(M.Dimensions.x,M.Dimensions.y);
            Vertices = new Vector3[M.Vertices.Length];
            for (int i = 0; i < M.Vertices.Length; i++)
            {
                Vertices[i] = new Vector3(M.Vertices[i].x, M.Vertices[i].y,M.Vertices[i].z);
            }
            Triangles = M.Triangles;
            UVs = new Vector2[M.UVs.Length];
            for (int i = 0; i < M.UVs.Length; i++)
            {
                UVs[i] = new Vector2(M.UVs[i].x, M.UVs[i].y);
            }
        }
        public MeshData ToMeshData()
        {
            MeshData M = new MeshData();
            M.Vertices = new UnityEngine.Vector3[Vertices.Length];
            for (int i = 0; i < Vertices.Length; i++)
            {
                M.Vertices[i] = new UnityEngine.Vector3(Vertices[i].x, Vertices[i].y, Vertices[i].z);
            }
            M.Triangles = Triangles;
            M.UVs = new UnityEngine.Vector2[UVs.Length];
            for (int i = 0; i < UVs.Length; i++)
            {
                M.UVs[i] = new UnityEngine.Vector2(UVs[i].x, UVs[i].y);
            }
            return M;
        }
    }
}
namespace VoxelWorld.Worldgen.ExtensionMethods
{
    public static class Extensions
    {
        public static SpecialConversionClasses.Vector3 ToSerialize(this UnityEngine.Vector3 t)
        {
            return new SpecialConversionClasses.Vector3(t.x,t.y,t.z);
        }
        public static SpecialConversionClasses.Vector2 ToSerialize(this UnityEngine.Vector2 t)
        {
            return new SpecialConversionClasses.Vector2(t.x, t.y);
        }
        public static Worldgen.Vector2 ToSerializeWV(this UnityEngine.Vector2 t)
        {
            return new Worldgen.Vector2((int)t.x,(int)t.y);
        }
        public static Worldgen.Vector3 ToSerializeWV(this UnityEngine.Vector3 t)
        {
            return new Worldgen.Vector3((int)t.x,(int)t.y,(int)t.z);
        }
    }
}