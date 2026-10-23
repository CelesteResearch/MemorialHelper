// Copyright (c) 2020 Maildropfolder; 2022 Communal Helper Team. Licensed under the MIT License.

using System;
using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Content.Dialogs;
using Celeste.Graphics.Display.Colors;
using Celeste.Core.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.MemorialHelper.Entities.Text
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.MemorialHelper.ParallaxText")]
    [CustomEntity("MemorialHelper/ParallaxText")]
    public class ParallaxText : SceneEntityState
    {
        private static readonly Vector2 camCenterOffset = new(160f, 90f);
        private static readonly float camScalar = 6f;

        private readonly Vector2 centerPos;
        private readonly string[] text;
        private readonly Vector2 textCenterPos;
        private readonly Vector2 textCenterOffset;
        private readonly bool outline;
        private readonly bool noFlag;
        private readonly string flag;
        private readonly Vector2 halfSize;
        private readonly Vector2 zeroAlphaDistance;
        private readonly Vector2 parallaxScalar;
        private readonly Vector2 textScalar;
        private readonly PackedColor textColor;
        private readonly PackedColor borderColor;

        private readonly ISourceEngineContext engine;
        private readonly string playerRegistryKey;
        private readonly OriginalGameTextProcess textProcess;
        private float alpha;

        public ParallaxText(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            textProcess = ((IOriginalGameTextEnvironment)engine).Text;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value;

            Vector2 textSize = Vector2.Zero;
            textScalar = new Vector2(data.Float("textScalarX", 1.25f), data.Float("textScalarY", 1.25f));
            textColor = SourceHexColor.HexToColor(data.Attr("color", "ffffff"));
            borderColor = SourceHexColor.HexToColor(data.Attr("borderColor", "000000"));

            //The 'error text' is part of the SC2020 hunt. If you get it unexpectedly, it means
            //		that the text you placed has no 'dialog' attribute, likely an ahorn/lonn integration error
            text = data.Has("dialog")
                ? textProcess.Dialog.Clean(data.Attr("dialog", "app_ending"))
                    .Split(new char[] { '\n', '\r' }, StringSplitOptions.None)
                : new string[] { "Extract the zip", "Find 'gymBG'", "Then open it as", "a .txt" };
            foreach (string line in text)
            {
                Vector2 lineSize = textProcess.ActiveFont.Measure(line);
                textSize.Y += lineSize.Y * textScalar.Y;
            }

            textSize.X = 0; //textWidth * textScalar.X
            textCenterOffset = (textSize / 2) - (new Vector2(data.Float("offsetX"), data.Float("offsetY")) * camScalar);

            outline = data.Bool("border");

            halfSize = new Vector2(data.Width / 2, data.Height / 2);

            centerPos = halfSize + data.Position + offset;
            textCenterPos = data.Nodes.Length == 0 ? centerPos : data.Nodes[0] + offset;
            parallaxScalar = new Vector2(data.Float("parallaxX", 1.75f), data.Float("parallaxY", 1.75f));

            zeroAlphaDistance = new Vector2(Math.Max(0f, data.Float("visibleDistanceX", 32f)),
                Math.Max(0f, data.Float("visibleDistanceY", 32f))) + halfSize;

            flag = data.Attr("flag");
            noFlag = flag.Length == 0;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            Update();
        }

        public override void Update()
        {
            var players = ((OriginalLevelSceneRoot)Scene).Scheduler.Entities.GetTrackedEntities(playerRegistryKey);
            RoomPlayerSession entity = players.Count > 0 ? (RoomPlayerSession)players[0] : null;
            if (entity != null)
            {
                alpha = ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeInOut(
                    ScalarMath.ClampedMap(Math.Abs(entity.Center.X - centerPos.X), halfSize.X, zeroAlphaDistance.X, 1f,
                        0f) * ScalarMath.ClampedMap(Math.Abs(entity.Center.Y - centerPos.Y), halfSize.Y,
                        zeroAlphaDistance.Y, 1f, 0f));
            }

            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            if (noFlag || ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag))
            {
                var cam = ((OriginalLevelSceneRoot)Scene).Camera;

                //parallax equation
                Vector2 pos =
                    (camScalar * (Vector2.Multiply(parallaxScalar, textCenterPos - cam.Position - camCenterOffset) +
                                  camCenterOffset)) - textCenterOffset;

                if (((IOriginalGameplayProcess)engine).SaveContext.CurrentSave != null &&
                    ((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.MirrorMode)
                {
                    pos.X = 1920f - pos.X;
                }

                foreach (string line in text)
                {
                    if (outline)
                    {
                        textProcess.ActiveFont.DrawOutline(context, this, line, pos, new Vector2(0.5f, 0f), textScalar,
                            textColor * alpha, 2f, borderColor * (alpha * alpha));
                    }
                    else
                    {
                        textProcess.ActiveFont.Draw(context, this, line, pos, new Vector2(0.5f, 0f), textScalar,
                            textColor * alpha);
                    }

                    pos += textProcess.ActiveFont.Measure(line).Y * new Vector2(0, textScalar.Y);
                }
            }
        }
    }
}
