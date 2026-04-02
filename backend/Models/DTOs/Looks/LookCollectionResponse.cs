using System.Collections.Generic;

namespace StyleScan.Backend.Models.DTOs.Looks
{
    public class LookCollectionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public List<LookResponse> Looks { get; set; } = new();
    }
}
