using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
//using Newtonsoft.Json;
using System.Text.Json;
using FortRise;


namespace TFModFortRiseCustomName
{
  public static class PlayerNameStorage
  {
    //public static string filePath = @".\FortRise\Mods\tf-mod-fortrise-custom-name\playerName.json";
    public static string filePath = Path.Combine(ModIO.GetRootPath(), "Saves", TFModFortRiseCustomNameModule.Instance.Meta.Name, $"{TFModFortRiseCustomNameModule.Instance.Meta.Name}.playerName.json");
    // ----------------------------------------------------
    // SAVE NAMES TO FILE
    // ----------------------------------------------------
    public static void Save(List<string> list)
    {
      try
      {
        //DON'T save the first name "P"
        //string json = JsonConvert.SerializeObject(list.GetRange(1, list.Count - 1), Formatting.Indented);
        string json = JsonSerializer.Serialize(list.GetRange(1, list.Count - 1), new JsonSerializerOptions
        {
          WriteIndented = true
        });
        // Ensure directory exists
        string folder = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(folder))
          Directory.CreateDirectory(folder);

        File.WriteAllText(filePath, json);
      }
      catch (Exception e)
      {
        //logger.LogError("Error saving player names: \" + {e}", e);
        //Logger.Error("Error saving player names: " + e);
      }
    }

    // ----------------------------------------------------
    // LOAD NAMES FROM FILE
    // ----------------------------------------------------
    public static List<string> Load()
    {
      try
      {
        // If file does not exist → create empty list file
        if (!File.Exists(filePath))
        {
          Save(new List<string>());
          return new List<string>();
        }

        string json = File.ReadAllText(filePath);

        //var list = JsonConvert.DeserializeObject<List<string>>(json);
        var list = JsonSerializer.Deserialize<List<string>>(json);

        return list ?? new List<string>();
      }
      catch (Exception e)
      {
        //Logger.Error("Error loading player names: " + e);
        return new List<string>();  // fallback
      }
    }
  }
}