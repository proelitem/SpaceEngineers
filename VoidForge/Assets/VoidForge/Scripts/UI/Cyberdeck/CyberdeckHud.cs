using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace VoidForge.UI.Cyberdeck
{
    public enum DeckPage { Inventory, Construction, Refinery, Assembly, Map, Systems }

    [RequireComponent(typeof(Canvas), typeof(CyberdeckSession))]
    public sealed partial class CyberdeckHud : MonoBehaviour
    {
        public const float Width = 1914, Height = 822;
        public CyberdeckAssets assets;
        public DeckPage page;
        public CyberdeckSession State { get; private set; }
        public readonly Dictionary<string, Button> Controls = new Dictionary<string, Button>();
        public event Action<string> ActionInvoked;
        public Func<string, bool> BlockAvailable;
        public Func<string, Vector3Int> BlockSize;
        public bool Materials { get; private set; }
        public int BlockIndex { get; private set; } = 2;
        public int Quantity { get; private set; } = 20;
        public float MapRange { get; private set; } = 2500;
        public float MapYaw { get; private set; } = -20;
        public string SelectedWaypoint { get; private set; }
        public float MapPitch { get; private set; } = 22;
        public bool Sector { get; private set; }
        public string Category { get; private set; } = "TOUS";
        public string BlockCategory { get; private set; } = "TOUS LES BLOCS";
        public bool Expanded { get; private set; }
        int recipeIndex, firstRow, gpsRow, firstRecipe;
        bool sourceOpen, categoryOpen, blockFilterOpen, costOpen;
        RectTransform sheet;
        string selectedItem;
        static readonly Color Ink = Hex("041116"), Panel = Hex("06313A"), Low = Hex("02161D"),
            Cyan = Hex("58D9E8"), Edge = Hex("3D9BAC"), White = Hex("F5F3EA"), Amber = Hex("FFB329"), Black = Hex("061116");
        static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");
        static readonly string[] BlockIds = { "Gyroscope", "Battery", "ArmorCube", "Cockpit", "Thruster", "Refinery", "Assembler", "Cargo", "ArmorSlope", "ArmorCorner", "SolarPanel", "Reactor" };
        static readonly string[] BlockNames = { "GYROSCOPE", "BATTERIE", "ARMURE", "COCKPIT", "PROPULSEUR", "RAFFINERIE", "ASSEMBLEUR", "SOUTE", "ARMURE INCLINÉE", "COIN D’ARMURE", "PANNEAU SOLAIRE", "RÉACTEUR" };
        static readonly string[] BlockIcons = { "block-gyroscope", "block-battery", "block-armor", "block-cockpit", "block-thruster", "block-refinery", "block-assembler", "block-cargo", "block-armor", "block-armor", "component-solar-cell", "component-reactor" };

        void Awake() => Initialize();
        public void Initialize()
        {
            if (State != null) State.Changed -= Rebuild;
            if (sheet == null) sheet = transform.Find("Deck interface") as RectTransform;
            State = GetComponent<CyberdeckSession>();
            State.Changed += Rebuild;
            if (assets == null) assets = Resources.Load<CyberdeckAssets>("VoidForge/Cyberdeck/Graphics");
            if (assets != null) Rebuild();
        }
        void OnDestroy() { if (State != null) State.Changed -= Rebuild; }
        public void Show(DeckPage next) { page = next; sourceOpen = categoryOpen = blockFilterOpen = costOpen = false; Rebuild(); }
        public void ResetView()
        {
            recipeIndex = firstRow = gpsRow = firstRecipe = 0; Materials = false; Quantity = 20; BlockIndex = 2;
            MapRange = 2500; MapYaw = -20; MapPitch = 22; Sector = false; Category = "TOUS";
            SelectedWaypoint = selectedItem = null; BlockCategory = "TOUS LES BLOCS";
        }
        public void SetExpanded(bool value) { Expanded = value; ActionInvoked?.Invoke(value ? "expand" : "collapse"); Rebuild(); }
        public bool Invoke(string id)
        {
            if (!Controls.TryGetValue(id, out var button) || !button.interactable || !button.gameObject.activeInHierarchy) return false;
            button.onClick.Invoke(); return true;
        }
        public void Zoom(float multiplier) { MapRange = Mathf.Clamp(MapRange * multiplier, 10, 1000000); Rebuild(); }
        public void Orbit(Vector2 delta) { MapYaw += delta.x; MapPitch = Mathf.Clamp(MapPitch + delta.y, 15, 75); Rebuild(); }
        public void Rebuild()
        {
            if (State == null) State = GetComponent<CyberdeckSession>();
            if (assets == null) return;
            if (sheet != null) { sheet.gameObject.SetActive(false); if (Application.isPlaying) Destroy(sheet.gameObject); else DestroyImmediate(sheet.gameObject); }
            Controls.Clear();
            sheet = Rect("Deck interface", transform, 0, 0, Width, Height);
            Box(sheet, 0, 0, Width, Height, Ink, Ink, 0, 0);
            Line(sheet, 0, 28, 39, 2, Cyan).transform.localRotation = Quaternion.Euler(0, 0, 45);
            Line(sheet, Width - 28, 0, 39, 2, Cyan).transform.localRotation = Quaternion.Euler(0, 0, -45);
            Line(sheet, 0, Height - 28, 39, 2, Cyan).transform.localRotation = Quaternion.Euler(0, 0, -45);
            Line(sheet, Width - 28, Height, 39, 2, Cyan).transform.localRotation = Quaternion.Euler(0, 0, 45);
            GridBackdrop();
            Header();
            switch (page)
            {
                case DeckPage.Inventory: Inventory(); break;
                case DeckPage.Construction: Construction(); break;
                case DeckPage.Refinery: case DeckPage.Assembly:
                    ShipTabs();
                    if (!State.cockpitConnected) LockedShip();
                    else if (page == DeckPage.Refinery) Refinery(); else Assembly();
                    break;
                case DeckPage.Map: Map(); break;
                case DeckPage.Systems: ShipTabs();Systems();break;
            }
            Navigation();
            if(renaming)RenameKeyboard();
            if (State.preview) Label(sheet, "APERÇU • DONNÉES DE RECETTE", 38, 798, 900, 22, 16, Cyan);
            else if (!string.IsNullOrEmpty(State.Notice)) Label(sheet, State.Notice, 38, 798, 1470, 22, 16, Cyan);
            Click("expand", "", 1790, 795, 84, 24, () => SetExpanded(!Expanded), false, null, Expanded ? "ui-minus" : "ui-plus", 16);
        }
        void Header()
        {
            string title = page == DeckPage.Inventory ? "INVENTAIRE" : page == DeckPage.Construction ? "CONSTRUCTION" : page == DeckPage.Map ? "NAVIGATION" : State.shipName;
            Label(sheet, "VF", 54, 3, 87, 62, 51, Cyan);
            Label(sheet, "/", 149, 3, 37, 62, 51, White);
            Label(sheet, title, 188, 3, 1125, 62, 49, White);
            Line(sheet, 48, 69, 1828, 3, Cyan);
            if (page == DeckPage.Refinery || page == DeckPage.Assembly || page == DeckPage.Systems)
            {
                Icon(sheet, "ui-seat", 1395, 11, 58, 45, White);
                Line(sheet, 1475, 13, 3, 43, Cyan);
                Label(sheet, State.cockpitConnected ? "MODULE CONNECTÉ" : "MODULE HORS PORTÉE", 1495, 7, 379, 53, 33, White, TextAnchor.MiddleRight);
            }
            else
            {
                Icon(sheet, "ui-battery", 1651, 18, 99, 37, White);
                if (State.battery >= 0) for (int i = 0; i < 4; i++) if (State.battery > i * .25f) Line(sheet, 1662 + i * 17, 26, 13, 20, Cyan);
                Label(sheet, State.battery < 0 ? "—" : Mathf.RoundToInt(State.battery * 100) + "%", 1763, 7, 111, 53, 43, White, TextAnchor.MiddleRight);
            }
        }
        void Navigation()
        {
            string[] ids = { "inventory", "construction", "ship", "map" };
            string[] names = { "INVENTAIRE", "CONSTRUIRE", "MODULE", "GPS / CARTE" };
            string[] icons = { "ui-inventory", "ui-construction", "ui-module", "ui-gps" };
            DeckPage[] pages = { DeckPage.Inventory, DeckPage.Construction, DeckPage.Systems, DeckPage.Map };
            int active = page == DeckPage.Inventory ? 0 : page == DeckPage.Construction ? 1 : page == DeckPage.Map ? 3 : 2;
            for (int i = 0; i < 4; i++)
            {
                int n = i; float x = 23 + i * 472;
                var button = Click("nav-" + ids[i], "", x, 679, 455, 111, () => Show(pages[n]), active == i);
                Color tint = active == i ? Black : White;
                Icon(button.transform, icons[i], 30, 26, i == 2 ? 112 : 73, 65, tint);
                Line(button.transform, i == 2 ? 170 : 128, 24, 3, 65, active == i ? Black : Cyan);
                Label(button.transform, names[i], i == 2 ? 195 : 154, 14, i == 2 ? 246 : 290, 83, 39, tint);
            }
        }
        void Inventory()
        {
            Box(sheet, 16, 92, 608, 556);
            Box(sheet, 16, 92, 608, 64, Panel, Low, 20, 2);
            Label(sheet, "OUTILS", 48, 94, 540, 60, 49);
            string[] labels = { "DRILL", "WELDER", "GRINDER" }, icons = { "tool-drill", "tool-welder", "tool-grinder" };
            for (int i = 0; i < 3; i++)
            {
                int n = i; float y = 168 + i * 160;
                bool active = State.equippedTool == i || State.selectedTool == i;
                var tool = Click("tool-" + i, "", 29, y, 585, 147, () => State.Equip(n), false, State.CanEquip(i));
                var panel = tool.GetComponent<DeckPanel>();
                if (active) { panel.color = Hex("2D2819"); panel.bottom = Hex("171B16"); panel.border = Amber; panel.thickness = 4; }
                Icon(tool.transform, icons[i], 16, 8, 310, 133, White);
                Label(tool.transform, labels[i], 342, active ? 16 : 34, 231, 65, 49);
                if (active)
                {
                    Box(tool.transform, 347, 91, 176, 46, Amber, Amber, 10, 0);
                    Icon(tool.transform, "ui-check", 360, 101, 34, 25, Black);
                    Label(tool.transform, State.equippedTool == i ? "ÉQUIPÉ" : "PRÊT", 405, 94, 115, 40, 27, Black);
                }
            }
            Click("cargo-source", "STOCK • " + State.cargoName, 645, 92, 1241, 65,
                () => { sourceOpen = !sourceOpen; Rebuild(); }, false, null, "ui-chevron", 46, true);
            Click("filter-ores", "MINERAIS", 645, 171, 322, 54, () => { Materials = false; firstRow = 0; Rebuild(); }, !Materials, null, null, 32);
            Click("filter-materials", "MATÉRIAUX", 979, 171, 354, 54, () => { Materials = true; firstRow = 0; Rebuild(); }, Materials, null, null, 32);
            if (!State.preview) Click("cargo-transfer", State.selectedStorage == "cargo" ? "PRENDRE AU SAC" : "DÉPOSER EN SOUTE", 1345, 171, 541, 54,
                () => State.Transfer(selectedItem), false, State.transferAvailable && State.items.Any(i => i.id == selectedItem), "ui-arrow", 28);
            Box(sheet, 646, 240, 1240, 407);
            Box(sheet, 646, 240, 1240, 46, Panel, Panel, 13, 2);
            Label(sheet, "NOM", 680, 242, 600, 42, 30);
            Label(sheet, "QTÉ", 1701, 242, 116, 42, 30);
            var visible = State.items.FindAll(i => i.material == Materials);
            if (!State.cargoConnected || visible.Count == 0)
                Empty(State.cargoConnected ? "SOUTE VIDE" : "AUCUNE SOUTE CONNECTÉE", "ui-inventory", 653, 300, 1177, 245);
            firstRow = Mathf.Clamp(firstRow, 0, Mathf.Max(0, visible.Count - 3));
            for (int i = 0; i < Mathf.Min(3, visible.Count - firstRow); i++)
            {
                var item = visible[i + firstRow]; float y = 287 + i * 92;
                var row = Click("item-" + item.id, "", 648, y, 1196, 91, () => { selectedItem = item.id; State.Notify(item.name + " • " + Amount(item.quantity) + " " + item.unit); });
                row.GetComponent<DeckPanel>().bevel = 0;
                row.GetComponent<DeckPanel>().thickness = selectedItem == item.id ? 3 : 1;
                if (selectedItem == item.id) row.GetComponent<DeckPanel>().border = Amber;
                Icon(row.transform, item.icon, 30, 7, 153, 78, White);
                Label(row.transform, item.name, 210, 0, 650, 90, 50);
                Label(row.transform, Amount(item.quantity) + " " + item.unit, 840, 0, 321, 90, 50, White, TextAnchor.MiddleRight);
            }
            Line(sheet, 1864, 305, 12, 233, Hex("08303A"));
            Line(sheet, 1864, 305 + (visible.Count < 4 ? 0 : firstRow * (143f / (visible.Count - 3))), 12, 87, Cyan);
            if (visible.Count > 3)
            {
                Click("cargo-up", "", 1817, 288, 68, 67, () => { firstRow--; Rebuild(); }, false, null, "ui-chevron", 30).transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, 180);
                Click("cargo-down", "", 1817, 493, 68, 67, () => { firstRow++; Rebuild(); }, false, null, "ui-chevron", 30);
            }
            Icon(sheet, "ui-inventory", 659, 581, 52, 46, Cyan);
            Bar(sheet, 727, 586, 834, 34, State.capacityLitres > 0 ? State.usedLitres / State.capacityLitres : 0, Amber);
            Label(sheet, Amount(State.usedLitres) + " / " + Amount(State.capacityLitres) + " L", 1585, 572, 283, 62, 39, White, TextAnchor.MiddleRight);
            if (sourceOpen)
            {
                if (State.storages.Count == 0)
                    Click("cargo-source-current", State.cargoConnected ? State.cargoName : "AUCUNE CONNEXION", 645, 159, 1241, 70, () => { sourceOpen = false; Rebuild(); }, true, null, "ui-check", 33, true);
                for (int n = 0; n < State.storages.Count; n++)
                {
                    var storage = State.storages[n]; Click("cargo-source-" + storage.id, storage.name + (storage.available ? "" : " • HORS PORTÉE"), 645, 159 + n * 72, 1241, 70,
                        () => { sourceOpen = false; firstRow = 0; selectedItem = null; State.SelectStorage(storage.id); Rebuild(); }, storage.id == State.selectedStorage, storage.available, "ui-check", 33, true);
                }
            }
        }
        void Construction()
        {
            Click("block-filter", BlockCategory, 35, 92, 1851, 65, () => { blockFilterOpen = !blockFilterOpen; Rebuild(); }, false, null, "ui-chevron", 45, true);
            int[] catalogue = BlockCategory == "STRUCTURE" ? new[] { 2, 8, 9 } : Enumerable.Range(0, BlockIds.Length).ToArray();
            int focus = Array.IndexOf(catalogue, BlockIndex);
            if (focus < 0) { focus = 0; BlockIndex = catalogue[focus]; }
            int centre = focus;
            Click("carousel-left", "", 28, 293, 74, 115, () => MoveBlock(-1), false, null, "ui-chevron", 45).transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, -90);
            Click("carousel-right", "", 1813, 293, 74, 115, () => MoveBlock(1), false, null, "ui-chevron", 45).transform.GetChild(0).localRotation = Quaternion.Euler(0, 0, 90);
            for (int j = -2; j <= 2; j++)
            {
                if (catalogue.Length == 3 && Mathf.Abs(j) > 1) continue;
                int index = catalogue[(centre + j + catalogue.Length) % catalogue.Length];
                float x = j < 0 ? 124 + (j + 2) * 329 : j == 0 ? 765 : 1168 + (j - 1) * 314;
                bool selected = j == 0; float w = selected ? 386 : 295, h = selected ? 332 : 284, y = selected ? 175 : 200;
                var card = Click("block-card-" + j, "", x, y, w, h, () => { BlockIndex = index; Rebuild(); });
                var panel = card.GetComponent<DeckPanel>();
                if (selected) { panel.border = Amber; panel.thickness = 6; panel.color = Hex("302819"); panel.bottom = Hex("191F1A"); }
                Icon(card.transform, BlockIcons[index], selected ? 20 : 16, 12, w - (selected ? 40 : 32), selected ? 244 : 205, White);
                Label(card.transform, BlockNames[index], 8, selected ? 251 : 215, w - 16, 59, selected ? 49 : 38, White, TextAnchor.MiddleCenter);
            }
            for (int i = 0; i < catalogue.Length; i++)
                Box(sheet, 770 + i * 30, 525, 16, 16, i == focus ? Amber : Ink, i == focus ? Amber : Cyan, 8, i == focus ? 0 : 2);
            Box(sheet, 30, 562, 1854, 93);
            string name = BlockIndex == 2 ? "ARMURE LÉGÈRE" : BlockNames[BlockIndex];
            Label(sheet, name, 69, 570, 527, 76, 48);
            Line(sheet, 610, 576, 3, 63, Cyan);
            var size = BlockSize != null ? BlockSize(BlockIds[BlockIndex]) : BlockIndex == 5 ? new Vector3Int(1, 2, 1) : BlockIndex == 6 ? new Vector3Int(2, 1, 1) : Vector3Int.one;
            Box(sheet, 664, 576, 244, 63);
            Label(sheet, $"{size.x} × {size.y} × {size.z}", 670, 577, 232, 60, 41, White, TextAnchor.MiddleCenter);
            Icon(sheet, "component-plates", 987, 582, 82, 50, White);
            var costs = State.BlockIngredients?.Invoke(BlockIds[BlockIndex]);
            if (costs != null && costs.Count > 0)
                Click("block-cost", "COÛT • DÉTAILS", 1078, 579, 310, 61, () => { costOpen = !costOpen; Rebuild(); }, false, null, null, 29);
            else Label(sheet, "COÛT À DÉFINIR", 1088, 581, 293, 53, 28, White);
            bool available = State.preview ? BlockIndex < 5 : BlockAvailable != null && BlockAvailable(BlockIds[BlockIndex]);
            Click("select-block", available ? State.selectedBlock==BlockIds[BlockIndex]?"DÉSÉLECTIONNER":"SÉLECTIONNER" : "INDISPONIBLE", 1425, 570, 450, 77,
                () => State.SelectBlock(BlockIds[BlockIndex]), true, available, null, 42);
            if (blockFilterOpen)
            {
                Click("block-filter-all", "TOUS LES BLOCS", 35, 160, 800, 78, () => { BlockCategory = "TOUS LES BLOCS"; blockFilterOpen = false; Rebuild(); }, true, null, null, 40);
                Click("block-filter-structure", "STRUCTURE", 35, 240, 800, 78, () => { BlockCategory = "STRUCTURE"; blockFilterOpen = false; Rebuild(); }, false, null, null, 40);
            }
            if (costOpen && costs != null)
            {
                Box(sheet, 936, 171, 948, 379, Ink, Amber, 16, 4);
                Label(sheet, "COMPOSANTS POUR UN BLOC", 966, 180, 780, 55, 35);
                Click("block-cost-close", "×", 1790, 179, 70, 55, () => { costOpen = false; Rebuild(); }, false, null, null, 37);
                for (int i = 0; i < costs.Count; i++)
                {
                    var cost = costs[i]; float y = 241 + i * 47;
                    Icon(sheet, cost.icon, 965, y, 65, 42, White);
                    Label(sheet, cost.name, 1045, y, 530, 44, 27);
                    Label(sheet, Amount(cost.stock) + " / " + Amount(cost.perUnit) + " " + cost.unit, 1580, y, 275, 44, 30, cost.stock >= cost.perUnit ? Cyan : Amber, TextAnchor.MiddleRight);
                }
            }
        }
        public void MoveBlock(int delta)
        { if (BlockCategory == "STRUCTURE") { int[] ids = { 2, 8, 9 }; int n = Array.IndexOf(ids, BlockIndex); BlockIndex = ids[(Mathf.Max(0, n) + delta + 3) % 3]; } else BlockIndex = (BlockIndex + delta + BlockIds.Length) % BlockIds.Length; costOpen = false; Rebuild(); }
        public void ScrollRecipes(int delta) { firstRecipe = Mathf.Clamp(firstRecipe + delta, 0, Mathf.Max(0, State.recipes.Count - 3)); Rebuild(); }
        public void ScrollInventory(int delta)
        {
            int count = State.items.Count(i => i.material == Materials);
            firstRow = Mathf.Clamp(firstRow + delta, 0, Mathf.Max(0, count - 3)); Rebuild();
        }
        void ShipTabs()
        {
            Click("ship-systems", "SYSTÈMES", 39, 90, 350, 64, () => Show(DeckPage.Systems), page==DeckPage.Systems, null, null, 36);
            Click("ship-refinery", "RAFFINERIE", 405, 90, 365, 64, () => Show(DeckPage.Refinery), page == DeckPage.Refinery, null, null, 40);
            Click("ship-assembly", "ASSEMBLAGE", 787, 90, 365, 64, () => Show(DeckPage.Assembly), page == DeckPage.Assembly, null, null, 40);
            if (State.cockpitConnected && !State.preview) Click("seat-exit", "SORTIR / RETOUR", 1360, 90, 520, 64, State.LeaveSeat, false, null, "ui-seat", 29);
        }
        void LockedShip() => Empty("APPROCHER LE MODULE POUR UTILISER SES SYSTÈMES", "ui-seat", 30, 178, 1855, 474);
        void Refinery()
        {
            Box(sheet, 28, 177, 452, 477);
            Icon(sheet, "block-refinery", 157, 184, 200, 222, White);
            Label(sheet, "RAFFINERIE 01", 56, 414, 400, 52, 39, White, TextAnchor.MiddleCenter);
            Click("refinery-power", State.refineryPowered ? "ACTIVE" : "ARRÊT", 52, 473, 403, 75, State.TogglePower,
                State.refineryPowered, State.refineryAvailable, "ui-power", 39);
            Box(sheet, 52, 565, 403, 72); Icon(sheet, "ui-energy", 98, 576, 50, 49, Cyan);
            Line(sheet, 190, 578, 3, 45, Cyan);
            Label(sheet, State.refineryAvailable ? Amount(State.refineryKw) + " kW" : "— kW", 217, 573, 227, 55, 42);
            Box(sheet, 497, 177, 738, 361);
            var feed = State.refining.FirstOrDefault();
            var products = State.refineryOutputs;
            Icon(sheet, feed != null ? feed.icon : State.preview ? "ore-iron" : "ui-ore", 548, 194, 245, 151, White);
            Icon(sheet, "ui-arrow", 829, 257, 83, 64, Amber);
            if (products.Count <= 1) Icon(sheet, products.Count == 1 ? products[0].icon : State.preview ? "ingot-iron" : "ui-inventory", 942, 199, 251, 144, White);
            else for (int i = 0; i < Mathf.Min(4, products.Count); i++) { Icon(sheet, products[i].icon, 929 + (i % 2) * 135, 199 + (i / 2) * 66, 124, 58, White); Label(sheet, Amount(products[i].quantity), 946 + (i % 2) * 135, 253 + (i / 2) * 66, 112, 24, 20, Cyan, TextAnchor.MiddleCenter); }
            Label(sheet, feed != null ? feed.name : State.preview ? "FER BRUT" : "AUCUN MINERAI", 529, 344, 300, 61, 32, White, TextAnchor.MiddleCenter);
            Label(sheet, products.Count == 1 ? products[0].name : products.Count > 1 ? "COPRODUITS" : "PRODUITS", 947, 344, 250, 61, 30, White, TextAnchor.MiddleCenter);
            if (!State.preview) Click("refinery-next", "›", 1152, 182, 67, 56, () => State.RefineNext(State.refining[1].id), false, State.refining.Count > 1, null, 36);
            Bar(sheet, 520, 426, 563, 42, State.refineryProgress, Cyan);
            Label(sheet, Mathf.RoundToInt(State.refineryProgress * 100) + "%", 1100, 414, 117, 67, 47, White, TextAnchor.MiddleRight);
            string status = !State.refineryAvailable ? "BLOC NON CONNECTÉ" : State.outputBlocked ? "SORTIE SATURÉE" : !State.refineryPowered ? "ÉNERGIE ABSENTE" : State.refining.Count == 0 ? "FILE VIDE" : State.refineryPaused ? "EN PAUSE" : "EN COURS";
            Label(sheet, status, 525, 476, 684, 55, 39, Cyan, TextAnchor.MiddleCenter);
            Box(sheet, 1253, 177, 634, 361);
            Label(sheet, "FILE DE TRAITEMENT", 1278, 184, 588, 54, 38);
            for (int i = 0; i < Mathf.Min(2, State.refining.Count); i++)
            {
                var item = State.refining[i]; float y = 247 + i * 130;
                Box(sheet, 1266, y, 613, 116);
                Icon(sheet, item.icon, 1285, y + 13, 121, 87, White); Line(sheet, 1418, y + 20, 3, 75, Cyan);
                Label(sheet, item.name + " × " + Amount(item.quantity) + " " + item.unit, 1438, y + 19, i == 0 ? 350 : 428, 75, 43);
                if (i == 0) Click("refinery-job-pause", "", 1798, y + 28, 64, 62, () => State.TogglePause(false), true, State.refineryAvailable, "ui-pause", 32);
            }
            Click("refinery-cargo", "SOUTE", 498, 559, 403, 94, () => Show(DeckPage.Inventory), true, null, "ui-inventory", 43);
            Click("refinery-pause", State.refineryPaused ? "REPRENDRE" : "PAUSE", 921, 559, 382, 94, () => State.TogglePause(false), false, State.refineryAvailable, "ui-pause", State.refineryPaused ? 30 : 43);
            Box(sheet, 1321, 559, 565, 94); Icon(sheet, "ui-check", 1377, 578, 57, 53, Cyan);
            Line(sheet, 1459, 577, 3, 56, Cyan);
            Label(sheet, State.outputBlocked ? "SORTIE SATURÉE" : "SORTIE DISPONIBLE", 1487, 575, 383, 64, 33);
        }
        void Assembly()
        {
            Box(sheet, 28, 172, 536, 501); Box(sheet, 580, 172, 662, 501); Box(sheet, 1257, 172, 630, 501);
            Label(sheet, "RECETTES", 59, 177, 485, 59, 41);
            Line(sheet, 30, 237, 532, 2, Edge);
            firstRecipe = Mathf.Clamp(firstRecipe, 0, Mathf.Max(0, State.recipes.Count - 3));
            if (State.recipes.Count > 3)
            {
                Click("recipes-up", "‹", 421, 180, 55, 50, () => ScrollRecipes(-1), false, firstRecipe > 0, null, 36);
                Click("recipes-down", "›", 485, 180, 55, 50, () => ScrollRecipes(1), false, firstRecipe + 3 < State.recipes.Count, null, 36);
            }
            for (int i = firstRecipe; i < State.recipes.Count && i < firstRecipe + 3; i++)
            {
                int n = i; var recipe = State.recipes[i]; float y = 261 + (i - firstRecipe) * 136;
                var row = Click("recipe-" + recipe.id, "", 44, y, 507, 121, () => { recipeIndex = n; Quantity = Mathf.Max(1, Mathf.Min(Quantity, State.Maximum(recipe))); Rebuild(); }, recipeIndex == i);
                Icon(row.transform, recipe.icon, 25, 12, 152, 97, White);
                Line(row.transform, 208, 20, 3, 80, recipeIndex == i ? Black : White);
                Label(row.transform, recipe.name, 240, 15, 260, 90, 43, recipeIndex == i ? Black : White);
            }
            var chosen = State.recipes.Count == 0 ? null : State.recipes[Mathf.Clamp(recipeIndex, 0, State.recipes.Count - 1)];
            if (chosen == null) Empty("AUCUNE RECETTE CONNECTÉE", "block-assembler", 590, 248, 640, 300);
            else
            {
                Label(sheet, chosen.id == "plates" ? "PLAQUES MÉTALLIQUES" : chosen.name, 626, 177, 599, 60, 39);
                Line(sheet, 582, 236, 658, 2, Edge);
                Icon(sheet, chosen.icon, 720, 242, 380, chosen.ingredients.Count > 0 ? 102 : 142, White);
                if (chosen.ingredients.Count > 0)
                {
                    for (int i = 0; i < Mathf.Min(4, chosen.ingredients.Count); i++)
                    {
                        var ingredient = chosen.ingredients[i]; float x = 608 + (i % 2) * 309, y = 350 + (i / 2) * 59;
                        Box(sheet, x, y, 302, 56); Icon(sheet, ingredient.icon, x + 7, y + 5, 54, 46, White);
                        Label(sheet, ingredient.name, x + 67, y + 1, 230, 25, 20);
                        Label(sheet, Amount(ingredient.stock) + " / " + Amount(ingredient.perUnit * Quantity) + " " + ingredient.unit, x + 67, y + 25, 230, 28, 25, ingredient.stock >= ingredient.perUnit * Quantity ? Cyan : Amber);
                    }
                }
                else
                {
                    Box(sheet, 608, 396, 618, 62);
                    Icon(sheet, "ingot-iron", 661, 402, 94, 50, White); Line(sheet, 770, 405, 3, 41, White);
                    var input = State.items.Find(i => i.id == chosen.inputId);
                    string stock = chosen.available ? chosen.inputName + " : " + Amount(input == null ? 0 : input.quantity) + " / " + Amount(chosen.inputPerUnit * Quantity) + " kg" : "RECETTE À DÉFINIR";
                    Label(sheet, stock, 793, 400, 420, 51, 37);
                }
                int maximum = State.Maximum(chosen);
                Click("quantity-minus", "", 629, 479, 128, 80, () => { Quantity = Mathf.Max(1, Quantity - 1); Rebuild(); }, false, Quantity > 1, "ui-minus", 42);
                Click("quantity-plus", "", 1066, 479, 128, 80, () => { Quantity = Mathf.Min(maximum, Quantity + 1); Rebuild(); }, false, Quantity < maximum, "ui-plus", 42);
                Label(sheet, "QUANTITÉ", 780, 470, 265, 31, 26, White, TextAnchor.MiddleCenter);
                Label(sheet, Quantity.ToString(), 780, 499, 265, 66, 61, White, TextAnchor.MiddleCenter);
                bool possible = maximum >= Quantity && State.assemblerAvailable && !State.outputBlocked;
                string reason = !State.assemblerAvailable ? "BLOC NON CONNECTÉ" : !chosen.available ? "RECETTE INDISPONIBLE" : State.outputBlocked ? "SORTIE SATURÉE" : maximum < Quantity ? "COMPOSANTS INSUFFISANTS" : "AJOUTER À LA FILE";
                Click("assembly-add", reason, 598, 576, 627, 83,
                    () => { if (!State.Enqueue(chosen, Quantity)) State.Notify("ASSEMBLEUR NON CONNECTÉ"); }, true, possible, "ui-inventory", possible ? 37 : 28);
            }
            Label(sheet, "FILE D’ASSEMBLAGE", 1286, 178, 582, 58, 39);
            for (int i = 0; i < Mathf.Min(2, State.assembly.Count); i++)
            {
                var job = State.assembly[i]; float y = 245 + i * 162;
                Box(sheet, 1273, y, 599, 146); Icon(sheet, job.icon, 1292, y + 29, 132, 91, White);
                Line(sheet, 1432, y + 18, 3, 110, Cyan);
                Label(sheet, job.name + " × " + job.quantity, 1455, y + 3, 330, 56, 34);
                if (!State.preview) Click("assembly-cancel-" + job.uid, "×", 1800, y + 5, 60, 50, () => State.CancelAssembly(job.uid), false, true, null, 38);
                if (job.progress > 0)
                {
                    Bar(sheet, 1456, y + 68, 312, 29, job.progress, Cyan);
                    Label(sheet, Mathf.RoundToInt(job.progress * 100) + "%", 1780, y + 61, 83, 45, 33);
                }
                Label(sheet, State.assemblyPaused ? "EN PAUSE" : job.progress > 0 ? "EN COURS" : "EN ATTENTE", 1452, y + 99, 402, 41, 29, Cyan, TextAnchor.MiddleCenter);
            }
            if (State.assembly.Count == 0) Empty("FILE VIDE", "block-assembler", 1270, 250, 600, 275);
            Click("assembly-pause", State.assemblyPaused ? "REPRENDRE" : "PAUSE", 1276, 562, 293, 78, () => State.TogglePause(true), false, State.assemblerAvailable, "ui-pause", State.assemblyPaused ? 22 : 37);
            Click("assembly-cargo", "SOUTE", 1586, 562, 287, 78, () => Show(DeckPage.Inventory), false, null, "ui-inventory", 36);
        }
        void Map()
        {
            Click("map-local", "LOCAL", 1053, 15, 248, 43, () => { Sector = false; MapRange = Mathf.Min(State.localMapRange, 80); Rebuild(); }, !Sector, null, null, 33);
            Click("map-sector", "SECTEUR", 1313, 15, 247, 43, () => { Sector = true; MapRange = State.localMapRange * 10; Rebuild(); }, Sector, null, null, 33);
            var panel = Box(sheet, 23, 97, 1225, 553);
            var map = Rect("Spatial grid", panel.transform, 3, 3, 1219, 547).gameObject.AddComponent<DeckMapGraphic>();
            map.hud = this; map.raycastTarget = false;
            var points = State.waypoints.FindAll(w => Category == "TOUS" || w.category == Category);
            if (SelectedWaypoint == null && points.Count > 0) SelectedWaypoint = points[0].id;
            var plotted = points.OrderBy(p => Vector3.SqrMagnitude(p.Position - State.playerPosition)).Take(5).ToList();
            var selectedPoint = points.Find(p => p.id == SelectedWaypoint);
            if (selectedPoint != null && !plotted.Contains(selectedPoint)) plotted.Add(selectedPoint);
            foreach (var point in plotted.OrderBy(p => p.id == SelectedWaypoint ? 1 : 0))
            {
                Vector2 pos = ProjectMap(point.Position);
                if (pos.x < 40 || pos.x > 1180 || pos.y < 35 || pos.y > 465) continue;
                bool selected = SelectedWaypoint == point.id || (SelectedWaypoint == null && points.IndexOf(point) == 0);
                float markerSize = selected ? 64 : 40;
                var marker = Click("map-marker-" + point.id, "", 23 + pos.x - markerSize * .5f, 97 + pos.y - markerSize * .5f, markerSize, markerSize,
                    () => { SelectedWaypoint = point.id; Rebuild(); });
                marker.GetComponent<DeckPanel>().color = Color.clear; marker.GetComponent<DeckPanel>().bottom = Color.clear;
                marker.GetComponent<DeckPanel>().border = Color.clear;
                Icon(marker.transform, point.icon, 0, 0, markerSize, markerSize, White);
                if (selected)
                {
                    float labelY = Mathf.Max(105, 97 + pos.y - 100);
                    Box(sheet, 23 + pos.x - 122, labelY, 244, 47, Ink, selected ? Amber : Cyan, 11, 3);
                    Label(sheet, point.name, 23 + pos.x - 116, labelY - 1, 232, 48, 28, White, TextAnchor.MiddleCenter);
                }
            }
            Icon(sheet, "map-ship", 606, 413, 55, 40, White);
            Box(sheet, 594, 487, 146, 47); Label(sheet, "VOUS", 598, 486, 138, 49, 31, White, TextAnchor.MiddleCenter);
            Click("map-zoom-in", "", 44, 555, 72, 69, () => Zoom(.7f), false, null, "ui-plus", 38);
            Click("map-zoom-out", "", 135, 555, 72, 69, () => Zoom(1.43f), false, null, "ui-minus", 38);
            Click("map-recenter", "", 225, 555, 74, 69, () => { MapYaw = -20; MapPitch = 22; Rebuild(); }, false, null, "ui-recenter", 43);
            Line(sheet, 526, 611, 79, 3, Cyan); Line(sheet, 711, 611, 75, 3, Cyan);
            Label(sheet, Distance(MapRange * .4f), 607, 582, 102, 47, 31, White, TextAnchor.MiddleCenter);
            Box(sheet, 1264, 99, 625, 550);
            Label(sheet, "REPÈRES GPS", 1291, 106, 345, 59, 39);
            Click("gps-filter", Category, 1640, 113, 233, 48, () => { categoryOpen = !categoryOpen; Rebuild(); }, false, null, "ui-chevron", 31, true);
            if (SelectedWaypoint == null && points.Count > 0) SelectedWaypoint = points[0].id;
            var chosen = points.Find(p => p.id == SelectedWaypoint);
            if (chosen == null && points.Count > 0) { chosen = points[0]; SelectedWaypoint = chosen.id; }
            gpsRow = Mathf.Clamp(gpsRow, 0, Mathf.Max(0, points.Count - 2));
            for (int i = 0; i < Mathf.Min(2, points.Count - gpsRow); i++)
            {
                var point = points[gpsRow + i]; bool active = point.id == SelectedWaypoint;
                var row = Click("gps-" + point.id, "", 1279, 190 + i * 86, 595, 73, () => { SelectedWaypoint = point.id; if (!Sector) MapRange = Mathf.Clamp(Vector3.Distance(State.playerPosition, point.Position) * 5, 10, Mathf.Max(80, State.localMapRange)); Rebuild(); });
                if (active) { var p = row.GetComponent<DeckPanel>(); p.border = Amber; p.color = Hex("2B2A1C"); p.bottom = Hex("19231F"); p.thickness = 3; }
                Icon(row.transform, point.category == "BASES" ? "ui-base" : point.category == "MINERAIS" ? "ui-ore" : "ui-beacon", 33, 14, 89, 47, White);
                Line(row.transform, 148, 12, 3, 48, active ? Amber : Cyan);
                Label(row.transform, point.name, 176, 5, 404, 63, 39);
            }
            if (points.Count > 2)
            {
                Click("gps-previous", "‹", 1803, 350, 33, 34, () => { gpsRow--; Rebuild(); }, false, gpsRow > 0, null, 30);
                Click("gps-next", "›", 1840, 350, 33, 34, () => { gpsRow++; Rebuild(); }, false, gpsRow + 2 < points.Count, null, 30);
            }
            Icon(sheet, "ui-gps", 1305, 372, 51, 55, Amber); Line(sheet, 1395, 376, 3, 51, Cyan);
            Label(sheet, chosen == null ? "AUCUN REPÈRE" : Distance(Vector3.Distance(State.playerPosition, chosen.Position)), 1430, 361, 440, 77, chosen == null ? 32 : 58);
            Vector3 position = chosen == null ? Vector3.zero : chosen.Position;
            float[] coords = { position.x, position.y, position.z }; string[] axes = { "X", "Y", "Z" };
            for (int i = 0; i < 3; i++)
            { Box(sheet, 1280 + i * 200, 447, 193, 52); Label(sheet, axes[i] + " " + coords[i].ToString("+0;-0;0", French), 1288 + i * 200, 446, 177, 54, 30, White, TextAnchor.MiddleCenter); }
            Click("gps-target", chosen != null && State.targetedWaypoint == chosen.id ? "CIBLE ACTIVE" : "CIBLER", 1280, 525, 593, 106,
                () => State.Target(chosen), true, chosen != null, "ui-recenter", 55);
            if (categoryOpen)
            {
                string[] filters = { "TOUS", "BASES", "MINERAIS", "REPÈRES" };
                for (int i = 0; i < filters.Length; i++)
                {
                    string f = filters[i]; Click("gps-filter-" + f, f, 1640, 165 + i * 66, 233, 64,
                        () => { Category = f; categoryOpen = false; gpsRow = 0; Rebuild(); }, Category == f, null, null, 30);
                }
            }
        }
        public Vector2 ProjectMap(Vector3 position)
        {
            Vector3 d = Quaternion.Euler(0, MapYaw, 0) * (position - State.playerPosition);
            float scale = 520 / MapRange;
            return new Vector2(620 + d.x * scale, 330 + (d.z * Mathf.Sin(MapPitch * Mathf.Deg2Rad) - d.y * Mathf.Cos(MapPitch * Mathf.Deg2Rad)) * scale);
        }
        void GridBackdrop()
        {
            for (int x = 60; x < Width; x += 155) Line(sheet, x, 168, 1, 490, new Color(.1f, .4f, .47f, .10f));
            for (int y = 198; y < 660; y += 143) Line(sheet, 23, y, 1865, 1, new Color(.1f, .4f, .47f, .08f));
        }
        void Empty(string message, string icon, float x, float y, float w, float h)
        { Icon(sheet, icon, x + w * .5f - 63, y + h * .15f, 126, 100, Cyan); Label(sheet, message, x + 20, y + h * .6f, w - 40, 75, 38, White, TextAnchor.MiddleCenter); }
        RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        DeckPanel Box(Transform parent, float x, float y, float w, float h, Color? fill = null, Color? edge = null, float bevel = 15, float thickness = 2.5f)
        {
            var graphic = Rect("Panel", parent, x, y, w, h).gameObject.AddComponent<DeckPanel>();
            graphic.color = fill ?? Panel; graphic.bottom = fill.HasValue ? fill.Value : Low;
            graphic.border = edge ?? Edge; graphic.bevel = bevel; graphic.thickness = thickness; graphic.raycastTarget = false; return graphic;
        }
        Text Label(Transform parent, string value, float x, float y, float w, float h, int size, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = Rect(value, parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = assets.font; text.fontStyle = assets.textStyle; text.fontSize = size; text.color = color ?? White;
            text.alignment = align; text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false; text.raycastTarget = false; text.text = value;
            float preferred = text.preferredWidth;
            if (preferred > w) text.fontSize = Mathf.Max(14, Mathf.FloorToInt(size * w / preferred));
            return text;
        }
        Image Line(Transform parent, float x, float y, float w, float h, Color color)
        { var image = Rect("Rule", parent, x, y, w, h).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        void Icon(Transform parent, string id, float x, float y, float w, float h, Color tint)
        {
            Texture2D texture = assets.Find(id); if (texture == null) return;
            var image = Rect(id, parent, x, y, w, h).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.color = tint; image.raycastTarget = false;
            if (id.StartsWith("ui-", StringComparison.Ordinal)) image.material = assets.glyphMaterial;
            // Import metadata carries the occupied UV bounds, keeping transparent padding
            // out of the layout without resampling or stretching the generated graphics.
            var trim = assets.Bounds(id);
            image.uvRect = trim;
            float aspect = texture.width * trim.width / (texture.height * trim.height);
            var rt = image.rectTransform;
            if (w / h > aspect) { float width = h * aspect; rt.anchoredPosition += Vector2.right * (w - width) * .5f; rt.sizeDelta = new Vector2(width, h); }
            else { float height = w / aspect; rt.anchoredPosition -= Vector2.up * (h - height) * .5f; rt.sizeDelta = new Vector2(w, height); }
            rt.pivot = new Vector2(.5f, .5f);
            rt.anchoredPosition += new Vector2(rt.sizeDelta.x * .5f, -rt.sizeDelta.y * .5f);
        }
        Button Click(string id, string title, float x, float y, float w, float h, Action action, bool active = false, bool? enabled = null, string icon = null, int font = 39, bool chevron = false)
        {
            var panel = Box(sheet, x, y, w, h, active ? Amber : (Color?)null, active ? Amber : Cyan, 15, active ? 4 : 2.5f);
            if (active) { panel.bottom = Hex("FFA91C"); panel.innerStroke = 2.5f; }
            panel.name = id; panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>(); button.targetGraphic = panel; button.transition = Selectable.Transition.None;
            button.interactable = enabled ?? true;
            button.onClick.AddListener(() => { ActionInvoked?.Invoke(id); action(); });
            var focus = panel.gameObject.AddComponent<DeckFocus>(); focus.panel = panel; focus.active = active;
            if (!button.interactable) { panel.color = Hex("213338"); panel.bottom = Low; panel.border = Edge; }
            Color tint = active && button.interactable ? Black : White;
            if (icon != null)
            {
                if (chevron) Icon(panel.transform, icon, w - 76, (h - 37) * .5f, 43, 37, tint);
                else if (string.IsNullOrEmpty(title)) Icon(panel.transform, icon, (w - font) * .5f, (h - font) * .5f, font, font, tint);
                else { Icon(panel.transform, icon, 30, h * .22f, h * .64f, h * .56f, tint); Line(panel.transform, h + 18, h * .17f, 3, h * .66f, tint); }
            }
            if (!string.IsNullOrEmpty(title))
            {
                float left = chevron ? 30 : icon == null ? 16 : h + 47;
                Label(panel.transform, title, left, 0, w - left - (chevron ? 90 : 16), h, font, tint,
                    chevron || icon != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            }
            Controls[id] = button; return button;
        }
        void Bar(Transform parent, float x, float y, float w, float h, float fraction, Color tint)
        {
            Box(parent, x, y, w, h, Low, Edge, 12, 2.5f);
            if (fraction > 0) Box(parent, x + 6, y + 6, Mathf.Max(3, (w - 12) * Mathf.Clamp01(fraction)), h - 12, tint, tint, 6, 0);
        }
        static string Amount(float value) => value.ToString("#,##0.###", French);
        static string Distance(float metres) => metres >= 1000 ? (metres / 1000).ToString("0.#", French) + " km" : Mathf.RoundToInt(metres) + " m";
        static Color Hex(string html) { ColorUtility.TryParseHtmlString("#" + html, out var color); return color; }
    }
}
