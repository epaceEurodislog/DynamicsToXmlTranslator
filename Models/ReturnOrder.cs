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
    /// Structure des données Return Order Dynamics selon le mapping fourni
    /// </summary>
    public class DynamicsReturnOrder
    {
        // Identifiants principaux
        public string? dataAreaId { get; set; }            // Société Référence → OPE_DAT.OPE_CCLI
        public string? ReturnItemNum { get; set; }         // Expected Receipt Number → REA_DAT.REA_RFCE
        public string? SalesId { get; set; }               // N° Commande Vente → REA_DAT.REA_RFTI
        public string? SalesStatus { get; set; }           // Statut Commande Vente
        public DateTime? ReturnDeadline { get; set; }      // Date de réception prévue → REA_DAT.REA_DALP

        // Fournisseur/Client
        public string? CustAccount { get; set; }           // Code tiers fournisseurs/Client → REA_DAT.REA_CTAF
        public string? SalesName { get; set; }             // Nom tiers fournisseurs/Client

        // Code disposition
        public string? ReturnDispositionCodeID { get; set; } // Code disposition

        // Détails ligne
        public int LineNum { get; set; }                   // Numéro ligne de commande → REA_DAT.REA_NoLR
        public string? ItemId { get; set; }                // Référence article → REA_DAT.ART_CODE
        public decimal ExpectedRetQty { get; set; }        // Quantité prévue → REA_DAT.REA_QTRE
        public string? InventLocationId { get; set; }      // Entrepot destinataire D365

        // Traçabilité
        public string? inventBatchId { get; set; }         // Lot → REA_DAT.REA_LOT1
        public string? inventSerialId { get; set; }        // Lot 2 (N° série Machine) → REA_DAT.REA_LOT2
        public DateTime? expDate { get; set; }             // DLUO → REA_DAT.REA_DLUO

        // Support et commentaires
        public string? Notes { get; set; }                 // Commentaires → REA_DAT.REA_COM

        // Propriétés d'export
        public bool XmlExported { get; set; } = false;
        public DateTime? XmlExportDate { get; set; }
        public string? XmlExportBatch { get; set; }
    }
}