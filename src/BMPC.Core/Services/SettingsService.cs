using BMPC.Core.Models;
using System.Text.Json;

namespace BMPC.Core.Services
{
    public class SettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(Constants.SettingsFile))
                {
                    return new AppSettings();
                }

                var json = File.ReadAllText(Constants.SettingsFile);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var dir = Path.GetDirectoryName(Constants.SettingsFile);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(Constants.SettingsFile, json);
        }
    }
}


