using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

namespace DynamicsToXmlTranslator.Mappers
{
    public class PackingSlipMapper
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PackingSlipMapper> _logger;
        private readonly Utf8TextProcessor _textProcessor;

        public PackingSlipMapper(IConfiguration configuration, ILogger<PackingSlipMapper> logger, Utf8TextProcessor textProcessor)
        {
            _configuration = configuration;
            _logger = logger;
            _textProcessor = textProcessor;
        }

        /// <summary>
        /// Convertit un groupe de Packing Slips Dynamics en structure complète SPEED
        /// AVEC traitement UTF-8 des caractères spéciaux
        /// </summary>
        public SpeedPackingSlipComplete? MapToSpeedComplete(List<PackingSlip> packingSlipsGroup)
        {
            if (packingSlipsGroup == null || !packingSlipsGroup.Any())
            {
                _logger.LogWarning("Groupe de Packing Slips vide");
                return null;
            }

            try
            {
                // Prendre le premier pour l'en-tête (tous ont la même commande)
                var firstSlip = packingSlipsGroup.First();
                var dynamics = firstSlip.DynamicsData;

                if (dynamics == null)
                {
                    _logger.LogWarning($"Packing Slip {firstSlip.PackingSlipId} n'a pas de données Dynamics");
                    return null;
                }

                var result = new SpeedPackingSlipComplete
                {
                    CommandReference = _textProcessor.ProcessCode(dynamics.transRefId),
                    OriginalPackingSlipIds = packingSlipsGroup.Select(ps => ps.Id).ToList()
                };

                // ========== CRÉER L'EN-TÊTE (une seule fois par commande) ==========
                result.Header = CreateHeader(dynamics);

                // ========== CRÉER LES LIGNES (une par article) ==========
                int lineNumber = 1; // Commencer à 10000 selon votre exemple
                foreach (var packingSlip in packingSlipsGroup)
                {
                    if (packingSlip.DynamicsData != null)
                    {
                        var line = CreateLine(packingSlip.DynamicsData, lineNumber);
                        if (line != null)
                        {
                            result.Lines.Add(line);
                            lineNumber += 1; // Incrémenter par 1
                        }
                    }
                }

                // Log des transformations UTF-8 si en mode debug
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    LogProcessingStats(dynamics, result.Header);
                }

                _logger.LogDebug($"Commande mappée: {dynamics.transRefId} → {result.Lines.Count} lignes");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du mapping du groupe de Packing Slips");
                return null;
            }
        }

        /// <summary>
        /// Crée l'en-tête de commande selon le format OPE
        /// ✅ MODIFIÉ : Gestion des champs OPE_ALPHA > 38
        /// </summary>
        private SpeedPackingSlipHeader CreateHeader(DynamicsPackingSlip dynamics)
        {
            var header = new SpeedPackingSlipHeader
            {
                // ========== VALEURS FIXES ==========
                ACT_CODE = "COSMETIQUE",
                OPE_TOP17 = "0",

                // ========== DATES ==========
                OPE_DACO = FormatDateForTxt(DateTime.Now),                    // Date du jour
                OPE_DALI = FormatDateForTxt(dynamics.DlvDate),               // Date livraison souhaitée

                // ========== RÉFÉRENCES ==========
                OPE_REDO = _textProcessor.ProcessCode(dynamics.transRefId),   // CLÉ DE LIAISON
                TIE_CODE = _textProcessor.ProcessCode(dynamics.customer),    // Code Tiers destinataire
                OPE_RTIE = ApplyReferenceRule(dynamics),                     // RG1 et RG2
                OPE_ALPHA17 = _textProcessor.ProcessCode(dynamics.pickingRouteID), // Numéro Document D365

                // ========== INFORMATIONS CLIENT ==========
                TIE_NOM = _textProcessor.ProcessName(dynamics.DeliveryName, 50),    // Nom Tiers
                OPE_ADVL = _textProcessor.ProcessName(dynamics.City, 50),           // Ville
                OPE_ADCP = _textProcessor.ProcessCode(dynamics.ZipCode),            // Code postal
                OPE_CPAY = _textProcessor.ProcessCode(dynamics.ISOcode),            // Pays ISO
                OPE_TEL = _textProcessor.ProcessCode(dynamics.Phone),               // Téléphone
                OPE_IMEL = _textProcessor.ProcessName(dynamics.Email, 100),         // Email

                // ========== TRANSPORT ==========
                OPE_CTRA = ApplyTransportCodeRule(dynamics.CarrierCode),     // RG3

                // ========== COMMENTAIRES ==========
                OPE_COBP = _textProcessor.ProcessName(dynamics.CommentPreparation, 255), // Commentaire Préparation
                OPE_COBL = _textProcessor.ProcessName(dynamics.CommentExpedition, 255),  // Commentaire Livraison

                // ========== AUTRES CHAMPS ==========
                OPE_ALPHA21 = _textProcessor.ProcessCode(dynamics.SalesOriginId),   // Canal de Ventes
                OPE_ALPHA6 = _textProcessor.ProcessCode(dynamics.SegmentId),        // Segment
                OPE_ALPHA31 = dynamics.SellableDays.ToString(),                     // Famille Classification (BASE)

                // ✅ NOUVEAU : Champs OPE_ALPHA > 38 (exemple - à adapter selon vos besoins)
                OPE_ALPHA39 = _textProcessor.ProcessCode(dynamics.SubsegmentId),      // Sous-Segment
                OPE_ALPHA41 = _textProcessor.ProcessCode(dynamics.CardTypeRemer),     // Type Carte Remerciement
                OPE_ALPHA42 = _textProcessor.ProcessCode(dynamics.BROrderGrouping),   // Code Regroupement
                OPE_ALPHA43 = dynamics.BRPreparationEnum.ToString(),                  // Délai Préparation

                // ========== CHAMPS VIDES (selon votre fichier exemple) ==========
                OPE_ADR4 = "",
                OPE_FAX = "",
                OPE_ALPHA1 = "",
                OPE_ALPHA5 = "",
                OPE_ALPHA9 = "",
                OPE_ALPHA15 = "",
                OPE_DATE15 = "",
                OPE_ALPHA19 = "",
                OPE_ALPHA20 = "",
                OPE_ALPHA22 = "",
                OPE_ALPHA23 = "",
                OPE_ALPHA24 = "",
                OPE_ALPHA25 = "",
                OPE_ALPHA34 = "",
                OPE_ALPHA35 = "",
                OPE_ALPHA36 = "",
                OPE_ALPHA37 = "",
                OPE_ALPHA38 = ""
            };

            // ========== TRAITEMENT SPÉCIAUX ==========
            ProcessAddress(dynamics.Street, header);                          // Répartition adresse sur 3 champs
            ProcessCarrierService(dynamics.CarrierServiceCode, header);       // RG4 : séparation par @

            return header;
        }

        /// <summary>
        /// Crée une ligne de commande selon le format OPL
        /// </summary>
        private SpeedPackingSlipLine CreateLine(DynamicsPackingSlip dynamics, int lineNumber)
        {
            var line = new SpeedPackingSlipLine
            {
                // ========== VALEURS PRINCIPALES ==========
                ACT_CODE = "COSMETIQUE",
                OPL_RCDO = _textProcessor.ProcessCode(dynamics.transRefId),   // CLÉ DE LIAISON
                OPL_RLDO = lineNumber.ToString(),                             // Numéro ligne (1, 2, etc.)
                ART_CODE = _textProcessor.ProcessCode(dynamics.itemId),       // Référence article
                OPL_QTAP = dynamics.qty,                                     // Quantité
                QUA_CODE = ApplyQualityCodeRule(dynamics.PdsDispositionCode), // Code Qualité

                // ========== TRAÇABILITÉ ==========
                OPL_LOT1 = _textProcessor.ProcessCode(dynamics.inventBatchId),    // Lot
                OPL_LOT2 = _textProcessor.ProcessCode(dynamics.inventSerialId),   // Lot 2
                OPL_DLOO = FormatDateForTxt(dynamics.expDate),                    // DLUO
                OPL_NoSU = _textProcessor.ProcessCode(dynamics.LicensePlateId),   // Support

                // ========== CONDITIONNEMENT (selon votre exemple) ==========
                OPL_CONDITIONNEMENT = ApplyConditionnementRule(dynamics.BRPackingCode),

                // ========== POIDS/VOLUME ==========
                OPL_POIDS = 0, // À calculer selon vos règles métier

                // ========== CHAMPS LIBRES ==========
                OPL_ALPHA1 = "",
                OPL_ALPHA2 = "",
                OPL_ALPHA3 = ""
            };

            return line;
        }

        /// <summary>
        /// RG1 et RG2: Gestion de OPE_RTIE selon le type de commande
        /// </summary>
        private string ApplyReferenceRule(DynamicsPackingSlip dynamics)
        {
            // RG1 : OPE_RTIE pour les commandes BTB (Business to Business)
            if (!string.IsNullOrEmpty(dynamics.PurchOrderFormNum) &&
                dynamics.SalesOriginId?.ToUpper() == "BTB")
            {
                return _textProcessor.ProcessCode(dynamics.PurchOrderFormNum);
            }

            // RG2 : OPE_RTIE pour les commandes BTC (Business to Consumer)
            if (!string.IsNullOrEmpty(dynamics.BRPortalOrderNumber) &&
                dynamics.SalesOriginId?.ToUpper() == "BTC")
            {
                return _textProcessor.ProcessCode(dynamics.BRPortalOrderNumber);
            }

            // Par défaut, utiliser la référence commande
            return _textProcessor.ProcessCode(dynamics.transRefId);
        }

        /// <summary>
        /// RG3: Gestion du code transport
        /// </summary>
        private string ApplyTransportCodeRule(string? carrierCode)
        {
            if (string.IsNullOrEmpty(carrierCode))
                return "A AFFECTER";

            string cleanCode = _textProcessor.ProcessCode(carrierCode);

            // Si le code est déjà "A AFFECTER", le garder tel quel
            if (cleanCode.ToUpper() == "A AFFECTER")
                return "A AFFECTER";

            return cleanCode;
        }

        /// <summary>
        /// RG4: Code Qualité avec traitement UTF-8
        /// </summary>
        private string ApplyQualityCodeRule(string? qualityCode)
        {
            if (string.IsNullOrEmpty(qualityCode))
                return "STD";

            // ✅ TRAITEMENT UTF-8 : Nettoyer avant traitement
            string cleanCode = _textProcessor.ProcessText(qualityCode).ToUpper().Trim();

            return cleanCode switch
            {
                "STANDARD" or "STD" => "STD",
                "BLOCKED_LOGISTICS" or "BQLOG" => "BQLOG",
                "BLOCKED_QA1" or "BQQA1" => "BQQA1",
                "BLOCKED_QA2" or "BQQA2" => "BQQA2",
                "LIBERE" or "LIBRE" => "STD",
                _ => "STD"
            };
        }

        /// <summary>
        /// Applique la règle de conditionnement selon le BRPackingCode
        /// </summary>
        private string ApplyConditionnementRule(string? packingCode)
        {
            if (string.IsNullOrEmpty(packingCode))
                return "UNITE";

            string cleanCode = _textProcessor.ProcessCode(packingCode).ToUpper();

            return cleanCode switch
            {
                "COLIS" => "COLIS",
                "BOITE" => "BOITE",
                "BARQUETTE" => "BARQUETTE",
                "UNITE" => "UNITE",
                _ => "UNITE"
            };
        }

        /// <summary>
        /// Traitement de l'adresse pour l'en-tête
        /// </summary>
        private void ProcessAddress(string? street, SpeedPackingSlipHeader header)
        {
            if (string.IsNullOrEmpty(street))
            {
                header.OPE_ADR1 = "";
                header.OPE_ADR2 = "";
                header.OPE_ADR3 = "";
                return;
            }

            string cleanStreet = _textProcessor.ProcessName(street, 150); // Max 150 pour 3 champs

            // Répartir sur 3 champs de 50 caractères max
            if (cleanStreet.Length <= 50)
            {
                header.OPE_ADR1 = cleanStreet;
                header.OPE_ADR2 = "";
                header.OPE_ADR3 = "";
            }
            else if (cleanStreet.Length <= 100)
            {
                header.OPE_ADR1 = cleanStreet.Substring(0, 50);
                header.OPE_ADR2 = cleanStreet.Substring(50);
                header.OPE_ADR3 = "";
            }
            else
            {
                header.OPE_ADR1 = cleanStreet.Substring(0, 50);
                header.OPE_ADR2 = cleanStreet.Substring(50, Math.Min(50, cleanStreet.Length - 50));
                if (cleanStreet.Length > 100)
                {
                    header.OPE_ADR3 = cleanStreet.Substring(100, Math.Min(50, cleanStreet.Length - 100));
                }
                else
                {
                    header.OPE_ADR3 = "";
                }
            }
        }

        /// <summary>
        /// Traitement du service transporteur pour l'en-tête
        /// </summary>
        private void ProcessCarrierService(string? carrierServiceCode, SpeedPackingSlipHeader header)
        {
            if (string.IsNullOrEmpty(carrierServiceCode))
            {
                header.OPE_ALPHA16 = "BR";
                header.OPE_ALPHA18 = "";
                return;
            }

            string cleanService = _textProcessor.ProcessCode(carrierServiceCode);

            // Séparer par @
            var parts = cleanService.Split('@');

            if (parts.Length >= 1)
            {
                header.OPE_ALPHA16 = parts[0].Trim();
            }

            if (parts.Length >= 2)
            {
                header.OPE_ALPHA18 = parts[1].Trim();
            }
        }

        /// <summary>
        /// Formate une date pour le fichier TXT (format YYYYMMDD)
        /// </summary>
        private string FormatDateForTxt(DateTime? date)
        {
            if (date == null || date == DateTime.MinValue)
                return "";

            // Si la date est 1900-01-01, considérer comme vide
            if (date.Value.Year == 1900)
                return "";

            return date.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Log des statistiques de traitement UTF-8 pour diagnostic
        /// </summary>
        private void LogProcessingStats(DynamicsPackingSlip dynamics, SpeedPackingSlipHeader header)
        {
            var nameStats = _textProcessor.GetProcessingStats(dynamics.DeliveryName, header.TIE_NOM);
            var addressStats = _textProcessor.GetProcessingStats(dynamics.Street, header.OPE_ADR1);
            var commentStats = _textProcessor.GetProcessingStats(dynamics.CommentPreparation, header.OPE_COBP);

            if (nameStats.TransformationApplied || addressStats.TransformationApplied || commentStats.TransformationApplied)
            {
                _logger.LogDebug($"Transformations UTF-8 appliquées pour le Packing Slip {dynamics.transRefId}:");

                if (nameStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Nom: '{dynamics.DeliveryName}' → '{header.TIE_NOM}' ({nameStats.OriginalLength}→{nameStats.ProcessedLength} chars)");
                }

                if (addressStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Adresse: '{dynamics.Street}' → '{header.OPE_ADR1}' ({addressStats.OriginalLength}→{addressStats.ProcessedLength} chars)");
                }

                if (commentStats.TransformationApplied)
                {
                    _logger.LogDebug($"  Commentaire: '{dynamics.CommentPreparation}' → '{header.OPE_COBP}' ({commentStats.OriginalLength}→{commentStats.ProcessedLength} chars)");
                }
            }
        }

        /// <summary>
        /// Valide qu'un Packing Slip a les données minimales requises
        /// </summary>
        public bool ValidatePackingSlip(PackingSlip packingSlip)
        {
            if (packingSlip?.DynamicsData == null)
                return false;

            var dynamics = packingSlip.DynamicsData;

            if (string.IsNullOrEmpty(dynamics.transRefId))
            {
                _logger.LogWarning("Packing Slip sans transRefId");
                return false;
            }

            if (string.IsNullOrEmpty(dynamics.customer))
            {
                _logger.LogWarning($"Packing Slip {dynamics.transRefId} sans customer");
                return false;
            }

            if (string.IsNullOrEmpty(dynamics.itemId))
            {
                _logger.LogWarning($"Packing Slip {dynamics.transRefId} sans itemId");
                return false;
            }

            if (dynamics.qty <= 0)
            {
                _logger.LogWarning($"Packing Slip {dynamics.transRefId} avec quantité invalide: {dynamics.qty}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Méthode utilitaire pour obtenir un résumé du Packing Slip mappé avec info UTF-8
        /// </summary>
        public string GetMappingSummary(PackingSlip packingSlip)
        {
            if (packingSlip?.DynamicsData == null)
                return "Aucune donnée disponible";

            var dynamics = packingSlip.DynamicsData;

            return $"=== MAPPING PACKING SLIP (selon mapping + UTF-8) ===\n" +
                   $"API → SPEED:\n" +
                   $"  dataAreaId: '{dynamics.dataAreaId}' → Société Référence (traité UTF-8)\n" +
                   $"  transRefId: '{dynamics.transRefId}' → OPE_REDO (traité UTF-8)\n" +
                   $"  customer: '{dynamics.customer}' → TIE_CODE (traité UTF-8)\n" +
                   $"  PurchOrderFormNum: '{dynamics.PurchOrderFormNum}' → OPE_RTIE (RG1 BTB, traité UTF-8)\n" +
                   $"  BRPortalOrderNumber: '{dynamics.BRPortalOrderNumber}' → OPE_RTIE (RG2 BTC, traité UTF-8)\n" +
                   $"  DeliveryName: '{dynamics.DeliveryName}' → TIE_NOM (traité UTF-8, max 50 chars)\n" +
                   $"  Street: '{dynamics.Street}' → OPE_ADR1/ADR2/ADR3 (traité UTF-8, réparti si > 50 chars)\n" +
                   $"  City: '{dynamics.City}' → OPE_ADVL (traité UTF-8)\n" +
                   $"  ZipCode: '{dynamics.ZipCode}' → OPE_ADCP (traité UTF-8)\n" +
                   $"  CarrierCode: '{dynamics.CarrierCode}' → OPE_CTRA (RG3: 'A AFFECTER' si vide, traité UTF-8)\n" +
                   $"  CarrierServiceCode: '{dynamics.CarrierServiceCode}' → OPE_ALPHA16@OPE_ALPHA18 (RG4: séparé par @, traité UTF-8)\n" +
                   $"  DlvDate: '{dynamics.DlvDate}' → OPE_DALI (format YYYYMMDD)\n" +
                   $"  CommentPreparation: '{dynamics.CommentPreparation}' → OPE_COBP (traité UTF-8)\n" +
                   $"  CommentExpedition: '{dynamics.CommentExpedition}' → OPE_COBL (traité UTF-8)\n" +
                   $"  SalesOriginId: '{dynamics.SalesOriginId}' → OPE_ALPHA21 (traité UTF-8)\n" +
                   $"  SegmentId: '{dynamics.SegmentId}' → OPE_ALPHA6 (traité UTF-8)\n" +
                   $"  SellableDays: {dynamics.SellableDays} → OPE_ALPHA31\n" +
                   $"  ACT_CODE: 'COSMETIQUE' (fixe)\n" +
                   $"  OPE_TOP17: '0' (fixe)\n" +
                   $"  ✅ TOUS LES CHAMPS TEXTE TRAITÉS AVEC NORMALISATION UTF-8\n" +
                   $"  ✅ GESTION AUTOMATIQUE DES ADRESSES > 50 CARACTÈRES\n" +
                   $"  ✅ RÈGLES DE GESTION RG1, RG2, RG3, RG4 APPLIQUÉES";
        }
    }
}