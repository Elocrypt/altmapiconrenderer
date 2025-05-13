using System;
using Vintagestory.API.Client;

namespace AltMapIconRenderer;

public class AltMapIconRendererConfigManager
{
    public const string filename = "altmapiconrenderer.json";

    protected ICoreClientAPI capi;

    protected AltMapIconRendererConfig config;

    public AltMapIconRendererConfigManager(ICoreClientAPI capi)
    {
        this.capi = capi;
        try
        {
            config = this.capi.LoadModConfig<AltMapIconRendererConfig>("altmapiconrenderer.json");
        }
        catch (Exception)
        {
        }
        if (config != null)
        {
            this.capi.StoreModConfig(config, "altmapiconrenderer.json");
            return;
        }
        config = new AltMapIconRendererConfig();
        this.capi.StoreModConfig(config, "altmapiconrenderer.json");
    }

    public AltMapIconRendererConfig Get()
    {
        return config;
    }

    public void Save()
    {
        capi.StoreModConfig(config, "altmapiconrenderer.json");
    }
}