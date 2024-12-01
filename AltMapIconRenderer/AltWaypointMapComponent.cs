using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AltMapIconRenderer;

public class AltWaypointMapComponent
{
	protected static Matrixf outline = new Matrixf();

	protected static Vec2f viewPos = new Vec2f();

	public static bool Render(WaypointMapComponent __instance, GuiElementMap map, float dt)
	{
		Traverse traverse = Traverse.Create(__instance);
		Vec2f vec2f = traverse.Field("viewPos").GetValue() as Vec2f;
		Vec4f value = traverse.Field("color").GetValue() as Vec4f;
		Waypoint waypoint = traverse.Field("waypoint").GetValue() as Waypoint;
		Matrixf matrixf = traverse.Field("mvMat").GetValue() as Matrixf;
		WaypointMapLayer waypointMapLayer = traverse.Field("wpLayer").GetValue() as WaypointMapLayer;
		bool flag = (bool)traverse.Field("mouseOver").GetValue();
		map.TranslateWorldPosToViewPos(waypoint.Position, ref vec2f);
		if (waypoint.Pinned)
		{
			map.Api.Render.PushScissor(null);
			map.ClampButPreserveAngle(ref vec2f, 2);
		}
		else if (vec2f.X < -10f || vec2f.Y < -10f || (double)vec2f.X > map.Bounds.OuterWidth + 10.0 || (double)vec2f.Y > map.Bounds.OuterHeight + 10.0)
		{
			return false;
		}
		float x = (float)(map.Bounds.renderX + (double)vec2f.X);
		float y = (float)(map.Bounds.renderY + (double)vec2f.Y);
		ICoreClientAPI api = map.Api;
		IShaderProgram engineShader = api.Render.GetEngineShader(EnumShaderProgram.Gui);
		engineShader.Uniform("rgbaIn", value);
		engineShader.Uniform("extraGlow", 0);
		engineShader.Uniform("applyColor", 0);
		engineShader.Uniform("noTexture", 0f);
		if (!waypointMapLayer.texturesByIcon.TryGetValue(waypoint.Icon, out var value2))
		{
			waypointMapLayer.texturesByIcon.TryGetValue("circle", out value2);
		}
		if (value2 != null)
		{
			float num = GameMath.Clamp(map.ZoomLevel, 0.75f, 1f);
			if (AltMapIconRendererSystem.hideWaypoints && !flag)
			{
				return false;
			}
			Vec4f value3 = (flag ? AltMapIconRendererSystem.rgbaHover : AltMapIconRendererSystem.rgbaOutline);
			if (AltMapIconRendererSystem.config.Get().square_waypoints)
			{
				engineShader.BindTexture2D("tex2d", AltMapIconRendererSystem.squareTex.TextureId, 0);
				engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
				matrixf.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)value2.Width * num, (float)value2.Height * num, 0f)
					.Scale(0.5f, 0.5f, 0f);
				outline.Set(matrixf.Values).Scale(AltMapIconRendererSystem.squareOutlineScale, AltMapIconRendererSystem.squareOutlineScale, 0f);
				engineShader.Uniform("rgbaIn", value3);
				engineShader.UniformMatrix("modelViewMatrix", outline.Values);
				api.Render.RenderMesh(waypointMapLayer.quadModel);
				engineShader.Uniform("rgbaIn", value);
				engineShader.UniformMatrix("modelViewMatrix", matrixf.Values);
				api.Render.RenderMesh(waypointMapLayer.quadModel);
			}
			engineShader.BindTexture2D("tex2d", value2.TextureId, 0);
			engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
			matrixf.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale((float)value2.Width * num, (float)value2.Height * num, 0f)
				.Scale(0.5f, 0.5f, 0f);
			for (int i = 0; i < matrixf.Values.Length; i++)
			{
				matrixf.Values[i] = (int)Math.Round(matrixf.Values[i]);
			}
			if (AltMapIconRendererSystem.config.Get().square_waypoints)
			{
				matrixf.Scale(0.85f, 0.85f, 0f);
				engineShader.Uniform("rgbaIn", value3);
				engineShader.UniformMatrix("modelViewMatrix", matrixf.Values);
				api.Render.RenderMesh(waypointMapLayer.quadModel);
			}
			else
			{
				float outlineOffset = AltMapIconRendererSystem.outlineOffset;
				for (int j = -1; j <= 1; j++)
				{
					for (int k = -1; k <= 1; k++)
					{
						outline.Set(matrixf.Values).Translate(outlineOffset * (float)k, outlineOffset * (float)j, 0f);
						engineShader.Uniform("rgbaIn", value3);
						engineShader.UniformMatrix("modelViewMatrix", outline.Values);
						api.Render.RenderMesh(waypointMapLayer.quadModel);
					}
				}
				engineShader.Uniform("rgbaIn", value);
				engineShader.UniformMatrix("modelViewMatrix", matrixf.Values);
				api.Render.RenderMesh(waypointMapLayer.quadModel);
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
		Traverse traverse = Traverse.Create(__instance);
		Waypoint waypoint = traverse.Field("waypoint").GetValue() as Waypoint;
		bool flag = (bool)traverse.Field("mouseOver").GetValue();
		mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
		double num = (double)viewPos.X + mapElem.Bounds.renderX;
		double num2 = (double)viewPos.Y + mapElem.Bounds.renderY;
		if (waypoint.Pinned)
		{
			mapElem.ClampButPreserveAngle(ref viewPos, 2);
			num = (double)viewPos.X + mapElem.Bounds.renderX;
			num2 = (double)viewPos.Y + mapElem.Bounds.renderY;
			num = (float)GameMath.Clamp(num, mapElem.Bounds.renderX + 2.0, mapElem.Bounds.renderX + mapElem.Bounds.InnerWidth - 2.0);
			num2 = (float)GameMath.Clamp(num2, mapElem.Bounds.renderY + 2.0, mapElem.Bounds.renderY + mapElem.Bounds.InnerHeight - 2.0);
		}
		double value = (double)args.X - num;
		double value2 = (double)args.Y - num2;
		float num3 = (float)GuiElement.scaled(8.0) * GameMath.Clamp(mapElem.ZoomLevel, 0.75f, 1f);
		flag = Math.Abs(value) < (double)num3 && Math.Abs(value2) < (double)num3;
		traverse.Field("mouseOver").SetValue(flag);
		if (flag)
		{
			hoverText.AppendLine(waypoint.Title);
		}
		return false;
	}

