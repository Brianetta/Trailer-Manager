using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using System;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string Version = "1.0.5";
        List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
        List<IMyMotorAdvancedStator> Hinges;
        List<IMyAttachableTopBlock> HingeParts = new List<IMyAttachableTopBlock>();
        Dictionary<IMyCubeGrid, Trailer> Trailers = new Dictionary<IMyCubeGrid, Trailer>();
        Dictionary<IMyCubeGrid, Coupling> Couplings = new Dictionary<IMyCubeGrid, Coupling>();
        List<Trailer> Consist = new List<Trailer>();
        List<IMyCubeGrid> GridsFound = new List<IMyCubeGrid>();
        const string Section = "trailer";
        MyIni ini = new MyIni();
        Trailer FirstTrailer;
        List<ManagedDisplay> Displays = new List<ManagedDisplay>();
        int selectedline = 0; // Menu selection position
        Trailer selectedtrailer; // Selected trailer in menu (to recalculate selectedline in the event of a rebuild)
        enum MenuOption { Top, AllTrailers, AllBatteries, AllHydrogen, Trailer, Config };
        enum TimerTask { Menu, Stow, Deploy, Toggle };
        MenuOption SelectedMenu = MenuOption.Top;
        List<MenuItem> TopMenu = new List<MenuItem>();
        List<MenuItem> AllTrailersMenu = new List<MenuItem>();
        List<MenuItem> AllBatteriesMenu = new List<MenuItem>();
        List<MenuItem> AllHydrogenMenu = new List<MenuItem>();
        List<MenuItem> ConfigurationMenu = new List<MenuItem>();
        IMyMotorAdvancedStator TractorHitch;
        private bool UnidentifiedTrailer;

        // Config settings
        internal bool CfgAutoDeploy = true;
        internal bool CfgMirror = true;

        // Lists used for the mirror feature
        private List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
        private List<IMyPowerProducer> Engines = new List<IMyPowerProducer>();
        private List<IMyGasTank> HTanks = new List<IMyGasTank>();
        private List<IMyGasGenerator> HGens = new List<IMyGasGenerator>();
        private List<IMyUserControllableGun> Weapons = new List<IMyUserControllableGun>();
        private IMyShipController Controller;

        // Previous states for mirror feature (prevents continually applying changes)
        ChargeMode PreviousChargeMode = ChargeMode.Auto;
        bool PreviousBatteryEnabled = true;
        bool PreviousStockpile = false;
        bool PreviousGenerator = true;
        bool PreviousEngines = true;
        bool PreviousWeapons = false;
        bool PreviousHandbrake = false;

        public void ClearMirrorLists()
        {
            Batteries.Clear();
            Engines.Clear();
            HTanks.Clear();
            HGens.Clear();
            Weapons.Clear();
            Controller = null;
        }

        // Methods for identifying the hydrogen blocks which lack unique interfaces.
        // Many thanks to Vox Serico for these methods.

        readonly MyDefinitionId
            _hydrogenEngineId = MyDefinitionId.Parse("MyObjectBuilder_HydrogenEngine/"),
            _hydrogenGasId = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen"),
            _oxygenTankId = MyDefinitionId.Parse("MyObjectBuilder_OxygenTank/");

        bool IsHydrogenEngine(IMyTerminalBlock block)
        {
            return IsHydrogenEngine(block.BlockDefinition);
        }
        bool IsHydrogenEngine(MyDefinitionId blockId)
        {
            return blockId.TypeId == _hydrogenEngineId.TypeId;
        }
        bool IsHydrogenTank(IMyTerminalBlock block)
        {
            if (block.BlockDefinition.TypeId != _oxygenTankId.TypeId)
                return false;

            var resourceSink = block.Components.Get<MyResourceSinkComponent>();
            return resourceSink != null && resourceSink.AcceptedResources.Contains(_hydrogenGasId);
        }

        struct MenuItem
        {
            public string Sprite;
            public float SpriteRotation;
            public Color SpriteColor;
            public Color TextColor;
            public string MenuText;
            public Action Action;
        }

        struct Feedback
        {
            public string Message;
            public string Sprite;
            public float SpriteRotation;
            public Color BackgroundColor;
            public Color TextColor;
            public int duration;
        }

        public void AllTrailersBatteryCharge(ChargeMode chargeMode)
        {
            foreach (var trailer in Consist)
                trailer.SetBatteryChargeMode(chargeMode);
        }
        public void AllTrailersDisableBattery()
        {
            foreach (var trailer in Consist)
                trailer.DisableBattery();
        }
        public void AllTrailersHydrogenStockpileOn()
        {
            foreach (var trailer in Consist)
                trailer.HydrogenTankStockpileOn();
        }
        public void AllTrailersHydrogenStockpileOff()
        {
            foreach (var trailer in Consist)
                trailer.HydrogenTankStockpileOff();
        }
        public void AllTrailersEnginesOff()
        {
            foreach (var trailer in Consist)
                trailer.EnginesOff();
        }
        public void AllTrailersEnginesOn()
        {
            foreach (var trailer in Consist)
                trailer.EnginesOn();
        }
        public void AllTrailersGasGeneratorsOn()
        {
            foreach (var trailer in Consist)
                trailer.GeneratorsOn();
        }
        public void AllTrailersGasGeneratorsOff()
        {
            foreach (var trailer in Consist)
                trailer.GeneratorsOff();
        }
        public void AllTrailersWheelsOff()
        {
            foreach (var trailer in Consist)
                trailer.WheelsOff();
        }
        public void AllTrailersHandbrakeOn()
        {
            foreach (var trailer in Consist)
                trailer.HandbrakeOn();
        }
        public void AllTrailersHandbrakeOff()
        {
            foreach (var trailer in Consist)
                trailer.HandbrakeOff();
        }
        public void AllTrailersDeploy()
        {
            foreach (var trailer in Consist)
                trailer.Deploy();
        }
        public void AllTrailersStow()
        {
            foreach (var trailer in Consist)
                trailer.Stow();
        }
        public void DeployLastTrailer()
        {
            if (Consist.Count > 0) Consist[Consist.Count - 1].Deploy();
        }
        public void SwitchRearConnector()
        {
            if (Consist.Count > 0) Consist[Consist.Count - 1].SwitchConnector();
        }
        public void DetachLastTrailer()
        {
            if (Consist.Count > 0) Consist[Consist.Count - 1].Detach();
        }
        public void AttachLastTrailer()
        {
            if (Consist.Count > 0)
                Consist[Consist.Count - 1].Attach();
            else
                if (null != TractorHitch)
                    TractorHitch.Attach();
        }
        public void AllTrailersWeaponsLive()
        {
            foreach (var trailer in Consist)
                trailer.WeaponsLive();
        }
        public void AllTrailersWeaponsSafe()
        {
            foreach (var trailer in Consist)
                trailer.WeaponsSafe();
        }

        public bool? StateToMirror(IEnumerable<IMyFunctionalBlock> blocks)
        {
            bool? ReturnState = null;
            foreach (var block in blocks)
            {
                if (null == ReturnState)
                    ReturnState = block.Enabled;
                if (block.Enabled != ReturnState.Value)
                    return null;
            }
            return ReturnState;
        }

        public ChargeMode? ChargeToMirror(IEnumerable<IMyBatteryBlock> batteries)
        {
            ChargeMode? ReturnState = null;
            foreach (var battery in batteries)
            {
                if (null == ReturnState)
                    ReturnState = battery.ChargeMode;
                if (battery.ChargeMode != ReturnState)
                    return null;
            }
            return ReturnState;
        }

        public bool? StockpileToMirror(IEnumerable<IMyGasTank> tanks)
        {
            bool? ReturnState = null;
            foreach (var tank in tanks)
            {
                if (null == ReturnState)
                    ReturnState = tank.Stockpile;
                if (tank.Stockpile != ReturnState)
                    return null;
            }
            return ReturnState;
        }

        public void Mirror()
        {
            bool? enabledState = null;
            ChargeMode? chargeMode = null;
            enabledState = StateToMirror(Batteries);
            if (enabledState.HasValue)
            {
                if (!enabledState.Value && PreviousBatteryEnabled)
                {
                    AllTrailersDisableBattery();
                }
                chargeMode = ChargeToMirror(Batteries);
                if ((enabledState.Value && !PreviousBatteryEnabled) || (chargeMode.HasValue && chargeMode != PreviousChargeMode))
                {
                    AllTrailersBatteryCharge(chargeMode.Value);
                    PreviousChargeMode = chargeMode.Value;
                }
                PreviousBatteryEnabled = enabledState.Value;
            }
            enabledState = StateToMirror(Weapons);
            if (enabledState.HasValue)
            {
                if (enabledState.Value != PreviousWeapons)
                    if (enabledState.Value)
                        AllTrailersWeaponsLive();
                    else
                        AllTrailersWeaponsSafe();
                PreviousWeapons = enabledState.Value;
            }
            enabledState = StateToMirror(Engines);
            if (enabledState.HasValue)
            {
                if (enabledState.Value != PreviousEngines)
                    if (enabledState.Value)
                        AllTrailersEnginesOn();
                    else
                        AllTrailersEnginesOff();
                PreviousEngines = enabledState.Value;
            }
            enabledState = StateToMirror(HGens);
            if (enabledState.HasValue)
            {
                if (enabledState.Value != PreviousGenerator)
                    if (enabledState.Value)
                        AllTrailersGasGeneratorsOn();
                    else
                        AllTrailersGasGeneratorsOff();
                PreviousGenerator = enabledState.Value;
            }
            enabledState = StockpileToMirror(HTanks);
            if (enabledState.HasValue)
            {
                if (enabledState.Value != PreviousStockpile)
                    if (enabledState.Value)
                        AllTrailersHydrogenStockpileOn();
                    else
                        AllTrailersHydrogenStockpileOff();
                PreviousStockpile = enabledState.Value;
            }
            if (null != Controller)
            {
                if (Controller.HandBrake && !PreviousHandbrake)
                {
                    AllTrailersHandbrakeOn();
                    PreviousHandbrake = true;
                }
                else if (!Controller.HandBrake && PreviousHandbrake)
                {
                    AllTrailersHandbrakeOff();
                    PreviousHandbrake = false;
                }
            }
        }

        public void LegacyUpdate()
        {
            GridTerminalSystem.GetBlocksOfType(Blocks, block => block.IsSameConstructAs(Me) && ((block is IMyMotorAdvancedStator) || (block is IMyTimerBlock)));
            Hinges = Blocks.OfType<IMyMotorAdvancedStator>().ToList();
            List<IMyTimerBlock> Timers = Blocks.OfType<IMyTimerBlock>().ToList();
            foreach (var hinge in Hinges)
            {
                ini.Clear();
                ini.TryParse(hinge.CustomData);
                if (!MyIni.HasSection(hinge.CustomData, Section))
                {
                    if (hinge.CubeGrid == Me.CubeGrid && hinge.CustomName.ToLower().Contains("hitch"))
                    {
                        this.TractorHitch = hinge;
                        ini.Set(Section, "hitch", true);
                    }
                    else if (hinge.CustomName.Contains("Solar")|| hinge.CustomName.Contains("Ramp"))
                    {
                        // Definitely want to ignore these guys
                        ;
                    }
                    else
                    {
                        // A new trailer has been identified, set a name and allow BuildAll() to run
                        if ((hinge.CustomName.ToLower().EndsWith("front") || hinge.CustomName.ToLower().EndsWith("steering")))
                        {
                            ini.Set(Section, "front", true);
                            ini.Set(Section, "name", hinge.CubeGrid.CustomName);
                            UnidentifiedTrailer = false;
                        } 
                        else if (hinge.CustomName.ToLower().EndsWith("rear"))
                        {
                            ini.Set(Section, "front", false);
                        }
                    }
                    hinge.CustomData = ini.ToString();
                }
            }
            foreach (var timer in Timers)
            {
                ini.Clear();
                ini.TryParse(timer.CustomData);
                if (!MyIni.HasSection(timer.CustomData, Section))
                {
                    if (timer.CustomName.ToLower().Contains("unpack"))
                        ini.Set(Section, "task", "deploy");
                    else if (timer.CustomName.ToLower().Contains("pack"))
                        ini.Set(Section, "task", "stow");
                    else if (timer.CustomName.ToLower().Contains("trailer") || timer.CustomName.ToLower().Contains("hook/unhook"))
                        ini.Set(Section, "task", "toggle");
                }
                timer.CustomData = ini.ToString();
            }
            BuildAll();
        }

        private void BuildConsist()
        {
            Trailers.Clear();
            Blocks.Clear();
            GridTerminalSystem.GetBlocksOfType(Blocks, block => block.IsSameConstructAs(Me));
            // Find all the hinges
            Hinges = Blocks.OfType<IMyMotorAdvancedStator>().ToList();
            GridsFound.Clear();
            HingeParts.Clear();
            ClearMirrorLists();
            FirstTrailer = null;
            // First iteration finds front hinges (by name) and creates the Trailer instances for them
            foreach (var hinge in Hinges)
            {
                // Only the first one found. If there's more than one, that's user error, but unlikely to matter.
                if (!GridsFound.Contains(hinge.CubeGrid) && ((MyIni.HasSection(hinge.CustomData, Section) && ini.TryParse(hinge.CustomData) && ini.Get(Section, "front").ToBoolean())))
                {
                    GridsFound.Add(hinge.CubeGrid);
                    Trailers.Add(hinge.CubeGrid, new Trailer(this, hinge));
                    Hinges.Remove(hinge);
                    if (hinge.IsAttached)
                        HingeParts.Add(hinge.Top);
                }
            }
            // Second iteration finds all of the hinges not matched in the first iteration
            foreach (var hinge in Hinges)
            {
                if (MyIni.HasSection(hinge.CustomData, Section) && ini.TryParse(hinge.CustomData))
                    if (ini.ContainsKey(Section,"front") && !ini.Get(Section, "front").ToBoolean() || GridsFound.Contains(hinge.CubeGrid))
                    {
                        if (Trailers.ContainsKey(hinge.CubeGrid))
                            Trailers[hinge.CubeGrid].SetRearHitch(hinge);
                        if (hinge.IsAttached)
                            HingeParts.Add(hinge.Top);
                        if (hinge.CubeGrid.Equals(Me.CubeGrid))
                            TractorHitch = hinge;
                    }
                    else if (hinge.CubeGrid.Equals(Me.CubeGrid) && ini.Get(Section, "hitch").ToBoolean())
                    {
                        TractorHitch = hinge;
                        if (hinge.IsAttached)
                            HingeParts.Add(hinge.Top);
                    }
            }

            foreach (var Trailer in Trailers.Values)
            {
                Trailer.ClearLists();
            }

            // Get a list of all the batteries in each trailer
            foreach (var Battery in Blocks.OfType<IMyBatteryBlock>())
            {
                if (Me.CubeGrid == Battery.CubeGrid)
                    Batteries.Add(Battery);
                else if (Trailers.ContainsKey(Battery.CubeGrid))
                    Trailers[Battery.CubeGrid].AddBattery(Battery);
            }
            // Get a list of all the wheel suspensions in each trailer
            foreach (var Wheel in Blocks.OfType<IMyMotorSuspension>())
            {
                if(Trailers.ContainsKey(Wheel.CubeGrid))
                    Trailers[Wheel.CubeGrid].AddWheel(Wheel);
            }
            // Get a list of all the hydrogen engines in each trailer
            foreach (var Engine in Blocks.OfType<IMyPowerProducer>())
            {
                if (IsHydrogenEngine(Engine))
                    if (Me.CubeGrid == Engine.CubeGrid)
                        Engines.Add(Engine);
                    else if (Trailers.ContainsKey(Engine.CubeGrid))
                        Trailers[Engine.CubeGrid].AddEngine(Engine);
            }
            // Get a list of all the hydrogen tanks in each trailer
            foreach (var Tank in Blocks.OfType<IMyGasTank>())
            {
                if (IsHydrogenTank(Tank))
                    if (Me.CubeGrid == Tank.CubeGrid)
                        HTanks.Add(Tank);
                    else if (Trailers.ContainsKey(Tank.CubeGrid))
                        Trailers[Tank.CubeGrid].AddHTank(Tank);
            }
            // Get a list of all the O2/H2 generators in each trailer
            foreach (var Gen in Blocks.OfType<IMyGasGenerator>())
            {
                if (Me.CubeGrid == Gen.CubeGrid)
                    HGens.Add(Gen);
                else if (Trailers.ContainsKey(Gen.CubeGrid))
                    Trailers[Gen.CubeGrid].AddHGen(Gen);
            }
            // Find all the weapons on a trailer
            foreach (var Weapon in Blocks.OfType<IMyUserControllableGun>())
            {
                if (Me.CubeGrid == Weapon.CubeGrid)
                    Weapons.Add(Weapon);
                else if (Trailers.ContainsKey(Weapon.CubeGrid))
                    Trailers[Weapon.CubeGrid].AddWeapon(Weapon);
            }
            // Find all the connectors on a trailer
            foreach (var Connector in Blocks.OfType<IMyShipConnector>())
            {
                if (Trailers.ContainsKey(Connector.CubeGrid))
                    Trailers[Connector.CubeGrid].AddConnector(Connector);
            }
            // Get a controller for the handbrake
            foreach (var Controller in Blocks.OfType<IMyShipController>())
            {
                if (Controller.CanControlShip)
                    if (Me.CubeGrid == Controller.CubeGrid)
                        this.Controller = Controller;
                    else if (Trailers.ContainsKey(Controller.CubeGrid))
                        Trailers[Controller.CubeGrid].AddController(Controller);
            }
            // Find all the timers on a trailer
            string taskname;
            foreach (var Timer in Blocks.OfType<IMyTimerBlock>())
            {
                if (Trailers.ContainsKey(Timer.CubeGrid))
                {
                    if (MyIni.HasSection(Timer.CustomData, Section) && ini.TryParse(Timer.CustomData))
                    {
                        taskname = ini.Get(Section, "task").ToString();
                        switch (taskname)
                        {
                            case "stow":
                            case "pack":
                                Trailers[Timer.CubeGrid].AddTimer(Timer, task:TimerTask.Stow);
                                break;
                            case "deploy":
                            case "unpack":
                                Trailers[Timer.CubeGrid].AddTimer(Timer, task:TimerTask.Deploy);
                                break;
                            case "toggle":
                                Trailers[Timer.CubeGrid].AddTimer(Timer, task:TimerTask.Toggle);
                                break;
                            default:
                                Trailers[Timer.CubeGrid].AddTimer(Timer, taskName: taskname.Length > 0 ? taskname : Timer.CustomName);
                                break;
                        }
                    }
                }
            }

            GridsFound.Clear();
            Couplings.Clear();
            // Find all grids with hinge parts on them (some of which will be all of the couplings)
            foreach (var part in HingeParts)
            {
                if (GridsFound.Contains(part.CubeGrid))
                {
                    // This is the second hinge part
                    Couplings[part.CubeGrid].AddPart(part);
                }
                else
                {
                    // This is the first hinge part
                    Couplings.Add(part.CubeGrid, new Coupling(part));
                    GridsFound.Add(part.CubeGrid);
                }
            }
            // Now weed out the grids where a second hinge/rotor part wasn't found
            // Yes, some might have rotor parts, but that's not a problem
            // Just want to exclude any that would give us null values.
            foreach (var grid in Couplings.Keys)
            {
                if (!Couplings[grid].HasTwoParts())
                    Couplings.Remove(grid);
            }
            // Hook up the first coupling to our tractor's tow hitch...
            IMyCubeGrid NextGrid;
            foreach (var coupling in Couplings.Values)
            {
                NextGrid = coupling.GetOtherGrid(Me.CubeGrid);
                if (null != NextGrid && Trailers.ContainsKey(NextGrid))
                    {
                    FirstTrailer = Trailers[NextGrid];
                    TractorHitch = (IMyMotorAdvancedStator)coupling.GetOtherHinge(NextGrid);
                    break;
                }
            }
            if (null == FirstTrailer && null != TractorHitch && TractorHitch.IsAttached)
            {
                ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Maroon, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Unsupported Trailer" });
            }
            // ...and connect the trailers to each other
            foreach (var trailer in Trailers.Values)
            {
                trailer.DetectNextTrailer();
            }
        }

        private void ArrangeTrailersIntoTrain(Trailer first)
        {
            Consist.Clear();
            var trailer = first;
            while (trailer != null)
            {
                Consist.Add(trailer);
                trailer = trailer.NextTrailer;
            }
        }

        private void ReadConfig()
        {
            ini.Clear();
            ini.TryParse(Me.CustomData);
            CfgAutoDeploy = ini.Get(Section,"autodeploy").ToBoolean();
            CfgMirror = ini.Get(Section,"mirror").ToBoolean();
        }

        private void WriteConfig()
        {
            ini.Clear();
            ini.TryParse(Me.CustomData);
            ini.Set(Section, "autodeploy", CfgAutoDeploy);
            ini.Set(Section, "mirror", CfgMirror);
            Me.CustomData = ini.ToString();
        }

        public Program()
        {
            ReadConfig();
            BuildAll();
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            Echo("Version "+Version);
        }

        private void ForceBuildAll()
        {
            UnidentifiedTrailer = false;
            ReadConfig();
            BuildAll();
        }

        private void BuildAll()
        {
            // If an unidentified trailer was found and hasn't been sorted, quit. This
            // isn't a cheap function.
            if (UnidentifiedTrailer) return;

            // BuildConsist populates Blocks, so we run that first.
            BuildConsist();
            FindDisplays();
            ArrangeTrailersIntoTrain(FirstTrailer);

            // Check whether we have something unidentified coupled to the end of our consist
            UnidentifiedTrailer = (Consist.Count > 0 && Consist[Consist.Count - 1].IsCoupled()) || (Consist.Count == 0 && null != TractorHitch && TractorHitch.IsAttached);

            BuildTopMenu();
            BuildAllTrailersMenu();
            BuildAllBatteriesMenu();
            BuildAllHydrogenMenu();
            BuildConfigurationMenu();

            RenderTopMenu();
            ActivateTopMenu();
        }

        public void ActivateAllBatteriesMenu()
        {
            SelectedMenu = MenuOption.AllBatteries;
            selectedline = 0;
        }
        public void ActivateAllHydrogenMenu()
        {
            SelectedMenu = MenuOption.AllHydrogen;
            selectedline = 0;
        }
        public void ActivateTopMenu()
        {
            if (SelectedMenu == MenuOption.Config)
                selectedline = TopMenu.Count - 1;
            else if (SelectedMenu == MenuOption.Trailer)
                selectedline = Consist.IndexOf(selectedtrailer) + 1;
            else
                selectedline = 0;
            SelectedMenu = MenuOption.Top;
        }
        public void ActivateAllTrailersMenu()
        {
            switch (SelectedMenu)
            {
                case MenuOption.AllBatteries:
                    selectedline = 6;
                    break;
                case MenuOption.AllHydrogen:
                    selectedline = 7;
                    break;
                default:
                    selectedline = 0;
                    break;
            }
            SelectedMenu = MenuOption.AllTrailers;
        }
        public void ActivateTrailerMenu()
        {
            SelectedMenu = MenuOption.Trailer;
            selectedtrailer = Consist[selectedline - 1];
            selectedline = 0;
        }
        public void ActivateConfigurationMenu()
        {
            SelectedMenu = MenuOption.Config;
            selectedline = 0;
        }

        private void BuildTopMenu()
        {
            TopMenu.Clear();
            TopMenu.Add(new MenuItem() { MenuText = "All Trailers...", TextColor = Color.White, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_20.dds", SpriteColor = Color.White, SpriteRotation = (float)(0.5f * Math.PI), Action = ActivateAllTrailersMenu });
            foreach (var trailer in Consist)
                TopMenu.Add(new MenuItem() { MenuText = trailer.Name, TextColor = Color.Gray, Sprite = "AH_BoreSight", SpriteColor = Color.White, Action = ActivateTrailerMenu });
            TopMenu.Add(new MenuItem() { MenuText = "Configuration...", TextColor = Color.White, Sprite = "Construction", SpriteColor = Color.White, Action = ActivateConfigurationMenu });
        }

        private void BuildAllBatteriesMenu()
        {
            AllBatteriesMenu.Clear();
            AllBatteriesMenu.Add(new MenuItem() { MenuText = "Back", TextColor = Color.Gray, Sprite = "AH_PullUp", SpriteColor = Color.White, SpriteRotation = (float)(1.5f * Math.PI), Action = ActivateAllTrailersMenu });
            AllBatteriesMenu.Add(new MenuItem() { MenuText = "All batteries recharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Yellow, Action = () => AllTrailersBatteryCharge(ChargeMode.Recharge) });
            AllBatteriesMenu.Add(new MenuItem() { MenuText = "All batteries auto", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Green, Action = () => AllTrailersBatteryCharge(ChargeMode.Auto) });
            AllBatteriesMenu.Add(new MenuItem() { MenuText = "All batteries discharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Cyan, Action = () => AllTrailersBatteryCharge(ChargeMode.Discharge) });
            AllBatteriesMenu.Add(new MenuItem() { MenuText = "All batteries off", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.DarkRed, Action = AllTrailersDisableBattery });
        }

        private void BuildAllTrailersMenu()
        {
            AllTrailersMenu.Clear();
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Back", TextColor = Color.Gray, Sprite = "AH_PullUp", SpriteColor = Color.White, SpriteRotation = (float)(1.5f * Math.PI), Action = ActivateTopMenu });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Pack all trailers", Sprite = "Arrow", TextColor = Color.Gray, SpriteColor = Color.YellowGreen, Action = AllTrailersStow });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Unpack rearmost trailer", Sprite = "Arrow", SpriteColor = Color.Green, SpriteRotation = (float)Math.PI, TextColor = Color.Gray, Action = DeployLastTrailer });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Detach rearmost trailer", Sprite = "Cross", SpriteColor = Color.Red, SpriteRotation = (float)Math.PI, TextColor = Color.Gray, Action = DetachLastTrailer });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Handbrake On", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersHandbrakeOn, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Handbrake Off", TextColor = Color.Gray, SpriteColor = Color.Yellow, Action = AllTrailersHandbrakeOff, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Batteries...", TextColor = Color.White, SpriteColor = Color.White, Action = ActivateAllBatteriesMenu, Sprite = "IconEnergy" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Hydrogen...", TextColor = Color.White, SpriteColor = Color.White, Action = ActivateAllHydrogenMenu, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Weapons Live", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersWeaponsLive, Sprite = "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Weapons Safe", TextColor = Color.Gray, SpriteColor = Color.Red, Action = AllTrailersWeaponsSafe, Sprite = "MyObjectBuilder_PhysicalGunObject/PreciseAutomaticRifleItem" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Switch Rear Connector", TextColor = Color.Gray, SpriteColor = Color.Yellow, Action = SwitchRearConnector, Sprite = "CircleHollow" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Unpack all trailers", Sprite = "Arrow", SpriteColor = Color.Green, SpriteRotation = (float)Math.PI, TextColor = Color.Gray, Action = AllTrailersDeploy });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Attach another trailer", Sprite = "Textures\\FactionLogo\\Traders\\TraderIcon_2.dds", TextColor = Color.Gray, SpriteColor = Color.YellowGreen, Action = AttachLastTrailer });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "De-power wheels", TextColor = Color.Gray, SpriteColor = Color.Red, Action = AllTrailersWheelsOff, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_22.dds" });
        }

        private void BuildAllHydrogenMenu()
        {
            AllHydrogenMenu.Clear();
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "Back", TextColor = Color.Gray, Sprite = "AH_PullUp", SpriteColor = Color.White, SpriteRotation = (float)(1.5f * Math.PI), Action = ActivateAllTrailersMenu });
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "Engines on", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersEnginesOn, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "Engines off", TextColor = Color.Gray, SpriteColor = Color.Red, Action = AllTrailersEnginesOff, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "H Tank Stockpile on", TextColor = Color.Gray, SpriteColor = Color.Cyan, Action = AllTrailersHydrogenStockpileOn, Sprite = "MyObjectBuilder_GasContainerObject/HydrogenBottle" });
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "H Tank Stockpile off", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersHydrogenStockpileOff, Sprite = "MyObjectBuilder_GasContainerObject/HydrogenBottle" });
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "Generators on", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersGasGeneratorsOn, Sprite = "MyObjectBuilder_Ore/Ice" });
            AllHydrogenMenu.Add(new MenuItem() { MenuText = "Generators off", TextColor = Color.Gray, SpriteColor = Color.Red, Action = AllTrailersGasGeneratorsOff, Sprite = "MyObjectBuilder_Ore/Ice" });
        }

        private void BuildConfigurationMenu()
        {
            ConfigurationMenu.Clear();
            ConfigurationMenu.Add(new MenuItem() { MenuText = "Back", TextColor = Color.Gray, Sprite = "AH_PullUp", SpriteColor = Color.White, SpriteRotation = (float)(1.5f * Math.PI), Action = ActivateTopMenu });
            ConfigurationMenu.Add(new MenuItem() { MenuText = "Toggle AutoDeploy", TextColor = CfgAutoDeploy ? Color.Gray : Color.DarkGray, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_33.dds", SpriteColor = CfgAutoDeploy?Color.Green:Color.Red, Action = ToggleAutoDeploy });
            ConfigurationMenu.Add(new MenuItem() { MenuText = "Toggle Mirroring", TextColor = CfgAutoDeploy ? Color.Gray : Color.DarkGray, Sprite = "Textures\\FactionLogo\\Traders\\TraderIcon_2.dds", SpriteColor = CfgMirror?Color.Green:Color.Red, Action = ToggleMirror });
            ConfigurationMenu.Add(new MenuItem() { MenuText = "Rebuild consist", TextColor = Color.Gray, Sprite = "Textures\\FactionLogo\\Builders\\BuilderIcon_16.dds", SpriteColor = Color.Cyan, Action = ForceBuildAll });
            ConfigurationMenu.Add(new MenuItem() { MenuText = "Detect trailer", TextColor = Color.Gray, Sprite = "Textures\\FactionLogo\\Builders\\BuilderIcon_1.dds", SpriteColor = Color.OrangeRed, Action = LegacyUpdate });
        }

        private void ToggleAutoDeploy()
        {
            CfgAutoDeploy = !CfgAutoDeploy;
            WriteConfig();
            BuildConfigurationMenu();
        }

        private void ToggleMirror()
        {
            CfgMirror = !CfgMirror;
            WriteConfig();
            BuildConfigurationMenu();
        }

        private void FindDisplays()
        {
            Displays.Clear();
            foreach (IMyTerminalBlock TextSurfaceProvider in Blocks.OfType<IMyTextSurfaceProvider>())
            {
                if (((IMyTextSurfaceProvider)TextSurfaceProvider).SurfaceCount > 0 && (MyIni.HasSection(TextSurfaceProvider.CustomData, Section)))
                {
                    ini.TryParse(TextSurfaceProvider.CustomData);
                    var displayNumber = ini.Get(Section, "display").ToUInt16();
                    if (displayNumber < ((IMyTextSurfaceProvider)TextSurfaceProvider).SurfaceCount || ((IMyTextSurfaceProvider)TextSurfaceProvider).SurfaceCount == 0)
                    {
                        var display = ((IMyTextSurfaceProvider)TextSurfaceProvider).GetSurface(ini.Get(Section, "display").ToInt16());
                        float scale = ini.Get(Section, "scale").ToSingle(1.0f);
                        Displays.Add(new ManagedDisplay(display, scale));
                    }
                    else
                    {
                        Echo("Warning: " + TextSurfaceProvider.CustomName + " doesn't have a display number " + ini.Get(Section, "display").ToString());
                    }
                }
            }
        }

        public void RenderTopMenu()
        {
            // The main menu
            foreach (var display in Displays)
                display.RenderMenu(selectedline, TopMenu);
        }

        public void RenderAllTrailersMenu()
        {
            // Menu with functions for all trailers
            foreach (var display in Displays)
                display.RenderMenu(selectedline, AllTrailersMenu);
        }

        public void RenderAllBatteriesMenu()
        {
            // Menu with battery charge functions for all trailers
            foreach (var display in Displays)
                display.RenderMenu(selectedline, AllBatteriesMenu);
        }

        public void RenderAllHydrogenMenu()
        {
            // Menu with battery charge functions for all trailers
            foreach (var display in Displays)
                display.RenderMenu(selectedline, AllHydrogenMenu);
        }

        public void RenderTrailerMenu()
        {
            // Menu specific to a trailer
            selectedtrailer.BuildMenu(ActivateTopMenu);
            foreach (var display in Displays)
                display.RenderMenu(selectedline, selectedtrailer.Menu);
        }

        public void RenderConfigurationMenu()
        {
            // The config menu
            foreach (var display in Displays)
                display.RenderMenu(selectedline, ConfigurationMenu);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
            {
                switch (argument.ToLower())
                {
                    case "legacyupdate":
                        LegacyUpdate();
                        break;
                    case "brakes on":
                        AllTrailersHandbrakeOn();
                        break;
                    case "brakes off":
                        AllTrailersHandbrakeOff();
                        break;
                    case "deploy":
                    case "unpack":
                        DeployLastTrailer();
                        break;
                    case "detach":
                        DetachLastTrailer();
                        break;
                    case "hitch":
                    case "attach":
                        AttachLastTrailer();
                        break;
                    case "connector":
                        SwitchRearConnector();
                        break;
                    case "allpack":
                        AllTrailersDeploy();
                        break;
                    case "weapons on":
                        AllTrailersWeaponsLive();
                        break;
                    case "weapons off":
                        AllTrailersWeaponsSafe();
                        break;
                    case "rebuild":
                        ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.DarkCyan, TextColor = Color.White, Message = "Rebuilding Train", Sprite = "Screen_LoadingBar", duration = 4 });
                        ForceBuildAll();
                        break;
                    case "up":
                        if (selectedline > 0)
                            --selectedline;
                        break;
                    case "down":
                        if (SelectedMenu == MenuOption.Top && selectedline < TopMenu.Count - 1)
                            ++selectedline;
                        if (SelectedMenu == MenuOption.AllTrailers && selectedline < AllTrailersMenu.Count - 1)
                            ++selectedline;
                        if (SelectedMenu == MenuOption.AllBatteries && selectedline < AllBatteriesMenu.Count - 1)
                            ++selectedline;
                        if (SelectedMenu == MenuOption.AllHydrogen && selectedline < AllHydrogenMenu.Count - 1)
                            ++selectedline;
                        if (SelectedMenu == MenuOption.Trailer && selectedline < selectedtrailer.Menu.Count - 1)
                            ++selectedline;
                        if (SelectedMenu == MenuOption.Config && selectedline < ConfigurationMenu.Count - 1)
                            ++selectedline;
                        break;
                    case "back":
                        if (SelectedMenu == MenuOption.AllBatteries || SelectedMenu == MenuOption.AllHydrogen)
                            ActivateAllTrailersMenu();
                        else
                            ActivateTopMenu();
                        break;
                    case "apply":
                    case "select":
                        switch (SelectedMenu)
                        {
                            case MenuOption.Top:
                                TopMenu[selectedline].Action();
                                break;
                            case MenuOption.AllTrailers:
                                AllTrailersMenu[selectedline].Action();
                                break;
                            case MenuOption.Trailer:
                                selectedtrailer.Menu[selectedline].Action();
                                break;
                            case MenuOption.Config:
                                ConfigurationMenu[selectedline].Action();
                                break;
                            case MenuOption.AllBatteries:
                                AllBatteriesMenu[selectedline].Action();
                                break;
                            case MenuOption.AllHydrogen:
                                AllHydrogenMenu[selectedline].Action();
                                break;
                            default:
                                selectedline = 0;
                                SelectedMenu = MenuOption.Top;
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            if ((updateSource & (UpdateType.Update10)) != 0)
            {
                ManagedDisplay.FeedbackTick();
                RefreshConsist();
            }

            switch (SelectedMenu)
            {
                case MenuOption.Top:
                    RenderTopMenu();
                    break;
                case MenuOption.AllTrailers:
                    RenderAllTrailersMenu();
                    break;
                case MenuOption.AllBatteries:
                    RenderAllBatteriesMenu();
                    break;
                case MenuOption.AllHydrogen:
                    RenderAllHydrogenMenu();
                    break;
                case MenuOption.Trailer:
                    RenderTrailerMenu();
                    break;
                case MenuOption.Config:
                    RenderConfigurationMenu();
                    break;
                default:
                    break;
            }
        }

        private void RefreshConsist()
        {
            // Deal with unexpectedly detached or attached first trailers
            if (null != TractorHitch)
                if (!TractorHitch.IsAttached && Consist.Count > 0)
                {
                    // Something was attached and now it isn't
                    FirstTrailer.Deploy();
                    FirstTrailer = null;
                    Consist.Clear();
                    BuildTopMenu();
                    ActivateTopMenu();
                    ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Red, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Trailer detached" });
                }
                else if (TractorHitch.IsAttached && Consist.Count == 0)
                {
                    // Nothing was attached, but now something is
                    BuildAll();
                    if (UnidentifiedTrailer)
                        ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Maroon, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Unknown trailer!" });
                    else
                        ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Green, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Trailer found" });
                }
            // Deal with unexpectedly detached or attached subsequent trailers
            for (int i = 0; i < Consist.Count - 1; ++i)
            {
                if (!Consist[i].IsCoupled())
                {
                    if (CfgAutoDeploy)
                        Consist[i].NextTrailer.Deploy();
                    Consist[i].NextTrailer = null;
                    ArrangeTrailersIntoTrain(FirstTrailer);
                    BuildTopMenu();
                    ActivateTopMenu();
                    ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Red, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Trailer detached" });
                }
            }
            if (Consist.Count > 0 && Consist[Consist.Count - 1].IsCoupled())
            {
                BuildAll();
                if (UnidentifiedTrailer)
                    ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Maroon, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Unknown trailer!" });
                else
                    ManagedDisplay.SetFeedback(new Feedback() { BackgroundColor = Color.Green, Sprite = "Danger", TextColor = Color.Yellow, duration = 8, Message = "Trailer found" });
            }
            // Mirror vehicle state in trailers
            if (CfgMirror)
                Mirror();
        }
    }
}
