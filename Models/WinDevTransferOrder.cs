using System.Xml.Serialization;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente la structure XML WINDEV pour les Transfer Orders
    /// </summary>
    [XmlRoot("WINDEV_TRANSFER_TABLE")]
    public class WinDevTransferTable
    {
        [XmlElement("Table")]
        public List<WinDevTransferOrder> TransferOrders { get; set; }

        public WinDevTransferTable()
        {
            TransferOrders = new List<WinDevTransferOrder>();
        }
    }

    /// <summary>
    /// Structure d'un Transfer Order WINDEV selon le mapping fourni
    /// </summary>
    public class WinDevTransferOrder
    {
        // ========== IDENTIFIANTS PRINCIPAUX ==========
        [XmlElement("REA_RFCE")]
        public string ReaRfce { get; set; } = ""; // ExpectedReceiptNumber → REA_DAT.REA_RFCE

        [XmlElement("REA_RFTI")]
        public string ReaRfti { get; set; } = ""; // TransferId → REA_DAT.REA_RFTI

        [XmlElement("REA_DALP")]
        public string ReaDalp { get; set; } = ""; // ReceiveDate → REA_DAT.REA_DALP (format DATE)

        // ========== FOURNISSEUR/CLIENT ==========
        [XmlElement("REA_CTAF")]
        public string ReaCtaf { get; set; } = ""; // XXXAccount → REA_DAT.REA_CTAF (RG2)

        // ========== DÉTAILS LIGNE ==========
        [XmlElement("REA_NoLR")]
        public int ReaNoLr { get; set; } = 0; // LineNum → REA_DAT.REA_NoLR

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; } = ""; // ItemId → REA_DAT.ART_CODE

        [XmlElement("REA_QTRE")]
        public decimal ReaQtre { get; set; } = 0; // QtyShipped → REA_DAT.REA_QTRE (RG3)

        // ========== TRAÇABILITÉ ==========
        [XmlElement("REA_LOT1")]
        public string ReaLot1 { get; set; } = ""; // inventBatchId → REA_DAT.REA_LOT1 (RG7)

        [XmlElement("REA_LOT2")]
        public string ReaLot2 { get; set; } = ""; // inventSerialId → REA_DAT.REA_LOT2 (RG8)

        [XmlElement("REA_DLUO")]
        public string ReaDluo { get; set; } = ""; // expDate → REA_DAT.REA_DLUO (format DATE)

        // ========== SUPPORT ET COMMENTAIRES ==========
        [XmlElement("REA_NoSU")]
        public string ReaNoSu { get; set; } = ""; // LicensePlateId → REA_DAT.REA_NoSU

        [XmlElement("REA_COM")]
        public string ReaCom { get; set; } = ""; // Notes → REA_DAT.REA_COM

        // ========== CODE QUALITÉ ==========
        [XmlElement("QUA_CODE")]
        public string QuaCode { get; set; } = ""; // PdsDispositionCode → REA_DAT.QUA_CODE (RG4)

        // ========== RÉFÉRENCE RÉSERVATION ==========
        [XmlElement("REA_RFAF")]
        public string ReaRfaf { get; set; } = ""; // REA_DAT.REA_RFAF

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
        [XmlElement("INT3PLStatus")]
        public string Int3PlStatus { get; set; } = ""; // INT3PLStatus

        [XmlElement("InventTransId")]
        public string InventTransId { get; set; } = ""; // InventTransId

        [XmlElement("InventLocationIdTo")]
        public string InventLocationIdTo { get; set; } = ""; // InventLocationIdTo

        [XmlElement("XXXName")]
        public string XxxName { get; set; } = ""; // XXXName (nom tiers - RG2)
    }
}