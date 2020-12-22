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
            private float StartHeight = 5f;
            private float HeadingHeight = 35f;
            private float LineHeight = 40f;
            private float BodyBeginsHeight = 65f; // StartHeight + HeadingHeight + 25;
            private float HeadingFontSize = 2.0f;
            private float RegularFontSize = 1.5f;
            private Vector2 Position;
            private Vector2 CursorDrawPosition;
            private int WindowSize;         // Number of lines shown on screen at once after heading
            private int WindowPosition = 0; // Number of lines scrolled away
            private int CursorMenuPosition; // Position of cursor within window
            static Program.Feedback Feedback;
            private float Scale;

            public ManagedDisplay(IMyTextSurface surface, float scale = 1.0f)
            {
                this.surface = surface;
                this.Scale = scale;

                // Scale everything!
                StartHeight *= scale;
                HeadingHeight *= scale;
                LineHeight *= scale;
                BodyBeginsHeight *= scale;
                HeadingFontSize *= scale;
                RegularFontSize *= scale;

                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;
                viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
                WindowSize = ((int)((viewport.Height - BodyBeginsHeight - 10 * scale) / LineHeight));
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

            private void AddHeading(int menuLength)
            {
                Position = new Vector2(viewport.Width / 2f - LineHeight, StartHeight) + viewport.Position;
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
                Position = new Vector2(viewport.Width - 2*LineHeight, LineHeight) + viewport.Position;
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "AH_BoreSight",
                    Color = (WindowPosition > 0) ? Color.OrangeRed : Color.Black.Alpha(0),
                    RotationOrScale = 1.5f * (float)Math.PI,
                    Size = new Vector2(LineHeight, LineHeight),
                    Position = Position,
                });
                Position += new Vector2(LineHeight, 0);
                frame.Add(new MySprite()
                {
                    Type = SpriteType.TEXTURE,
                    Data = "AH_BoreSight",
                    Color = (WindowPosition + WindowSize < menuLength) ? Color.OrangeRed : Color.Black.Alpha(0),
                    RotationOrScale = 0.5f * (float)Math.PI,
                    Size = new Vector2(LineHeight, LineHeight),
                    Position = Position,
                });
                Position = new Vector2(viewport.Width / 2f - LineHeight, StartHeight+HeadingHeight) + viewport.Position;
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

            private void AddMenuItem(MenuItem menuItem)
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
                float SpriteOffset = 25f * Scale;
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

            internal void RenderMenu(int selectedline, List<MenuItem> menuItems)
            {
                SetWindowPosition(selectedline);
                frame = surface.DrawFrame();
                CursorDrawPosition = new Vector2(0, BodyBeginsHeight + LineHeight + LineHeight * CursorMenuPosition) + viewport.Position;
                DrawCursor();
                AddHeading(menuItems.Count);
                Position.X = surface.TextPadding;
                int renderLineCount = 0;
                foreach (var menuItem in menuItems)
                {
                    if (renderLineCount >= WindowPosition  && renderLineCount < WindowPosition + WindowSize)
                        AddMenuItem(menuItem);
                    ++renderLineCount;
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
                if (CursorMenuPosition >= WindowSize)
                {
                    CursorMenuPosition = WindowSize - 1;
                    WindowPosition = selectedline - (WindowSize - 1);
                }
            }
        }
    }
}
