using System.ComponentModel.DataAnnotations;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un Transfer Order tel qu'il est stocké dans la table JSON_IN
    /// </summary>
    public class TransferOrder
    {
        // Correspondance avec la table JSON_IN
        public int Id { get; set; }                    // JSON_KEYU (PK)
        public string JsonData { get; set; }           // JSON_DATA
        public string ContentHash { get; set; }        // JSON_HASH
        public string? ApiEndpoint { get; set; }       // JSON_FROM
        public string? TransferOrderId { get; set; }   // Extrait de JSON_BKEY
        public DateTime FirstSeenAt { get; set; }      // JSON_CRDA
        public DateTime LastUpdatedAt { get; set; }    // JSON_CRDA
        public int UpdateCount { get; set; } = 0;      // Calculé

        // Propriétés extraites du JSON Dynamics
        public DynamicsTransferOrder? DynamicsData { get; set; }
    }

    /// <summary>
    /// Structure des données Transfer Order Dynamics selon votre JSON
    /// </summary>
    public class DynamicsTransferOrder
    {
        // Identifiants principaux
        public string? dataAreaId { get; set; }            // "br" → Société Référence
        public string? TransferId { get; set; }            // "OT000021" → N° Commande Transfer
        public string? ExpectedReceiptNumber { get; set; } // "OT000021" → Expected Receipt Number
        public DateTime? ReceiveDate { get; set; }         // "2025-06-16T12:00:00Z" → Date de réception prévue

        // Détails ligne
        public int LineNum { get; set; }                   // 1 → Numéro ligne de commande
        public string? ItemId { get; set; }                // "CRDEF500" → Référence article
        public decimal QtyShipped { get; set; }            // 1000 → Quantité prévue
        public string? InventLocationIdTo { get; set; }    // "12" → Entrepot destinataire D365

        // Traçabilité
        public string? inventBatchId { get; set; }         // "50022" → Lot
        public string? inventSerialId { get; set; }        // "" → Lot 2 (N° série Machine)
        public DateTime? expDate { get; set; }             // "2029-05-20T12:00:00Z" → DLUO

        // Support et commentaires
        public string? Notes { get; set; }                 // "Urgent" → Commentaires
        public string? LicensePlateId { get; set; }        // "VSTEST009" → Numéro support (SSCC entrant)

        // Code qualité
        public string? PdsDispositionCode { get; set; }    // "Libéré" → Code Qualité

        // Statut et transaction
        public string? INT3PLStatus { get; set; }          // "None" → Statut 3PL
        public string? InventTransId { get; set; }         // "BR-001654" → Transaction ID

        public string? LotID { get; set; }

        // Propriétés d'export
        public bool XmlExported { get; set; } = false;
        public DateTime? XmlExportDate { get; set; }
        public string? XmlExportBatch { get; set; }
    }
}