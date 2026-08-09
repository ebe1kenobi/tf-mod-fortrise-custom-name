namespace TFModFortRiseCustomName;

/// <summary>
/// Copie locale de l'interface publiee par le mod Profiles.
///
/// L'interop de FortRise passe par un proxy construit sur la forme des membres : les
/// deux mods n'ont pas besoin de partager un assembly, seulement des signatures
/// identiques. Toute modification ici doit rester alignee sur
/// <c>TFModFortRiseProfiles.IProfilesModApi</c>.
/// </summary>
public partial interface IProfilesModApi
{
  bool HandlesRollcall { get; }
  string GetProfileName(int playerIndex);
}
