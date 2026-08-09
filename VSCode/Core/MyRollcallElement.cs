using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Xml.Linq;
using FortRise;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using MonoMod.Utils;
using TowerFall;


namespace TFModFortRiseCustomName
{
  public class MyRollcallElement : IHookable
  {
    public static Dictionary<int, String> playerName = new Dictionary<int, String>(8);
    public static Dictionary<int, Text> playerNameText = new Dictionary<int, Text>(8);
    public static List<string> playerNamesAvailable = new List<string>();

    public static void Load(IHarmony harmony)
    {
      harmony.Patch(
          AccessTools.DeclaredConstructor(typeof(RollcallElement), [
                                                                        typeof(int),
                                                                    ]),
          postfix: new HarmonyMethod(ctor_patch)
      );

      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(RollcallElement), nameof(RollcallElement.Render)),
          postfix: new HarmonyMethod(Render_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(RollcallElement), "NotJoinedUpdate"),
          prefix: new HarmonyMethod(NotJoinedUpdate_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(RollcallElement), "ForceStart"),
          postfix: new HarmonyMethod(ForceStart_patch)
      );
      harmony.Patch(
          AccessTools.DeclaredMethod(typeof(RollcallElement), "StartVersus"),
          postfix: new HarmonyMethod(StartVersus_patch)
      );
    }

    static private void savePlayerNamesAvailable() {
      PlayerNameStorage.Save(playerNamesAvailable);
    }

    public static void ForceStart_patch(RollcallElement __instance){
      savePlayerNamesAvailable();
    }

    public static void StartVersus_patch(RollcallElement __instance){
      savePlayerNamesAvailable();
    }


    public static void Render_patch(RollcallElement __instance ){
      // Profiles affiche lui-meme le nom sous le portrait : il n'y a plus de Text a
      // repositionner, et le chercher ne rendrait rien.
      if (ProfilesImport.HandlesRollcall) return;

      if (TFGame.Players.Length > 4)
      {
        int currentPlayerIndex = DynamicData.For(__instance).Get<int>("playerIndex");
        //get the Text for the player name with position -30
        if (__instance.Components == null)
        {
          return;
        }
        Text positionText = null;
        for (var i = 0; i < __instance.Components.Count; i++)
        {
          if (__instance.Components[i].GetType().ToString() != "Monocle.Text") continue;
          Text text = (Text)__instance.Components[i];
          if (text.Position.X != -30) continue;
          var dynData = DynamicData.For(text);
          String textText = dynData.Get<String>("text");
          if (textText.Length == 0) continue;
          if (!textText.Equals(MyRollcallElement.GetPlayerName(currentPlayerIndex))) continue;
          dynData.Dispose();
          positionText = text;
          break;
        }

        if (positionText == null) return;

        // we must update the Y position because the constructor is called only once if we change between 4 ou 8player mode
        if (WiderSetHelper.IsWide)
        {   //Don't work
          positionText.Position.Y = -40;
        }
        else
        {
          positionText.Position.Y = -60;
        }
      }
    }

    public static void ctor_patch(RollcallElement __instance, int playerIndex)
    {
      var dynData = DynamicData.For(__instance);

      // L'initialisation a lieu dans les deux cas : le reste du mod (indicateur de
      // joueur, resultats de manche) lit playerName et planterait sur une entree
      // absente. Seul l'affichage sur l'ecran de selection est cede a Profiles.
      setInfoPlayerName(playerIndex);

      if (!ProfilesImport.HandlesRollcall)
      {
        __instance.Add((Component)playerNameText[playerIndex]);
      }

      dynData.Dispose();
    }

    // Initialise playerName/playerNameText a la demande. Necessaire pour le mode
    // tournoi qui ne charge pas l'ecran de selection d'archer : ctor_patch n'y est
    // jamais appele, donc sans cette init paresseuse SetPlayerName planterait sur
    // playerNameText[playerIndex] (KeyNotFoundException).
    public static void setInfoPlayerName(int playerIndex)
    {
      Color color = Color.White;
      Vector2 positionText;
      if (TFGame.Players.Length > 4)
      {
        if (WiderSetHelper.IsWide)
        {
          positionText = new Vector2(-30, -40);
        }
        else
        {
          positionText = new Vector2(-30, -60);
        }
      }
      else
      {
        positionText = new Vector2(-30, -60);
      }
      if (!playerName.ContainsKey(playerIndex))
      {
        // playerNamesAvailable est rempli en tache de fond au chargement du jeu et
        // peut etre encore vide, ou le rester si le fichier de noms est absent. Le
        // prefixe "P" est celui que la liste porte en tete quand elle existe.
        String prefix = playerNamesAvailable.Count > 0 ? playerNamesAvailable[0] : "P";
        String name = prefix + (playerIndex + 1);
        playerName[playerIndex] = name;
        playerNameText[playerIndex] = new Text(TFGame.Font, name, positionText, color, Text.HorizontalAlign.Left, Text.VerticalAlign.Bottom);
      }
    }

    public static void SetPlayerName(int playerIndex, String newName)
    {
      setInfoPlayerName(playerIndex);
      playerName[playerIndex] = newName;
      var dynData = DynamicData.For(playerNameText[playerIndex]);
      dynData.Set("text", newName);
      dynData.Dispose();
    }

    public static String GetPlayerName(int playerIndex)
    {
      // Quand Profiles tient le rollcall, c'est lui qui decide du nom : l'indicateur
      // de joueur et les resultats de manche doivent montrer le profil choisi. La
      // table locale est mise a jour au passage pour que tout ce qui la lit
      // directement reste coherent.
      string fromProfile = ProfilesImport.NameOf(playerIndex);
      if (fromProfile != null)
      {
        SetPlayerName(playerIndex, fromProfile);
        return fromProfile;
      }

      // Les modes qui ne passent pas par l'ecran de selection n'ont jamais fait
      // l'initialisation : sans cet appel, la lecture leve KeyNotFoundException.
      setInfoPlayerName(playerIndex);
      return playerName[playerIndex];
    }

    public static string getNextName(int playerIndex)
    {
      int currentNameIndex = getCurrentNameIndex(playerIndex);
      string nextName = "";

      // if player name is not P1..8 or one in the playerName.json file, do not modify
      //if (getCurrentNameIndex(playerIndex) == 0 || !playerNamesAvailable.Contains(getCurrentName(playerIndex))) {
      if (getCurrentNameIndex(playerIndex) == -1) {
        return GetPlayerName(playerIndex);
        //return $"A{playerIndex}"; // getCurrentName(playerIndex);
      }

      // Construire la liste des noms déjà utilisés
      var usedNames = new HashSet<string>();
      foreach (var kvp in playerName)
      {
        if (kvp.Value != null)
        {
          string txt = kvp.Value;
          if (!string.IsNullOrEmpty(txt))
            usedNames.Add(txt);
        }
      }

      // Rechercher le prochain nom disponible
      int totalNames = playerNamesAvailable.Count;
      for (int i = 1; i <= totalNames; i++)
      {
        int nextIndex = (currentNameIndex + i) % totalNames;
        string candidate = playerNamesAvailable[nextIndex];

        if (!usedNames.Contains(candidate))
        {
          nextName = candidate;
          break;
        }
      }

      // Si rien trouvé (très peu probable), on prend le premier nom
      if (string.IsNullOrEmpty(nextName))
        nextName = playerNamesAvailable[0];

      // Si c’est le premier nom ("P" par exemple), on ajoute le numéro du joueur
      if (nextName == playerNamesAvailable[0])
      {
        return nextName + (playerIndex + 1);
      }

      return nextName;
    }

    //static public string getCurrentName(int playerIndex)
    //{
    //  //var dynData = DynamicData.For(playerName[playerIndex]);
    //  return playerName[playerIndex];
    //}

    static public int getCurrentNameIndex(int playerIndex)
    {
      //var dynData = DynamicData.For(playerName[playerIndex]);
      String currentName = playerName[playerIndex];
      int index = -1;

      // Si le nom commence par 'P' et a une longueur de 2 → on renvoie 0
      if (!string.IsNullOrEmpty(currentName) &&
          currentName.Length == 2 &&
          currentName.StartsWith("P"))
      {
        index = 0;
      }
      else
      {
        // Sinon, on cherche l'indice dans la liste des noms disponibles
        index = playerNamesAvailable.IndexOf(currentName);

        // Si le nom n’existe pas dans la liste, on renvoie 0 par défaut
        //if (index < 0)
        //  index = 0;
      }

      return index;
    }

    public static bool NotJoinedUpdate_patch(RollcallElement __instance)
    {
      // Profiles lit le meme bouton (Y / "arrows") pour faire defiler ses profils :
      // sans ce retrait, une pression declencherait les deux cyclages a la fois.
      if (ProfilesImport.HandlesRollcall)
      {
        return true;
      }

      if (VirtualKeyboard.KeyboardActive)
      {
        return false; // ignore l’input, le Rollcall ne réagit pas
      }
      var dynData = DynamicData.For(__instance);

      int playerIndex = (int)dynData.Get("playerIndex");

      if (dynData.Get("input") == null)
        return true;

      var input = DynamicData.For(dynData.Get("input"));
      if (input == null)
        return true;
      InputState inputState = input.Invoke<InputState>("GetState");
      if ((getCurrentNameIndex(playerIndex) != -1))
      {
        if (inputState.ArrowsPressed && inputState.MoveY == -1)
        {
          __instance.Scene.Add(new VirtualKeyboard(playerIndex));
        }
        else if (inputState.ArrowsPressed)
        {
          //self.Scene.Add(new VirtualKeyboard(playerIndex));
          SetPlayerName(playerIndex, getNextName(playerIndex));
        }
        //move to next name
        //if ((bool)input.Get("MenuAlt2")){
        //  SetPlayerName(playerIndex, getNextName(playerIndex));
        //}
      }
      dynData.Dispose();

      return true;
    }
  }
}
