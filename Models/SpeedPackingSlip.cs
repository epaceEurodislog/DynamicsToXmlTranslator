namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un en-tête de commande SPEED pour export TXT (fichier CDEN_LTRF)
    /// </summary>
    public class SpeedPackingSlipHeader
    {
        // ========== EN-TÊTE OPE selon votre exemple CDEN_LTRF ==========
        public string ACT_CODE { get; set; } = "LTRF";                    // Fixe
        public string OPE_DACO { get; set; } = "";                       // Date commande (YYYYMMDD)
        public string OPE_REDO { get; set; } = "";                       // transRefId → CLÉ DE LIAISON
        public string TIE_CODE { get; set; } = "";                       // customer → Code Tiers destinataire
        public string OPE_RTIE { get; set; } = "";                       // PurchOrderFormNum/BRPortalOrderNumber selon RG
        public string TIE_NOM { get; set; } = "";                        // DeliveryName → Nom Tiers Destinataire
        public string OPE_ADR1 { get; set; } = "";                       // Street → Adresse 1
        public string OPE_ADR2 { get; set; } = "";                       // Street suite si > 50 chars
        public string OPE_ADR3 { get; set; } = "";                       // Street suite si > 50 chars
        public string OPE_ADR4 { get; set; } = "";                       // Vide
        public string OPE_ADVL { get; set; } = "";                       // City → Ville
        public string OPE_ADCP { get; set; } = "";                       // ZipCode → Code postal
        public string OPE_CPAY { get; set; } = "";                       // ISOcode → Pays
        public string OPE_COBP { get; set; } = "";                       // CommentPreparation → Commentaire Préparation
        public string OPE_COBL { get; set; } = "";                       // CommentExpedition → Commentaire Livraison
        public string OPE_DALI { get; set; } = "";                       // DlvDate → Date livraison (YYYYMMDD)
        public string OPE_CTRA { get; set; } = "";                       // CarrierCode → Code Transport
        public string OPE_ALPHA16 { get; set; } = "";                    // CarrierServiceCode partie 1 (avant @)
        public string OPE_ALPHA17 { get; set; } = "";                    // pickingRouteID → Numéro Document D365
        public string OPE_ALPHA18 { get; set; } = "";                    // CarrierServiceCode partie 2 (après @)
        public string OPE_ALPHA19 { get; set; } = "";                    // Vide
        public string OPE_ALPHA20 { get; set; } = "";                    // Vide
        public string OPE_ALPHA21 { get; set; } = "";                    // SalesOriginId → Canal de Ventes
        public string OPE_ALPHA22 { get; set; } = "";                    // Vide
        public string OPE_ALPHA23 { get; set; } = "";                    // Vide
        public string OPE_ALPHA24 { get; set; } = "";                    // Vide
        public string OPE_ALPHA25 { get; set; } = "";                    // Vide
        public string OPE_TEL { get; set; } = "";                        // Phone → Téléphone
        public string OPE_FAX { get; set; } = "";                        // Vide
        public string OPE_IMEL { get; set; } = "";                       // Email → Mail
        public string OPE_ALPHA1 { get; set; } = "";                     // Vide
        public string OPE_ALPHA5 { get; set; } = "";                     // Vide
        public string OPE_ALPHA6 { get; set; } = "";                     // SegmentId → Segment
        public string OPE_ALPHA9 { get; set; } = "";                     // Vide
        public string OPE_ALPHA15 { get; set; } = "";                    // Vide
        public string OPE_DATE15 { get; set; } = "";                     // Vide
        public string OPE_ALPHA31 { get; set; } = "";                    // SellableDays → Famille Classification + FORMAT SPÉCIAL
        public string OPE_ALPHA34 { get; set; } = "";                    // Vide
        public string OPE_ALPHA35 { get; set; } = "";                    // Vide
        public string OPE_ALPHA36 { get; set; } = "";                    // Vide
        public string OPE_ALPHA37 { get; set; } = "";                    // Vide
        public string OPE_ALPHA38 { get; set; } = "";                    // Vide
        public string OPE_TOP17 { get; set; } = "0";                     // Fixe à 0

        // ✅ NOUVEAU : Champs virtuels pour OPE_ALPHA > 38 (stockés dans OPE_ALPHA31)
        public string OPE_ALPHA39 { get; set; } = "";                    // Stocké dans OPE_ALPHA31
        public string OPE_ALPHA40 { get; set; } = "";                    // Stocké dans OPE_ALPHA31  
        public string OPE_ALPHA41 { get; set; } = "";                    // Stocké dans OPE_ALPHA31
        public string OPE_ALPHA42 { get; set; } = "";                    // Stocké dans OPE_ALPHA31
        public string OPE_ALPHA43 { get; set; } = "";                    // Stocké dans OPE_ALPHA31

        /// <summary>
        /// ✅ NOUVEAU : Construit le format spécial pour OPE_ALPHA31
        /// Format: donneralpha31[donneealpha39_donneealpha41_donneealpha42_donneealpha43]
        /// </summary>
        public string GetFormattedOpeAlpha31()
        {
            // Valeur de base (SellableDays)
            string baseValue = OPE_ALPHA31 ?? "";

            // Collecter les valeurs des champs > 38 (en excluant ALPHA40 selon votre exemple)
            var extraValues = new List<string>();

            if (!string.IsNullOrEmpty(OPE_ALPHA39)) extraValues.Add(OPE_ALPHA39);
            if (!string.IsNullOrEmpty(OPE_ALPHA41)) extraValues.Add(OPE_ALPHA41);
            if (!string.IsNullOrEmpty(OPE_ALPHA42)) extraValues.Add(OPE_ALPHA42);
            if (!string.IsNullOrEmpty(OPE_ALPHA43)) extraValues.Add(OPE_ALPHA43);

            // Si pas de valeurs supplémentaires, retourner la valeur de base
            if (!extraValues.Any())
            {
                return baseValue;
            }

            // Construire le format spécial : valeurBase[extra1_extra2_extra3_extra4]
            string extraPart = string.Join("_", extraValues);
            return $"{baseValue}[{extraPart}]";
        }
    }

    /// <summary>
    /// Représente une ligne de commande SPEED pour export TXT (fichier CDLG_LTRF)
    /// Selon votre exemple : LTRF|VC713873|10000|LTRF725|240|STD|7.780||||COLIS|||
    /// </summary>
    public class SpeedPackingSlipLine
    {
        // ========== LIGNE OPL selon votre exemple CDLG_LTRF ==========
        public string ACT_CODE { get; set; } = "LTRF";                   // Fixe (colonne 1)
        public string OPL_RCDO { get; set; } = "";                      // transRefId → CLÉ DE LIAISON (colonne 2) 
        public string OPL_RLDO { get; set; } = "";                      // LineNumber → Numéro Ligne (colonne 3)
        public string ART_CODE { get; set; } = "";                      // itemId → Référence article (colonne 4)
        public decimal OPL_QTAP { get; set; } = 0;                     // qty → Quantité (colonne 5)
        public string QUA_CODE { get; set; } = "";                     // PdsDispositionCode → Code Qualité (colonne 6)
        public decimal OPL_POIDS { get; set; } = 0;                    // Poids/Volume (colonne 7)
        public string OPL_LOT1 { get; set; } = "";                     // inventBatchId → Lot (colonne 8)
        public string OPL_LOT2 { get; set; } = "";                     // inventSerialId → Lot 2 (colonne 9)
        public string OPL_DLOO { get; set; } = "";                     // expDate → DLUO (colonne 10)
        public string OPL_NoSU { get; set; } = "";                     // LicensePlateId → Support (colonne 11)
        public string OPL_CONDITIONNEMENT { get; set; } = "";          // Type conditionnement (colonne 12)
        public string OPL_ALPHA1 { get; set; } = "";                   // Champ libre 1 (colonne 13)
        public string OPL_ALPHA2 { get; set; } = "";                   // Champ libre 2 (colonne 14)
        public string OPL_ALPHA3 { get; set; } = "";                   // Champ libre 3 (colonne 15)
    }

    /// <summary>
    /// Conteneur pour un export complet (en-tête + lignes)
    /// </summary>
    public class SpeedPackingSlipComplete
    {
        public SpeedPackingSlipHeader Header { get; set; }
        public List<SpeedPackingSlipLine> Lines { get; set; } = new List<SpeedPackingSlipLine>();

        // Données originales pour traçabilité
        public List<int> OriginalPackingSlipIds { get; set; } = new List<int>();
        public string CommandReference { get; set; } = ""; // OPE_REDO = OPL_RCDO
    }

    /// <summary>
    /// Résultat d'un export complet (2 fichiers)
    /// </summary>
    public class PackingSlipExportResult
    {
        public string? HeaderFilePath { get; set; }      // Chemin CDEN_LTRF_xxx.TXT
        public string? LinesFilePath { get; set; }       // Chemin CDLG_LTRF_xxx.TXT
        public int HeaderCount { get; set; }             // Nombre d'en-têtes exportés
        public int LinesCount { get; set; }              // Nombre de lignes exportées
        public List<int> ExportedPackingSlipIds { get; set; } = new List<int>();
    }
}