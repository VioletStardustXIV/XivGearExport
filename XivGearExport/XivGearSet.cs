using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory;
using System.Collections.Immutable;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Newtonsoft.Json;

namespace XivGearExport
{
    public class Materia
    {
        [JsonProperty("id")]
        public int Id { get; set; }
    }

    public class Item
    {
        [JsonProperty("id")]
        public uint Id { get; set; }
        
        [JsonProperty("materia")]
        public IList<Materia>? Materia { get; set; }
    }
    
    public class Weapon : Item
    {
        [JsonProperty("relicStats")]
        public RelicStats? RelicStats { get; set; }
    }
    
    public class RelicStats
    {
        [JsonProperty("dhit")]
        public int DirectHit { get; set; }
        
        [JsonProperty("crit")]
        public int Crit { get; set; }
        
        [JsonProperty("tenacity")]
        public int Tenacity { get; set; }
        
        [JsonProperty("determination")]
        public int Determination { get; set; }
        
        [JsonProperty("skillspeed")]
        public int SkillSpeed { get; set; }
        
        [JsonProperty("spellspeed")]
        public int SpellSpeed { get; set; }
        
        [JsonProperty("piety")]
        public int Piety { get; set; }
    }

    public class XivGearItems
    {
        private const uint ItemIdHqOffset = 1_000_000;

        [JsonProperty("Weapon")]
        public Item? Weapon { get; set; }
        
        [JsonProperty("OffHand")]
        public Item? OffHand { get; set; }
        
        [JsonProperty("Head")]
        public Item? Head { get; set; }
        
        [JsonProperty("Body")]
        public Item? Body { get; set; }
        
        [JsonProperty("Hand")]
        public Item? Hand { get; set; }
        
        [JsonProperty("Legs")]
        public Item? Legs { get; set; }
        
        [JsonProperty("Feet")]
        public Item? Feet { get; set; }
        
        [JsonProperty("Ears")]
        public Item? Ears { get; set; }
        
        [JsonProperty("Neck")]
        public Item? Neck { get; set; }
        
        [JsonProperty("Wrist")]
        public Item? Wrist { get; set; }
        
        [JsonProperty("RingLeft")]
        public Item? RingLeft { get; set; }
        
        [JsonProperty("RingRight")]
        public Item? RingRight { get; set; }


        private static uint GetMateriaItemId(ushort materiaId, byte materiaGrade, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet)
        {
            if (!materiaSheet.TryGetRow(materiaId, out var materiaRow))
            {
                return 0;
            }

            // For normal materia, row is materiaId (crit/det/etc), column is materia grade (I, XII, XI, etc)
            var materiaItem = materiaRow.Item[materiaGrade];

            return materiaItem.RowId;
        }

        private static List<uint> GetItemMateriaIds(GameInventoryItem item, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet)
        {
            // returns a list of materia ids that are melded to item
            var materiaList = new List<uint>();

            // iterate over materia slots
            for (var i = 0; i < item.Materia.Length; i++)
            {
                var materiaId = item.Materia[i];
                var materiaGrade = item.MateriaGrade[i];

                // no materia in this slot, assume all after are empty
                if (materiaId == 0)
                {
                    break;
                }

                // add item id of materia to list
                try
                {
                    var materiaItemId = GetMateriaItemId(materiaId, materiaGrade, materiaSheet);
                    if (materiaItemId != 0)
                    {
                        materiaList.Add(materiaItemId);
                    }
                }
                catch (Exception)
                {
                    // We can just ignore this. No use handling weird materia.
                }
            }

            return materiaList;
        }
        
        private static List<uint> GetGearsetItemMateriaIds(RaptureGearsetModule.GearsetItem item, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet)
        {
            // returns a list of materia ids that are melded to item
            var materiaList = new List<uint>();

            // iterate over materia slots
            for (var i = 0; i < item.Materia.Length; i++)
            {
                var materiaId = item.Materia[i];
                var materiaGrade = item.MateriaGrades[i];

                // no materia in this slot, assume all after are empty
                if (materiaId == 0)
                {
                    break;
                }

                // add item id of materia to list
                try
                {
                    var materiaItemId = GetMateriaItemId(materiaId, materiaGrade, materiaSheet);
                    if (materiaItemId != 0)
                    {
                        materiaList.Add(materiaItemId);
                    }
                }
                catch (Exception)
                {
                    // We can just ignore this. No use handling weird materia.
                }
            }

            return materiaList;
        }

