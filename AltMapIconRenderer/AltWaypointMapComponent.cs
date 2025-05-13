using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AltMapIconRenderer
{
    public class AltWaypointMapComponent
    {
        protected static Matrixf outline = new Matrixf();
        protected static Vec2f viewPos = new Vec2f();
        public static bool Render(WaypointMapComponent __instance, GuiElementMap map, float dt)
        {
            Traverse self = Traverse.Create(__instance);
            Vec2f viewPos = self.Field("viewPos").GetValue() as Vec2f;
            Vec4f color = self.Field("color").GetValue() as Vec4f;
            Waypoint waypoint = self.Field("waypoint").GetValue() as Waypoint;
            Matrixf mvMat = self.Field("mvMat").GetValue() as Matrixf;
            WaypointMapLayer wpLayer = self.Field("wpLayer").GetValue() as WaypointMapLayer;
            bool mouseOver = (bool)self.Field("mouseOver").GetValue();
            map.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
            if (waypoint.Pinned)
            {
                map.Api.Render.PushScissor(null);
                map.ClampButPreserveAngle(ref viewPos, 2);
            }
            else
            {
                if (viewPos.X < -10 || viewPos.Y < -10 || viewPos.X > map.Bounds.OuterWidth + 10 || viewPos.Y > map.Bounds.OuterHeight + 10)
                {
                    return false;
                }
            }
            float x = (float)(map.Bounds.renderX + viewPos.X);
            float y = (float)(map.Bounds.renderY + viewPos.Y);
            ICoreClientAPI api = map.Api;
            IShaderProgram prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
            prog.Uniform("rgbaIn", color);
            prog.Uniform("extraGlow", 0);
            prog.Uniform("applyColor", 0);
            prog.Uniform("noTexture", 0f);
            LoadedTexture tex;
            if (!wpLayer.texturesByIcon.TryGetValue(waypoint.Icon, out tex))
            {
                wpLayer.texturesByIcon.TryGetValue("circle", out tex);
            }
            if (tex != null)
            {
                float zoom = GameMath.Clamp(map.ZoomLevel, AltMapIconRendererSystem.zoomMin, AltMapIconRendererSystem.zoomMax);
                if (AltMapIconRendererSystem.hideWaypoints && !mouseOver)
                {
                    return false;
                }
                Vec4f outlineColour = mouseOver ? AltMapIconRendererSystem.rgbaHover : AltMapIconRendererSystem.rgbaOutline;
                if (AltMapIconRendererSystem.config.Get().square_waypoints)
                {
                    prog.BindTexture2D("tex2d", AltMapIconRendererSystem.squareTex.TextureId, 0);
                    prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
                    mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60).Scale(tex.Width * zoom, tex.Height * zoom, 0).Scale(AltMapIconRendererSystem.waypointScale, AltMapIconRendererSystem.waypointScale, 0);
                    AltWaypointMapComponent.outline.Set(mvMat.Values).Scale(AltMapIconRendererSystem.squareOutlineScale, AltMapIconRendererSystem.squareOutlineScale, 0);
                    prog.Uniform("rgbaIn", outlineColour);
                    prog.UniformMatrix("modelViewMatrix", AltWaypointMapComponent.outline.Values);
                    api.Render.RenderMesh(wpLayer.quadModel);
                    prog.Uniform("rgbaIn", color);
                    prog.UniformMatrix("modelViewMatrix", mvMat.Values);
                    api.Render.RenderMesh(wpLayer.quadModel);
                }
                prog.BindTexture2D("tex2d", tex.TextureId, 0);
                prog.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
                mvMat.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60).Scale(tex.Width * zoom, tex.Height * zoom, 0).Scale(AltMapIconRendererSystem.waypointScale, AltMapIconRendererSystem.waypointScale, 0);
                for (int i = 0; i < mvMat.Values.Length; i++)
                {
                    mvMat.Values[i] = (int)Math.Round(mvMat.Values[i]);
                }
                if (AltMapIconRendererSystem.config.Get().square_waypoints)
                {
                    mvMat.Scale(0.85f, 0.85f, 0);
                    prog.Uniform("rgbaIn", outlineColour);
                    prog.UniformMatrix("modelViewMatrix", mvMat.Values);
                    api.Render.RenderMesh(wpLayer.quadModel);
                }
                else
                {
                    float offset = AltMapIconRendererSystem.outlineOffset;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            AltWaypointMapComponent.outline.Set(mvMat.Values).Translate(offset * ox, offset * oy, 0);
                            prog.Uniform("rgbaIn", outlineColour);
                            prog.UniformMatrix("modelViewMatrix", AltWaypointMapComponent.outline.Values);
                            api.Render.RenderMesh(wpLayer.quadModel);
                        }
                    }
                    prog.Uniform("rgbaIn", color);
                    prog.UniformMatrix("modelViewMatrix", mvMat.Values);
                    api.Render.RenderMesh(wpLayer.quadModel);
                }
            }
            if (waypoint.Pinned)
            {
                map.Api.Render.PopScissor();
            }
            return false;
        }
        public static bool OnMouseMove(WaypointMapComponent __instance, MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            Traverse self = Traverse.Create(__instance);
            Waypoint waypoint = self.Field("waypoint").GetValue() as Waypoint;
            bool mouseOver = (bool)self.Field("mouseOver").GetValue();
            mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref AltWaypointMapComponent.viewPos);
            double x = AltWaypointMapComponent.viewPos.X + mapElem.Bounds.renderX;
            double y = AltWaypointMapComponent.viewPos.Y + mapElem.Bounds.renderY;
            if (waypoint.Pinned)
            {
                mapElem.ClampButPreserveAngle(ref AltWaypointMapComponent.viewPos, 2);
                x = AltWaypointMapComponent.viewPos.X + mapElem.Bounds.renderX;
                y = AltWaypointMapComponent.viewPos.Y + mapElem.Bounds.renderY;
                x = (float)GameMath.Clamp(x, mapElem.Bounds.renderX + 2, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2);
                y = (float)GameMath.Clamp(y, mapElem.Bounds.renderY + 2, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2);
            }
            double dX = args.X - x;
            double dY = args.Y - y;
            float hoverSize = (float)GuiElement.scaled(AltMapIconRendererSystem.hoverSize) * GameMath.Clamp(mapElem.ZoomLevel, AltMapIconRendererSystem.zoomMin, AltMapIconRendererSystem.zoomMax);
            mouseOver = Math.Abs(dX) < hoverSize && Math.Abs(dY) < hoverSize;
            self.Field("mouseOver").SetValue(mouseOver);
            if (mouseOver)
            {
                hoverText.AppendLine(waypoint.Title);
            }
            return false;
        }
        public static bool OnMouseUpOnElement(WaypointMapComponent __instance, MouseEvent args, GuiElementMap mapElem)
        {
            Traverse self = Traverse.Create(__instance);
            ICoreClientAPI capi = self.Field("capi").GetValue() as ICoreClientAPI;
            Waypoint waypoint = self.Field("waypoint").GetValue() as Waypoint;
            int waypointIndex = (int)self.Field("waypointIndex").GetValue();
            GuiDialogEditWayPoint editWpDlg = self.Field("editWpDlg").GetValue() as GuiDialogEditWayPoint;
            bool mouseOver = (bool)self.Field("mouseOver").GetValue();
            if (args.Button == EnumMouseButton.Right)
            {
                mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref AltWaypointMapComponent.viewPos);
                if (mouseOver)
                {
                    if (editWpDlg != null)
                    {
                        editWpDlg.TryClose();
                        editWpDlg.Dispose();
                    }
                    var mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
                    editWpDlg = new GuiDialogEditWayPoint(capi, mapdlg.MapLayers.FirstOrDefault(l => l is WaypointMapLayer) as WaypointMapLayer, waypoint, waypointIndex);
                    editWpDlg.TryOpen();
                    editWpDlg.OnClosed += () => capi.Gui.RequestFocus(mapdlg);
                    args.Handled = true;
                }
            }
            return false;
        }
    }
}