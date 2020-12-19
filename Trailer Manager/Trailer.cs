using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program
    {
        class Trailer
        {
            private IMyMotorAdvancedStator ForwardHitch, RearHitch;
            private IMyCubeGrid Grid;
            public string Name;
            public Trailer NextTrailer;
            private Program program;
            private List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
            private List<IMyMotorSuspension> Wheels = new List<IMyMotorSuspension>();

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
                this.ForwardHitch = forwardHitch;
            }

            public void SetRearHitch(IMyMotorAdvancedStator hinge)
            {
                this.RearHitch = hinge;
            }

            public void AddBattery(IMyBatteryBlock battery)
            {
                Batteries.Add(battery);
            }

            public void SetBatteryChargeMode(ChargeMode chargeMode)
            {
                foreach (var battery in Batteries)
                {
                    battery.ChargeMode = chargeMode;
                }
            }
            public void EnableBattery()
            {
                foreach (var battery in Batteries)
                {
                    battery.Enabled = true;
                }
            }
            public void DisableBattery()
            {
                foreach (var battery in Batteries)
                {
                    battery.Enabled = false;
                }
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
