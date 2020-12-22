using Sandbox.ModAPI.Ingame;
using System;
using VRage.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;
using Sandbox.Game.Entities;

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
            private List<IMyTimerBlock> Timers = new List<IMyTimerBlock>();
            private List<IMyUserControllableGun> Weapons = new List<IMyUserControllableGun>();
            private IMyShipController controller;
            private IMyTimerBlock StowTimer, DeployTimer;

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

            public void ClearLists()
            {
                Batteries.Clear();
                Wheels.Clear();
                Engines.Clear();
                HTanks.Clear();
                HGens.Clear();
                Timers.Clear();
                Weapons.Clear();
            }

            public bool IsCoupled()
            {
                return null != RearHitch && RearHitch.IsAttached;
            }

            public void SetRearHitch(IMyMotorAdvancedStator hinge)
            {
                this.RearHitch = hinge;
            }

            public void Attach()
            {
                if (null != RearHitch && !RearHitch.IsAttached)
                    RearHitch.Attach();
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
                HGens.Add(generator);
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
            public void AddWeapon(IMyUserControllableGun Weapon)
            {
                Weapons.Add(Weapon);
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
                    controller.HandBrake = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "Handbrake disengaged", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds", duration = 4 });
            }
            public void Deploy()
            {
                if (null != DeployTimer)
                {
                    // Put handbrakes and rotor lock on, assuming that the Timer will toggle these
                    if (null != controller)
                        controller.HandBrake = true;
                    ForwardHitch.RotorLock = true;
                    // Trigger the timer, which will run in the next frame
                    DeployTimer.Trigger();
                }
                else
                {
                    if (null != controller)
                        controller.HandBrake = false;
                    ForwardHitch.RotorLock = false;
                }
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.Yellow, Message = "Trailer Deployed", Sprite = "Arrow", duration = 4 });
            }
            public void Stow()
            {
                if (null != StowTimer)
                {
                    // Put handbrakes and rotor lock on, assuming that the Timer will toggle these
                    if (null != controller)
                        controller.HandBrake = true;
                    ForwardHitch.RotorLock = true;
                    // Trigger the timer, which will run in the next frame
                    StowTimer.Trigger();
                }
                else
                {
                    if (null != controller)
                        controller.HandBrake = false;
                    ForwardHitch.RotorLock = false;
                }
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Black, TextColor = Color.GreenYellow, Message = "Trailer Stowed", Sprite = "Arrow", SpriteRotation = (float)Math.PI, duration = 4 });
            }
            public void Detach()
            {
                Deploy();
                // Get the grid that's towing me
                IMyCubeGrid TowingGrid = program.Couplings[ForwardHitch.TopGrid].GetOtherGrid(Grid);

                // Get the thing that's towing me to detach me
                program.Couplings[ForwardHitch.TopGrid].GetOtherHinge(Grid).Detach();

                // Now remove me and all subsequent trailers from the Consist
                if (TowingGrid == program.Me.CubeGrid)
                {
                    // I'm being towed by the tractor vehicle
                    program.FirstTrailer = null;
                }
                else
                {
                    // I'm being towed by some trailer, let's find it
                    program.Trailers[program.Couplings[ForwardHitch.TopGrid].GetOtherGrid(Grid)].NextTrailer = null;
                }
                // Now whatever is towing me, has forgotten me. Rebuild the Consist.
                program.ArrangeTrailersIntoTrain(program.FirstTrailer);
                program.BuildTopMenu();
                program.ActivateTopMenu();
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Maroon, TextColor = Color.Yellow, Message = "Detached", Sprite = "Cross", duration = 4 });
            }
            public void WeaponsLive()
            {
                foreach (var Weapon in Weapons)
                    Weapon.Enabled = true;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.Maroon, TextColor = Color.Green, Message = "Weapons Live", Sprite = "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem", duration = 4 });
            }
            public void WeaponsSafe()
            {
                foreach (var Weapon in Weapons)
                    Weapon.Enabled = false;
                ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.SaddleBrown, TextColor = Color.Red, Message = "Weapons Safe", Sprite = "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem", duration = 4 });
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
                        if (null != NextGrid && program.Trailers.ContainsKey(NextGrid))
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
                Menu.Add(new MenuItem() { MenuText = "Unpack / Deploy", Sprite = "Arrow", SpriteColor = Color.Green, SpriteRotation = (float)Math.PI, TextColor = Color.Gray, Action = Deploy });
                Menu.Add(new MenuItem() { MenuText = "Pack / Stow for travel", Sprite = "Arrow", SpriteColor = Color.Green, TextColor = Color.Gray, Action = Stow });
                Menu.Add(new MenuItem() { MenuText = "Detach this trailer", Sprite = "Cross", SpriteColor = Color.Red, TextColor = Color.Gray, Action = Detach });
                if (null != RearHitch && !RearHitch.IsAttached)
                    Menu.Add(new MenuItem() { MenuText = "Attach another trailer", Sprite = "Textures\\FactionLogo\\Traders\\TraderIcon_2.dds", TextColor = Color.Gray, SpriteColor = Color.YellowGreen, Action = RearHitch.Attach });
                if (Batteries.Count > 0)
                {
                    Menu.Add(new MenuItem() { MenuText = "Batteries recharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Yellow, Action = () => SetBatteryChargeMode(ChargeMode.Recharge) });
                    Menu.Add(new MenuItem() { MenuText = "Batteries auto", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Green, Action = () => SetBatteryChargeMode(ChargeMode.Auto) });
                    Menu.Add(new MenuItem() { MenuText = "Batteries discharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Cyan, Action = () => SetBatteryChargeMode(ChargeMode.Discharge) });
                    Menu.Add(new MenuItem() { MenuText = "Batteries off", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.DarkRed, Action = DisableBattery });
                }
                if (Engines.Count > 0)
                {
                    Menu.Add(new MenuItem() { MenuText = "Engines on", TextColor = Color.Gray, SpriteColor = Color.Green, Action = EnginesOn, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
                    Menu.Add(new MenuItem() { MenuText = "Engines off", TextColor = Color.Gray, SpriteColor = Color.Red, Action = EnginesOff, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
                }
                if (HTanks.Count > 0)
                {
                    Menu.Add(new MenuItem() { MenuText = "H Tank Stockpile on", TextColor = Color.Gray, SpriteColor = Color.Cyan, Action = HydrogenTankStockpileOn, Sprite = "MyObjectBuilder_GasContainerObject/HydrogenBottle" });
                    Menu.Add(new MenuItem() { MenuText = "H Tank Stockpile off", TextColor = Color.Gray, SpriteColor = Color.Green, Action = HydrogenTankStockpileOff, Sprite = "MyObjectBuilder_GasContainerObject/HydrogenBottle" });
                }
                if (HGens.Count > 0)
                {
                    Menu.Add(new MenuItem() { MenuText = "Generators on", TextColor = Color.Gray, SpriteColor = Color.Green, Action = GeneratorsOn, Sprite = "MyObjectBuilder_Ore/Ice" });
                    Menu.Add(new MenuItem() { MenuText = "Generators off", TextColor = Color.Gray, SpriteColor = Color.Red, Action = GeneratorsOff, Sprite = "MyObjectBuilder_Ore/Ice" });
                }
                if (Timers.Count > 0)
                {
                    foreach (var Timer in Timers)
                    {
                        Menu.Add(new MenuItem() { MenuText = Timer.CustomName, TextColor = Color.Gray, SpriteColor = Color.Blue, Action = Timer.Trigger, Sprite = "Textures\\FactionLogo\\Builders\\BuilderIcon_1.dds" });
                    }
                }
                if (Weapons.Count > 0)
                {
                    Menu.Add(new MenuItem() { MenuText = "Weapons Live", TextColor = Color.Gray, SpriteColor = Color.Green, Action = WeaponsLive, Sprite = "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem" });
                    Menu.Add(new MenuItem() { MenuText = "Weapons Safe", TextColor = Color.Gray, SpriteColor = Color.Red, Action = WeaponsSafe, Sprite = "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem" });
                }
                if (null != controller)
                {
                    if (controller.HandBrake)
                        Menu.Add(new MenuItem() { MenuText = "Disengage Handbrake", TextColor = Color.Gray, SpriteColor = Color.Yellow, Action = HandbrakeOff, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds" });
                    else
                        Menu.Add(new MenuItem() { MenuText = "Engage Handbrake", TextColor = Color.Gray, SpriteColor = Color.Green, Action = HandbrakeOn, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds" });
                }
            }
        }
    }
}
