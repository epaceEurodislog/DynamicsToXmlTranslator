namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un article tel qu'il est stocké dans la base de données (format JSON Dynamics)
    /// </summary>
    public class Article
    {
        public int Id { get; set; }
        public string JsonData { get; set; }
        public string ItemId { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        
        // Propriétés extraites du JSON Dynamics
        public DynamicsArticle DynamicsData { get; set; }
    }

    /// <summary>
    /// Structure des données Dynamics (correspond au JSON de l'API)
    /// </summary>
    public class DynamicsArticle
    {
        public string ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string BarcodeNumber { get; set; }
        public string ItemGroupId { get; set; }
        public string ProductTypeId { get; set; }
        public string SalesUnitSymbol { get; set; }
        public decimal NetWeight { get; set; }
        public decimal GrossWeight { get; set; }
        public string VendorAccountNumber { get; set; }
        public string VendorItemNumber { get; set; }
        public decimal SalesPrice { get; set; }
        public string ItemDimensionGroupId { get; set; }
        public string StorageDimensionGroupId { get; set; }
        public string TrackingDimensionGroupId { get; set; }
        public int ShelfLifePeriodDays { get; set; }
        public string DataAreaId { get; set; }
        
        // Ajoutez d'autres propriétés selon vos besoins
    }
}