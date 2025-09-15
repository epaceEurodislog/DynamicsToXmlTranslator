namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un article tel qu'il est stocké dans votre table JSON_IN
    /// </summary>
    public class Article
    {
        // Correspondance avec la nouvelle table JSON_IN
        public int Id { get; set; }                    // JSON_KEY (PK)
        public string JsonData { get; set; }           // JSON_DATA
        public string ContentHash { get; set; }        // JSON_HASH (nouveau champ)
        public string? ApiEndpoint { get; set; }       // JSON_FROM
        public string? ItemId { get; set; }            // Extrait de JSON_BKEY
        public DateTime FirstSeenAt { get; set; }      // JSON_CRD
        public DateTime LastUpdatedAt { get; set; }    // JSON_CRD (même valeur)
        public int UpdateCount { get; set; } = 0;      // Calculé (non stocké)

        // Propriétés extraites du JSON Dynamics
        public DynamicsArticle? DynamicsData { get; set; }
    }

    /// <summary>
    /// Structure des données Dynamics (correspond au nouveau JSON de l'API)
    /// </summary>
    public class DynamicsArticle
    {
        // Propriétés principales
        public string? ItemId { get; set; }
        public string? Name { get; set; }  // Remplace ItemName
        public string? Category { get; set; }  // Nouveau champ
        public string? ExternalItemId { get; set; }  // Nouveau champ

        // Propriétés de poids et dimensions
        public decimal GrossWeight { get; set; }
        public decimal Weight { get; set; }  // Poids net
        public decimal Height { get; set; }
        public decimal Width { get; set; }
        public decimal Depth { get; set; }
        public decimal grossHeight { get; set; }  // Dimensions brutes
        public decimal grossWidth { get; set; }
        public decimal grossDepth { get; set; }

        // Code-barres et identification
        public string? itemBarCode { get; set; }  // Remplace BarcodeNumber
        public string? ItemGroupId { get; set; }
        public string? UnitId { get; set; }  // Remplace SalesUnitSymbol

        // Propriétés de suivi et statut
        public string? INT3PLStatus { get; set; }  // Nouveau champ
        public int TrackingLot1 { get; set; }  // Nouveau champ
        public int TrackingLot2 { get; set; }  // Nouveau champ
        public int TrackingProoftag { get; set; }  // Nouveau champ
        public int TrackingDLCDDLUO { get; set; }  // Nouveau champ

        // Durée de vie et cycle produit
        public int PdsShelfLife { get; set; }  // Remplace ShelfLifePeriodDays
        public string? ProductLifecycleStateId { get; set; }  // Nouveau champ
        public string? ProducVersionAttribute { get; set; }  // Nouveau champ

        // Facteurs et quantités
        public int FactorColli { get; set; }  // Nouveau champ
        public int FactorPallet { get; set; }  // Nouveau champ

        // Informations réglementaires
        public string? IntrastatCommodity { get; set; }  // Nouveau champ
        public string? OrigCountryRegionId { get; set; }  // Nouveau champ
        public int HMIMIndicator { get; set; }  // Nouveau champ

        // Zone de données
        public string? dataAreaId { get; set; }  // Remplace DataAreaId

        public bool XmlExported { get; set; } = false;
        public DateTime? XmlExportDate { get; set; }
        public string? XmlExportBatch { get; set; }
    }
}