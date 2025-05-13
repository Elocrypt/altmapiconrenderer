using System;
using System.Collections.Generic;
using System.Reflection;
using Cairo;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AltMapIconRenderer;

public class AltMapIconRendererSystem : ModSystem
{
    public const string harmonyId = "altmapiconrenderer";

    public static float waypointScale = 0.5f;

    public static float vinconomyIconScale = 1.0f;

    public const float playerScale = 0.75f;

    public const float hoverSize = 8f;

    public const float zoomMin = 0.75f;

    public const float zoomMax = 1f;

    public const float outlineOffsetDefault = 0.2f;

    public const float outlineOffsetThin = 0.1f;

    public const float squareOutlineScaleDefault = 1.2f;

    public const float squareOutlineScaleThin = 1.1f;

    public const string boolWordTrue = "on";

    public const string boolWordFalse = "off";

    public static readonly string[] boolWords = new string[2] { "on", "off" };

    public const string thinWordTrue = "thin";

    public const string thinWordFalse = "thick";

    public static readonly string[] thinWords = new string[2] { "thin", "thick" };

    public static Vec4f rgbaOutline = new Vec4f(0f, 0f, 0f, 1f);

    public static Vec4f rgbaHover = new Vec4f(0.75f, 0f, 0f, 1f);

    public static float outlineOffset;

    public static float squareOutlineScale;

    public static bool hideWaypoints;

    public static AltMapIconRendererConfigManager config;

    public static LoadedTexture squareTex;

    public static Vec4f playerColour;

    protected ICoreClientAPI capi;

    protected Harmony harmony;

