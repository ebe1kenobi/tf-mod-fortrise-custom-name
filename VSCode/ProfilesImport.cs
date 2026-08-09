using System;

namespace TFModFortRiseCustomName
{
  /// <summary>
  /// Acces au mod Profiles, quand il est installe.
  ///
  /// Profiles fait defiler les profils sur l'ecran de selection des archers avec le
  /// bouton Y, qui est exactement celui dont CustomName se sert pour son cyclage de
  /// noms. Les deux repondraient a la meme pression. Plutot que d'arbitrer par ordre
  /// de chargement, CustomName demande a Profiles s'il tient le rollcall et, si oui,
  /// n'y touche plus : ni cyclage, ni clavier virtuel, ni libelle sous le portrait.
  ///
  /// Ce que CustomName continue de faire dans les deux cas, c'est afficher le nom en
  /// jeu (indicateur de joueur, resultats de manche) ; la source du nom devient
  /// simplement le profil.
  ///
  /// L'API est optionnelle : sans Profiles installe, tout se comporte comme avant.
  /// </summary>
  public static class ProfilesImport
  {
    internal static IProfilesModApi Api;

    public static bool HandlesRollcall
    {
      get
      {
        if (Api == null)
        {
          return false;
        }

        try
        {
          return Api.HandlesRollcall;
        }
        catch (Exception ex)
        {
          Logger.Info($"[Profiles] HandlesRollcall a echoue : {ex.Message}");
          return false;
        }
      }
    }

    /// <returns>Le nom du profil de ce joueur, ou null s'il n'y en a pas.</returns>
    public static string NameOf(int playerIndex)
    {
      if (Api == null)
      {
        return null;
      }

      try
      {
        string name = Api.GetProfileName(playerIndex);
        return string.IsNullOrEmpty(name) ? null : name;
      }
      catch (Exception ex)
      {
        Logger.Info($"[Profiles] GetProfileName({playerIndex}) a echoue : {ex.Message}");
        return null;
      }
    }
  }
}
