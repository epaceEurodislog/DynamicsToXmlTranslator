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
    /// Structure d'un article WINDEV selon le document Excel de correspondance
    /// </summary>
    public class WinDevArticle
    {
        // ========== IDENTIFIANTS PRINCIPAUX ==========
        [XmlElement("ACT_CODE")]
        public string ActCode { get; set; } = "COSMETIQUE"; // Fixe

        [XmlElement("ART_CCLI")]
        public string ArtCcli { get; set; } = ""; // dataAreaId → ART_CCLI (code client/activité)

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; } = ""; // "BR" + ItemId → ART_CODE (format: BRSHSEBO500)

        [XmlElement("ART_CODC")]
        public string ArtCodc { get; set; } = ""; // ItemId → ART_PAR.ART_CODC

        [XmlElement("ART_DESL")]
        public string ArtDesl { get; set; } = ""; // Name → ART_PAR.ART_DESL

        // ========== UNITÉ ET CODE-BARRES ==========
        [XmlElement("ART_ALPHA2")]
        public string ArtAlpha2 { get; set; } = ""; // UnitId → ART_PAR.ART_ALPHA2

        [XmlElement("ART_EANU")]
        public string ArtEanu { get; set; } = ""; // itemBarCode → ART_PAR.ART_EANU

        // ========== CATÉGORIES ET GROUPES ==========
        [XmlElement("ART_ALPHA17")]
        public string ArtAlpha17 { get; set; } = ""; // Category → ART.ALPHA17

        [XmlElement("ART_ALPHA3")]
        public string ArtAlpha3 { get; set; } = ""; // OrigCountryRegionId → ART.ALPHA3

        // ========== STATUT ET POIDS ==========
        [XmlElement("ART_STAT")]
        public string ArtStat { get; set; } = ""; // ProductLifecycleStateId → ART_PAR.ART_STAT

        [XmlElement("ART_POIU")]
        public decimal ArtPoiu { get; set; } = 0; // GrossWeight → ART_PAR.ART_POIU

        // ========== CONDITIONNEMENT ==========
        [XmlElement("ART_QTEC")]
        public int ArtQtec { get; set; } = 0; // FactorColli → ART_PAR.ART_QTEC

        [XmlElement("ART_QTEP")]
        public int ArtQtep { get; set; } = 0; // FactorPallet → ART_PAR.ART_QTEP

        // ========== DURÉE DE VIE ==========
        [XmlElement("ART_NUM19")]
        public int ArtNum19 { get; set; } = 0; // PdsShelfLife → ART_PAR.ART_NUM19

        // ========== IDENTIFIANTS EXTERNES ==========
        [XmlElement("ART_ALPHA8")]
        public string ArtAlpha8 { get; set; } = ""; // ExternalItemId → ART_PAR.ART_ALPHA8

        // ========== TRAÇABILITÉ ==========
        [XmlElement("ART_DLUO")]
        public int ArtDluo { get; set; } = 0; // TrackingDLCDDLUO → ART_PAR.ART_DLUO

        [XmlElement("ART_LOT1")]
        public int ArtLot1 { get; set; } = 0; // TrackingLot1 → ART_PAR.ART_LOT1

        [XmlElement("ART_LOT2")]
        public int ArtLot2 { get; set; } = 0; // TrackingLot2 → ART_PAR.ART_LOT2

        [XmlElement("ART_NSS")]
        public int ArtNss { get; set; } = 0; // TrackingProoftag → ART_PAR.ART_NSS

        // ========== RÉTIQUETAGE ==========
        [XmlElement("ART_TOP1")]
        public int ArtTop1 { get; set; } = 0; // ProducVersionAttribute → ART_PAR.ART_TOP1

        // ========== DIMENSIONS BRUTES ==========
        [XmlElement("ART_LONU")]
        public decimal ArtLonu { get; set; } = 0; // grossDepth → ART_PAR.ART_LONU

        [XmlElement("ART_LARU")]
        public decimal ArtLaru { get; set; } = 0; // grossWidth → ART_PAR.ART_LARU

        [XmlElement("ART_HAUU")]
        public decimal ArtHauu { get; set; } = 0; // grossHeight → ART_PAR.ART_HAUU

        // ========== MATIÈRE DANGEREUSE ==========
        [XmlElement("ART_TOP17")]
        public int ArtTop17 { get; set; } = 0; // HMIMIndicator → ART_PAR.ART_TOP17

        // ========== DIMENSIONS NETTES ==========
        [XmlElement("ART_LONC")]
        public decimal ArtLonc { get; set; } = 0; // Depth → ART_PAR.ART_LONC

        [XmlElement("ART_LARC")]
        public decimal ArtLarc { get; set; } = 0; // Width → ART_PAR.ART_LARC

        [XmlElement("ART_HAUC")]
        public decimal ArtHauc { get; set; } = 0; // Height → ART_PAR.ART_HAUC

        [XmlElement("ART_POIC")]
        public decimal ArtPoic { get; set; } = 0; // Weight → ART_PAR.ART_POIC

        // ========== CHAMPS SUPPLÉMENTAIRES (non mappés) ==========
        [XmlElement("ART_ALPHA14")]
        public string ArtAlpha14 { get; set; } = "";

        [XmlElement("ART_RSTK")]
        public string ArtRstk { get; set; } = "";

        [XmlElement("ART_UNI")]
        public int ArtUni { get; set; } = 0;

        [XmlElement("ART_SPCB")]
        public string ArtSpcb { get; set; } = ""; // Retour en string selon vos RG

        [XmlElement("ART_COLI")]
        public int ArtColi { get; set; } = 0;

        [XmlElement("ART_PAL")]
        public int ArtPal { get; set; } = 0;

        [XmlElement("ART_NUM18")]
        public int ArtNum18 { get; set; } = 0;

        [XmlElement("ART_ALPHA18")]
        public string ArtAlpha18 { get; set; } = "";

        [XmlElement("ART_ALPHA24")]
        public string ArtAlpha24 { get; set; } = "";

        [XmlElement("ART_ALPHA26")]
        public string ArtAlpha26 { get; set; } = "";

        [XmlElement("ART_NSE")]
        public string ArtNse { get; set; } = ""; // Retour en string selon vos RG

        [XmlElement("ART_EANC")]
        public string ArtEanc { get; set; } = "";
    }
}