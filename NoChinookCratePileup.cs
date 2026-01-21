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
    [Info("No Chinook Crate Pileup", "VisEntities", "1.1.1")]
    [Description("Prevents multiple chinook crates from piling up in the same area.")]
    public class NoChinookCratePileup : RustPlugin
    {
        #region Fields

        private static NoChinookCratePileup _plugin;
        private static Configuration _config;
        private Dictionary<HackableLockedCrate, Timer> _pendingCrates = new Dictionary<HackableLockedCrate, Timer>();

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
            foreach (var kvp in _pendingCrates)
            {
                if (kvp.Value != null)
                    kvp.Value.Destroy();
            }
            _pendingCrates.Clear();
            _pendingCrates = null;
            _config = null;
            _plugin = null;
        }

        private void OnEntitySpawned(HackableLockedCrate crate)
        {
            if (crate == null)
                return;

            if (!crate.wasDropped)
                return;

            StartLandingCheck(crate);
        }

        private void OnEntityKill(HackableLockedCrate crate)
        {
            if (crate == null)
                return;

            StopLandingCheck(crate);
        }

        private void OnCrateLanded(HackableLockedCrate newCrate)
        {
            if (newCrate == null)
                return;

            StopLandingCheck(newCrate);
            RemoveNearbyCrates(newCrate);
        }

        #endregion Oxide Hooks

        #region Core Logic

        private void StartLandingCheck(HackableLockedCrate crate)
        {
            if (_pendingCrates.ContainsKey(crate))
                return;

            Timer checkTimer = timer.Repeat(0.1f, 0, () =>
            {
                if (crate == null || crate.IsDestroyed)
                {
                    StopLandingCheck(crate);
                    return;
                }

                if (crate.hasLanded)
                {
                    StopLandingCheck(crate);
                    RemoveNearbyCrates(crate);
                }
            });

            _pendingCrates[crate] = checkTimer;
        }

        private void StopLandingCheck(HackableLockedCrate crate)
        {
            if (crate == null)
                return;

            Timer existingTimer;
            if (!_pendingCrates.TryGetValue(crate, out existingTimer))
                return;

            if (existingTimer != null)
                existingTimer.Destroy();

            _pendingCrates.Remove(crate);
        }

        private void RemoveNearbyCrates(HackableLockedCrate newCrate)
        {
            if (newCrate == null || newCrate.IsDestroyed)
                return;

            List<HackableLockedCrate> nearbyCrates = Pool.Get<List<HackableLockedCrate>>();
            Vis.Entities(newCrate.transform.position, _config.NearbyCrateSearchRadius, nearbyCrates);

            foreach (HackableLockedCrate crate in nearbyCrates)
            {
                if (crate == newCrate)
                    continue;

                if (crate == null || crate.IsDestroyed)
                    continue;

                if (!crate.hasLanded)
                    continue;

                if (crate.IsBeingHacked())
                    continue;

                crate.Kill();
            }

            Pool.FreeUnmanaged(ref nearbyCrates);
        }

        #endregion Core Logic
    }
}