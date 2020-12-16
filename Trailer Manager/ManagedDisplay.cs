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
            private int RenderPosition = 0; // Current line being rendered in menu

            public String Debug()
            {
                return($"******\nWindowSize:{WindowSize}\nWindowPosition:{WindowPosition}\nCursorMenuPosition:{CursorMenuPosition}\nRenderPosition:{RenderPosition}");
            }

            public ManagedDisplay(IMyTextSurface surface)
            {
                this.surface = surface;
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
                WindowSize = (((int)viewport.Height - BodyBeginsHeight - 10) / LineHeight) - 1;
            }

            private void AddSelectionBar(ref MySpriteDrawFrame frame)
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

            private void AddBackMenu(ref MySpriteDrawFrame frame)
            {
                const float SpriteOffset = 25f;
                Position += new Vector2(0, LineHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "AH_PullUp",
                    Position = Position + new Vector2(0, SpriteOffset),
                    RotationOrScale = (float)(1.5f * Math.PI),
                    Size = new Vector2(LineHeight, LineHeight),
                    Color = Color.White,
                });
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Back",
                    Position = Position + new Vector2(LineHeight * 1.2f, 0),
                    RotationOrScale = RegularFontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
            }

            private void AddAllTrailersMenu(ref MySpriteDrawFrame frame, int trailerCount)
            {
                Position += new Vector2(0, LineHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "All Trailers (" + (trailerCount == 0 ? "none" : trailerCount.ToString()) + ")",
                    Position = Position,
                    RotationOrScale = RegularFontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
            }

            private void AddTrailerLine(ref MySpriteDrawFrame frame, Trailer trailer)
            {
                Position += new Vector2(0, LineHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = trailer.Name,
                    Position = Position,
                    RotationOrScale = RegularFontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
            }

            private void AddConfigurationMenu(ref MySpriteDrawFrame frame)
            {
                const float SpriteOffset = 25f;
                Position += new Vector2(0, LineHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "Construction",
                    Position = Position + new Vector2(0, SpriteOffset),
                    RotationOrScale = (float)(1.5f * Math.PI),
                    Size = new Vector2(LineHeight, LineHeight),
                    Color = Color.White,
                });
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "Configuration",
                    Position = Position + new Vector2(LineHeight * 1.2f, 0),
                    RotationOrScale = RegularFontSize,
                    Color = Color.White,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
            }
            public void Render(List<Trailer> train, int selectedline, Trailer selectedtrailer)
            {
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
                AddSelectionBar(ref frame);
                AddHeading(ref frame);
                Position.X=surface.TextPadding;
                int renderLineCount = 0;
                if(WindowPosition==renderLineCount)
                    AddAllTrailersMenu(ref frame, train.Count);
                foreach (var trailer in train)
                {
                    ++renderLineCount;
                    if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition+WindowSize)
                        AddTrailerLine(ref frame, trailer);
                }
                ++renderLineCount;
                if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition+WindowSize)
                    AddConfigurationMenu(ref frame);
                frame.Dispose();
            }

        }
    }
}
