namespace StyleScan.Backend.Models.DTOs.Avatar
{
    public class UpdateAvatarRequest
    {
        public string? Name { get; set; }
        public string? Gender { get; set; }
        public string? BodyType { get; set; }
        public string? SkinTone { get; set; }
        public AvatarMeasurements? Measurements { get; set; }
    }

    public class AvatarMeasurements
    {
        public double Height { get; set; }
        public double Chest { get; set; }
        public double Waist { get; set; }
        public double Hips { get; set; }
    }
}
