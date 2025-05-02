using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory;
using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Lumina.Excel;
using Newtonsoft.Json;

namespace XivGearExport
{
    internal class Materia
    {
        [JsonProperty("id")]
        public int id { get; set; }
    }

    internal class Item
    {
        [JsonProperty("id")]
        public uint Id { get; set; }
        
        [JsonProperty("materia")]
        public IList<Materia>? Materia { get; set; }
    }

    internal class XivGearItems
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

        public static List<uint> GetItemMateriaIds(GameInventoryItem item, ExcelSheet<Lumina.Excel.Sheets.Materia> materiaSheet)
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

        private static IList<Materia> MapMateriaFromGameMateria(List<uint> materia)
        {
            return materia.Select(m => new Materia
            {
                id = (int)m
            }).ToImmutableList();
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

                var itemId = item.ItemId;
                if (itemId > ItemIdHqOffset)
                {
                    itemId -= ItemIdHqOffset; 
                }

                if (item.InventorySlot == 0)
                {
                    items.Weapon = new Item
                    {
                        Id = itemId,
                        Materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 1)
                {
                    items.OffHand = new Item
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
    }

    internal class XivGearSet
    {
        [JsonProperty("name")]
        public required string Name { get; set; }
        
        [JsonProperty("items")]
        public required XivGearItems Items { get; set; }
    }
}
