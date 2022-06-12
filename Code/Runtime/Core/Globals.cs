using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deform
{
    static class Globals
    {
        internal const bool IsHybridRenderer =
#if ENABLE_HYBRID_RENDERER_V2
            true;
#else
            false;
#endif
    }
}
