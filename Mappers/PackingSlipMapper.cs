using System;
using System.Globalization;
using DynamicsToXmlTranslator.Models;
using DynamicsToXmlTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;
using static DynamicsToXmlTranslator.Models.SpeedPackingSlipHeader;

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
        /// ✅ MODIFIÉ : Utiliser la nouvelle méthode avec UTF-8 complet
        /// </summary>
        private SpeedPackingSlipHeader CreateHeader(DynamicsPackingSlip dynamics)
        {
            return CreateHeaderWithFullUtf8(dynamics);
        }

        /// <summary>
        /// ✅ CORRIGÉ : Traitement de l'adresse avec UTF-8 pour l'en-tête
        /// Applique le traitement UTF-8 complet sur l'adresse AVANT la répartition
        /// </summary>
        private void ProcessAddressWithUtf8(string? street, SpeedPackingSlipHeader header)
        {
            if (string.IsNullOrEmpty(street))
            {
                header.OPE_ADR1 = "";
                header.OPE_ADR2 = "";
                header.OPE_ADR3 = "";
                return;
            }

            // ✅ CORRECTION PRINCIPALE : Appliquer le traitement UTF-8 complet avec suppression des entités
            string cleanStreet = _textProcessor.ProcessName(street, 150); // UTF-8 + max 150 pour 3 champs

            // ✅ AJOUT : Log pour diagnostic
            if (_logger.IsEnabled(LogLevel.Debug) && street != cleanStreet)
            {
                _logger.LogDebug($"Adresse UTF-8 transformée: '{street}' → '{cleanStreet}'");
            }

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

            // ✅ AJOUT : Log du résultat final
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Adresse répartie: ADR1='{header.OPE_ADR1}' ADR2='{header.OPE_ADR2}' ADR3='{header.OPE_ADR3}'");
            }
        }

        /// <summary>
        /// ✅ MODIFIÉ : Traitement UTF-8 complet pour TOUS les champs texte d'en-tête + TIE_CODE dans ALPHA31
        /// </summary>
        private SpeedPackingSlipHeader CreateHeaderWithFullUtf8(DynamicsPackingSlip dynamics)
        {
            var header = new SpeedPackingSlipHeader
            {
                // ========== VALEURS FIXES ==========
                ACT_CODE = "COSMETIQUE",
                OPE_TOP17 = "0",

                // ========== DATES ==========
                OPE_DACO = FormatDateForTxt(DateTime.Now),
                OPE_DALI = FormatDateForTxt(dynamics.DlvDate),

                // ========== RÉFÉRENCES AVEC UTF-8 ==========
                OPE_REDO = _textProcessor.ProcessCode(dynamics.transRefId),
                TIE_CODE = _textProcessor.ProcessCode(dynamics.customer), // ✅ IMPORTANT : Bien définir TIE_CODE ici
                OPE_RTIE = ApplyReferenceRuleWithUtf8(dynamics),
                OPE_ALPHA17 = _textProcessor.ProcessCode(dynamics.pickingRouteID),

                // ========== INFORMATIONS CLIENT AVEC UTF-8 COMPLET ==========
                TIE_NOM = _textProcessor.ProcessName(dynamics.DeliveryName, 50),
                OPE_ADVL = _textProcessor.ProcessName(dynamics.City, 50),
                OPE_ADCP = _textProcessor.ProcessCode(dynamics.ZipCode),
                OPE_CPAY = _textProcessor.ProcessCode(dynamics.ISOcode),
                OPE_TEL = _textProcessor.ProcessCode(dynamics.Phone),
                OPE_IMEL = _textProcessor.ProcessName(dynamics.Email, 100),
                OPE_CONT = _textProcessor.ProcessName(dynamics.Contact, 50),

                // ========== TRANSPORT AVEC UTF-8 ==========
                OPE_CTRA = ApplyTransportCodeRuleWithUtf8(dynamics.CarrierCode),

                // ========== COMMENTAIRES AVEC UTF-8 COMPLET ==========
                OPE_COBP = _textProcessor.ProcessName(dynamics.CommentPreparation, 255),
                OPE_COME = _textProcessor.ProcessName(dynamics.CommentExpedition, 255),
                OPE_COBL = "",

                // ========== AUTRES CHAMPS AVEC UTF-8 ==========
                OPE_ALPHA21 = _textProcessor.ProcessCode(dynamics.SalesOriginId),
                OPE_ALPHA6 = _textProcessor.ProcessCode(dynamics.SegmentId),
                OPE_ALPHA31 = dynamics.SellableDays.ToString(), // ✅ Valeur de base, TIE_CODE sera ajouté via GetFormattedOpeAlpha31()
                OPE_ALPHA45 = _textProcessor.ProcessCode(dynamics.CardTypeRemer),
                OPE_ALPHA46 = _textProcessor.ProcessCode(dynamics.BRTransportModeCode),
                OPE_ALPHA47 = _textProcessor.ProcessCode(dynamics.BoxTypeBtc),
                OPE_ALPHA48 = _textProcessor.ProcessCode(dynamics.DlvTermId),
                OPE_GRP = _textProcessor.ProcessCode(dynamics.BROrderGrouping),
                OPE_MPCO = _textProcessor.ProcessCode(dynamics.BRPackingCode),
                OPE_UNICODE11 = _textProcessor.ProcessName(dynamics.MessagePerso, 255),
                OPE_TOP25 = dynamics.BRPreparationEnum.ToString(),
                OPE_TOP28 = dynamics.BRShippingDocumentEnum.ToString(),

                // ========== CHAMPS VIDES ==========
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
                OPE_ALPHA38 = "",
                STATUT = ""
            };

            // ========== TRAITEMENT SPÉCIAUX AVEC UTF-8 ==========
            ProcessAddressWithUtf8(dynamics.Street, header);
            ProcessCarrierServiceWithUtf8(dynamics.CarrierServiceCode, header);

            return header;
        }

        /// <summary>
        /// ✅ CORRIGÉ : RG3 avec traitement UTF-8 complet + normalisation A_AFFECTER → A AFFECTER
        /// </summary>
        private string ApplyTransportCodeRuleWithUtf8(string? carrierCode)
        {
            if (string.IsNullOrEmpty(carrierCode))
                return "A AFFECTER";

            string cleanCode = _textProcessor.ProcessCode(carrierCode);

            // ✅ NOUVEAU : Normaliser A_AFFECTER vers A AFFECTER
            if (cleanCode.ToUpper() == "A_AFFECTER")
                return "A AFFECTER";

            if (cleanCode.ToUpper() == "A AFFECTER")
                return "A AFFECTER";

            return cleanCode;
        }

        /// <summary>
        /// ✅ CORRIGÉ : RG1 et RG2 avec traitement UTF-8 complet
        /// </summary>
        private string ApplyReferenceRuleWithUtf8(DynamicsPackingSlip dynamics)
        {
            // RG1 : OPE_RTIE pour les commandes BTB
            if (!string.IsNullOrEmpty(dynamics.PurchOrderFormNum) &&
                dynamics.SalesOriginId?.ToUpper() == "BTB")
            {
                return _textProcessor.ProcessCode(dynamics.PurchOrderFormNum);
            }

            // RG2 : OPE_RTIE pour les commandes BTC
            if (!string.IsNullOrEmpty(dynamics.BRPortalOrderNumber) &&
                dynamics.SalesOriginId?.ToUpper() == "BTC")
            {
                return _textProcessor.ProcessCode(dynamics.BRPortalOrderNumber);
            }

            // Par défaut, utiliser la référence commande
            return _textProcessor.ProcessCode(dynamics.transRefId);
        }

        /// <summary>
        /// ✅ CORRIGÉ : Traitement service transporteur avec UTF-8 (mais préservation + et @)
        /// </summary>
        private void ProcessCarrierServiceWithUtf8(string? carrierServiceCode, SpeedPackingSlipHeader header)
        {
            if (string.IsNullOrEmpty(carrierServiceCode))
            {
                header.OPE_ALPHA40 = "";
                header.OPE_ALPHA41 = "";
                return;
            }

            // ✅ NOUVEAU : Appliquer d'abord le traitement UTF-8 mais préserver + et @
            string utf8Processed = _textProcessor.ProcessText(carrierServiceCode);

            // Restaurer les caractères + et @ s'ils ont été supprimés
            string serviceCode = RestorePlusAndAt(carrierServiceCode, utf8Processed);

            // Log pour diagnostic
            if (_logger.IsEnabled(LogLevel.Debug) && carrierServiceCode != serviceCode)
            {
                _logger.LogDebug($"Service transporteur UTF-8: '{carrierServiceCode}' → '{serviceCode}'");
            }

            // Cas spécial MAD
            if (serviceCode.Equals("MAD", StringComparison.OrdinalIgnoreCase))
            {
                header.OPE_ALPHA40 = "MAD";
                header.OPE_ALPHA41 = "";
                return;
            }

            // Séparer par @
            var parts = serviceCode.Split('@');

            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                header.OPE_ALPHA40 = parts[0].Trim();
            }
            else
            {
                header.OPE_ALPHA40 = "";
            }

            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                header.OPE_ALPHA41 = parts[1].Trim();
            }
            else
            {
                header.OPE_ALPHA41 = "";
            }
        }

        /// <summary>
        /// ✅ NOUVEAU : Restaure les caractères + et @ après traitement UTF-8
        /// </summary>
        private string RestorePlusAndAt(string original, string processed)
        {
            if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(processed))
                return processed ?? "";

            var result = processed;

            // Restaurer les + s'ils ont été supprimés
            if (original.Contains("+") && !result.Contains("+"))
            {
                // Rechercher la position approximative du + dans l'original
                for (int i = 0; i < original.Length && i < result.Length; i++)
                {
                    if (original[i] == '+')
                    {
                        result = result.Insert(Math.Min(i, result.Length), "+");
                        break;
                    }
                }
            }

            // Restaurer les @ s'ils ont été supprimés
            if (original.Contains("@") && !result.Contains("@"))
            {
                var originalParts = original.Split('@');
                if (originalParts.Length == 2)
                {
                    // Trouver la position approximative du @
                    var firstPartProcessed = _textProcessor.ProcessText(originalParts[0]);
                    var secondPartProcessed = _textProcessor.ProcessText(originalParts[1]);
                    result = firstPartProcessed + "@" + secondPartProcessed;
                }
            }

            return result;
        }



        /// <summary>
        /// Crée une ligne de commande selon le format OPL
        /// ✅ MODIFIÉ : Préfixe "BR" ajouté aux codes articles
        /// </summary>
        private SpeedPackingSlipLine CreateLine(DynamicsPackingSlip dynamics, int lineNumber)
        {
            var line = new SpeedPackingSlipLine
            {
                // ========== VALEURS PRINCIPALES ==========
                ACT_CODE = "COSMETIQUE",
                OPL_RCDO = _textProcessor.ProcessCode(dynamics.transRefId),   // CLÉ DE LIAISON ✅ CORRECT
                OPL_RLDO = lineNumber.ToString(),                             // Numéro ligne ✅ CORRECT
                ART_CODE = "BR" + _textProcessor.ProcessCode(dynamics.itemId), // ✅ MODIFIÉ : Préfixe "BR" ajouté
                OPL_QTAP = dynamics.qty,                                     // Quantité ✅ CORRECT => Mappage Exact de la table CDLG = OPL_QTOC

                // ✅ CORRIGÉ : Mapping selon client
                QUA_CODE = ApplyQualityCodeRuleCorrected(dynamics.PdsDispositionCode), // Code Qualité

                //AJOUT RD 01/08/2025
                OPL_ALPHA1 = "",

                // ✅ CORRIGÉ : Mapping inversé selon client
                OPL_LOT1 = _textProcessor.ProcessCode(dynamics.inventBatchId),   // Lot 1 = inventBatchId

                //MODIF ET AJOUT RD 01/08/2025

                //OPL_LOT2 = _textProcessor.ProcessCode(dynamics.inventSerialId),    // Lot 2 = inventSerialId ==> TRANSFERER SUR OPL_ALPHA4
                //OPL_DLOO = FormatDateForTxt(dynamics.expDate),                    // DLUO = expDate ✅ CORRECT ==> TRANSFERER SUR OPL_DLC
                //OPL_NoSU = _textProcessor.ProcessCode(dynamics.LicensePlateId),   // Support ✅ CORRECT ==> TRANSFERER SUR OPL_ALPHA6

                OPL_DLC = FormatDateForTxt(dynamics.expDate),                       // DLUO ==> OPL_DLOO
                OPL_ALPHA3 = "",
                OPL_ALPHA4 = _textProcessor.ProcessCode(dynamics.inventSerialId),  // Lot 2 = OPL_LOT2
                OPL_ALPHA5 = _textProcessor.ProcessCode(dynamics.LicensePlateId),   // Numero de contenant = OPL_NOSU
                OPL_ALPHA6 = "",
                OPL_ALPHA7 = "",
                STATUT = "",

                //PLUS NECESSAIRE

                // ========== CONDITIONNEMENT ==========
                //OPL_CONDITIONNEMENT = ApplyConditionnementRule(dynamics.BRPackingCode), // ✅ CORRECT

                // ========== POIDS/VOLUME ==========
                //OPL_POIDS = 0, // À calculer selon vos règles métier

                // ========== CHAMPS LIBRES ==========
                //OPL_ALPHA1 = "",
                //OPL_ALPHA2 = "",
                //OPL_ALPHA3 = ""

                //FIN DE MODIF ET AJOUT RD 01/08/2025
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
        /// ✅ CORRIGÉ : Code Qualité selon mapping client (STD par défaut)
        /// </summary>
        private string ApplyQualityCodeRuleCorrected(string? qualityCode)
        {
            if (string.IsNullOrEmpty(qualityCode))
                return "STD"; // ✅ CORRIGÉ : STD par défaut selon client

            string cleanCode = _textProcessor.ProcessText(qualityCode).ToUpper().Trim();

            // TODO: Utiliser la table de correspondance fournie par le client
            return cleanCode switch
            {
                "STANDARD" or "STD" => "STD",
                "BLOCKED_LOGISTICS" or "BQLOG" => "BQLOG",
                "BLOCKED_QA1" or "BQQA1" => "BQQA1",
                "BLOCKED_QA2" or "BQQA2" => "BQQA2",
                "LIBERE" or "LIBRE" => "STD",
                _ => "STD" // ✅ CORRIGÉ : Par défaut STD
            };
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
        /// ✅ CORRIGÉ : Traitement du service transporteur SANS processeur UTF-8 pour préserver + et @
        /// Format: "J+2@18" → alpha40="J+2", alpha41="18"
        /// Format: "MAD" → alpha40="MAD", alpha41=""
        /// Format vide → alpha40="", alpha41=""
        /// </summary>
        private void ProcessCarrierServiceCorrected(string? carrierServiceCode, SpeedPackingSlipHeader header)
        {
            if (string.IsNullOrEmpty(carrierServiceCode))
            {
                header.OPE_ALPHA40 = "";  // ✅ CORRIGÉ : Vide au lieu de "BR"
                header.OPE_ALPHA41 = "";
                return;
            }

            // ✅ NOUVEAU : Traitement SANS UTF-8 pour préserver + et @
            string cleanService = CleanCarrierServiceOnly(carrierServiceCode);

            // Cas spécial MAD : pas de séparation
            if (cleanService.Equals("MAD", StringComparison.OrdinalIgnoreCase))
            {
                header.OPE_ALPHA40 = "MAD";
                header.OPE_ALPHA41 = "";
                return;
            }

            // Séparer par @ pour les autres cas
            var parts = cleanService.Split('@');

            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                header.OPE_ALPHA40 = parts[0].Trim(); // Garde le + intact
            }
            else
            {
                header.OPE_ALPHA40 = ""; // ✅ CORRIGÉ : Vide au lieu de "BR"
            }

            if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                header.OPE_ALPHA41 = parts[1].Trim();
            }
            else
            {
                header.OPE_ALPHA41 = "";
            }
        }

        /// <summary>
        /// ✅ NOUVEAU : Nettoyage minimal pour CarrierServiceCode SANS processeur UTF-8
        /// Préserve les caractères + et @ qui sont importants pour ce champ
        /// </summary>
        private string CleanCarrierServiceOnly(string carrierServiceCode)
        {
            if (string.IsNullOrEmpty(carrierServiceCode))
                return "";

            // Nettoyage minimal : seulement les caractères de contrôle et espaces superflus
            return carrierServiceCode
                .Replace("\r", "")      // Supprimer retours chariot
                .Replace("\n", "")      // Supprimer sauts de ligne  
                .Replace("\t", " ")     // Remplacer tabulations par espaces
                .Trim();                // Supprimer espaces début/fin
                                        // IMPORTANT : On garde + et @ intacts !
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