	public static bool OnMouseUpOnElement(WaypointMapComponent __instance, MouseEvent args, GuiElementMap mapElem)
	{
		Traverse traverse = Traverse.Create(__instance);
		ICoreClientAPI capi = traverse.Field("capi").GetValue() as ICoreClientAPI;
		Waypoint waypoint = traverse.Field("waypoint").GetValue() as Waypoint;
		int index = (int)traverse.Field("waypointIndex").GetValue();
		GuiDialogEditWayPoint guiDialogEditWayPoint = traverse.Field("editWpDlg").GetValue() as GuiDialogEditWayPoint;
		bool flag = (bool)traverse.Field("mouseOver").GetValue();
		if (args.Button == EnumMouseButton.Right)
		{
			mapElem.TranslateWorldPosToViewPos(waypoint.Position, ref viewPos);
			if (flag)
			{
				if (guiDialogEditWayPoint != null)
				{
					guiDialogEditWayPoint.TryClose();
					guiDialogEditWayPoint.Dispose();
				}
				GuiDialogWorldMap mapdlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
				guiDialogEditWayPoint = new GuiDialogEditWayPoint(capi, mapdlg.MapLayers.FirstOrDefault((MapLayer l) => l is WaypointMapLayer) as WaypointMapLayer, waypoint, index);
				guiDialogEditWayPoint.TryOpen();
				guiDialogEditWayPoint.OnClosed += delegate
				{
					capi.Gui.RequestFocus(mapdlg);
				};
				args.Handled = true;
			}
		}
		return false;
	}
}
