using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoxelWorld.Worldgen
{
    public partial class Island
    {
        void GenerateMiddleShapes()
        {
            if (IslandSize == IslandSize.Medium)
                GenerateMedium();
            if (IslandSize == IslandSize.Huge)
                GenerateHuge();
            if (IslandSize == IslandSize.Large)
                GenerateLarge();
        }

        void GenerateMedium() { }
        void GenerateHuge() { }
        void GenerateLarge() { }     
        
           
    }
}
