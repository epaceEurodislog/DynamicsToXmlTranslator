using System.ComponentModel.DataAnnotations;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un Return Order tel qu'il est stocké dans la table JSON_IN
    /// </summary>
    public class ReturnOrder
    {
        // Correspondance avec la table JSON_IN
        public int Id { get; set; }                    // JSON_KEYU (PK)
        public string JsonData { get; set; }           // JSON_DATA
        public string ContentHash { get; set; }        // JSON_HASH
        public string? ApiEndpoint { get; set; }       // JSON_FROM
        public string? ReturnOrderId { get; set; }     // Extrait de JSON_BKEY
        public DateTime FirstSeenAt { get; set; }      // JSON_CRDA
        public DateTime LastUpdatedAt { get; set; }    // JSON_CRDA
        public int UpdateCount { get; set; } = 0;      // Calculé

        // Propriétés extraites du JSON Dynamics
        public DynamicsReturnOrder? DynamicsData { get; set; }
    }

    /// <summary>
    /// Structure des données Return Order Dynamics selon votre JSON
    /// </summary>
    public class DynamicsReturnOrder
    {
        // Identifiants principaux
        public string? dataAreaId { get; set; }            // "br" → Société Référence
        public string? SalesId { get; set; }               // "SO000191" → N° Commande Vente
        public string? ReturnItemNum { get; set; }         // "OR000021" → Expected Receipt Number
        public string? SalesStatus { get; set; }           // "Backorder" → Statut Commande Vente
        public DateTime? ReturnDeadline { get; set; }      // "2025-06-30T12:00:00Z" → Date de réception prévue

        // Fournisseur/Client
        public string? CustAccount { get; set; }           // "C0000002" → Code tiers client
        public string? SalesName { get; set; }             // "ALMEDA ENTERPRISE COMPANY LTD" → Nom tiers client

        // Code disposition
        public string? ReturnDispositionCodeId { get; set; } // "" → Code disposition

        // Détails ligne
        public int LineNum { get; set; }                   // 1 → Numéro ligne de commande
        public string? ItemId { get; set; }                // "BAINP200" → Référence article
        public decimal ExpectedRetQty { get; set; }        // 0 → Quantité prévue
        public string? InventLocationId { get; set; }      // "12" → Entrepot destinataire D365

        // Traçabilité
        public string? inventBatchId { get; set; }         // "" → Lot
        public string? inventSerialId { get; set; }        // "" → Lot 2 (N° série Machine)
        public DateTime? expDate { get; set; }             // "1900-01-01T12:00:00Z" → DLUO

        // Support et commentaires
        public string? Notes { get; set; }                 // "" → Commentaires

        // Statut 3PL
        public string? INT3PLStatus { get; set; }          // "None" → Statut 3PL

        public string? LotID { get; set; }                   // LotID

        // Propriétés d'export
        public bool XmlExported { get; set; } = false;
        public DateTime? XmlExportDate { get; set; }
        public string? XmlExportBatch { get; set; }
    }
}