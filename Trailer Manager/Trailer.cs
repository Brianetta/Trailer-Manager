using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using SpaceEngineers.Game.ModAPI.Ingame;
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
            private IMyShipController controller;
            private IMyTimerBlock StowTimer, DeployTimer;
            private List<IMyTimerBlock> Timers = new List<IMyTimerBlock>();

            internal List<MenuItem> Menu = new List<MenuItem>();

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
            public void AddController(IMyShipController controller)
            {
                this.controller = controller;
            }
            public void AddTimer(IMyTimerBlock timer, TimerTask task = TimerTask.Menu)
            {
                // Pack is used to flag when a timer is for stowing (packing) or deploying (unpacking)
                if (task == TimerTask.Stow)
                    StowTimer = timer;
                else if (task == TimerTask.Deploy)
                    DeployTimer = timer;
                else Timers.Add(timer);
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
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Green, Message = "O2/H2 Gen On", Sprite = "MyObjectBuilder_Ore/Ice", duration = 4 });
            }
            public void GeneratorsOff()
            {
                foreach (var gen in HGens)
                    gen.Enabled = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "O2/H2 Gen Off", Sprite = "MyObjectBuilder_Ore/Ice", duration = 4 });
            }
            public void WheelsOff()
            {
                foreach (var Wheel in Wheels)
                    Wheel.Enabled = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Blue, Message = "Wheels powered off", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds", duration = 4 });
            }
            public void HandbrakeOn()
            {
                if (null != controller)
                    controller.HandBrake = true;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Green, Message = "Handbrake engaged", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds", duration = 4 });
            }
            public void HandbrakeOff()
            {
                if (null != controller)
                    controller.HandBrake =false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "Handbrake disengaged", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds", duration = 4 });
            }
            public void Stow()
            {
                StowTimer.Trigger();
                if (null != controller)
                    controller.HandBrake = true;
                ForwardHitch.RotorLock = true;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "Trailer Stowed", Sprite = "Arrow", duration = 4 });
            }
            public void Deploy()
            {
                DeployTimer.Trigger();
                if (null != controller)
                    controller.HandBrake = false;
                ForwardHitch.RotorLock = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.GreenYellow, Message = "Trailer Deployed", Sprite = "Arrow",SpriteRotation = (float)Math.PI, duration = 4 });
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

            public void BuildMenu(Action BackMenuAction)
            {
                Menu.Clear();
                Menu.Add(new MenuItem() { MenuText = Name, TextColor = Color.White, Sprite = "AH_PullUp", SpriteColor = Color.White, SpriteRotation = (float)(1.5f * Math.PI), Action = BackMenuAction });
                if (Batteries.Count > 0)
                {
                    Menu.Add(new MenuItem() { MenuText = "Unpack / Deploy", Sprite = "Arrow", SpriteRotation = (float)Math.PI, TextColor = Color.Gray, SpriteColor = Color.Green, Action = Deploy });
                    Menu.Add(new MenuItem() { MenuText = "Pack / Stow for travel", Sprite = "Arrow", TextColor = Color.Gray, SpriteColor = Color.YellowGreen, Action = Stow });
                    Menu.Add(new MenuItem() { MenuText = "Batteries recharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Yellow, Action = () => SetBatteryChargeMode(ChargeMode.Recharge) });
                    Menu.Add(new MenuItem() { MenuText = "Batteries auto", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Green, Action = () => SetBatteryChargeMode(ChargeMode.Auto) });
                    Menu.Add(new MenuItem() { MenuText = "Batteries discharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Cyan, Action = () => SetBatteryChargeMode(ChargeMode.Discharge) });
                    Menu.Add(new MenuItem() { MenuText = "Batteries off", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.DarkRed, Action = DisableBattery });
                }
            }
        }
    }
}
