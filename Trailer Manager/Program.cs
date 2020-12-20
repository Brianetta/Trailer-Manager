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
        enum MenuOption {Top, AllTrailers, Trailer, Config };
        MenuOption SelectedMenu = MenuOption.Top;
        List<MenuItem> AllTrailersMenu = new List<MenuItem>();
        List<MenuItem> TrailerMenu = new List<MenuItem>();
        List<MenuItem> ConfigurationMenu = new List<MenuItem>();

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
            return resourceSink == null ? false : resourceSink.AcceptedResources.Contains(_hydrogenGasId);
        }

        struct MenuItem
        {
            public String Sprite;
            public float SpriteRotation;
            public Color SpriteColor;
            public Color TextColor;
            public String MenuText;
            public Action Action;
        }

        struct Feedback
        {
            public String Message;
            public String Sprite;
            public float SpriteRotation;
            public Color BackgroundColor;
            public Color TextColor;
            public int duration;
        }

        public void AllTrailersBatteryCharge(ChargeMode chargeMode)
        {
            foreach (var trailer in Consist)
            {

                trailer.SetBatteryChargeMode(chargeMode);
            }
        }
        public void AllTrailersDisableBattery()
        {
            foreach (var trailer in Consist)
            {
                trailer.DisableBattery();
            }
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

        private void LegacyUpdate()
        {
            GridTerminalSystem.GetBlocksOfType(Blocks, block => block.IsSameConstructAs(Me) && ((block is IMyMotorAdvancedStator) || (block is IMyTimerBlock)));
            Hinges = Blocks.OfType<IMyMotorAdvancedStator>().ToList();
            List<IMyTimerBlock> Timers = Blocks.OfType<IMyTimerBlock>().ToList();
            foreach (var hinge in Hinges)
            {
                bool rear = (hinge.CustomName.Contains(" Hinge Rear"));
                hinge.CustomData = "[" + Section + "]\nrear=" + rear.ToString() + (rear ? "" : "\nname=" + hinge.CubeGrid.CustomName);
                Echo(hinge.CustomName);
            }
            foreach (var timer in Timers)
            {
                if (timer.CustomName.Contains(" Pack"))
                    timer.CustomData = "[" + Section + "]\ntask=stow";
                if (timer.CustomName.Contains(" Unpack"))
                    timer.CustomData = "[" + Section + "]\ntask=deploy";
                Echo(timer.CustomName);
            }
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
            FirstTrailer = null;
            // First iteration finds front hinges (by name) and creates the Trailer instances for them
            foreach (var hinge in Hinges.ToList())
            {
                // Only the first one found. If there's more than one, that's user error, but unlikely to matter.
                if (!GridsFound.Contains(hinge.CubeGrid) && ((MyIni.HasSection(hinge.CustomData,Section) && ini.TryParse(hinge.CustomData) && !ini.Get(Section, "rear").ToBoolean())))
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
                if ((ini.TryParse(hinge.CustomData) && ini.Get(Section, "hitch").ToBoolean()) || GridsFound.Contains(hinge.CubeGrid))
                {
                    Trailers[hinge.CubeGrid].SetRearHitch(hinge);
                    if (hinge.IsAttached)
                        HingeParts.Add(hinge.Top);
                }
                else if (hinge.CubeGrid.Equals(Me.CubeGrid))
                {
                    if (hinge.IsAttached)
                        HingeParts.Add(hinge.Top);
                }
            }

            // Get a list of all the batteries in each trailer
            foreach(var Battery in Blocks.OfType<IMyBatteryBlock>().ToList())
            {
                if(Trailers.ContainsKey(Battery.CubeGrid))
                    Trailers[Battery.CubeGrid].AddBattery(Battery);
            }
            // Get a list of all the wheel suspensions in each trailer
            foreach (var Wheel in Blocks.OfType<IMyMotorSuspension>().ToList())
            {
                if (Trailers.ContainsKey(Wheel.CubeGrid))
                    Trailers[Wheel.CubeGrid].AddWheel(Wheel);
            }
            // Get a list of all the hydrogen engines in each trailer
            foreach (var Engine in Blocks.OfType<IMyPowerProducer>().ToList())
            {
                if (Trailers.ContainsKey(Engine.CubeGrid) && IsHydrogenEngine(Engine))
                    Trailers[Engine.CubeGrid].AddEngine(Engine);
            }
            // Get a list of all the hydrogen tanks in each trailer
            foreach (var Tank in Blocks.OfType<IMyGasTank>().ToList())
            {
                if (Trailers.ContainsKey(Tank.CubeGrid) && IsHydrogenTank(Tank))
                    Trailers[Tank.CubeGrid].AddHTank(Tank);
            }           
            // Get a list of all the O2/H2 generators in each trailer
            foreach (var Gen in Blocks.OfType<IMyGasGenerator>().ToList())
            {
                if (Trailers.ContainsKey(Gen.CubeGrid))
                    Trailers[Gen.CubeGrid].AddHGen(Gen);
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
            foreach (var grid in Couplings.Keys.ToList())
            {
                if (!Couplings[grid].HasTwoParts())
                    Couplings.Remove(grid);
            }
            // Hook up the first coupling to our tractor's tow hitch...
            IMyCubeGrid NextGrid;
            foreach (var coupling in Couplings.Values)
            {
                NextGrid = coupling.GetOtherGrid(Me.CubeGrid);
                if (null != NextGrid)
                {
                    FirstTrailer = Trailers[NextGrid];
                    break;
                }
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
                Echo(trailer.Name);
                Consist.Add(trailer);
                trailer = trailer.NextTrailer;
            }
        }

        public Program()
        {
            // BuildConsist populates Blocks, so we run that first.

            BuildConsist();
            FindDisplays();
            ArrangeTrailersIntoTrain(FirstTrailer);

            AllTrailersMenu.Add(new MenuItem() { MenuText = "All batteries recharge", TextColor=Color.Gray, Sprite = "IconEnergy",SpriteColor=Color.Yellow, Action = () => AllTrailersBatteryCharge(ChargeMode.Recharge)});
            AllTrailersMenu.Add(new MenuItem() { MenuText = "All batteries auto", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Green, Action = () => AllTrailersBatteryCharge(ChargeMode.Auto)});
            AllTrailersMenu.Add(new MenuItem() { MenuText = "All batteries discharge", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.Cyan, Action = () => AllTrailersBatteryCharge(ChargeMode.Discharge)});
            AllTrailersMenu.Add(new MenuItem() { MenuText = "All batteries off", TextColor = Color.Gray, Sprite = "IconEnergy", SpriteColor = Color.DarkRed, Action = AllTrailersDisableBattery });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Engines on", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersEnginesOn, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Engines off", TextColor = Color.Gray, SpriteColor = Color.Red, Action = AllTrailersEnginesOff, Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_27.dds" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "H Tank Stockpile on", TextColor = Color.Gray, SpriteColor = Color.Cyan, Action = AllTrailersHydrogenStockpileOn, Sprite = "MyObjectBuilder_GasContainerObject/HydrogenBottle" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "H Tank Stockpile off", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersHydrogenStockpileOff, Sprite = "MyObjectBuilder_GasContainerObject/HydrogenBottle" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Generators on", TextColor = Color.Gray, SpriteColor = Color.Green, Action = AllTrailersGasGeneratorsOn, Sprite = "MyObjectBuilder_Ore/Ice" });
            AllTrailersMenu.Add(new MenuItem() { MenuText = "Generators off", TextColor = Color.Gray, SpriteColor = Color.Red, Action = AllTrailersGasGeneratorsOff, Sprite = "MyObjectBuilder_Ore/Ice" });

            foreach (var display in Displays)
            {
                display.RenderTopMenu(Consist, selectedline, selectedtrailer);
            }

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private void FindDisplays()
        {
            foreach (IMyTerminalBlock TextSurfaceProvider in Blocks.OfType<IMyTextSurfaceProvider>())
            {
                if (((IMyTextSurfaceProvider)TextSurfaceProvider).SurfaceCount > 0 && (MyIni.HasSection(TextSurfaceProvider.CustomData, Section)))
                {
                    ini.TryParse(TextSurfaceProvider.CustomData);
                    var displayNumber = ini.Get(Section, "display").ToUInt16();
                    if (displayNumber < ((IMyTextSurfaceProvider)TextSurfaceProvider).SurfaceCount)
                    {
                        var display = ((IMyTextSurfaceProvider)TextSurfaceProvider).GetSurface(ini.Get(Section, "display").ToInt16());
                        Displays.Add(new ManagedDisplay(display));
                    }
                    else
                    {
                        Echo("Warning: " + TextSurfaceProvider.CustomName + " doesn't have a display number " + ini.Get(Section, "display").ToString());
                    }
                }
            }
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void RenderTopMenu()
        {
            // The main menu
            foreach (var display in Displays)
            {
                display.RenderTopMenu(Consist, selectedline, selectedtrailer);
            }
        }

        public void RenderAllTrailersMenu()
        {
            // Menu with functions for all trailers
            foreach (var display in Displays)
            {
                display.RenderAllTrailersMenu(Consist, selectedline, AllTrailersMenu);
            }
        }

        public void RenderTrailerMenu()
        {
            // Menu specific to a trailer
        }

        public void RenderConfigurationMenu()
        {
            // The config menu
            foreach (var display in Displays)
            {
                //display.RenderConfigurationMenu(Train, selectedline, AllTrailersMenu);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if((updateSource & (UpdateType.Terminal | UpdateType.Trigger))!= 0)
            {
                if (argument == "LegacyUpdate")
                { 
                    LegacyUpdate();
                    BuildConsist();
                    ArrangeTrailersIntoTrain(FirstTrailer);
                }
                switch (argument.ToLower())
                {
                    case "rebuild":
                        BuildConsist();
                        ArrangeTrailersIntoTrain(FirstTrailer);
                        ManagedDisplay.SetFeedback(new Feedback { BackgroundColor = Color.DarkCyan, TextColor = Color.White, Message = "Rebuilding Train", Sprite = "Screen_LoadingBar", duration = 4 });
                        break;
                    case "up":
                        if (selectedline > 0) --selectedline;
                        break;
                    case "down":
                        if (SelectedMenu == MenuOption.Top)
                            if (selectedline < Consist.Count + 1) ++selectedline;
                        if (SelectedMenu == MenuOption.AllTrailers)
                            if (selectedline < AllTrailersMenu.Count) ++selectedline;
                        break;
                    case "apply":
                        switch (SelectedMenu)
                        {
                            case MenuOption.Top:
                                if (selectedline == 0)
                                    SelectedMenu = MenuOption.AllTrailers;
                                else if (selectedline > Consist.Count)
                                    SelectedMenu = MenuOption.Trailer;
                                else
                                    selectedtrailer = Consist[selectedline];
                                selectedline = 0;
                                break;
                            case MenuOption.AllTrailers:
                                Echo(selectedline.ToString());
                                if (selectedline == 0)
                                    SelectedMenu = MenuOption.Top;
                                else
                                    AllTrailersMenu[selectedline - 1].Action();
                                break;
                            case MenuOption.Trailer:
                                if (selectedline == 0)
                                {
                                    SelectedMenu = MenuOption.Top;
                                    if (Consist.Contains(selectedtrailer))
                                        selectedline = Consist.IndexOf(selectedtrailer);
                                }
                                break;
                            case MenuOption.Config:
                                selectedline = Consist.Count + 1;
                                SelectedMenu = MenuOption.Top;
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

            if((updateSource & (UpdateType.Update10))!= 0)
            {
                ManagedDisplay.FeedbackTick();
            }

            switch (SelectedMenu)
            {
                case MenuOption.Top:
                    RenderTopMenu();
                    break;
                case MenuOption.AllTrailers:
                    RenderAllTrailersMenu();
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
    }
}
