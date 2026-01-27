using System;
using System.Collections.Generic;

namespace DomusMercatoris.Core.Exceptions
{
    public class StockInsufficientException : Exception
    {
        public List<StockAdjustment> Adjustments { get; }

        public StockInsufficientException(List<StockAdjustment> adjustments) 
            : base("Stock insufficient for some items.")
        {
            Adjustments = adjustments;
        }
    }

    public class StockAdjustment
    {
        public long ProductId { get; set; }
        public long? VariantProductId { get; set; }
        public string ProductName { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
    }
}
