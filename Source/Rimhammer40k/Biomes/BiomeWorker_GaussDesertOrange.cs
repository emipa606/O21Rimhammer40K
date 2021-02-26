﻿using RimWorld;
using RimWorld.Planet;

namespace Rimhammer40k.Biomes
{
    public class BiomeWorker_GaussDesertOrange : BiomeWorker
    {
        public override float GetScore(Tile tile, int tileID)
        {
            float result;
            if (tile.WaterCovered)
            {
                result = -100f;
            }
            else if (tile.temperature < 34f)
            {
                result = 0f;
            }
            else if (tile.rainfall < 600f || tile.rainfall >= 2000f)
            {
                result = 0f;
            }
            else
            {
                result = 22.5f + (tile.temperature * 1.8f) + ((tile.rainfall - 600f) / 85f);
            }

            return result;
        }
    }
}