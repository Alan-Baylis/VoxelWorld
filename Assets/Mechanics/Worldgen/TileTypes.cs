namespace VoxelWorld.Worldgen 
{
    public enum TileType : byte
    {
        NullByte = 0,
        Water, //1
        Land, //2 
        Mountain //3
    }

    public enum SubTileType : byte
    { 
        NullByte = 0,
        Mountain,
    }

}