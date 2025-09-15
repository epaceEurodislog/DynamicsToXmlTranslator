using System.ComponentModel.DataAnnotations;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un Packing Slip tel qu'il est stocké dans la table JSON_IN
    /// </summary>
    public class PackingSlip
    {
        // Correspondance avec la table JSON_IN
        public int Id { get; set; }                    // JSON_KEYU (PK)
        public string JsonData { get; set; }           // JSON_DATA
        public string ContentHash { get; set; }        // JSON_HASH
        public string? ApiEndpoint { get; set; }       // JSON_FROM
        public string? PackingSlipId { get; set; }     // Extrait de JSON_BKEY
        public DateTime FirstSeenAt { get; set; }      // JSON_CRDA
        public DateTime LastUpdatedAt { get; set; }    // JSON_CRDA
        public int UpdateCount { get; set; } = 0;      // Calculé

        // Propriétés extraites du JSON Dynamics
        public DynamicsPackingSlip? DynamicsData { get; set; }
    }

    /// <summary>
    /// Structure des données Packing Slip Dynamics selon votre JSON
    /// </summary>
    public class DynamicsPackingSlip
    {
        // Identifiants principaux
        public string? dataAreaId { get; set; }            // "br" → Société Référence
        public long WMSTRansRecId { get; set; }            // 5637148326 → ID technique
        public string? BRPortalOrderNumber { get; set; }   // "00004677" → Référence Commande Web
        public string? INT3PLStatus { get; set; }          // "ProcessedBy3PL" → Statut 3PL
        public DateTime? DlvDate { get; set; }             // "2025-06-26T12:00:00Z" → Date expédition souhaitée
        public string? transRefId { get; set; }            // "SO000011" → Référence Commande D365
        public string? transType { get; set; }             // "Sales" → Type Commande ventes D365
        public string? DlvTermId { get; set; }             // "DDP" → Incoterm
        public string? PdsDispositionCode { get; set; }    // "" → Code Qualité
        public string? expeditionStatus { get; set; }      // "Activated" → Statut Traitement D365
        public string? Contact { get; set; }               // "VERONIQUE MAILLARD" → Contact Tiers Destinataire
        public string? pickingRouteID { get; set; }        // "PP000071" → Numéro Document D365
        public string? BoxTypeBtc { get; set; }            // "" → Type Boite BtC
        public string? Street { get; set; }                // "11, RUE DU GENERAL LECLERC" → Adresse 1
        public string? CarrierServiceCode { get; set; }    // "J+1@12" → Code service Transport
        public int SellableDays { get; set; }              // 365 → Famille Classification
        public string? Description { get; set; }           // "MASQUE BAIN DE PLANTES 100 ml" → Désignation article
        public string? BRTransportModeCode { get; set; }   // "R" → Type Transport Si Dangereux
        public string? inventBatchId { get; set; }         // "" → Lot
        public string? BROrderGrouping { get; set; }       // "" → Code Regroupement
        public string? CommentPreparation { get; set; }    // "Commande urgente" → Commentaire Préparation
        public string? Email { get; set; }                 // "veronique.maillard@test.fr" → Mail Tiers Destinataire
        public string? SalesOriginId { get; set; }         // "BTB" → Canal de Ventes
        public string? PurchOrderFormNum { get; set; }     // "PO89777" → Référence PO Destinataire
        public string? CarrierCode { get; set; }           // "A AFFECTER" → Code Transport
        public string? inventSerialId { get; set; }        // "" → Lot 2 (N° serie Machine)
        public string? ISOcode { get; set; }               // "FR" → Pays Tiers Destinataire
        public string? SegmentId { get; set; }             // "Pro" → Segment
        public string? BRPackingCode { get; set; }         // "COLIS" → Type Colisage
        public string? Phone { get; set; }                 // "+33240471298" → Téléphone Tiers Destinataire
        public DateTime? expDate { get; set; }             // "1900-01-01T12:00:00Z" → DLUO
        public string? itemBarCode { get; set; }           // "3700693203265" → EAN13
        public string? CustClassificationId { get; set; }  // "BRPRIO" → Priorité sur les stocks
        public string? inventLocationId { get; set; }      // "12" → Entrepot destinataire D365
        public string? CommentExpedition { get; set; }     // "Livraison avant 15H..." → Commentaire Expedition
        public string? City { get; set; }                  // "NANTES" → Ville Tiers Destinataire
        public int BRPreparationEnum { get; set; }         // 0 → Délai Préparation
        public string? CardTypeRemer { get; set; }         // "P" → Type Carte Remerciement
        public string? MessagePerso { get; set; }          // "" → Message Remerciement personnalisé
        public string? LicensePlateId { get; set; }        // "" → Numéro Support
        public string? SubsegmentId { get; set; }          // "Day Spa" → Sous-Segment
        public string? CountryRegionId { get; set; }       // "FRA" → Pays/Région
        public string? itemId { get; set; }                // "BAINP100" → Référence article
        public string? customer { get; set; }              // "C0000071" → Code Tiers destinataire
        public decimal qty { get; set; }                   // 20 → Quantité commandée
        public string? DeliveryName { get; set; }          // "SARL AIR MARIN" → Nom Tiers Destinataire
        public int BRShippingDocumentEnum { get; set; }    // 1 → Documentation Expédition
        public string? ZipCode { get; set; }               // "44000" → CP Tiers Destinataire

        // Propriétés d'export
        public bool TxtExported { get; set; } = false;
        public DateTime? TxtExportDate { get; set; }
        public string? TxtExportBatch { get; set; }
    }
}