using System.Reflection;
using HarmonyLib;
using Verse;

namespace Rimhammer40k
{
    [StaticConstructorOnStartup]
    public static class Rimhammer40kPatches
    {
        static Rimhammer40kPatches()
        {
            var Rimhammer40k = new Harmony("com.rimhammer40k.rimworld.mod");

            Rimhammer40k.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}