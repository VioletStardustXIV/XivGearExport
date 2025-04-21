using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Inventory;
using System.Collections.Immutable;
using Lumina.Excel;

namespace XivGearExport
{
    internal class Materia
    {
        public int id { get; set; }
    }

    internal class Item
    {
        public uint id { get; set; }
        public IList<Materia> materia { get; set; }
    }

    internal class XivGearItems
    {
        public static readonly uint ItemIdHqOffset = 1_000_000;

        public Item Weapon { get; set; }
        public Item OffHand { get; set; }
        public Item Head { get; set; }
        public Item Body { get; set; }
        public Item Hand { get; set; }
        public Item Legs { get; set; }
        public Item Feet { get; set; }
        public Item Ears { get; set; }
        public Item Neck { get; set; }
        public Item Wrist { get; set; }
        public Item RingLeft { get; set; }
        public Item RingRight { get; set; }


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
                catch (Exception e)
                {
                    // We can just ignore this. No use handling weird materia.
                }
            }

            return materiaList;
        }

        public static IList<Materia> MapMateriaFromGameMateria(List<uint> materia)
        {
            return materia.Select(m =>
             {
                 return new Materia
                 {
                     id = (int)m
                 };
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
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 1)
                {
                    items.OffHand = new Item
                    {
                        id = itemId,
                        materia = []
                    };
                }

                if (item.InventorySlot == 2)
                {
                    items.Head = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 3)
                {
                    items.Body = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 4)
                {
                    items.Hand = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 6)
                {
                    items.Legs = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 7)
                {
                    items.Feet = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 8)
                {
                    items.Ears = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 9)
                {
                    items.Neck = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 10)
                {
                    items.Wrist = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 11)
                {
                    items.RingLeft = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }

                if (item.InventorySlot == 12)
                {
                    items.RingRight = new Item
                    {
                        id = itemId,
                        materia = MapMateriaFromGameMateria(GetItemMateriaIds(item, materiaSheet)),
                    };
                }
            }

            return items;


        }
    }

    internal class XivGearSet
    {
        public string name { get; set; }
        public XivGearItems items { get; set; }
    }
}