    public override void StartClientSide(ICoreClientAPI capi)
    {
        base.StartClientSide(capi);
        this.capi = capi;
        config = new AltMapIconRendererConfigManager(this.capi);
        playerColour = new Vec4f();
        hideWaypoints = false;
        generateSquareTexture();
        SetPlayerColour(config.Get().player_colour);
        SetThinOutlines(config.Get().thin_outlines);
        waypointScale = config.Get().waypoint_scale;
        vinconomyIconScale = config.Get().waypoint_scale;
        this.capi.ChatCommands.Create("amir").BeginSubCommand("square").WithDescription("Render waypoint icons as squares.")
            .WithArgs(this.capi.ChatCommands.Parsers.WordRange("off/on", boolWords))
            .HandleWith(cmdSquare)
            .EndSubCommand()
            .BeginSubCommand("outline")
            .WithDescription("Set waypoint icon outline style.")
            .WithArgs(this.capi.ChatCommands.Parsers.WordRange("thin/thick", thinWords))
            .HandleWith(cmdOutline)
            .EndSubCommand()
            .BeginSubCommand("pc")
            .WithDescription("Set the player pin's colour.")
            .WithArgs(this.capi.ChatCommands.Parsers.Word("hex"))
            .HandleWith(cmdPc)
            .EndSubCommand()
            .BeginSubCommand("hide")
            .WithDescription("Hide waypoint icons.")
            .HandleWith(cmdHide)
            .EndSubCommand()
            .BeginSubCommand("show")
            .WithDescription("Show waypoint icons.")
            .HandleWith(cmdShow)
            .EndSubCommand()
            .BeginSubCommand("help")
            .WithDescription("Show Alternative Map Icon Renderer command help.")
            .HandleWith(cmdHelp)
            .EndSubCommand()
            .BeginSubCommand("pin")
            .WithDescription("Toggle pinned rendering of player icons (on/off).")
            .WithArgs(this.capi.ChatCommands.Parsers.WordRange("on/off", boolWords))
            .HandleWith(cmdPin)
            .EndSubCommand()
            .BeginSubCommand("size")
            .WithDescription("Set waypoint icon scale (e.g. 0.5 = half size, 2.0 = double).")
            .WithArgs(this.capi.ChatCommands.Parsers.Float("scale"))
            .HandleWith(cmdSize)
            .EndSubCommand();
        harmony = new Harmony("altmapiconrenderer");
        harmony.Patch(typeof(WaypointMapComponent).GetMethod("Render", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltWaypointMapComponent).GetMethod("Render")));
        harmony.Patch(typeof(WaypointMapComponent).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltWaypointMapComponent).GetMethod("OnMouseMove")));
        harmony.Patch(typeof(WaypointMapComponent).GetMethod("OnMouseUpOnElement", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltWaypointMapComponent).GetMethod("OnMouseUpOnElement")));
        harmony.Patch(typeof(EntityMapComponent).GetMethod("Render", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltEntityMapComponent).GetMethod("Render")));
        harmony.Patch(typeof(EntityMapComponent).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltEntityMapComponent).GetMethod("OnMouseMove")));
        var shopType = AccessTools.TypeByName("Viconomy.Map.ShopMapComponent");
        if (shopType != null)
        {
            var renderMethod = AccessTools.Method(shopType, "Render");
            if (renderMethod != null)
            {
                harmony.Patch(renderMethod, prefix: new HarmonyMethod(typeof(AltMapIconRendererSystem), nameof(RenderShopPrefix)));
                capi.Logger.Notification("[AltMapIconRenderer] Patched Vinconomy shop icon rendering.");
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        if (squareTex != null)
        {
            squareTex.Dispose();
        }
        harmony.UnpatchAll("altmapiconrenderer");
    }

    public void SetPlayerColour(string hex)
    {
        try
        {
            double[] array = ColorUtil.Hex2Doubles(hex);
            playerColour.R = (float)array[0];
            playerColour.G = (float)array[1];
            playerColour.B = (float)array[2];
            if (array.Length >= 4)
            {
                playerColour.A = (float)array[3];
            }
        }
        catch (Exception)
        {
            playerColour.R = 1f;
            playerColour.G = 1f;
            playerColour.B = 1f;
            playerColour.A = 1f;
        }
    }

    public void SetThinOutlines(bool thin)
    {
        outlineOffset = (thin ? 0.1f : 0.2f);
        squareOutlineScale = (thin ? 1.1f : 1.2f);
    }



    protected TextCommandResult cmdSquare(TextCommandCallingArgs args)
    {
        string text = (string)args[0];
        string text2 = text;
        string text3 = text2;
        bool flag = text3 == "on";
        config.Get().square_waypoints = flag;
        config.Save();
        return TextCommandResult.Success(print("Square waypoints " + (flag ? "enabled" : "disabled") + "."));
    }

    protected TextCommandResult cmdOutline(TextCommandCallingArgs args)
    {
        string text = (string)args[0];
        string text2 = text;
        string text3 = text2;
        bool flag = text3 == "thin";
        SetThinOutlines(flag);
        config.Get().thin_outlines = flag;
        config.Save();
        return TextCommandResult.Success(print("Thin outlines " + (flag ? "enabled" : "disabled") + "."));
    }

    protected TextCommandResult cmdPc(TextCommandCallingArgs args)
    {
        string text = (string)args[0];
        SetPlayerColour(text);
        config.Get().player_colour = vec4f2hex(playerColour);
        config.Save();
        return TextCommandResult.Success(print("Player colour set."));
    }

    protected TextCommandResult cmdHide(TextCommandCallingArgs args)
    {
        hideWaypoints = true;
        return TextCommandResult.Success(print("Icons hidden."));
    }

    protected TextCommandResult cmdShow(TextCommandCallingArgs args)
    {
        hideWaypoints = false;
        return TextCommandResult.Success(print("Icons shown."));
    }

    protected TextCommandResult cmdHelp(TextCommandCallingArgs args)
    {
        string message = "[Alternative Map Icon Renderer] Instructions:\nTo toggle square waypoints, use:\t<code>.amir square [off, on]</code>\nTo modify the waypoint outline style, use:\t<code>.amir outline [thick, thin]</code>\nTo modify the player pin colour, use:\t<code>.amir pc [#colour]</code>\nTo temporarily hide waypoint icons, use:\t<code>.amir hide</code>\nTo show waypoint icons again, use:\t<code>.amir show</code>";
        return TextCommandResult.Success(message);
    }

    protected TextCommandResult cmdPin(TextCommandCallingArgs args)
    {
        string input = (string)args[0];
        bool enable = input == "on";
        config.Get().pin_player_icons = enable;
        config.Save();
        return TextCommandResult.Success(print($"Player icon pinning {(enable ? "enabled" : "disabled")}."));
    }

    protected TextCommandResult cmdSize(TextCommandCallingArgs args)
    {
        float scale = (float)args[0];
        waypointScale = scale;
        config.Get().waypoint_scale = scale;
        config.Save();
        return TextCommandResult.Success($"[AltMapIconRenderer] Waypoint icon scale set to {scale:0.00}.");
    }

    protected static string print(string text)
    {
        return "[Alternative Map Icon Renderer] " + text;
    }

    public static string vec4f2hex(Vec4f colour)
    {
        if (colour.A != 1f)
        {
            return $"#{(int)(255f * colour.R):X2}{(int)(255f * colour.G):X2}{(int)(255f * colour.B):X2}{(int)(255f * colour.A):X2}";
        }
        return $"#{(int)(255f * colour.R):X2}{(int)(255f * colour.G):X2}{(int)(255f * colour.B):X2}";
    }

    protected void generateSquareTexture()
    {
        squareTex = new LoadedTexture(capi);
        ImageSurface imageSurface = new ImageSurface(Format.Argb32, 20, 20);
        Context context = new Context(imageSurface);
        context.SetSourceRGBA(1.0, 1.0, 1.0, 1.0);
        context.Rectangle(0.0, 0.0, 20.0, 20.0);
        context.Fill();
        capi.Gui.LoadOrUpdateCairoTexture(imageSurface, linearMag: true, ref squareTex);
        context.Dispose();
        imageSurface.Dispose();
    }

    public static bool RenderShopPrefix(object __instance, GuiElementMap map, float dt)
    {
        Traverse self = Traverse.Create(__instance);
        var capi = self.Field("capi").GetValue<ICoreClientAPI>();
        var waypoint = self.Field("waypoint").GetValue() as dynamic;
        var wpLayer = self.Field("wpLayer").GetValue();
        var texturesByIcon = Traverse.Create(wpLayer).Field("texturesByIcon").GetValue<Dictionary<string, LoadedTexture>>();
        var quadModel = Traverse.Create(wpLayer).Field("quadModel").GetValue<MeshRef>();
        var mvMat = self.Field("mvMat").GetValue<Matrixf>();

        if (waypoint == null || !waypoint.IsWaypointBroadcasted) return false;

        var api = capi;
        var prog = api.Render.GetEngineShader(EnumShaderProgram.Gui);
        Vec2f viewPos = new Vec2f();
        map.TranslateWorldPosToViewPos(new Vec3d(waypoint.X, waypoint.Y, waypoint.Z), ref viewPos);

        if (viewPos.X < -10 || viewPos.Y < -10 || viewPos.X > map.Bounds.OuterWidth + 10 || viewPos.Y > map.Bounds.OuterHeight + 10)
            return false;

        float x = (float)(map.Bounds.renderX + viewPos.X);
        float y = (float)(map.Bounds.renderY + viewPos.Y);

        if (!texturesByIcon.TryGetValue(waypoint.WaypointIcon, out LoadedTexture tex))
        {
            tex = texturesByIcon.TryGetValue("genericViconShop", out LoadedTexture fallback) ? fallback : null;
        }
        if (tex == null) return false;

        Vec4f color = new Vec4f();
        ColorUtil.ToRGBAVec4f(waypoint.WaypointColor, ref color);
        color.A = 1f;

        var isHovered = self.Field("mouseOver").GetValue<bool>();
        Vec4f iconColor = isHovered ? AltMapIconRendererSystem.rgbaHover : color;
        prog.Uniform("rgbaIn", iconColor);
        prog.Uniform("extraGlow", 0);
        prog.Uniform("applyColor", 0);
        prog.Uniform("noTexture", 0f);
        prog.BindTexture2D("tex2d", tex.TextureId, 0);
        prog.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);

        mvMat.Set(capi.Render.CurrentModelviewMatrix)
             .Translate(x, y, 60)
             .Scale(tex.Width, tex.Height, 0)
             .Scale(AltMapIconRendererSystem.waypointScale, AltMapIconRendererSystem.waypointScale, 0);

        if (AltMapIconRendererSystem.config.Get().square_waypoints)
        {
            Matrixf outlineMat = mvMat.Clone().Scale(
                AltMapIconRendererSystem.squareOutlineScale,
                AltMapIconRendererSystem.squareOutlineScale,
                0
            );
            prog.Uniform("rgbaIn", AltMapIconRendererSystem.rgbaOutline);
            prog.UniformMatrix("modelViewMatrix", outlineMat.Values);
            capi.Render.RenderMesh(quadModel);
        }
        else if (self.Field("mouseOver").GetValue<bool>())
        {
            float offset = AltMapIconRendererSystem.outlineOffset;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0) continue;

                    Matrixf outlineMat = mvMat.Clone().Translate(offset * ox, offset * oy, 0);
                    prog.Uniform("rgbaIn", AltMapIconRendererSystem.rgbaHover);
                    prog.UniformMatrix("modelViewMatrix", outlineMat.Values);
                    capi.Render.RenderMesh(quadModel);
                }
            }
        }

        prog.UniformMatrix("modelViewMatrix", mvMat.Values);
        capi.Render.RenderMesh(quadModel);

        return false; // Skip original Vinconomy Render()
    }

}
