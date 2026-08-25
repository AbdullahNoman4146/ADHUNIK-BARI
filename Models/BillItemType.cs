namespace ADHUNIK_BARI.Models
{
    /// <summary>
    /// Enum for standard billing item types in the apartment building
    /// </summary>
    public enum BillItemType
    {
        HouseRent = 1,
        ServiceCharge = 2,
        Gas = 3,
        Water = 4,
        Electricity = 5,
        Maintenance = 6,
        Other = 7
    }

    /// <summary>
    /// Helper class for BillItemType constants and descriptions
    /// </summary>
    public static class BillItemTypes
    {
        public const string HouseRent = "HouseRent";
        public const string ServiceCharge = "ServiceCharge";
        public const string Gas = "Gas";
        public const string Water = "Water";
        public const string Electricity = "Electricity";
        public const string Maintenance = "Maintenance";
        public const string Other = "Other";

        public static string GetDescription(string itemType) => itemType switch
        {
            HouseRent => "House Rent",
            ServiceCharge => "Service Charge",
            Gas => "Gas Bill",
            Water => "Water Bill",
            Electricity => "Electricity Bill",
            Maintenance => "Maintenance Fee",
            Other => "Other Charges",
            _ => "Unknown"
        };

        public static List<string> GetAllTypes() => new()
        {
            HouseRent,
            ServiceCharge,
            Gas,
            Water,
            Electricity,
            Maintenance,
            Other
        };
    }
}
