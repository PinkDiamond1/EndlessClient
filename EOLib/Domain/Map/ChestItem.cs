﻿using Amadevus.RecordGenerator;

namespace EOLib.Domain.Map
{
    [Record]
    public sealed partial class ChestItem
    {
        public short ItemID { get; }

        public int Amount { get; }

        public int Slot { get; }
    }
}
