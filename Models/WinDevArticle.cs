using System.Xml.Serialization;

namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente la structure XML attendue par WINDEV
    /// </summary>
    [XmlRoot("WINDEV_TABLE")]
    public class WinDevTable
    {
        [XmlElement("Table")]
        public List<WinDevArticle> Articles { get; set; }

        public WinDevTable()
        {
            Articles = new List<WinDevArticle>();
        }
    }

    /// <summary>
    /// Structure simplifiée d'un article WINDEV - Uniquement les champs mappés avec l'API
    /// </summary>
    public class WinDevArticle
    {
        // ========== IDENTIFIANTS PRINCIPAUX ==========
        [XmlElement("ACT_CODE")]
        public string ActCode { get; set; } = ""; // dataAreaId (ex: "br")

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; } = ""; // ItemId (ex: "SHSEBO500")

        [XmlElement("ART_DESL")]
        public string ArtDesl { get; set; } = ""; // Name (description longue)

        // ========== CODE-BARRES ==========
        [XmlElement("ART_EANU")]
        public string ArtEanu { get; set; } = ""; // itemBarCode (EAN unité)

        // ========== POIDS ET DIMENSIONS ==========
        [XmlElement("ART_POIU")]
        public decimal ArtPoiu { get; set; } = 0; // GrossWeight (poids brut)

        [XmlElement("ART_POIN")]
        public decimal ArtPoin { get; set; } = 0; // Weight (poids net)

        [XmlElement("ART_HAUT")]
        public decimal ArtHaut { get; set; } = 0; // Height

        [XmlElement("ART_LARG")]
        public decimal ArtLarg { get; set; } = 0; // Width

        [XmlElement("ART_PROF")]
        public decimal ArtProf { get; set; } = 0; // Depth

        [XmlElement("ART_HAUTB")]
        public decimal ArtHautb { get; set; } = 0; // grossHeight

        [XmlElement("ART_LARGB")]
        public decimal ArtLargb { get; set; } = 0; // grossWidth

        [XmlElement("ART_PROFB")]
        public decimal ArtProfb { get; set; } = 0; // grossDepth

        // ========== FACTEURS DE CONDITIONNEMENT ==========
        [XmlElement("ART_COLI")]
        public int ArtColi { get; set; } = 0; // FactorColli

        [XmlElement("ART_PAL")]
        public int ArtPal { get; set; } = 0; // FactorPallet

        // ========== UNITÉ ==========
        [XmlElement("ART_UNITE")]
        public string ArtUnite { get; set; } = ""; // UnitId

        // ========== GROUPES ET CATÉGORIES ==========
        [XmlElement("ART_GROUPE")]
        public string ArtGroupe { get; set; } = ""; // ItemGroupId

        [XmlElement("ART_CATEG")]
        public string ArtCateg { get; set; } = ""; // Category

        // ========== STATUTS ET INDICATEURS ==========
        [XmlElement("ART_STAT3PL")]
        public string ArtStat3pl { get; set; } = ""; // INT3PLStatus

        [XmlElement("ART_HMIM")]
        public int ArtHmim { get; set; } = 0; // HMIMIndicator

        [XmlElement("ART_LIFECYCLE")]
        public string ArtLifecycle { get; set; } = ""; // ProductLifecycleStateId

        [XmlElement("ART_VERSION")]
        public string ArtVersion { get; set; } = ""; // ProducVersionAttribute

        // ========== DURÉE DE VIE ==========
        [XmlElement("ART_DDLC")]
        public int ArtDdlc { get; set; } = 0; // PdsShelfLife (en minutes)

        // ========== SUIVI ET TRAÇABILITÉ ==========
        [XmlElement("ART_LOT1")]
        public int ArtLot1 { get; set; } = 0; // TrackingLot1

        [XmlElement("ART_LOT2")]
        public int ArtLot2 { get; set; } = 0; // TrackingLot2

        [XmlElement("ART_DLUO")]
        public int ArtDluo { get; set; } = 0; // TrackingDLCDDLUO

        [XmlElement("ART_PROOF")]
        public int ArtProof { get; set; } = 0; // TrackingProoftag

        // ========== RÉFÉRENCES EXTERNES ==========
        [XmlElement("ART_EXTID")]
        public string ArtExtid { get; set; } = ""; // ExternalItemId

        [XmlElement("ART_INTRASTAT")]
        public string ArtIntrastat { get; set; } = ""; // IntrastatCommodity

        [XmlElement("ART_ORIGINE")]
        public string ArtOrigine { get; set; } = ""; // OrigCountryRegionId

        // ========== VOLUME CALCULÉ ==========
        [XmlElement("ART_VOLUME")]
        public decimal ArtVolume { get; set; } = 0; // Calculé : Height × Width × Depth
    }
}