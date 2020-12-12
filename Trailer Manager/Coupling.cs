﻿using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        class Coupling
        {
            private IMyAttachableTopBlock A, B;

            public Coupling(IMyAttachableTopBlock part)
            {
                this.A = part;
            }

            public void AddPart(IMyAttachableTopBlock part)
            {
                this.B = part;
            }

            public bool HasTwoParts()
            {
                return null != B && !A.Equals(B);
            }

            public bool ContainsPart(IMyAttachableTopBlock part)
            {
                return (A.Equals(part) || B.Equals(part));
            }

            public IMyCubeGrid getOtherGrid(IMyCubeGrid grid)
            {
                if (grid.Equals(A.Base.CubeGrid) && B.IsAttached)
                    return B.Base.CubeGrid;
                if (grid.Equals(B.Base.CubeGrid) && A.IsAttached)
                    return A.Base.CubeGrid;
                return null;
            }
        }
    }
}