        private static IList<Materia> MapMateriaFromGameMateria(List<uint> materia)
        {
            return materia.Select(m => new Materia
            {
                Id = (int)m
            }).ToImmutableList();
        }

        // HQ Items have a weirdly high item offset that xivgear.app doesn't use.
        private static uint ApplyHqOffset(uint itemId)
        {
            if (itemId > ItemIdHqOffset)
            {
                itemId -= ItemIdHqOffset; 
            }

            return itemId;
        }

        public static XivGearItems CreateItemsFromGameInventoryItems(ReadOnlySpan<GameInventoryItem> gameInventoryItems, 
            ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet, 
            ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> mandervilleSheet,
            ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> bozjaSheet)
        {
            var items = new XivGearItems();

            foreach (var item in gameInventoryItems)
            {
                if (item.ItemId == 0)
                {
                    continue;
                }

                var itemId = ApplyHqOffset(item.ItemId);

                if (item.InventorySlot == 0)
                {
                    items.Weapon = new Weapon
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                        RelicStats = GetRelicStats(mandervilleSheet, bozjaSheet, materiaSheet, itemId, item.Materia, item.MateriaGrade)
                    };
                }

                if (item.InventorySlot == 1)
                {
                    items.OffHand = new Weapon
                    {
                        Id = itemId,
                        Materia = [],
                        RelicStats = GetRelicStats(mandervilleSheet, bozjaSheet, materiaSheet, itemId, item.Materia, item.MateriaGrade)
                    };
                }

                if (item.InventorySlot == 2)
                {
                    items.Head = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 3)
                {
                    items.Body = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 4)
                {
                    items.Hand = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 6)
                {
                    items.Legs = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 7)
                {
                    items.Feet = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 8)
                {
                    items.Ears = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 9)
                {
                    items.Neck = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 10)
                {
                    items.Wrist = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 11)
                {
                    items.RingLeft = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 12)
                {
                    items.RingRight = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }
            }

            return items;
        }
        
        public static unsafe XivGearItems CreateItemsFromGearset(RaptureGearsetModule.GearsetEntry* gearset, 
            ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet,
            ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> mandervilleSheet,
            ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> bozjaSheet)
        {
            var items = new XivGearItems();
            
            var mainHand = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.MainHand);
            items.Weapon = new Weapon
            {
                Id = ApplyHqOffset(mainHand.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(mainHand, materiaSheet)),
                RelicStats = GetRelicStats(mandervilleSheet, bozjaSheet, materiaSheet, mainHand.ItemId, mainHand.Materia, mainHand.MateriaGrades),
            };
            
