﻿using System;
using System.Reflection;
using Celeste.Mod.MoreLockBlocks.Entities;
using MonoMod.RuntimeDetour;

namespace Celeste.Mod.MoreLockBlocks;

public class MoreLockBlocksModule : EverestModule
{
    public static MoreLockBlocksModule Instance { get; private set; }

    public override Type SettingsType => typeof(MoreLockBlocksModuleSettings);
    public static MoreLockBlocksModuleSettings Settings => (MoreLockBlocksModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(MoreLockBlocksModuleSession);
    public static MoreLockBlocksModuleSession Session => (MoreLockBlocksModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(MoreLockBlocksModuleSaveData);
    public static MoreLockBlocksModuleSaveData SaveData => (MoreLockBlocksModuleSaveData)Instance._SaveData;

    private static readonly FieldInfo contentLoaded = typeof(Everest).GetField("_ContentLoaded", BindingFlags.NonPublic | BindingFlags.Static);
    private static Hook modRegisterHook = null;

    internal bool DzhakeHelperLoaded;

    public MoreLockBlocksModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(MoreLockBlocksModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(MoreLockBlocksModule), LogLevel.Info);
#endif
    }

    private void HookMods()
    {
        if (!DzhakeHelperLoaded && Everest.Loader.DependencyLoaded(new EverestModuleMetadata { Name = "DzhakeHelper", Version = new Version(1, 4, 9) }))
        {
            LoadDzhakeHelper();
        }
    }

    public override void Load()
    {
        // TODO: apply any hooks that should always be active
        GlassLockBlockController.Load();

        modRegisterHook = new Hook(typeof(Everest).GetMethod("Register"), typeof(MoreLockBlocksModule).GetMethod("Everest_Register", BindingFlags.NonPublic | BindingFlags.Instance), this);
    }

    public override void Unload()
    {
        // TODO: unapply any hooks applied in Load()
        GlassLockBlockController.Unload();

        modRegisterHook?.Dispose();
        modRegisterHook = null;

        if (DzhakeHelperLoaded)
        {
            UnloadDzhakeHelper();
        }
    }

    public override void LoadContent(bool firstLoad)
    {
        MoreLockBlocksGFX.LoadContent();

        HookMods();
    }

    private void LoadDzhakeHelper()
    {
        DzhakeHelperLoaded = true;
    }

    private void UnloadDzhakeHelper()
    {
        DzhakeHelperLoaded = false;
    }

    private void Everest_Register(Action<EverestModule> orig, EverestModule module)
    {
        orig(module);

        if ((bool)contentLoaded.GetValue(null))
        {
            // the game was already initialized and a new mod was loaded at runtime:
            // make sure whe applied all mod hooks we want to apply.
            HookMods();
        }
    }
}