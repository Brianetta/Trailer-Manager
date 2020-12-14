using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program
    {
        class Trailer
        {
            private IMyMotorAdvancedStator RearHitch;
            private IMyCubeGrid Grid;
            public String Name;
            public Trailer NextTrailer;
            private Program program;

            public Trailer(Program program, IMyMotorAdvancedStator forwardHitch)
            {
                this.Grid = forwardHitch.CubeGrid;
                this.program = program;
                if (program.ini.TryParse(forwardHitch.CustomData))
                {
                    this.Name = program.ini.Get(Program.Section, "name").ToString();
                    if (null == this.Name || this.Name.Length == 0)
                        this.Name = forwardHitch.CubeGrid.CustomName;
                }
                else
                {
                    this.Name = forwardHitch.CubeGrid.CustomName;
                }
            }

            public void SetRearHitch(IMyMotorAdvancedStator hinge)
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
                    if (null != RearHitch && RearHitch.IsAttached && coupling.ContainsPart(RearHitch.Top))
                    {
                        NextGrid = coupling.GetOtherGrid(this.Grid);
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
