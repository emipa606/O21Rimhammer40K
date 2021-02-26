using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Rimhammer40k.Spore
{
    public class OrkoidShroom : Plant
    {
        private int ColonistOrkCount
        {
            get { return Map.mapPawns.AllPawns.Count(x => x.IsColonist && x.def.defName == "Alien_Ork"); }
        }

        private int ColonistGrotCount
        {
            get { return Map.mapPawns.AllPawns.Count(x => x.IsColonist && x.def.defName == "Alien_Grot"); }
        }

        public override void TickLong()
        {
            base.TickLong();
            if (TicksUntilFullyGrown == 0)
            {
                TrySpawn();
            }
        }

        private void TrySpawn()
        {
            var whichOrkoid = Random.Range(0, 100);
            if (whichOrkoid < 66)
            {
                if (CanSpawnOrk())
                {
                    var pawn = GenerateOrk();
                    GenSpawn.Spawn(pawn, Position, Map);
                }
                else if (CanSpawnGrot())
                {
                    var pawn = GenerateGrot();
                    GenSpawn.Spawn(pawn, Position, Map);
                }
            }
            else
            {
                if (CanSpawnGrot())
                {
                    var pawn = GenerateGrot();
                    GenSpawn.Spawn(pawn, Position, Map);
                }
                else if (CanSpawnOrk())
                {
                    var pawn = GenerateOrk();
                    GenSpawn.Spawn(pawn, Position, Map);
                }
            }

            Destroy();
        }

        private Pawn GenerateOrk()
        {
            Log.Message("Spawning Ork...");
            var request = new PawnGenerationRequest(
                PawnKindDef.Named("FeralOrk"),
                Faction.OfPlayer,
                fixedBiologicalAge: 0,
                fixedChronologicalAge: 0,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                allowFood: false);
            var pawn = PawnGenerator.GeneratePawn(request);

            pawn?.equipment?.DestroyAllEquipment();
            pawn?.apparel?.DestroyAll();
            pawn?.inventory?.DestroyAll();

            return pawn;
        }

        private Pawn GenerateGrot()
        {
            Log.Message("Spawning Grot...");
            var request = new PawnGenerationRequest(
                PawnKindDef.Named("FeralGrot"),
                Faction.OfPlayer,
                fixedBiologicalAge: 0,
                fixedChronologicalAge: 0,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                colonistRelationChanceFactor: 0f,
                allowFood: false);
            var pawn = PawnGenerator.GeneratePawn(request);

            pawn?.equipment?.DestroyAllEquipment();
            pawn?.apparel?.DestroyAll();
            pawn?.inventory?.DestroyAll();

            return pawn;
        }

        private bool CanSpawnOrk()
        {
            if (ColonistOrkCount > Rimhammer40kMod.maxOrkPopulation)
            {
                return false;
            }

            return true;
        }

        private bool CanSpawnGrot()
        {
            if (ColonistGrotCount <= Rimhammer40kMod.maxOrkPopulation)
            {
                return true;
            }

            Log.Message("Current Grot Count: " +
                        Map.mapPawns.AllPawns.Count(x => x.IsColonist && x.def.defName == "Alien_Grot"));
            Log.Message("Max Grot Count: " + Rimhammer40kMod.maxGrotPopulation);
            return false;
        }
    }
}