            var offHand = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.OffHand);
            items.OffHand = new Weapon
            {
                Id = ApplyHqOffset(offHand.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(offHand, materiaSheet)),
                RelicStats = GetRelicStats(mandervilleSheet, bozjaSheet, materiaSheet, offHand.ItemId, offHand.Materia, offHand.MateriaGrades),
            };
            
            var head = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Head);
            items.Head = new Item
            {
                Id = ApplyHqOffset(head.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(head, materiaSheet)),
            };
            
            var body = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Body);
            items.Body = new Item
            {
                Id = ApplyHqOffset(body.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(body, materiaSheet)),
            };
            
            var hands = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Hands);
            items.Hand = new Item
            {
                Id = ApplyHqOffset(hands.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(hands, materiaSheet)),
            };
            
            var legs = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Legs);
            items.Legs = new Item
            {
                Id = ApplyHqOffset(legs.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(legs, materiaSheet)),
            };
            
            var feet = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Feet);
            items.Feet = new Item
            {
                Id = ApplyHqOffset(feet.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(feet, materiaSheet)),
            };
            
            var ears = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Ears);
            items.Ears = new Item
            {
                Id = ApplyHqOffset(ears.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(ears, materiaSheet)),
            };
            
            var neck = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Neck);
            items.Neck = new Item
            {
                Id = ApplyHqOffset(neck.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(neck, materiaSheet)),
            };
            
            var wrists = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.Wrists);
            items.Wrist = new Item
            {
                Id = ApplyHqOffset(wrists.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(wrists, materiaSheet)),
            };
            
            var ringRight = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.RingRight);
            items.RingRight = new Item
            {
                Id = ApplyHqOffset(ringRight.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(ringRight, materiaSheet)),
            };
            
            var ringLeft = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.RingLeft);
            
            items.RingLeft = new Item
            {
                Id = ApplyHqOffset(ringLeft.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(ringLeft, materiaSheet)),
            };
            
            return items;
        }

        public static RelicStats GetRelicStats(ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> mandervilleSheet,
            ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> bozjaSheet, 
            ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet, 
            uint itemId, ReadOnlySpan<ushort> materia, ReadOnlySpan<byte> materiaGrades)
        {
            var mandervilleStats = GetMandervilleStats(mandervilleSheet, materiaSheet, itemId, materia, materiaGrades);
            if (mandervilleStats != null)
            {
                return mandervilleStats;
            }
            
            var bozjaStats = GetResistanceWeaponStats(bozjaSheet, materiaSheet, itemId, materia, materiaGrades);
            if (bozjaStats != null)
            {
                return bozjaStats;
            }
            
            return new RelicStats();
        }
        
        public static RelicStats? GetMandervilleStats(ExcelSheet<Lumina.Excel.Sheets.MandervilleWeaponEnhance> mandervilleSheet, 
            ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet, 
            uint itemId, ReadOnlySpan<ushort> materia, ReadOnlySpan<byte> materiaGrades) 
        {
            if (!mandervilleSheet.TryGetRow(itemId, out var mandervilleWeaponEnhanceRow))
            {
                // If the item id isn't in this sheet, it's not a Manderville relic.
                return null;
            }
       
            var stats = new RelicStats();
        
            // In theory there should only be three stats, but there's five
            // materia slots.
            for (var i = 0; i < 4; i++)
            {
                var materiaId = materia[i];
                if (materiaId == 0)
                {
                    continue;
                }

                if (!materiaSheet.TryGetRow(materiaId, out var materiaRow))
                {
                    continue;
                }

                var valueIndex = materiaGrades[i];
                var statValue = materiaRow.Value[valueIndex];

                switch (materiaId)
                {
                    case 1403:
                        stats.Crit = statValue;
                        break;
                    case 1404:
                        stats.DirectHit = statValue;
                        break;
                    case 1405:
                        stats.Determination = statValue;
                        break;
                    case 1406:
                        stats.SkillSpeed = statValue;
                        break;
                    case 1407:
                        stats.SpellSpeed = statValue;
                        break;
                    case 1408:
                        stats.Tenacity = statValue;
                        break;
                    case 1409:
                        stats.Piety = statValue;
                        break;
                }
            }

            return stats;
        }
        
         public static RelicStats? GetResistanceWeaponStats(ExcelSheet<Lumina.Excel.Sheets.ResistanceWeaponAdjust> bozjaSheet, 
             ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet, 
             uint itemId, ReadOnlySpan<ushort> materia, ReadOnlySpan<byte> materiaGrades) 
        {
            if (!bozjaSheet.TryGetRow(itemId, out var resistanceWeaponAdjustRow))
            {
                // If the item id isn't in this sheet, it's not a Bozja relic.
                return null;
            }
       
            var stats = new RelicStats();
        
            // In theory there should only be three stats, but there's five
            // materia slots.
            for (var i = 0; i < 4; i++)
            {
                var materiaId = materia[i];
                if (materiaId == 0)
                {
                    continue;
                }

                if (!materiaSheet.TryGetRow(materiaId, out var materiaRow))
                {
                    continue;
                }
                
                var valueIndex = materiaGrades[i];
                var statValue = materiaRow.Value[valueIndex];
                var affectedStatId = materiaRow.BaseParam.RowId;
                switch (affectedStatId)
                {
                    case 19:
                        stats.Tenacity = statValue;
                        break;
                    case 27:
                        stats.Crit = statValue;
                        break;
                    case 44:
                        stats.Determination = statValue;
                        break;
                    case 22:
                        stats.DirectHit = statValue;
                        break;
                    case 45:
                        stats.SkillSpeed = statValue;
                        break;
                    case 46:
                        stats.SpellSpeed = statValue;
                        break;
                    case 6:
                        stats.Piety = statValue;
                        break;
                }
            }

            return stats;
        }
    }
    
    

    internal class XivGearSet
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        
        [JsonProperty("items")]
        public required XivGearItems Items { get; set; }
    }
}
