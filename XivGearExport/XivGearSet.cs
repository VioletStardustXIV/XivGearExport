using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel;
using Newtonsoft.Json;

namespace XivGearExport
{
    public class Materia
    {
        [JsonProperty("id")]
        public int id { get; set; }
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
        public int? DirectHit { get; set; }
        
        [JsonProperty("crit")]
        public int? Crit { get; set; }
        
        [JsonProperty("tenacity")]
        public int? Tenacity { get; set; }
        
        [JsonProperty("determination")]
        public int? Determination { get; set; }
        
        [JsonProperty("skillspeed")]
        public int? SkillSpeed { get; set; }
        
        [JsonProperty("spellspeed")]
        public int? SpellSpeed { get; set; }
        
        [JsonProperty("piety")]
        public int? Piety { get; set; }
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

            // row is materiaId (the type: crt, det, etc), column is materia grade (I, II, III, etc)
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
                id = (int)m
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

        public static XivGearItems CreateItemsFromGameInventoryItems(ReadOnlySpan<GameInventoryItem> gameInventoryItems, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet)
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
                        // TODO: If relic, add relic stats for main and offhand.
                    };
                }

                if (item.InventorySlot == 1)
                {
                    items.OffHand = new Weapon
                    {
                        Id = itemId,
                        Materia = []
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
        
        public static unsafe XivGearItems CreateItemsFromGearset(RaptureGearsetModule.GearsetEntry* gearset, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet)
        {
            var items = new XivGearItems();
            
            var mainHand = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.MainHand);
            items.Weapon = new Weapon
            {
                Id = ApplyHqOffset(mainHand.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(mainHand, materiaSheet)),
                // TODO: If relic, add relic stats for main and offhand.
            };
            
            var offHand = gearset->GetItem(RaptureGearsetModule.GearsetItemIndex.OffHand);
            items.OffHand = new Weapon
            {
                Id = ApplyHqOffset(offHand.ItemId),
                Materia = MapMateriaFromGameMateria(GetGearsetItemMateriaIds(offHand, materiaSheet)),
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
    }

    internal class XivGearSet
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        
        [JsonProperty("items")]
        public required XivGearItems Items { get; set; }
    }
}
