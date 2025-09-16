namespace DynamicsToXmlTranslator.Models
{
    /// <summary>
    /// Représente un en-tête de commande SPEED pour export TXT (fichier CDEN_COSMESTIQUE)
    /// </summary>
    /// <summary>
    /// Représente un en-tête de commande SPEED pour export TXT (fichier CDEN_COSMETIQUE)
    /// </summary>
    public class SpeedPackingSlipHeader
    {
        // ========== EN-TÊTE OPE selon mapping client ==========
        public string ACT_CODE { get; set; } = "COSMETIQUE";              // ✅ MODIFIÉ : COSMETIQUE au lieu de LTRF
        public string OPE_DACO { get; set; } = "";                        // Date commande (YYYYMMDD)
        public string OPE_REDO { get; set; } = "";                        // transRefId → CLÉ DE LIAISON ✅ CORRECT
        public string TIE_CODE { get; set; } = "";                        // customer → Code Tiers destinataire dans alpha31
        public string OPE_RTIE { get; set; } = "";                        // PurchOrderFormNum/BRPortalOrderNumber selon RG ✅ CORRECT
        public string TIE_NOM { get; set; } = "";                         // DeliveryName → Nom Tiers Destinataire ✅ CORRECT
        public string OPE_ADR1 { get; set; } = "";                        // Street → Adresse 1 ✅ CORRECT
        public string OPE_ADR2 { get; set; } = "";                        // Street suite si > 50 chars ✅ CORRECT
        public string OPE_ADR3 { get; set; } = "";                        // Street suite si > 50 chars ✅ CORRECT
        public string OPE_ADR4 { get; set; } = "";                        // Vide ✅ CORRECT
        public string OPE_ADVL { get; set; } = "";                        // City → Ville ✅ CORRECT
        public string OPE_ADCP { get; set; } = "";                        // ZipCode → Code postal ✅ CORRECT
        public string OPE_CPAY { get; set; } = "";                        // ISOcode → Pays ✅ CORRECT
        public string OPE_COBP { get; set; } = "";                        // CommentPreparation → Commentaire Préparation ✅ CORRECT
        public string OPE_COBL { get; set; } = "";                        // Vide selon nouveau mapping
        public string OPE_DALI { get; set; } = "";                        // DlvDate → Date livraison (YYYYMMDD) ✅ CORRECT
        public string OPE_CTRA { get; set; } = "";                        // CarrierCode → Code Transport ✅ CORRECT

        // ✅ CORRECTION : Selon mapping client, CarrierServiceCode va dans ALPHA40@ALPHA41
        public string OPE_ALPHA16 { get; set; } = "";                     // Vide maintenant
        public string OPE_ALPHA17 { get; set; } = "";                     // pickingRouteID → Numéro Document D365 ✅ CORRECT
        public string OPE_ALPHA18 { get; set; } = "";                     // Vide maintenant

        public string OPE_ALPHA19 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA20 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA21 { get; set; } = "";                     // SalesOriginId → Canal de Ventes ✅ CORRECT
        public string OPE_ALPHA22 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA23 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA24 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA25 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_TEL { get; set; } = "";                         // Phone → Téléphone ✅ CORRECT
        public string OPE_FAX { get; set; } = "";                         // Vide ✅ CORRECT
        public string OPE_IMEL { get; set; } = "";                        // Email → Mail ✅ CORRECT
        public string OPE_ALPHA1 { get; set; } = "";                      // Vide ✅ CORRECT
        public string OPE_ALPHA5 { get; set; } = "";                      // Vide ✅ CORRECT
        public string OPE_ALPHA6 { get; set; } = "";                      // SegmentId → Segment ✅ CORRECT
        public string OPE_ALPHA9 { get; set; } = "";                      // Vide ✅ CORRECT
        public string OPE_ALPHA15 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_DATE15 { get; set; } = "";                      // Vide ✅ CORRECT
        public string OPE_ALPHA31 { get; set; } = "";                     // SellableDays → Famille Classification + FORMAT SPÉCIAL ✅ CORRECT
        public string OPE_ALPHA34 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA35 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA36 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA37 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_ALPHA38 { get; set; } = "";                     // Vide ✅ CORRECT
        public string OPE_TOP17 { get; set; } = "0";                      // Fixe à 0 ✅ CORRECT

        // ✅ NOUVEAUX CHAMPS SELON MAPPING CLIENT :
        public string OPE_CONT { get; set; } = "";                        // Contact
        public string OPE_COME { get; set; } = "";                        // CommentExpedition → Commentaire Expedition dans alpha31
        public string OPE_ALPHA45 { get; set; } = "";                     // CardTypeRemer
        public string OPE_ALPHA46 { get; set; } = "";                     // BRTransportModeCode
        public string OPE_ALPHA47 { get; set; } = "";                     // BoxTypeBtc
        public string OPE_ALPHA48 { get; set; } = "";                     // DlvTermId
        public string OPE_GRP { get; set; } = "";                         // BROrderGrouping  → Code Regroupement dans alpha31
        public string OPE_MPCO { get; set; } = "";                        // BRPackingCode  → Type Colisage dans alpha31
        public string OPE_UNICODE11 { get; set; } = "";                   // MessagePerso  → Message Remerciement personnalisé dans alpha31
        public string OPE_TOP25 { get; set; } = "";                           // BRPreparationEnum  → Délai Préparation dans alpha31
        public string OPE_TOP28 { get; set; } = "";                       // BRShippingDocumentEnum  → Documentation Expédition dans alpha31

        // ✅ CHAMPS VIRTUELS POUR ALPHA > 38 (selon mapping client) :
        public string OPE_ALPHA40 { get; set; } = "";                     // CarrierServiceCode partie 1 (avant @)
        public string OPE_ALPHA41 { get; set; } = "";                     // CarrierServiceCode partie 2 (après @)

        // ✅ AUTRES CHAMPS > 38 (non dans le mapping client mais à conserver pour compatibilité)
        public string OPE_ALPHA39 { get; set; } = "";                     // Stocké dans OPE_ALPHA31
        public string OPE_ALPHA42 { get; set; } = "";                     // Stocké dans OPE_ALPHA31
        public string OPE_ALPHA43 { get; set; } = "";                     // Stocké dans OPE_ALPHA31

        public string STATUT { get; set; } = "";  // Nouveau champ statut (vide)

        /// <summary>
        /// ✅ MODIFIÉ : Construit le format spécial pour OPE_ALPHA31 avec TOUS les champs ALPHA > 38 + TIE_CODE selon mapping client
        /// Format: donneralpha31[tiecode:TIE_CODE_alpha40:donneealpha40_alpha41:donneealpha41_alpha45:donneealpha45_etc]
        /// </summary>

        public string GetFormattedOpeAlpha31()
        {
            // Valeur de base (SellableDays)
            string baseValue = OPE_ALPHA31 ?? "";

            // ✅ COLLECTER TOUS LES CHAMPS ALPHA > 38 + TIE_CODE selon le mapping client avec leurs libellés
            // AJOUT RD 31/07/2025 => 
            var extraValues = new List<string>();

            // ALPHA40 et ALPHA41 : CarrierServiceCode (séparé par @)
            if (!string.IsNullOrEmpty(OPE_ALPHA40)) extraValues.Add($"alpha40:{OPE_ALPHA40}");
            if (!string.IsNullOrEmpty(OPE_ALPHA41)) extraValues.Add($"alpha41:{OPE_ALPHA41}");

            // ALPHA45 : CardTypeRemer
            if (!string.IsNullOrEmpty(OPE_ALPHA45)) extraValues.Add($"alpha45:{OPE_ALPHA45}");

            // ALPHA46 : BRTransportModeCode
            if (!string.IsNullOrEmpty(OPE_ALPHA46)) extraValues.Add($"alpha46:{OPE_ALPHA46}");

            // ALPHA47 : BoxTypeBtc
            if (!string.IsNullOrEmpty(OPE_ALPHA47)) extraValues.Add($"alpha47:{OPE_ALPHA47}");

            // ALPHA48 : DlvTermId
            if (!string.IsNullOrEmpty(OPE_ALPHA48)) extraValues.Add($"alpha48:{OPE_ALPHA48}");

            // ✅ CHAMPS ADDITIONNELS (non dans le mapping client mais disponibles pour compatibilité)
            if (!string.IsNullOrEmpty(OPE_ALPHA39)) extraValues.Add($"alpha39:{OPE_ALPHA39}");
            if (!string.IsNullOrEmpty(OPE_ALPHA42)) extraValues.Add($"alpha42:{OPE_ALPHA42}");
            if (!string.IsNullOrEmpty(OPE_ALPHA43)) extraValues.Add($"alpha43:{OPE_ALPHA43}");

            // ✅ NOUVEAU : TIE_CODE en premier (selon votre demande)
            if (!string.IsNullOrEmpty(TIE_CODE)) extraValues.Add($"tiecode:{TIE_CODE}");

            ////////////////////////////// AJOUT RD 31/07/2025 /////////////////////////////////

            // TOP25 : BRPreparationEnum
            if (!string.IsNullOrEmpty(OPE_TOP25)) extraValues.Add($"top25:{OPE_TOP25}");
            // TOP28 : BRShippingDocumentEnum
            if (!string.IsNullOrEmpty(OPE_TOP28)) extraValues.Add($"top28:{OPE_TOP28}");
            // GRP : BROrderGrouping
            if (!string.IsNullOrEmpty(OPE_GRP)) extraValues.Add($"grp:{OPE_GRP}");
            // MPCO : BRPackingCode
            if (!string.IsNullOrEmpty(OPE_MPCO)) extraValues.Add($"mpco:{OPE_MPCO}");
            // COME : CommentExpedition
            if (!string.IsNullOrEmpty(OPE_COME)) extraValues.Add($"come:{OPE_COME}");
            // UNICODE11 : MessagePerso
            if (!string.IsNullOrEmpty(OPE_UNICODE11)) extraValues.Add($"unicode11:{OPE_UNICODE11}");

            /////////////////////////////// FIN DE L'AJOUT ///////////////////////////////////////

            // Si pas de valeurs supplémentaires, retourner la valeur de base
            if (!extraValues.Any())
            {
                return baseValue;
            }

            // Construire le format spécial : valeurBase[tiecode:valeur_alpha40:valeur_etc]
            string extraPart = string.Join("_", extraValues);

            // AJOUT RD 
            //extraPart = Truncate(extraPart,500);

            return $"{baseValue}[{extraPart}]";
        }

        /// <summary>
        /// Représente une ligne de commande SPEED pour export TXT (fichier CDLG_COSMETIQUE)
        /// </summary>
        public class SpeedPackingSlipLine
        {
            // ========== LIGNE OPL selon mapping client ==========
            public string ACT_CODE { get; set; } = "COSMETIQUE";              // ✅ MODIFIÉ : COSMETIQUE au lieu de LTRF
            public string OPL_RCDO { get; set; } = "";                        // transRefId → CLÉ DE LIAISON ✅ CORRECT
            public string OPL_RLDO { get; set; } = "";                        // LineNumber → Numéro Ligne ✅ CORRECT
            public string ART_CODE { get; set; } = "";                        // itemId → Référence article ✅ CORRECT
            public decimal OPL_QTAP { get; set; } = 0;                        // qty → Quantité ✅ CORRECT
            public string QUA_CODE { get; set; } = "";                        // PdsDispositionCode → Code Qualité ✅ CORRECT
            //public decimal OPL_POIDS { get; set; } = 0;                       // Poids/Volume ✅ CORRECT

            // ✅ ATTENTION : Selon mapping client, il y a une inversion par rapport aux commentaires
            //public string OPL_LOT1 { get; set; } = "";                        // inventBatchId → Lot 1 (selon mapping)
            //public string OPL_LOT2 { get; set; } = "";                        // inventSerialId → Lot 2 (selon mapping)
            //public string OPL_DLOO { get; set; } = "";                        // expDate → DLUO ✅ CORRECT
            //public string OPL_NoSU { get; set; } = "";                        // LicensePlateId → Support ✅ CORRECT
            //public string OPL_CONDITIONNEMENT { get; set; } = "";             // Type conditionnement ✅ CORRECT
            //public string OPL_ALPHA1 { get; set; } = "";                      // Champ libre 1 ✅ CORRECT
            //public string OPL_ALPHA2 { get; set; } = "";                      // Champ libre 2 ✅ CORRECT
            //public string OPL_ALPHA3 { get; set; } = "";                      // Champ libre 3 ✅ CORRECT

            // AJOUT RD 01/08/2025
            public string OPL_ALPHA1 { get; set; } = "";
            public string OPL_LOT1 { get; set; } = "";                          // inventBatchId → Lot 1 (selon mapping)
            public string OPL_DLC { get; set; } = "";                           // expDate → DLUO ✅ CORRECT
            public string OPL_ALPHA3 { get; set; } = "";
            public string OPL_ALPHA4 { get; set; } = "";                        // inventSerialId → Lot 2
            public string OPL_ALPHA5 { get; set; } = "";                        // LicensePlateId → Support ✅ CORRECT
            public string OPL_ALPHA6 { get; set; } = "";
            public string OPL_ALPHA7 { get; set; } = "";
            public string STATUT { get; set; } = "";

            // FIN AJOUT
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
            public string? HeaderFilePath { get; set; }      // Chemin CDEN_COSMETIQUE_xxx.TXT
            public string? LinesFilePath { get; set; }       // Chemin CDLG_COSMETIQUE_xxx.TXT
            public int HeaderCount { get; set; }             // Nombre d'en-têtes exportés
            public int LinesCount { get; set; }              // Nombre de lignes exportées
            public List<int> ExportedPackingSlipIds { get; set; } = new List<int>();
        }
    }
}