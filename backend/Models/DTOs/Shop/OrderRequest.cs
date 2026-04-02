using System;
using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Shop
{
    public class OrderRequest
    {
        public Guid? LookId { get; set; }
        public List<OrderItemRequest> Items { get; set; } = new();
        public ShippingAddressRequest? ShippingAddress { get; set; }
    }

    public class OrderItemRequest
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string Size { get; set; } = string.Empty;
    }

    public class ShippingAddressRequest
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
    }
}
