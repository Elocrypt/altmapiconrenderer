using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AltMapIconRenderer;

public class AltEntityMapComponent
{
	public static bool Render(EntityMapComponent __instance, GuiElementMap map, float dt)
	{
		Traverse traverse = Traverse.Create(__instance);
		ICoreClientAPI coreClientAPI = traverse.Field("capi").GetValue() as ICoreClientAPI;
		Entity entity = traverse.Field("entity").GetValue() as Entity;
		MeshRef meshRef = traverse.Field("quadModel").GetValue() as MeshRef;
		LoadedTexture loadedTexture = traverse.Field("Texture").GetValue() as LoadedTexture;
		Vec2f viewPos = traverse.Field("viewPos").GetValue() as Vec2f;
		Matrixf matrixf = traverse.Field("mvMat").GetValue() as Matrixf;
		IPlayer player = (entity as EntityPlayer)?.Player;
		if (player != null && player.WorldData?.CurrentGameMode == EnumGameMode.Spectator)
		{
			return false;
		}
		EntityPlayer obj = entity as EntityPlayer;
		if (obj != null && obj.Controls.Sneak && player != coreClientAPI.World.Player)
		{
			return false;
		}
		map.TranslateWorldPosToViewPos(entity.Pos.XYZ, ref viewPos);
		float x = (float)(map.Bounds.renderX + (double)viewPos.X);
		float y = (float)(map.Bounds.renderY + (double)viewPos.Y);
		ICoreClientAPI api = map.Api;
		if (loadedTexture.Disposed)
		{
			throw new Exception("Fatal. Trying to render a disposed texture");
		}
		if (meshRef.Disposed)
		{
			throw new Exception("Fatal. Trying to render a disposed texture");
		}
		coreClientAPI.Render.GlToggleBlend(blend: true);
		IShaderProgram engineShader = api.Render.GetEngineShader(EnumShaderProgram.Gui);
		engineShader.Uniform("rgbaIn", AltMapIconRendererSystem.playerColour);
		engineShader.Uniform("extraGlow", 0);
		engineShader.Uniform("applyColor", 0);
		engineShader.Uniform("noTexture", 0f);
		engineShader.BindTexture2D("tex2d", loadedTexture.TextureId, 0);
		matrixf.Set(api.Render.CurrentModelviewMatrix).Translate(x, y, 60f).Scale(loadedTexture.Width, loadedTexture.Height, 0f)
			.Scale(0.75f, 0.75f, 0f)
			.RotateZ(-entity.Pos.Yaw + (float)Math.PI);
		engineShader.UniformMatrix("projectionMatrix", api.Render.CurrentProjectionMatrix);
		engineShader.UniformMatrix("modelViewMatrix", matrixf.Values);
		api.Render.RenderMesh(meshRef);
		return false;
	}
}
