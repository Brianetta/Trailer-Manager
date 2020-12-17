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
    partial class Program
    {
        class ManagedDisplay
        {
            private IMyTextSurface surface;
            private RectangleF viewport;
            private MySpriteDrawFrame frame;
            private const int StartHeight = 5;
            private const int HeadingHeight = 35;
            private const int LineHeight = 40;
            private const int BodyBeginsHeight = StartHeight + HeadingHeight + 25;
            private const float HeadingFontSize = 2.0f;
            private const float RegularFontSize = 1.5f;
            private Vector2 Position;
            private Vector2 CursorDrawPosition;
            private int WindowSize;         // Number of lines shown on screen at once after heading
            private int WindowPosition = 0; // Number of lines scrolled away
            private int CursorMenuPosition; // Position of cursor within window
            Program.MenuOption LastSelectedMenu = MenuOption.Top;

            public ManagedDisplay(IMyTextSurface surface)
            {
                this.surface = surface;
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
                WindowSize = (((int)viewport.Height - BodyBeginsHeight - 10) / LineHeight) - 1;
            }

            private void DrawCursor(ref MySpriteDrawFrame frame)
            {
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "SquareSimple",
                    Position = CursorDrawPosition,
                    Color = Color.OrangeRed,
                    Size = new Vector2(viewport.Width, LineHeight)
                });
            }

            private void AddHeading(ref MySpriteDrawFrame frame)
            {
                Position = new Vector2(viewport.Width / 2f, StartHeight) + viewport.Position;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Trailer Manager",
                    Position = Position,
                    RotationOrScale = HeadingFontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.CENTER /* Center the text on the position */,
                    FontId = "White"
                });
                Position += new Vector2(0, HeadingHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "----------------------------",
                    Position = Position,
                    RotationOrScale = RegularFontSize,
                    Color = Color.OrangeRed,
                    Alignment = TextAlignment.CENTER,
                    FontId = "White"
                });
            }

            private void AddBackMenu()
            {
                AddMenuItem(new MenuItem() { MenuText = "Back", Sprite = "AH_PullUp" ,SpriteRotation = (float)(1.5f*Math.PI)});
            }

            private void AddAllTrailersMenu(ref MySpriteDrawFrame frame, int trailerCount)
            {
                AddMenuItem(new MenuItem() { MenuText = "All Trailers", Sprite = "Textures\\FactionLogo\\Others\\OtherIcon_20.dds", SpriteRotation = (float)(0.5f*Math.PI)});
            }

            private void AddTrailerLine(Trailer trailer)
            {
                AddMenuItem(new MenuItem() { MenuText = trailer.Name, Sprite = "AH_BoreSight"});
            }

            private void AddMenuItem(Program.MenuItem menuItem)
            {
                const float SpriteOffset = 25f;
                Position += new Vector2(0, LineHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = menuItem.Sprite,
                    Position = Position + new Vector2(0, SpriteOffset),
                    RotationOrScale = menuItem.SpriteRotation,
                    Size = new Vector2(LineHeight, LineHeight),
                    Color = Color.White,
                });
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = menuItem.MenuText,
                    Position = Position + new Vector2(LineHeight * 1.2f, 0),
                    RotationOrScale = RegularFontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
            }

            private void AddConfigurationMenu()
            {
                AddMenuItem(new MenuItem() { MenuText = "Configuration", Sprite = "Construction" });
            }
            public void RenderTopMenu(List<Trailer> train, int selectedline, Trailer selectedtrailer)
            {
                if (LastSelectedMenu != MenuOption.Top)
                {
                    WindowPosition = 0;
                    LastSelectedMenu = MenuOption.Top;
                }
                frame = surface.DrawFrame();
                CursorMenuPosition = selectedline - WindowPosition;
                if (CursorMenuPosition < 0)
                {
                    CursorMenuPosition = 0;
                    WindowPosition = selectedline;
                }
                if (CursorMenuPosition > WindowSize)
                {
                    CursorMenuPosition = WindowSize;
                    WindowPosition = selectedline - WindowSize;
                }
                if (WindowPosition < 0) WindowPosition = 0;
                CursorDrawPosition = new Vector2(0, BodyBeginsHeight + LineHeight + LineHeight * CursorMenuPosition) + viewport.Position;
                DrawCursor(ref frame);
                AddHeading(ref frame);
                Position.X=surface.TextPadding;
                int renderLineCount = 0;
                if(WindowPosition==renderLineCount)
                    AddAllTrailersMenu(ref frame, train.Count);
                foreach (var trailer in train)
                {
                    ++renderLineCount;
                    if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition+WindowSize)
                        AddTrailerLine(trailer);
                }
                ++renderLineCount;
                if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition+WindowSize)
                    AddConfigurationMenu();
                frame.Dispose();
            }

            internal void RenderAllTrailersMenu(List<Trailer> train, int selectedline)
            {
                if (LastSelectedMenu != MenuOption.AllTrailers)
                {
                    WindowPosition = 0;
                    LastSelectedMenu = MenuOption.AllTrailers;
                    CursorMenuPosition = 0;
                }
                frame = surface.DrawFrame();
                CursorDrawPosition = new Vector2(0, BodyBeginsHeight + LineHeight + LineHeight * CursorMenuPosition) + viewport.Position;
                DrawCursor(ref frame);
                AddHeading(ref frame);
                Position.X = surface.TextPadding;
                AddBackMenu();
                frame.Dispose();
            }
        }
    }
}
