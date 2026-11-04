using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidForge.UI.Cyberdeck
{
    [Serializable] public sealed class DeckItem
    {
        public string id, name, icon, unit = "kg";
        public float quantity;
        public bool material;
        public DeckItem(string id, string name, string icon, float quantity, bool material = false, string unit = "kg")
        { this.id = id; this.name = name; this.icon = icon; this.quantity = quantity; this.material = material; this.unit = unit; }
    }
    [Serializable] public sealed class DeckRecipe
    {
        public string id, name, icon, inputId, inputName;
        public float inputPerUnit;
        public bool available;
        public int maxCraftable = -1;
        public List<DeckIngredient> ingredients = new List<DeckIngredient>();
    }
    [Serializable] public sealed class DeckIngredient { public string id, name, icon, unit; public float perUnit, stock; }
    [Serializable] public sealed class DeckStorage { public string id, name; public bool available; }
    [Serializable] public sealed class DeckJob
    {
        public string uid, recipeId, name, icon;
        public int quantity;
        [Range(0, 1)] public float progress;
    }
    [Serializable] public sealed class DeckWaypoint
    {
        public string id, name, category, icon;
        public Vector3 position;
        [NonSerialized] public Transform target;
        public Vector3 Position => target != null ? target.position : position;
    }

    // UI state and command boundary. Gameplay backends publish snapshots and validate commands.
    // No quantities or recipes from the concept art become production game balance.
    public sealed class CyberdeckSession : MonoBehaviour
    {
        public bool preview;
        public bool cargoConnected, cockpitConnected, refineryAvailable, assemblerAvailable;
        public string cargoName = "AUCUNE SOUTE CONNECTÉE", shipName = "VAISSEAU";
        public float usedLitres, capacityLitres, battery = -1;
        public bool refineryPowered, refineryPaused, assemblyPaused, outputBlocked;
        public float refineryProgress, refineryKw;
        public int equippedTool = -1;
        public int selectedTool = -1;
        public string selectedBlock, targetedWaypoint;
        public Vector3 playerPosition;
        public float localMapRange = 2500;
        public List<DeckItem> items = new List<DeckItem>();
        public List<DeckRecipe> recipes = new List<DeckRecipe>();
        public List<DeckJob> assembly = new List<DeckJob>();
        public List<DeckItem> refining = new List<DeckItem>();
        public List<DeckWaypoint> waypoints = new List<DeckWaypoint>();
        public List<DeckStorage> storages = new List<DeckStorage>();
        public List<DeckItem> refineryOutputs = new List<DeckItem>();
        public string selectedStorage = "backpack";
        public bool transferAvailable;
        public bool[] toolsAvailable;
        public float refineryBatch;
        public Func<string, int, bool> AssemblyCommand;
        public Func<string, List<DeckIngredient>> BlockIngredients;
        public event Action<string> StorageRequested, TransferRequested, CancelAssemblyRequested, RefineryOreRequested;
        public event Action SeatExitRequested;
        public event Action Changed;
        public event Action<int> EquipRequested;
        public event Action<string> BlockRequested;
        public event Action<string, int> AssemblyRequested;
        public event Action<bool> RefineryPowerRequested, RefineryPauseRequested, AssemblyPauseRequested;
        public string Notice { get; private set; }
        public bool CanEquip(int tool) => tool >= 0 && tool < 3 && (toolsAvailable == null || toolsAvailable.Length == 0 || (tool < toolsAvailable.Length && toolsAvailable[tool]));
        public void Publish() => Changed?.Invoke();
        public void Notify(string message) { Notice = message; Publish(); }
        public void Equip(int tool)
        {
            if (!CanEquip(tool)) { Notify("OUTIL ABSENT DU SAC"); return; }
            selectedTool = tool;
            if (preview) { equippedTool = tool; Publish(); }
            else EquipRequested?.Invoke(tool);
        }
        public void SelectBlock(string id) { selectedBlock = id; Notify("BLOC SÉLECTIONNÉ • " + id.ToUpperInvariant()); BlockRequested?.Invoke(id); }
        public void SelectStorage(string id) => StorageRequested?.Invoke(id);
        public void Transfer(string id) => TransferRequested?.Invoke(id);
        public void CancelAssembly(string id) => CancelAssemblyRequested?.Invoke(id);
        public void RefineNext(string id) => RefineryOreRequested?.Invoke(id);
        public void LeaveSeat() => SeatExitRequested?.Invoke();
        public void DisconnectCockpit()
        {
            cockpitConnected = refineryAvailable = assemblerAvailable = false;
            Publish();
        }
        public int Maximum(DeckRecipe recipe)
        {
            if (recipe != null && recipe.available && recipe.maxCraftable >= 0) return Mathf.Clamp(recipe.maxCraftable, 0, 999);
            if (recipe != null && recipe.available && recipe.ingredients.Count > 0)
            {
                int n = 999; foreach (var input in recipe.ingredients) n = Mathf.Min(n, input.perUnit > 0 ? Mathf.FloorToInt(input.stock / input.perUnit) : 0); return n;
            }
            if (!cargoConnected || recipe == null || !recipe.available || recipe.inputPerUnit <= 0) return 0;
            var stock = items.Find(i => i.id == recipe.inputId);
            return stock == null ? 0 : Mathf.Clamp(Mathf.FloorToInt(stock.quantity / recipe.inputPerUnit), 0, 999);
        }
        public bool Enqueue(DeckRecipe recipe, int quantity)
        {
            if (!cockpitConnected || !assemblerAvailable || outputBlocked || quantity < 1 || quantity > Maximum(recipe)) return false;
            if (!preview) { if (AssemblyCommand != null) return AssemblyCommand(recipe.id, quantity); AssemblyRequested?.Invoke(recipe.id, quantity); return AssemblyRequested != null; }
            items.Find(i => i.id == recipe.inputId).quantity -= recipe.inputPerUnit * quantity;
            assembly.Add(new DeckJob { recipeId = recipe.id, name = recipe.name, icon = recipe.icon, quantity = quantity });
            Publish(); return true;
        }
        public void TogglePower()
        {
            if (!cockpitConnected || !refineryAvailable) return;
            if (preview) { refineryPowered = !refineryPowered; Publish(); }
            else RefineryPowerRequested?.Invoke(!refineryPowered);
        }
        public void TogglePause(bool assembling)
        {
            if (!cockpitConnected || (assembling ? !assemblerAvailable : !refineryAvailable)) return;
            if (preview) { if (assembling) assemblyPaused = !assemblyPaused; else refineryPaused = !refineryPaused; Publish(); }
            else if (assembling) AssemblyPauseRequested?.Invoke(!assemblyPaused);
            else RefineryPauseRequested?.Invoke(!refineryPaused);
        }
        public void Target(DeckWaypoint point)
        { if (point == null) return; targetedWaypoint = point.id; Notify("CIBLE GPS • " + point.name); }

        public void LoadPreview()
        {
            preview = true; cargoConnected = cockpitConnected = refineryAvailable = assemblerAvailable = true;
            cargoName = shipName = "ODYSSEY-01"; battery = .86f;
            usedLitres = 1240; capacityLitres = 2000; equippedTool = selectedTool = 0;
            refineryPowered = true; refineryPaused = assemblyPaused = outputBlocked = false;
            refineryProgress = .62f; refineryKw = 120;
            items.Clear(); refining.Clear(); recipes.Clear(); assembly.Clear(); waypoints.Clear();
            items.Add(new DeckItem("iron-ore", "FER", "ore-iron", 480));
            items.Add(new DeckItem("nickel-ore", "NICKEL", "ore-nickel", 120));
            items.Add(new DeckItem("silicon-ore", "SILICIUM", "ore-silicon", 85));
            items.Add(new DeckItem("iron", "FER", "ingot-iron", 120, true));
            items.Add(new DeckItem("plates", "PLAQUES", "component-plates", 24, true, "u"));
            items.Add(new DeckItem("tubes", "TUBES", "component-tubes", 8, true, "u"));
            refining.Add(items[0]); refining.Add(items[1]);
            recipes.Add(new DeckRecipe { id = "plates", name = "PLAQUES", icon = "component-plates", inputId = "iron", inputName = "FER", inputPerUnit = 2, available = true });
            // Unspecified recipes remain unavailable, including in the preview.
            recipes.Add(new DeckRecipe { id = "tubes", name = "TUBES", icon = "component-tubes" });
            recipes.Add(new DeckRecipe { id = "circuits", name = "CIRCUITS", icon = "component-circuits" });
            assembly.Add(new DeckJob { recipeId = "plates", name = "PLAQUES", icon = "component-plates", quantity = 20, progress = .45f });
            assembly.Add(new DeckJob { recipeId = "tubes", name = "TUBES", icon = "component-tubes", quantity = 8 });
            waypoints.Add(new DeckWaypoint { id = "outpost", name = "AVANT-POSTE", category = "BASES", icon = "map-outpost", position = new Vector3(1240, 180, -2045) });
            waypoints.Add(new DeckWaypoint { id = "iron-a07", name = "FER A-07", category = "MINERAIS", icon = "map-asteroid", position = new Vector3(-800, 50, -1500) });
            playerPosition = Vector3.zero; targetedWaypoint = null; Notice = null; Publish();
        }
    }
}
