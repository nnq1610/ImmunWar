using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImmunWar.Core.Config
{
    public sealed class CatalogLoader
    {
        public GameCatalog Catalog { get; }
        private readonly Dictionary<string, GameConfig> _lookup;

        public CatalogLoader(GameCatalog catalog)
        {
            var validation = ConfigValidator.Validate(catalog);
            if (!validation.IsValid) throw new InvalidOperationException(string.Join("\n", validation.Errors));
            Catalog = catalog;
            // A missing reference (e.g. an asset Unity failed to import) must not take the whole game down.
            var configs = catalog.AllConfigs().ToArray();
            if (configs.Any(x => !x)) Debug.LogWarning("GameCatalog contains missing config references; they are ignored.");
            _lookup = configs.Where(x => x).ToDictionary(x => x.Id, x => x, StringComparer.Ordinal);
        }

        public T Get<T>(string id) where T : GameConfig
        {
            return _lookup.TryGetValue(id, out var config) ? config as T : null;
        }

        public static CatalogLoader LoadFromResources(string path = "ImmuneWar/GameCatalog")
        {
            var catalog = Resources.Load<GameCatalog>(path);
            if (catalog == null) throw new InvalidOperationException($"Catalog not found at Resources/{path}.");
            return new CatalogLoader(catalog);
        }
    }
}

