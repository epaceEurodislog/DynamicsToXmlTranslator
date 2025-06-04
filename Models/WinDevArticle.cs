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
    /// Structure d'un article dans le format WINDEV
    /// </summary>
    public class WinDevArticle
    {
        [XmlElement("ACT_CODE")]
        public string ActCode { get; set; }

        [XmlElement("ART_CODE")]
        public string ArtCode { get; set; }

        [XmlElement("TIE_CODE")]
        public string TieCode { get; set; }

        [XmlElement("ART_DESC")]
        public string ArtDesc { get; set; }

        [XmlElement("ART_DESL")]
        public string ArtDesl { get; set; }

        [XmlElement("ART_EANU")]
        public string ArtEanu { get; set; }

        [XmlElement("ART_EANC")]
        public string ArtEanc { get; set; }

        [XmlElement("ART_QTEC")]
        public int ArtQtec { get; set; }

        [XmlElement("ART_QTEP")]
        public int ArtQtep { get; set; }

        [XmlElement("ART_STAT")]
        public int ArtStat { get; set; }

        [XmlElement("ART_NVAL")]
        public int ArtNval { get; set; }

        [XmlElement("ART_ALPHA2")]
        public string ArtAlpha2 { get; set; }

        [XmlElement("ART_DDLC")]
        public int ArtDdlc { get; set; }

        [XmlElement("ART_ALPHA8")]
        public string ArtAlpha8 { get; set; }

        [XmlElement("ART_ALPHA9")]
        public string ArtAlpha9 { get; set; }

        [XmlElement("ART_POIU")]
        public int ArtPoiu { get; set; }

        [XmlElement("ART_PRIX")]
        public decimal ArtPrix { get; set; }

        [XmlElement("ART_ALPHA14")]
        public string ArtAlpha14 { get; set; }

        [XmlElement("ART_UNI")]
        public int ArtUni { get; set; }

        [XmlElement("ART_NUM19")]
        public int ArtNum19 { get; set; }

        [XmlElement("ART_NUM21")]
        public int ArtNum21 { get; set; }

        [XmlElement("ART_ALPHA18")]
        public string ArtAlpha18 { get; set; }

        [XmlElement("ART_ALPHA15")]
        public string ArtAlpha15 { get; set; }

        [XmlElement("ART_TPVE")]
        public int ArtTpve { get; set; }

        [XmlElement("ART_TPVS")]
        public int ArtTpvs { get; set; }

        [XmlElement("ART_ALPHA26")]
        public string ArtAlpha26 { get; set; }

        [XmlElement("ART_ALPHA25")]
        public string ArtAlpha25 { get; set; }

        [XmlElement("ART_TOP18")]
        public int ArtTop18 { get; set; }

        [XmlElement("ART_ALPHA19")]
        public string ArtAlpha19 { get; set; }
    }
}