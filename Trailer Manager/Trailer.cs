using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;

namespace IngameScript
{
    partial class Program
    {
        class Trailer
        {
            private IMyMotorAdvancedStator ForwardHitch, RearHitch;
            private IMyCubeGrid Grid;
            public String Name;
            public Trailer NextTrailer;
            private Program program;

            public Trailer(Program program, IMyMotorAdvancedStator forwardHitch)
            {
                this.ForwardHitch = forwardHitch;
                this.Grid = forwardHitch.CubeGrid;
                this.Name = forwardHitch.CustomName.Replace("Hinge Front","") + ' ' + forwardHitch.CubeGrid.CustomName;
                this.program = program;
            }

            public void setRearHitch(IMyMotorAdvancedStator hinge)
            {
                this.RearHitch = hinge;
            }

            public IMyCubeGrid GetGrid()
            {
                return this.Grid;
            }

            public bool DetectNextTrailer()
            {
                IMyCubeGrid NextGrid;
                foreach (var coupling in program.Couplings.Values)
                {
                    if (RearHitch.IsAttached && coupling.ContainsPart(RearHitch.Top))
                    {
                        NextGrid = coupling.getOtherGrid(this.Grid);
                        if (null != NextGrid)
                        {
                            NextTrailer = program.Trailers[NextGrid];
                            return true;
                        }
                    }
                }
                return false;
            }
        }
    }
}
