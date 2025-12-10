/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("No Chinook Crate Pileup", "VisEntities", "1.1.0")]
    [Description("Prevents multiple chinook crates from piling up in the same area.")]
    public class NoChinookCratePileup : RustPlugin
    {
        #region Fields

        private static NoChinookCratePileup _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Nearby Crate Search Radius")]
            public float NearbyCrateSearchRadius { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                NearbyCrateSearchRadius = 1.5f
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnCrateLanded(HackableLockedCrate newCrate)
        {
            if (newCrate == null)
                return;

            List<HackableLockedCrate> nearbyCrates = Pool.Get<List<HackableLockedCrate>>();
            Vis.Entities(newCrate.transform.position, _config.NearbyCrateSearchRadius, nearbyCrates);

            foreach (HackableLockedCrate crate in nearbyCrates)
            {
                if (crate == newCrate)
                    continue;

                if (crate != null && crate.hasLanded && !crate.IsBeingHacked())
                    crate.Kill();
            }

            Pool.FreeUnmanaged(ref nearbyCrates);
        }

        #endregion Oxide Hooks
    }
}