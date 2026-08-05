using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TowerFall;

namespace TFModFortRiseCustomName
{
  /// <summary>
  /// Clavier virtuel de saisie d'un nom de joueur.
  ///
  /// La classe, son constructeur et KeyboardActive gardent leur forme d'origine :
  /// MyRollcallElement s'appuie dessus pour ouvrir le clavier et geler le rollcall
  /// pendant la saisie.
  ///
  /// Deux differences avec la version precedente :
  ///
  /// - la grille est une vraie grille : haut/bas/gauche/droite suivent les lignes
  ///   et les colonnes, au lieu de se deplacer sur un index lineaire, et le curseur
  ///   defile tant qu'une direction est maintenue ;
  ///
  /// - la frappe passe par la saisie texte du systeme (TextInputEXT, le mecanisme
  ///   qu'utilise UIInputText de FortRise) et non par des codes de touches. Keys.D8
  ///   designe la touche a la position du 8 en QWERTY : en AZERTY elle produisait
  ///   "8" au lieu du tiret bas. TextInputEXT rend le caractere reellement tape,
  ///   disposition clavier et Shift compris.
  ///
  /// A noter : au clavier, TowerFall mappe MenuConfirm sur la touche de saut et
  /// MenuBack sur celle de tir. Les entrees de type KeyboardInput sont donc exclues
  /// de la navigation, sans quoi taper ces lettres validerait ou fermerait l'ecran.
  /// </summary>
  public class VirtualKeyboard : Entity
  {
    public static bool KeyboardActive = false;

    private const int Columns = 10;
    private const int MaxLength = 12;

    // Une case par caractere saisissable. Tout caractere tape qui n'y figure pas
    // est refuse : la police du jeu ne sait pas tout rendre.
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .'-_?!:()\\/";

    // Repetition quand on maintient une direction : une premiere pause pour
    // permettre le pas a pas, puis un defilement continu.
    private const float RepeatFirstDelay = 0.35f;
    private const float RepeatDelay = 0.07f;

    private static readonly Vector2 GridOrigin = new Vector2(70f, 92f);
    private const float CellW = 18f;
    private const float CellH = 16f;

    private readonly int playerIndex;

    private string current = "";
    private int selected;
    private float repeatTimer;
    private int heldX, heldY;
    private float blink;
    private string error;

    public VirtualKeyboard(int playerIndex)
    {
      this.playerIndex = playerIndex;
      Depth = -100000;
      KeyboardActive = true;   // gele le rollcall pendant la saisie
    }

    public override void Added()
    {
      base.Added();
      TextInputEXT.TextInput += HandleChar;
      TextInputEXT.StartTextInput();
      Sounds.ui_pause.Play(160f);
    }

    public override void Removed()
    {
      base.Removed();
      // L'evenement est statique : ne pas se desabonner laisserait cet ecran
      // capter la frappe pour toute la duree du jeu.
      TextInputEXT.TextInput -= HandleChar;
      TextInputEXT.StopTextInput();
      KeyboardActive = false;
    }

