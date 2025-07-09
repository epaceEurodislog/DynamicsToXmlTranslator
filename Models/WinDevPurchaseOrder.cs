using System.Xml.Serialization;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente la structure XML WINDEV pour les Purchase Orders
    /// </summary>
    [XmlRoot("WINDEV_PURCHASE_TABLE")]
    public class WinDevPurchaseTable
    {
        [XmlElement("Table")]
        public List<WinDevPurchaseOrder> PurchaseOrders { get; set; }

        public WinDevPurchaseTable()
        {
            PurchaseOrders = new List<WinDevPurchaseOrder>();
        }
    }

    /// <summary>
    /// Structure d'un Purchase Order WINDEV selon le mapping fourni
    /// </summary>
    public class WinDevPurchaseOrder
    {
        // ========== IDENTIFIANTS PRINCIPAUX ==========
        [XmlElement("REA_RFCE")]
        public string ReaRfce { get; set; } = ""; // PurchOrderDocNum → REA_DAT.REA_RFCE

        [XmlElement("REA_RFTI")]
        public string ReaRfti { get; set; } = ""; // PurchId → REA_DAT.REA_RFTI

        [XmlElement("REA_DALP")]
        public string ReaDalp { get; set; } = ""; // ReceiptDate → REA_DAT.REA_DALP (format DATE)

        // ========== FOURNISSEUR ==========
        [XmlElement("REA_CTAF")]
        public string ReaCtaf { get; set; } = ""; // OrderAccount → REA_DAT.REA_CTAF

        // ========== DÉTAILS LIGNE ==========
        [XmlElement("REA_NoLR")]
        public int ReaNoLr { get; set; } = 0; // LineNumber → REA_DAT.REA_NoLR

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; } = ""; // ItemId → REA_DAT.ART_CODE

        [XmlElement("REA_QTRE")]
        public decimal ReaQtre { get; set; } = 0; // QtyOrdered → REA_DAT.REA_QTRE

        // ========== TRAÇABILITÉ ==========
        [XmlElement("REA_LOT1")]
        public string ReaLot1 { get; set; } = ""; // Lot → REA_DAT.REA_LOT1 (RG7)

        [XmlElement("REA_LOT2")]
        public string ReaLot2 { get; set; } = ""; // Lot2 → REA_DAT.REA_LOT2 (RG8)

        [XmlElement("REA_DLUO")]
        public string ReaDluo { get; set; } = ""; // DLUO → REA_DAT.REA_DLUO (format DATE)

        // ========== NUMÉRO SUPPORT ET COMMENTAIRES ==========
        [XmlElement("REA_NoSU")]
        public string ReaNoSu { get; set; } = ""; // SupportNumber → REA_DAT.REA_NoSU

        [XmlElement("REA_COM")]
        public string ReaCom { get; set; } = ""; // Notes → REA_DAT.REA_COM

        // ========== CODE QUALITÉ ==========
        [XmlElement("QUA_CODE")]
        public string QuaCode { get; set; } = ""; // QualityCode → REA_DAT.QUA_CODE (RG4)

        // ========== RÉFÉRENCE RÉSERVATION ==========
        [XmlElement("REA_RFAF")]
        public string ReaRfaf { get; set; } = ""; // ReservationRef → REA_DAT.REA_RFAF

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
        [XmlElement("PurchTableVersion")]
        public string PurchTableVersion { get; set; } = ""; // PurchTableVersion

        [XmlElement("INT3PLStatus")]
        public string Int3PlStatus { get; set; } = ""; // INT3PLStatus

        // ========== DATES SUPPLÉMENTAIRES ==========
        [XmlElement("DeliveryDate")]
        public string DeliveryDate { get; set; } = ""; // DeliveryDate (format DATE)

        [XmlElement("InventLocationId")]
        public string InventLocationId { get; set; } = ""; // InventLocationId

        [XmlElement("DocumentState")]
        public string DocumentState { get; set; } = ""; // DocumentState

        [XmlElement("PurchName")]
        public string PurchName { get; set; } = ""; // PurchName (nom fournisseur)
    }
}