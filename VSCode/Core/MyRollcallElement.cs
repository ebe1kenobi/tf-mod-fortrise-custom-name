using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Xml.Linq;
using TowerFall;


namespace TFModFortRiseCustomName
{
  public class MyRollcallElement
  {
    public static Dictionary<int, String> playerName = new Dictionary<int, String>(8);
    public static Dictionary<int, Text> playerNameText = new Dictionary<int, Text>(8);
    public static List<string> playerNamesAvailable = new List<string>();

    internal static void Load()
    {
      On.TowerFall.RollcallElement.ctor += ctor_patch;
      On.TowerFall.RollcallElement.Render += Render_patch;
      On.TowerFall.RollcallElement.NotJoinedUpdate += NotJoinedUpdate_patch;
      On.TowerFall.RollcallElement.ForceStart += ForceStart_patch;
      On.TowerFall.RollcallElement.StartVersus += StartVersus_patch;
    }

    internal static void Unload()
    {
      On.TowerFall.RollcallElement.ctor -= ctor_patch;
      On.TowerFall.RollcallElement.Render -= Render_patch;
      On.TowerFall.RollcallElement.NotJoinedUpdate -= NotJoinedUpdate_patch;
      On.TowerFall.RollcallElement.ForceStart -= ForceStart_patch;
      On.TowerFall.RollcallElement.StartVersus -= StartVersus_patch;
    }

    public MyRollcallElement() { }

    static private void savePlayerNamesAvailable() {
      PlayerNameStorage.Save(playerNamesAvailable);
    }

    public static void ForceStart_patch(On.TowerFall.RollcallElement.orig_ForceStart orig, global::TowerFall.RollcallElement self){
      savePlayerNamesAvailable();
      orig(self);
    }

    public static void StartVersus_patch(On.TowerFall.RollcallElement.orig_StartVersus orig, global::TowerFall.RollcallElement self){
      savePlayerNamesAvailable();
      orig(self);
    }


    public static void Render_patch(On.TowerFall.RollcallElement.orig_Render orig, global::TowerFall.RollcallElement self) {
      orig(self);
      if (TFGame.Players.Length > 4)
      {
        int currentPlayerIndex = DynamicData.For(self).Get<int>("playerIndex");
        //get the Text for the player name with position -30
        if (self.Components == null)
        {
          return;
        }
        Text positionText = null;
        for (var i = 0; i < self.Components.Count; i++)
        {
          if (self.Components[i].GetType().ToString() != "Monocle.Text") continue;
          Text text = (Text)self.Components[i];
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
        if (EightPlayerImport.LaunchedEightPlayer())
        {   //Don't work
          positionText.Position.Y = -40;
        }
        else
        {
          positionText.Position.Y = -60;
        }
      }
    }

    public static void ctor_patch(On.TowerFall.RollcallElement.orig_ctor orig, global::TowerFall.RollcallElement self, int playerIndex)
    {
      typeof(EightPlayerImport).ModInterop();
      orig(self, playerIndex);
      var dynData = DynamicData.For(self);

      Color color = Color.White;
      Vector2 positionText;
      if (TFGame.Players.Length > 4)
      {
        if (EightPlayerImport.LaunchedEightPlayer()) { 
          positionText = new Vector2(-30, -40);
        } else {
          positionText = new Vector2(-30, -60);  
        }
      }
      else
      {
        positionText = new Vector2(-30, -60); 
      }
      //to do once for the game
      if (!playerName.ContainsKey(playerIndex)) {
        String name = playerNamesAvailable[0] + (playerIndex + 1);
        playerName[playerIndex] = name;
        playerNameText[playerIndex] = new Text(TFGame.Font, name, positionText, color, Text.HorizontalAlign.Left, Text.VerticalAlign.Bottom);
      }

      self.Add((Component)playerNameText[playerIndex]);

      dynData.Dispose();
    }

    public static void SetPlayerName(int playerIndex, String newName)
    {
      playerName[playerIndex] = newName;
      var dynData = DynamicData.For(playerNameText[playerIndex]);
      dynData.Set("text", newName);
      dynData.Dispose();
    }

    public static String GetPlayerName(int playerIndex)
    {
      return playerName[playerIndex];
    }

    public static string getNextName(int playerIndex)
    {
      int currentNameIndex = getCurrentNameIndex(playerIndex);
      string nextName = "";

      // if player name is not P1..8 or one in the playerName.json file, do not modify
      if (getCurrentNameIndex(playerIndex) == -1)
      {
        return GetPlayerName(playerIndex);
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

    public static int NotJoinedUpdate_patch(On.TowerFall.RollcallElement.orig_NotJoinedUpdate orig, global::TowerFall.RollcallElement self)
    {
      if (VirtualKeyboard.KeyboardActive)
      {
        return 0; // ignore l’input, le Rollcall ne réagit pas
      }
      var dynData = DynamicData.For(self);

      int playerIndex = (int)dynData.Get("playerIndex");

      if (dynData.Get("input") == null)
        return orig(self);

      var input = DynamicData.For(dynData.Get("input"));
      if (input == null)
        return orig(self);
      InputState inputState = input.Invoke<InputState>("GetState");

      //forbid change name for name not in available name
      if ((getCurrentNameIndex(playerIndex) != -1)) {
        if (inputState.ArrowsPressed && inputState.MoveY == -1)
        {
          self.Scene.Add(new VirtualKeyboard(playerIndex));
        } else if (inputState.ArrowsPressed)
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

      return orig(self);
    }
  }
}
