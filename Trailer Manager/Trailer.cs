using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRageMath;

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
            private List<IMyPowerProducer> Engines = new List<IMyPowerProducer>();
            private List<IMyGasTank> HTanks = new List<IMyGasTank>();
            private List<IMyGasGenerator> HGens = new List<IMyGasGenerator>();

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
            public void AddWheel(IMyMotorSuspension wheel)
            {
                Wheels.Add(wheel);
            }
            public void AddEngine(IMyPowerProducer engine)
            {
                Engines.Add(engine);
            }
            public void AddHTank(IMyGasTank tank)
            {
                HTanks.Add(tank);
            }
            public void AddHGen(IMyGasGenerator generator)
            {
                HGens.Add(generator );
            }

            public void SetBatteryChargeMode(ChargeMode chargeMode)
            {
                foreach (var battery in Batteries)
                {
                    battery.Enabled = true;
                    battery.ChargeMode = chargeMode;
                }
                Color ChargeColor = Color.Green;
                if (chargeMode == ChargeMode.Recharge)
                    ChargeColor = Color.Yellow;
                if (chargeMode == ChargeMode.Discharge)
                    ChargeColor = Color.Cyan;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = ChargeColor, Message = chargeMode.ToString(), Sprite = "IconEnergy", duration = 4 });
            }
            public void DisableBattery()
            {
                foreach (var battery in Batteries)
                {
                    battery.Enabled = false;
                }
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.DarkRed, Message = "Batteries off", Sprite = "IconEnergy", duration = 4 });
            }

            public void HydrogenTankStockpileOn()
            {
                foreach (var tank in HTanks)
                    tank.Stockpile = true;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Cyan, Message = "Stockpile H On", Sprite = "IconHydrogen", duration = 4 });
            }
            public void HydrogenTankStockpileOff()
            {
                foreach (var tank in HTanks)
                    tank.Stockpile = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Green, Message = "Stockpile H Off", Sprite = "IconHydrogen", duration = 4 });
            }

            public void EnginesOn()
            {
                foreach (var engine in Engines)
                    engine.Enabled = true;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Green, Message = "Engines On", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds", duration = 4 });
            }
            public void EnginesOff()
            {
                foreach (var engine in Engines)
                    engine.Enabled = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "Engines Off", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds", duration = 4 });
            }

            public void GeneratorsOn()
            {
                foreach (var gen in HGens)
                    gen.Enabled = true;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Green, Message = "Engines On", Sprite = "MyObjectBuilder_Ore/Ice", duration = 4 });
            }
            public void GeneratorsOff()
            {
                foreach (var gen in HGens)
                    gen.Enabled = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "Engines Off", Sprite = "MyObjectBuilder_Ore/Ice", duration = 4 });
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