    /// <summary>
    /// Caractere reellement produit par le clavier (disposition et Shift compris).
    /// FNA fait passer par ce meme canal le retour arriere (8) et l'entree (10).
    /// </summary>
    private void HandleChar(char c)
    {
      if (c == 8)
      {
        Backspace();
        return;
      }

      if (c == 10 || c == 13)
      {
        Validate();
        return;
      }

      if (c == '\t') return;

      char upper = char.ToUpperInvariant(c);
      if (Charset.IndexOf(upper) < 0)
      {
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      Append(upper);
    }

    // --- saisie ---

    private void Append(char c)
    {
      if (current.Length >= MaxLength)
      {
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }
      current += c;
      error = null;
      Sounds.ui_move1.Play(160f, 1f);
    }

    private void Backspace()
    {
      if (current.Length == 0) return;
      current = current.Substring(0, current.Length - 1);
      error = null;
      Sounds.ui_move1.Play(160f, 1f);
    }

    private void Validate()
    {
      string name = current.Trim().ToUpper();

      if (name.Length == 0)
      {
        error = "EMPTY NAME";
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      if (MyRollcallElement.playerNamesAvailable.Contains(name))
      {
        error = "ALREADY IN THE LIST";
        Sounds.ui_invalid.Play(160f, 1f);
        return;
      }

      MyRollcallElement.playerNamesAvailable.Add(name);
      MyRollcallElement.SetPlayerName(playerIndex, name);

      // Le rollcall ne sauvegarde qu'au demarrage du versus : sans cela, quitter
      // le menu autrement ferait perdre le nom qu'on vient de saisir.
      try
      {
        PlayerNameStorage.Save(MyRollcallElement.playerNamesAvailable);
      }
      catch (Exception ex)
      {
        Logger.Info($"[VirtualKeyboard] sauvegarde des noms impossible : {ex.Message}");
      }

      Sounds.ui_click.Play(160f, 1f);
      Close();
    }

    private void Cancel()
    {
      Sounds.ui_click.Play(160f, 1f);
      Close();
    }

    private void Close()
    {
      KeyboardActive = false;
      RemoveSelf();
    }

    // --- boucle ---

    public override void Update()
    {
      base.Update();
      repeatTimer -= Engine.DeltaTime;
      blink += Engine.DeltaTime;

      // Seules les touches que la saisie texte ne transmet pas sont lues ici.
      if (MInput.Keyboard != null)
      {
        if (MInput.Keyboard.Pressed(Keys.Escape)) Cancel();
        if (MInput.Keyboard.Pressed(Keys.Delete)) Backspace();
      }

      UpdateGamepad();
    }

    /// <summary>
    /// Navigation a la manette. Seul le joueur qui a ouvert le clavier la pilote :
    /// c'est son nom qu'il saisit.
    /// </summary>
    private void UpdateGamepad()
    {
      PlayerInput input = playerIndex >= 0 && playerIndex < TFGame.PlayerInputs.Length
          ? TFGame.PlayerInputs[playerIndex]
          : null;

      // Joueur au clavier : il saisit via TextInputEXT, et lire ses actions de menu
      // validerait ou fermerait des qu'il tape la lettre de saut ou de tir.
      if (input == null || input is KeyboardInput)
      {
        UpdateRepeat(0, 0);
        return;
      }

      // ...Check et non le pressed : c'est ce qui fait defiler le curseur tant que
      // la direction est maintenue.
      int dx = input.MenuRightCheck ? 1 : (input.MenuLeftCheck ? -1 : 0);
      int dy = input.MenuDownCheck ? 1 : (input.MenuUpCheck ? -1 : 0);

      if (input.MenuConfirm) Append(Charset[selected]);
      if (input.MenuAlt) Backspace();
      if (input.MenuStart) Validate();
      if (input.MenuBack) Cancel();

      UpdateRepeat(dx, dy);
    }

    /// <summary>
    /// Deplacement au maintien : un pas immediat, une pause, puis un defilement
    /// continu. Le compteur repart des que la direction change ou est relachee,
    /// pour que le pas a pas reste possible.
    /// </summary>
    private void UpdateRepeat(int dx, int dy)
    {
      if (dx == 0 && dy == 0)
      {
        heldX = heldY = 0;
        repeatTimer = 0f;
        return;
      }

      if (dx != heldX || dy != heldY)
      {
        heldX = dx;
        heldY = dy;
        repeatTimer = RepeatFirstDelay;
        Move(dx, dy);
        return;
      }

      if (repeatTimer <= 0f)
      {
        repeatTimer = RepeatDelay;
        Move(dx, dy);
      }
    }

    private void Move(int dx, int dy)
    {
      int index = selected + dx + dy * Columns;

      // On borne au lieu de boucler : sauter d'un bout a l'autre de la grille sur
      // une simple pression est desorientant.
      if (index < 0 || index >= Charset.Length) return;

      selected = index;
      Sounds.ui_move1.Play(160f, 1f);
    }

    // --- rendu ---

    public override void Render()
    {
      Draw.Rect(0f, 0f, 320f, 240f, new Color(0, 0, 0, 200));

      Draw.OutlineTextCentered(TFGame.Font, "ENTER A NAME",
          new Vector2(160f, 26f), Color.White, 1.2f);

      // Curseur clignotant : montre qu'on peut taper directement.
      string shown = current + (((int)(blink * 2f) % 2) == 0 ? "_" : " ");
      Draw.OutlineTextCentered(TFGame.Font, shown,
          new Vector2(160f, 52f), Calc.HexToColor("FFEC5E"), 1.4f);

      if (error != null)
        Draw.TextCentered(TFGame.Font, error, new Vector2(160f, 70f), Color.Red);

      for (int i = 0; i < Charset.Length; i++)
      {
        int col = i % Columns;
        int row = i / Columns;
        Vector2 pos = GridOrigin + new Vector2(col * CellW, row * CellH);

        bool active = i == selected;
        // "SP" et non "_" : le tiret bas est un caractere a part entiere de la
        // grille, les confondre rendrait l'un des deux introuvable.
        string label = Charset[i] == ' ' ? "SP" : Charset[i].ToString();

        if (active)
          Draw.Rect(pos.X - 8f, pos.Y - 7f, 16f, 14f, Color.White * 0.25f);

        Draw.TextCentered(TFGame.Font, label, pos,
            active ? Calc.HexToColor("FFEC5E") : Color.White);
      }

      Draw.TextCentered(TFGame.Font, "TYPE ON KEYBOARD OR PICK A LETTER",
          new Vector2(160f, 196f), Color.Gray * 0.8f);
      Draw.TextCentered(TFGame.Font, "A: LETTER  RB: DELETE  START/ENTER: OK  B/ESC: CANCEL",
          new Vector2(160f, 210f), Color.Gray * 0.8f);
    }
  }
}
