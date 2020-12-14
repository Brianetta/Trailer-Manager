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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
        List<IMyMotorAdvancedStator> Hinges;
        List<IMyAttachableTopBlock> HingeParts = new List<IMyAttachableTopBlock>();
        Dictionary<IMyCubeGrid, Trailer> Trailers = new Dictionary<IMyCubeGrid, Trailer>();
        Dictionary<IMyCubeGrid, Coupling> Couplings = new Dictionary<IMyCubeGrid, Coupling>();
        List<IMyCubeGrid> GridsFound = new List<IMyCubeGrid>();
        const string Section = "trailer";
        MyIni ini = new MyIni();
        Trailer FirstTrailer;

        private void BuildConsist()
        {
            GridTerminalSystem.GetBlocksOfType(Blocks, block => block.IsSameConstructAs(Me));
            // Find all the hinges
            Hinges = Blocks.OfType<IMyMotorAdvancedStator>().ToList();
            GridsFound.Clear();
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

            GridsFound.Clear();
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
            foreach (Trailer trailer in Trailers.Values)
            {
                trailer.DetectNextTrailer();
            }
        }

        private void RecurseTrailers(Trailer trailer)
        {
            if (null == trailer)
            {
                Echo("End of train");
            }
            else
            {
                Echo(trailer.Name);
                RecurseTrailers(trailer.NextTrailer);
            }
        }

        public Program()
        {
            BuildConsist();
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

        public void Main(string argument, UpdateType updateSource)
        {
            RecurseTrailers(FirstTrailer);
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
        }
    }
}
