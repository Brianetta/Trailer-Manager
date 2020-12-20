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
            static Program.Feedback Feedback;

            public ManagedDisplay(IMyTextSurface surface)
            {
                this.surface = surface;
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
                WindowSize = (((int)viewport.Height - BodyBeginsHeight - 10) / LineHeight) - 1;
            }

            public static void FeedbackTick()
            {
                --Feedback.duration;
            }

            public static void SetFeedback(Program.Feedback feedback)
            {
                Feedback = feedback;
            }

            private void ShowFeedback()
            {
                if (Feedback.duration > 0)
                {
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = "SquareSimple",
                        Position = new Vector2(0, viewport.Y + LineHeight),
                        Color = Feedback.BackgroundColor,
                        Size = new Vector2(viewport.Width, LineHeight * 2)
                    }); 
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXTURE,
                        Data = Feedback.Sprite,
                        Position = new Vector2(LineHeight, viewport.Y + LineHeight * 1.25f),
                        Color = Feedback.TextColor,
                        Alignment = TextAlignment.CENTER /* Center the text on the position */,
                        Size = new Vector2(LineHeight, LineHeight),
                        FontId = "White"
                    });
                    frame.Add(new MySprite()
                    {
                        Type = SpriteType.TEXT,
                        Data = Feedback.Message,
                        Position = new Vector2(2 * LineHeight, viewport.Y + HeadingHeight / 2),
                        RotationOrScale = HeadingFontSize,
                        Color = Feedback.TextColor,
                        Alignment = TextAlignment.LEFT /* Center the text on the position */,
                        FontId = "White"
                    });
                }
            }

            private void DrawCursor()
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

            private void AddHeading()
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

            private void AddBackMenu(string name = null)
            {
                AddMenuItem(menuText: "Back "+name, sprite: "AH_PullUp", spriteRotation: (float)(1.5f * Math.PI));
            }

            private void AddAllTrailersMenu(ref MySpriteDrawFrame frame, int trailerCount)
            {
                AddMenuItem(menuText: "All Trailers...", textColor: Color.White, sprite: "Textures\\FactionLogo\\Others\\OtherIcon_20.dds", spriteRotation: (float)(0.5f * Math.PI));
            }

            private void AddTrailerLine(Trailer trailer)
            {
                AddMenuItem(menuText: trailer.Name, sprite: "AH_BoreSight");
            }

            private void AddMenuItem(Program.MenuItem menuItem)
            {
                AddMenuItem(
                    menuText: menuItem.MenuText,
                    sprite: menuItem.Sprite,
                    spriteRotation: menuItem.SpriteRotation,
                    spriteColor: menuItem.SpriteColor,
                    textColor: menuItem.TextColor
                    );
            }

            private void AddMenuItem(string menuText, string sprite = "SquareSimple", float spriteRotation = 0, Color? spriteColor = null, Color? textColor = null)
            {
                const float SpriteOffset = 25f;
                Position += new Vector2(0, LineHeight);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = sprite,
                    Position = Position + new Vector2(0, SpriteOffset),
                    RotationOrScale = spriteRotation,
                    Size = new Vector2(LineHeight, LineHeight),
                    Color = spriteColor ?? Color.White,
                });
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = menuText,
                    Position = Position + new Vector2(LineHeight * 1.2f, 0),
                    RotationOrScale = RegularFontSize,
                    Color = textColor ?? Color.Gray,
                    Alignment = TextAlignment.LEFT,
                    FontId = "White"
                });
            }

            private void AddConfigurationMenu()
            {
                AddMenuItem(menuText: "Configuration", sprite: "Construction", textColor: Color.White);
            }
            internal void RenderTopMenu(List<Trailer> train, int selectedline, Trailer selectedtrailer)
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
                DrawCursor();
                AddHeading();
                Position.X=surface.TextPadding;
                int renderLineCount = 0;
                if(WindowPosition==renderLineCount)
                    AddAllTrailersMenu(ref frame, train.Count);
                foreach (var trailer in train)
                {
                    ++renderLineCount;
                    if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition + WindowSize)
                        AddTrailerLine(trailer);
                }
                ++renderLineCount;
                if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition+WindowSize)
                    AddConfigurationMenu();
                ShowFeedback();
                frame.Dispose();
            }

            internal void RenderSubMenu( int selectedline, List<Program.MenuItem> menuItems, MenuOption menuOption)
            {
                SetWindowPosition(selectedline);
                frame = surface.DrawFrame();
                CursorDrawPosition = new Vector2(0, BodyBeginsHeight + LineHeight + LineHeight * CursorMenuPosition) + viewport.Position;
                DrawCursor();
                AddHeading();
                Position.X = surface.TextPadding;
                int renderLineCount = 0;
                if (WindowPosition == renderLineCount)
                    AddBackMenu();
                foreach (var menuItem in menuItems)
                {
                    ++renderLineCount;
                    if (WindowPosition <= renderLineCount && renderLineCount <= WindowPosition + WindowSize)
                        AddMenuItem(menuItem);
                }
                ShowFeedback();
                frame.Dispose();
            }

            private void SetWindowPosition(int selectedline)
            {
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
            }
        }
    }
}
