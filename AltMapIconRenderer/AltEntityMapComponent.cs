using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Text;

namespace AltMapIconRenderer;

public class AltEntityMapComponent
{
    protected static Vec2f viewPos = new Vec2f();
    protected static Matrixf outline = new Matrixf();

    public static bool Render(EntityMapComponent __instance, GuiElementMap map, float dt)
    {
        Traverse self = Traverse.Create(__instance);
        ICoreClientAPI capi = self.Field("capi").GetValue<ICoreClientAPI>();
        Entity entity = self.Field("entity").GetValue<Entity>();
        MeshRef quadModel = self.Field("quadModel").GetValue<MeshRef>();
        LoadedTexture texture = self.Field("Texture").GetValue<LoadedTexture>();
        Vec2f viewPos = self.Field("viewPos").GetValue<Vec2f>();
        Matrixf mvMat = self.Field("mvMat").GetValue<Matrixf>();
        var player = (entity as EntityPlayer)?.Player;

        if (player?.WorldData?.CurrentGameMode == EnumGameMode.Spectator == true)
            return false;

        if ((entity as EntityPlayer)?.Controls.Sneak == true && player != capi.World.Player)
            return false;

        map.TranslateWorldPosToViewPos(entity.Pos.XYZ, ref viewPos);

        if (AltMapIconRendererSystem.config.Get().pin_player_icons)
        {
            map.ClampButPreserveAngle(ref viewPos, 2);
            map.Api.Render.PushScissor(null);
        }
        else
        {
            if (viewPos.X < -10 || viewPos.Y < -10 || viewPos.X > map.Bounds.OuterWidth + 10 || viewPos.Y > map.Bounds.OuterHeight + 10)
                return false;
        }

        float x = (float)(map.Bounds.renderX + viewPos.X);
        float y = (float)(map.Bounds.renderY + viewPos.Y);
        float zoom = GameMath.Clamp(map.ZoomLevel, AltMapIconRendererSystem.zoomMin, AltMapIconRendererSystem.zoomMax);

        if (texture?.Disposed == true)
            throw new Exception("Fatal. Trying to render a disposed texture");
        if (quadModel?.Disposed == true)
            throw new Exception("Fatal. Trying to render a disposed mesh");

        capi.Render.GlToggleBlend(true);

        var prog = capi.Render.GetEngineShader(EnumShaderProgram.Gui);
        bool mouseOver = self.Field("mouseOver").GetValue<bool>();

        Vec4f outlineColor = mouseOver ? AltMapIconRendererSystem.rgbaHover : AltMapIconRendererSystem.rgbaOutline;
        Vec4f iconColor = mouseOver ? AltMapIconRendererSystem.rgbaHover : AltMapIconRendererSystem.playerColour;

        prog.Uniform("extraGlow", mouseOver ? 1 : 0);
        prog.Uniform("applyColor", mouseOver? 1 : 0);
        prog.Uniform("noTexture", 0f);
        prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
        prog.BindTexture2D("tex2d", texture.TextureId, 0);

        mvMat.Set(capi.Render.CurrentModelviewMatrix)
             .Translate(x, y, 60)
             .Scale(texture.Width * zoom, texture.Height * zoom, 0)
             .Scale(AltMapIconRendererSystem.playerScale, AltMapIconRendererSystem.playerScale, 0)
             .RotateZ(-entity.Pos.Yaw + 180 * GameMath.DEG2RAD);

        if (mouseOver)
        {
            float offset = AltMapIconRendererSystem.outlineOffset;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;

                    outline.Set(mvMat.Values).Translate(offset * ox, offset * oy, 0);
                    prog.Uniform("rgbaIn", AltMapIconRendererSystem.rgbaHover);
                    prog.UniformMatrix("modelViewMatrix", outline.Values);
                    capi.Render.RenderMesh(quadModel);
                }
            }
        }

        prog.Uniform("rgbaIn", iconColor);
        prog.UniformMatrix("modelViewMatrix", mvMat.Values);
        capi.Render.RenderMesh(quadModel);
        if (AltMapIconRendererSystem.config.Get().pin_player_icons)
        {
            map.Api.Render.PopScissor();
        }
        return false;
    }

    public static bool OnMouseMove(EntityMapComponent __instance, MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        var self = Traverse.Create(__instance);
        var entity = self.Field("entity").GetValue<Entity>();
        if (entity == null) return false;

        var capi = self.Field("capi").GetValue<ICoreClientAPI>();

        mapElem.TranslateWorldPosToViewPos(entity.Pos.XYZ, ref viewPos);

        if (AltMapIconRendererSystem.config.Get().pin_player_icons)
        {
            mapElem.ClampButPreserveAngle(ref viewPos, 2);
        }

        float x = (float)(mapElem.Bounds.renderX + viewPos.X);
        float y = (float)(mapElem.Bounds.renderY + viewPos.Y);

        float hoverSize = (float)GuiElement.scaled(AltMapIconRendererSystem.hoverSize) *
                          GameMath.Clamp(mapElem.ZoomLevel, AltMapIconRendererSystem.zoomMin, AltMapIconRendererSystem.zoomMax);

        double dx = args.X - x;
        double dy = args.Y - y;

        bool mouseOver = Math.Abs(dx) < hoverSize && Math.Abs(dy) < hoverSize;
        self.Field("mouseOver").SetValue(mouseOver);

        if (mouseOver && entity is EntityPlayer eplayer)
        {
            var pos = entity.Pos.AsBlockPos;
            var spawn = capi.World.DefaultSpawnPosition.AsBlockPos;
            string name = eplayer.Player?.PlayerName ?? "Player";
            hoverText.AppendLine(name);
            hoverText.AppendLine($"{pos.X - spawn.X}, {pos.Y - spawn.Y}, {pos.Z - spawn.Z}");
        }

        return false;
    }
}