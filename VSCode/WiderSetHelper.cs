namespace TFModFortRiseCustomName
{
    public static class WiderSetHelper
  {
    public static bool IsWide
    {
      get => TFModFortRiseCustomNameModule.WiderSet?.IsWide ?? false;
    }
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
