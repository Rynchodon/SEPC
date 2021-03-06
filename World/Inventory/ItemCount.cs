/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Definitions; // from Sandbox.Game.dll
using Sandbox.Game.Entities; // from Sandbox.Game.dll
using Sandbox.ModAPI;

using VRage; // from VRage.dll and VRage.Library.dll
using VRage.Game; // from VRage.Game.dll
using VRage.Game.Entity; // from VRage.Game.dll
using VRage.Game.ModAPI; // from VRage.Game.dll
using VRage.ModAPI; // from VRage.Game.dll
using VRage.ObjectBuilders;

using SEGarden.Definitions;
using SEGarden.Logging;

namespace SEPC.World.Inventory
{

    public class ItemCount
    {

        public readonly MyFixedPoint Amount;
        public readonly MyDefinitionBase Definition;

        public ItemCount(MyFixedPoint amount, MyDefinitionBase definition)
        {
            Amount = amount;
            Definition = definition;
        }

        public ItemCount(ItemCountDefinition definition)
        {
            Amount = (MyFixedPoint)definition.Count;
            Item = new PhysicalItemType(definition.TypeName, definition.SubtypeName);
        }

        public ItemCountDefinition GetDefinition()
        {
            return new ItemCountDefinition()
            {
                TypeName = Item.TypeName,
                SubtypeName = Item.SubtypeName,
                Count = (double)Amount
            };
        }

    }
}
*/