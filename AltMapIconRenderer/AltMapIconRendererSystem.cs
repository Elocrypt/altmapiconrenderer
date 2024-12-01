using System;
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

	public const float waypointScale = 0.5f;

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
			.EndSubCommand();
		harmony = new Harmony("altmapiconrenderer");
		harmony.Patch(typeof(WaypointMapComponent).GetMethod("Render", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltWaypointMapComponent).GetMethod("Render")));
		harmony.Patch(typeof(WaypointMapComponent).GetMethod("OnMouseMove", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltWaypointMapComponent).GetMethod("OnMouseMove")));
		harmony.Patch(typeof(WaypointMapComponent).GetMethod("OnMouseUpOnElement", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltWaypointMapComponent).GetMethod("OnMouseUpOnElement")));
		harmony.Patch(typeof(EntityMapComponent).GetMethod("Render", BindingFlags.Instance | BindingFlags.Public), new HarmonyMethod(typeof(AltEntityMapComponent).GetMethod("Render")));
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
}
