using System.ComponentModel.DataAnnotations;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un Purchase Order tel qu'il est stocké dans la table JSON_IN
    /// </summary>
    public class PurchaseOrder
    {
        // Correspondance avec la table JSON_IN
        public int Id { get; set; }                    // JSON_KEYU (PK)
        public string JsonData { get; set; }           // JSON_DATA
        public string ContentHash { get; set; }        // JSON_HASH
        public string? ApiEndpoint { get; set; }       // JSON_FROM
        public string? PurchaseOrderId { get; set; }   // Extrait de JSON_BKEY
        public DateTime FirstSeenAt { get; set; }      // JSON_CRDA
        public DateTime LastUpdatedAt { get; set; }    // JSON_CRDA
        public int UpdateCount { get; set; } = 0;      // Calculé

        // Propriétés extraites du JSON Dynamics
        public DynamicsPurchaseOrder? DynamicsData { get; set; }
    }

    /// <summary>
    /// Structure des données Purchase Order Dynamics
    /// </summary>
    public class DynamicsPurchaseOrder
    {
        // Identifiants principaux
        public string? PurchOrderDocNum { get; set; }      // Numéro Attendu Réception
        public string? PurchId { get; set; }               // N° Commande Achat
        public string? DocumentState { get; set; }         // Statut Commande Achat
        public string? PurchTableVersion { get; set; }     // Version table achat
        public string? INT3PLStatus { get; set; }          // Statut 3PL

        // Dates
        public DateTime? ReceiptDate { get; set; }         // Date de réception prévue
        public DateTime? ConfirmedDlv { get; set; }        // Date réception confirmée
        public DateTime? DeliveryDate { get; set; }        // Date réception demandée

        // Fournisseur
        public string? OrderAccount { get; set; }          // Code tiers fournisseur
        public string? PurchName { get; set; }             // Nom tiers fournisseur

        // Détails ligne
        public int LineNumber { get; set; }                // Numéro ligne de commande
        public string? ItemId { get; set; }                // Référence article
        public decimal QtyOrdered { get; set; }            // Quantité prévue
        public string? InventLocationId { get; set; }      // Entrepôt destinataire

        // Traçabilité
        public string? Lot { get; set; }                   // Lot
        public string? Lot2 { get; set; }                  // Lot 2 (N° série Machine)
        public DateTime? DLUO { get; set; }                // DLUO

        // Numéro support et commentaires
        public string? SupportNumber { get; set; }         // Numéro support (SSCC entrant)
        public string? Notes { get; set; }                 // Commentaires
        public string? QualityCode { get; set; }           // Code Qualité
        public string? ReservationRef { get; set; }        // Référence réservation

        // Zone de données
        public string? dataAreaId { get; set; }            // Zone de données
        public string? LotID { get; set; }                   // LotID

        // Propriétés d'export
        public bool XmlExported { get; set; } = false;
        public DateTime? XmlExportDate { get; set; }
        public string? XmlExportBatch { get; set; }

    }
}