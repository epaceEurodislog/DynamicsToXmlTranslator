using System.Xml.Serialization;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente la structure XML WINDEV pour les Return Orders
    /// </summary>
    [XmlRoot("CF_ATTENDUS_COSMETIQUE")]
    public class WinDevReturnTable
    {
        [XmlElement("LIGNE")]
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
        [XmlElement("ACT_CODE")]
        public string ActCode { get; set; } = "COSMETIQUE"; // VALEUR FIXE

        [XmlElement("REA_CCLI")]
        public string ReaCcli { get; set; } = "BR"; // VALEUR FIXE
        
        [XmlElement("REA_RFCE")]
        public string ReaRfce { get; set; } = ""; // SalesId (modifié)

        [XmlElement("REA_RFTI")]
        public string ReaRfti { get; set; } = ""; // ReturnItemNum (modifié)

        [XmlElement("REA_RFCL")]
        public int ReaRfcl { get; set; } // Même valeur que REA_NoLR

        [XmlElement("REA_TYAT")]
        public string ReaTyat { get; set; } = "100"; // 100 pour Return Orders

        [XmlElement("REA_DALP")]
        public string ReaDalp { get; set; } = ""; // ReturnDeadline

        [XmlElement("REA_CTAF")]
        public string ReaCtaf { get; set; } = ""; // CustAccount

        [XmlElement("REA_ALPHA3")]
        public string ReaAlpha3 { get; set; } = ""; // SalesName

        [XmlElement("REA_NoLR")]
        public int ReaNoLr { get; set; } = 0; // LineNum

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; } = ""; // "BR" + ItemId

        [XmlElement("REA_QTRE")]
        public decimal ReaQtre { get; set; } = 0; // ExpectedRetQty

        [XmlElement("REA_QTRC")]
        public decimal ReaQtrc { get; set; } = 0; // Même valeur que REA_QTRE

        [XmlElement("REA_LOT1")]
        public string ReaLot1 { get; set; } = ""; // inventBatchId

        [XmlElement("REA_LOT2")]
        public string ReaLot2 { get; set; } = ""; // inventSerialId

        [XmlElement("REA_DLUO")]
        public string ReaDluo { get; set; } = ""; // expDate

        [XmlElement("REA_NoSU")]
        public string ReaNoSu { get; set; } = ""; // Numéro support

        [XmlElement("REA_COM")]
        public string ReaCom { get; set; } = ""; // Notes

        [XmlElement("QUA_CODE")]
        public string QuaCode { get; set; } = ""; // ReturnDispositionCodeId

        [XmlElement("REA_RFAF")]
        public string ReaRfaf { get; set; } = ""; // Référence réservation

        [XmlElement("REA_ALPHA2")]
        public string ReaAlpha2 { get; set; } = ""; // LotID
        public bool ShouldSerializeReaAlpha2() => !string.IsNullOrEmpty(ReaAlpha2);

        [XmlElement("REA_ALPHA5")]
        public string ReaAlpha5 { get; set; } = "";
        public bool ShouldSerializeReaAlpha5() => !string.IsNullOrEmpty(ReaAlpha5);

        [XmlElement("REA_ALPHA1")]
        public string ReaAlpha1 { get; set; } = "";
        public bool ShouldSerializeReaAlpha1() => !string.IsNullOrEmpty(ReaAlpha1);

        [XmlElement("REA_ALPHA11")]
        public string ReaAlpha11 { get; set; } = "NIVEAU3"; // VALEUR FIXE

        [XmlElement("REA_ALPHA12")]
        public string ReaAlpha12 { get; set; } = "NORMAL"; // VALEUR FIXE
    }
}