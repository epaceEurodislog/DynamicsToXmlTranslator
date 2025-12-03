namespace DynamicsToXmlTranslator
{
    /// <summary>
    /// Constantes centralisées pour les noms de fichiers générés par l'application.
    /// Modifiez ces valeurs pour changer les noms de fichiers sans toucher au reste du code.
    /// </summary>
    public static class FileNameConstants
    {
        // ========================================
        // ARTICLES
        // ========================================
        /// <summary>
        /// Préfixe des fichiers XML d'articles.
        /// Format final: ARTICLE_COSMETIQUE_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string ARTICLE_PREFIX = "ARTICLE_COSMETIQUE";

        /// <summary>
        /// Préfixe des fichiers XML d'articles par lot.
        /// Format final: ARTICLE_COSMETIQUE_LOT001_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string ARTICLE_BATCH_PREFIX = "ARTICLE_COSMETIQUE_LOT";


        // ========================================
        // PURCHASE ORDERS (Commandes d'achat)
        // ========================================
        /// <summary>
        /// Préfixe des fichiers XML de commandes d'achat.
        /// Format final: RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string PURCHASE_ORDER_PREFIX = "RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT";

        /// <summary>
        /// Préfixe des fichiers XML de commandes d'achat par lot.
        /// Format final: RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_LOT001_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string PURCHASE_ORDER_BATCH_PREFIX = "RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_LOT";

        /// <summary>
        /// Préfixe pour les fichiers de test de commandes d'achat vides.
        /// </summary>
        public const string PURCHASE_ORDER_TEST_EMPTY = "RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_TEST_VIDE";


        // ========================================
        // RETURN ORDERS (Commandes de retour)
        // ========================================
        /// <summary>
        /// Préfixe des fichiers XML de commandes de retour.
        /// Format final: RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string RETURN_ORDER_PREFIX = "RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT";

        /// <summary>
        /// Préfixe des fichiers XML de commandes de retour par lot.
        /// Format final: RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT_LOT001_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string RETURN_ORDER_BATCH_PREFIX = "RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT_LOT";

        /// <summary>
        /// Préfixe pour les fichiers de test de commandes de retour vides.
        /// </summary>
        public const string RETURN_ORDER_TEST_EMPTY = "RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT_TEST_VIDE";


        // ========================================
        // TRANSFER ORDERS (Ordres de transfert)
        // ========================================
        /// <summary>
        /// Préfixe des fichiers XML d'ordres de transfert.
        /// Format final: RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string TRANSFER_ORDER_PREFIX = "RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT";

        /// <summary>
        /// Préfixe des fichiers XML d'ordres de transfert par lot.
        /// Format final: RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT_LOT001_YYYYMMDD_HHMMSS.XML
        /// </summary>
        public const string TRANSFER_ORDER_BATCH_PREFIX = "RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT_LOT";

        /// <summary>
        /// Préfixe pour les fichiers de test d'ordres de transfert vides.
        /// </summary>
        public const string TRANSFER_ORDER_TEST_EMPTY = "RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT_TEST_VIDE";


        // ========================================
        // PACKING SLIPS (Bordereaux d'expédition)
        // ========================================
        /// <summary>
        /// Préfixe des fichiers TXT d'en-têtes de bordereaux (CDEN).
        /// Format final: CDEN_COSMETIQUE_API-IT-RCT_YYYYMMDD_HHMMSS.TXT
        /// </summary>
        public const string PACKING_SLIP_HEADER_PREFIX = "CDEN_COSMETIQUE_API-IT-RCT";

        /// <summary>
        /// Préfixe des fichiers TXT de lignes de bordereaux (CDLG).
        /// Format final: CDLG_COSMETIQUE_API-IT-RCT_YYYYMMDD_HHMMSS.TXT
        /// </summary>
        public const string PACKING_SLIP_LINES_PREFIX = "CDLG_COSMETIQUE_API-IT-RCT";


        // ========================================
        // FORMATS DE DATE/HEURE
        // ========================================
        /// <summary>
        /// Format de date utilisé dans les noms de fichiers (YYYYMMDD).
        /// </summary>
        public const string DATE_FORMAT = "yyyyMMdd";

        /// <summary>
        /// Format d'heure utilisé dans les noms de fichiers (HHMMSS).
        /// </summary>
        public const string TIME_FORMAT = "HHmmss";

        /// <summary>
        /// Format de timestamp complet (YYYYMMDD_HHMMSS).
        /// </summary>
        public const string TIMESTAMP_FORMAT = "yyyyMMdd_HHmmss";


        // ========================================
        // EXTENSIONS
        // ========================================
        /// <summary>
        /// Extension pour les fichiers XML.
        /// </summary>
        public const string XML_EXTENSION = ".XML";

        /// <summary>
        /// Extension pour les fichiers TXT.
        /// </summary>
        public const string TXT_EXTENSION = ".TXT";
    }
}
