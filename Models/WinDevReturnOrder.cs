using System.Xml.Serialization;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente la structure XML WINDEV pour les Return Orders
    /// </summary>
    [XmlRoot("WINDEV_RETURN_TABLE")]
    public class WinDevReturnTable
    {
        [XmlElement("Table")]
        public List<WinDevReturnOrder> ReturnOrders { get; set; }

        public WinDevReturnTable()
        {
            ReturnOrders = new List<WinDevReturnOrder>();
        }
    }

    /// <summary>
    /// Structure d'un Return Order WINDEV selon le mapping fourni
    /// </summary>
    public class WinDevReturnOrder
    {
        // ========== IDENTIFIANTS PRINCIPAUX ==========
        [XmlElement("OPE_CCLI")]
        public string OpeCcli { get; set; } = ""; // dataAreaId → OPE_DAT.OPE_CCLI (Société Référence)

        [XmlElement("REA_RFCE")]
        public string ReaRfce { get; set; } = ""; // ReturnItemNum → REA_DAT.REA_RFCE (Expected Receipt Number)

        [XmlElement("REA_RFTI")]
        public string ReaRfti { get; set; } = ""; // SalesId → REA_DAT.REA_RFTI (N° Commande Vente)

        [XmlElement("REA_DALP")]
        public string ReaDalp { get; set; } = ""; // ReturnDeadline → REA_DAT.REA_DALP (Date de réception prévue)

        // ========== FOURNISSEUR/CLIENT ==========
        [XmlElement("REA_CTAF")]
        public string ReaCtaf { get; set; } = ""; // CustAccount → REA_DAT.REA_CTAF (Code tiers fournisseurs/Client)

        // ========== DÉTAILS LIGNE ==========
        [XmlElement("REA_NoLR")]
        public int ReaNoLr { get; set; } = 0; // LineNum → REA_DAT.REA_NoLR (Numéro ligne de commande)

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; } = ""; // ItemId → REA_DAT.ART_CODE (Référence article)

        [XmlElement("REA_QTRE")]
        public decimal ReaQtre { get; set; } = 0; // ExpectedRetQty → REA_DAT.REA_QTRE (Quantité prévue)

        // ========== TRAÇABILITÉ ==========
        [XmlElement("REA_LOT1")]
        public string ReaLot1 { get; set; } = ""; // inventBatchId → REA_DAT.REA_LOT1 (Lot) avec RG7

        [XmlElement("REA_LOT2")]
        public string ReaLot2 { get; set; } = ""; // inventSerialId → REA_DAT.REA_LOT2 (Lot 2) avec RG8

        [XmlElement("REA_DLUO")]
        public string ReaDluo { get; set; } = ""; // expDate → REA_DAT.REA_DLUO (DLUO)

        // ========== SUPPORT ET COMMENTAIRES ==========
        [XmlElement("REA_NoSU")]
        public string ReaNoSu { get; set; } = ""; // Numéro support (SSCC entrant)

        [XmlElement("REA_COM")]
        public string ReaCom { get; set; } = ""; // Notes → REA_DAT.REA_COM (Commentaires)

        // ========== CODE QUALITÉ ==========
        [XmlElement("QUA_CODE")]
        public string QuaCode { get; set; } = ""; // Code Qualité → REA_DAT.QUA_CODE (RG4)

        // ========== RÉFÉRENCE RÉSERVATION ==========
        [XmlElement("REA_RFAF")]
        public string ReaRfaf { get; set; } = ""; // Référence réservation → REA_DAT.REA_RFAF

        // ========== VALEURS FIXES ==========
        [XmlElement("ACT_CODE")]
        public string ActCode { get; set; } = "COSMETIQUE"; // VALEUR FIXE = COSMETIQUE

        [XmlElement("CCLI")]
        public string Ccli { get; set; } = "BR"; // VALEUR FIXE = BR

        // ========== CHAMPS AVEC RÈGLES DE GESTION ==========
        [XmlElement("REA_ALPHA5")]
        public string ReaAlpha5 { get; set; } = ""; // Contrôle Qualité (RG5)

        [XmlElement("REA_ALPHA1")]
        public string ReaAlpha1 { get; set; } = ""; // Type de réception (RG6)

        [XmlElement("REA_ALPHA11")]
        public string ReaAlpha11 { get; set; } = "NIVEAU3"; // VALEUR FIXE = NIVEAU3

        [XmlElement("REA_ALPHA12")]
        public string ReaAlpha12 { get; set; } = "NORMAL"; // VALEUR FIXE = NORMAL

        // ========== CHAMPS SUPPLÉMENTAIRES ==========
        [XmlElement("SalesStatus")]
        public string SalesStatus { get; set; } = ""; // Statut Commande Vente

        [XmlElement("ReturnDispositionCodeID")]
        public string ReturnDispositionCodeId { get; set; } = ""; // Code disposition

        [XmlElement("SalesName")]
        public string SalesName { get; set; } = ""; // Nom tiers fournisseurs/Client

        [XmlElement("InventLocationId")]
        public string InventLocationId { get; set; } = ""; // Entrepot destinataire D365
    }
}