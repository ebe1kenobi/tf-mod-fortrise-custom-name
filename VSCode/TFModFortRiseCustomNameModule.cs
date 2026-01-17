using MonoMod.ModInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using FortRise;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TFModFortRiseCustomName
{
  public class TFModFortRiseCustomNameModule : Mod 
  {
    public static TFModFortRiseCustomNameModule Instance;
    //public static TFModFortRiseCustomNameSettings Settings => Instance.GetSettings<TFModFortRiseCustomNameSettings>()!;

    internal Type[] Hookables = [
        typeof(MyPlayerIndicator),
        typeof(MyRollcallElement),
        typeof(MyTFGame),
        typeof(MyVersusRoundResults),
    ];

    public override object? GetApi()
    {
      return new ApiImplementation();
    }

    public TFModFortRiseCustomNameModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
      Instance = this;
      //TFModFortRiseCustomName.Logger.Init("ModCustomName");

      foreach (var hookable in Hookables)
      {
        hookable.GetMethod(nameof(IHookable.Load))!.Invoke(null, [context.Harmony]);
      }

      //typeof(ModExports).ModInterop();
    }

    //public override ModuleSettings CreateSettings()
    //{
    //  return new TFModFortRiseCustomNameSettings();
    //}
  }

  //[ModExportName("com.fortrise.TFModFortRiseCustomName")]
  //public static class ModExports
  //{
  //  public static void SetPlayerName(int playerIndex, String playerName)
  //  {
  //    TFModFortRiseCustomName.MyRollcallElement.SetPlayerName(playerIndex, playerName);
  //  }

  //  public static String GetPlayerName(int playerIndex)
  //  {
  //    return TFModFortRiseCustomName.MyRollcallElement.GetPlayerName(playerIndex);
  //  }
  //} 
}
