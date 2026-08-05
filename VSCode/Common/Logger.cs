using System;
using System.IO;
using FortRise;

namespace TFModFortRiseCustomName
{
  public static class Logger {
    static CustomLogger logger;

    /// <summary>
    /// Ouvre le journal du mod dans le dossier Logs de FortRise, a cote de ceux du
    /// launcher, sous la forme "NomDuMod_aaaa-MM-jj_HH-mm-ss.log".
    ///
    /// Avant, l'appelant fournissait un dossier : selon les mods c'etait un chemin
    /// relatif (donc un dossier cree a la racine de TowerFall) ou l'espace de
    /// sauvegarde du mod, et le fichier n'etait nomme que par un compteur de ticks,
    /// impossible a rattacher a un mod ou a une date.
    /// </summary>
    public static void Init(string modName) {
      string path = Path.Combine(
          ModIO.GetRootPath(), "Logs",
          $"{modName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
      logger = new CustomLogger(path);
    }

    public static void WriteLine(string message) {
      logger.WriteLine(message);
    }

    public static void Log(string message, string level) {
      logger.Log(message, level);
    }

    public static void Error(string message) {
      logger.Error(message);
    }

    public static void Info(string message) {
      logger.Info(message);
    }
  }
}
