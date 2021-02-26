using System;
using HugsLib;

namespace Rimhammer40k
{
    [EarlyInit]
    public class Rimhammer40kController : ModBase
    {
        public override string ModIdentifier => "Rimhammer40k";

        [Obsolete("Override EarlyInitialize instead (typo).")]
        public override void EarlyInitalize()
        {
            Logger.Message(":: Rimhammer40k Active ::");
        }
    }
}