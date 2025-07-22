using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace DynamicsToXmlTranslator.Services
{
    /// <summary>
    /// Service de traitement des caractères spéciaux et normalisation UTF-8 pour export XML
    /// VERSION BULLETPROOF - GARANTIE SANS DOUBLONS
    /// </summary>
    public class Utf8TextProcessor
    {
        private readonly ILogger<Utf8TextProcessor> _logger;
        private readonly Dictionary<string, string> _characterMapping;
        private readonly Regex _nonAsciiRegex;

        public Utf8TextProcessor(ILogger<Utf8TextProcessor> logger)
        {
            _logger = logger;
            _characterMapping = InitializeCharacterMapping();
            _nonAsciiRegex = new Regex(@"[^\x00-\x7F]", RegexOptions.Compiled);
        }

        /// <summary>
        /// Traite et normalise un texte pour l'export XML
        /// </summary>
        public string ProcessText(string? input, int? maxLength = null)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            try
            {
                // ✅ ÉTAPE 1 : Normalisation Unicode AVANT tout traitement
                string normalized = input.Normalize(NormalizationForm.FormKD);

                // ✅ ÉTAPE 2 : Nettoyage des entités HTML/XML AVANT les remplacements
                string cleaned = UnescapeHtmlEntities(normalized);

                // ✅ ÉTAPE 3 : Remplacement des caractères spéciaux
                string processed = ReplaceSpecialCharacters(cleaned);

                // ✅ ÉTAPE 4 : Suppression caractères de contrôle
                processed = RemoveControlCharacters(processed);

                // ✅ ÉTAPE 5 : Suppression accents restants
                processed = RemoveAccents(processed);

                // ✅ ÉTAPE 6 : Échappement XML (EN DERNIER pour éviter doubles échappements)
                processed = EscapeXmlCharacters(processed);

                if (maxLength.HasValue && processed.Length > maxLength.Value)
                {
                    processed = processed.Substring(0, maxLength.Value);
                    _logger.LogDebug($"Texte tronqué à {maxLength.Value} caractères: '{input}' → '{processed}'");
                }

                ValidateXmlCompatibility(processed);

                if (input != processed)
                {
                    _logger.LogTrace($"Texte transformé: '{input}' → '{processed}'");
                }

                return processed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors du traitement du texte: '{input}'");
                return CleanBasicText(input, maxLength);
            }
        }

        /// <summary>
        /// ✅ MODIFIÉ : Supprime complètement les entités HTML/XML communes (remplacées par du vide)
        /// </summary>
        private string UnescapeHtmlEntities(string input)
        {
            return input
                .Replace("&amp;", "")        // Entité & → supprimé
                .Replace("&apos;", "")       // Entité apostrophe → supprimé
                .Replace("&quot;", "")       // Entité guillemets → supprimé
                .Replace("&lt;", "")         // Entité < → supprimé
                .Replace("&gt;", "")         // Entité > → supprimé
                .Replace("&#39;", "")        // Code numérique apostrophe → supprimé
                .Replace("&#x27;", "")       // Code hexadécimal apostrophe → supprimé
                .Replace("&#34;", "")        // Code numérique guillemets → supprimé
                .Replace("&#x22;", "");      // Code hexadécimal guillemets → supprimé
        }

        /// <summary>
        /// Traite spécifiquement les codes articles et identifiants
        /// </summary>
        public string ProcessCode(string? code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            string processed = ProcessText(code);
            processed = processed.Replace(" ", "_");
            processed = Regex.Replace(processed, @"[^a-zA-Z0-9_\-.]", "");
            return processed.ToUpper();
        }

        /// <summary>
        /// Traite les noms et descriptions avec préservation maximale
        /// </summary>
        public string ProcessName(string? name, int? maxLength = null)
        {
            if (string.IsNullOrEmpty(name))
                return "";

            string processed = ProcessText(name, maxLength);
            processed = Regex.Replace(processed, @"\s+", " ");
            processed = processed.Trim();
            return processed;
        }

        /// <summary>
        /// ✅ MODIFIÉ : Dictionnaire de caractères spéciaux SANS les entités HTML
        /// </summary>
        private Dictionary<string, string> InitializeCharacterMapping()
        {
            var mapping = new Dictionary<string, string>();

            // ========== ÉTAPE 1: RÈGLE SPÉCIALE & (caractère direct, pas l'entité) ==========
            TryAdd(mapping, "&", "et");
            TryAdd(mapping, " & ", " et ");

            // ========== ÉTAPE 2: VOYELLES MINUSCULES ==========
            TryAdd(mapping, "à", "a"); TryAdd(mapping, "á", "a"); TryAdd(mapping, "â", "a"); TryAdd(mapping, "ã", "a"); TryAdd(mapping, "ä", "a"); TryAdd(mapping, "å", "a");
            TryAdd(mapping, "è", "e"); TryAdd(mapping, "é", "e"); TryAdd(mapping, "ê", "e"); TryAdd(mapping, "ë", "e");
            TryAdd(mapping, "ì", "i"); TryAdd(mapping, "í", "i"); TryAdd(mapping, "î", "i"); TryAdd(mapping, "ï", "i");
            TryAdd(mapping, "ò", "o"); TryAdd(mapping, "ó", "o"); TryAdd(mapping, "ô", "o"); TryAdd(mapping, "õ", "o"); TryAdd(mapping, "ö", "o");
            TryAdd(mapping, "ù", "u"); TryAdd(mapping, "ú", "u"); TryAdd(mapping, "û", "u"); TryAdd(mapping, "ü", "u");

            // ========== ÉTAPE 3: CONSONNES SPÉCIALES MINUSCULES ==========
            TryAdd(mapping, "ç", "c"); TryAdd(mapping, "ñ", "n"); TryAdd(mapping, "ÿ", "y"); TryAdd(mapping, "ý", "y");

            // ========== ÉTAPE 4: VOYELLES MAJUSCULES ==========
            TryAdd(mapping, "À", "A"); TryAdd(mapping, "Á", "A"); TryAdd(mapping, "Â", "A"); TryAdd(mapping, "Ã", "A"); TryAdd(mapping, "Ä", "A"); TryAdd(mapping, "Å", "A");
            TryAdd(mapping, "È", "E"); TryAdd(mapping, "É", "E"); TryAdd(mapping, "Ê", "E"); TryAdd(mapping, "Ë", "E");
            TryAdd(mapping, "Ì", "I"); TryAdd(mapping, "Í", "I"); TryAdd(mapping, "Î", "I"); TryAdd(mapping, "Ï", "I");
            TryAdd(mapping, "Ò", "O"); TryAdd(mapping, "Ó", "O"); TryAdd(mapping, "Ô", "O"); TryAdd(mapping, "Õ", "O"); TryAdd(mapping, "Ö", "O");
            TryAdd(mapping, "Ù", "U"); TryAdd(mapping, "Ú", "U"); TryAdd(mapping, "Û", "U"); TryAdd(mapping, "Ü", "U");

            // ========== ÉTAPE 5: CONSONNES SPÉCIALES MAJUSCULES ==========
            TryAdd(mapping, "Ç", "C"); TryAdd(mapping, "Ñ", "N"); TryAdd(mapping, "Ÿ", "Y"); TryAdd(mapping, "Ý", "Y");

            // ========== ÉTAPE 6: LIGATURES ==========
            TryAdd(mapping, "œ", "oe"); TryAdd(mapping, "Œ", "OE"); TryAdd(mapping, "æ", "ae"); TryAdd(mapping, "Æ", "AE"); TryAdd(mapping, "ß", "ss");

            // ========== ÉTAPE 7: DEVISES ==========
            TryAdd(mapping, "€", "EUR"); TryAdd(mapping, "$", "USD"); TryAdd(mapping, "£", "GBP");

            // ========== ÉTAPE 8: SYMBOLES ==========
            TryAdd(mapping, "°", "deg"); TryAdd(mapping, "©", "(C)"); TryAdd(mapping, "®", "(R)"); TryAdd(mapping, "™", "(TM)");

            // ========== ÉTAPE 9: GUILLEMETS (codes Unicode explicites) ==========
            TryAdd(mapping, "\u201C", "\""); // "
            TryAdd(mapping, "\u201D", "\""); // "
            TryAdd(mapping, "\u2018", "'");  // '
            TryAdd(mapping, "\u2019", "'");  // '
            TryAdd(mapping, "\u00AB", "\""); // «
            TryAdd(mapping, "\u00BB", "\""); // »

            // ========== ÉTAPE 10: TIRETS ==========
            TryAdd(mapping, "\u2013", "-"); // –
            TryAdd(mapping, "\u2014", "-"); // —

            // ========== ÉTAPE 11: ESPACES SPÉCIAUX (codes Unicode explicites uniquement) ==========
            TryAdd(mapping, "\u00A0", " ");  // Espace insécable
            TryAdd(mapping, "\u2009", " ");  // Espace fine
            TryAdd(mapping, "\u2008", " ");  // Espace de ponctuation
            TryAdd(mapping, "\u2006", " ");  // Espace d'un sixième de cadratin
            TryAdd(mapping, "\u2007", " ");  // Espace de chiffre

            // ========== ÉTAPE 12: CARACTÈRES MATHÉMATIQUES ==========
            TryAdd(mapping, "×", "x"); TryAdd(mapping, "÷", "/"); TryAdd(mapping, "±", "+/-");

            // ========== ÉTAPE 13: PONCTUATION SPÉCIALE ==========
            TryAdd(mapping, "…", "..."); TryAdd(mapping, "‚", ","); TryAdd(mapping, "„", "\"");

            // ========== ÉTAPE 14: CARACTÈRES DE CONTRÔLE (codes Unicode explicites) ==========
            for (int i = 0; i <= 31; i++)
            {
                if (i != 9 && i != 10 && i != 13) // Garder \t, \n, \r
                {
                    TryAdd(mapping, ((char)i).ToString(), "");
                }
            }
            TryAdd(mapping, "\u007F", ""); // DEL

            _logger.LogInformation($"✅ Dictionnaire UTF-8 initialisé avec {mapping.Count} mappings sans doublons");
            return mapping;
        }

        /// <summary>
        /// ✅ MÉTHODE BULLETPROOF : Ajoute une clé seulement si elle n'existe pas déjà
        /// </summary>
        private void TryAdd(Dictionary<string, string> dict, string key, string value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }
            else
            {
                _logger.LogWarning($"Clé dupliquée ignorée: '{key}' = '{value}'");
            }
        }

        /// <summary>
        /// Remplace les caractères spéciaux connus
        /// </summary>
        private string ReplaceSpecialCharacters(string input)
        {
            foreach (var mapping in _characterMapping)
            {
                input = input.Replace(mapping.Key, mapping.Value);
            }
            return input;
        }

        /// <summary>
        /// Supprime les caractères de contrôle Unicode
        /// </summary>
        private string RemoveControlCharacters(string input)
        {
            var result = new StringBuilder();
            foreach (char c in input)
            {
                if (!char.IsControl(c) || c == '\t' || c == '\n' || c == '\r')
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Supprime les accents des caractères restants
        /// </summary>
        private string RemoveAccents(string input)
        {
            var normalizedString = input.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// ✅ MODIFIÉ : Échappe les caractères spéciaux XML (SANS traiter & qui est déjà géré)
        /// </summary>
        private string EscapeXmlCharacters(string input)
        {
            // Ne pas échapper & car il est déjà traité comme "et"
            return input
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");  // Apostrophe devient &apos; en dernier
        }

        public void LogCorrectionExamples(ILogger logger)
        {
            var examples = new Dictionary<string, string>
    {
        {"Adrech de la Colle d&apos;Ausse", "Adrech de la Colle dAusse"},
        {"L&apos;Oréal & Co", "LOreal et Co"},
        {"Beauté &amp; Santé", "Beaute  Sante"},
        {"Crème &quot;premium&quot;", "Creme premium"},
        {"Société &amp; Associés", "Societe  Associes"},
        {"L&apos;Occitane en Provence", "LOccitane en Provence"},
        {"Prix &lt;100&gt; euros", "Prix 100 euros"}
    };

            logger.LogInformation("=== EXEMPLES DE CORRECTIONS UTF-8 (ENTITÉS SUPPRIMÉES) ===");
            foreach (var example in examples)
            {
                var processed = ProcessText(example.Key);
                var status = processed == example.Value ? "✅" : "❌";

                logger.LogInformation($"{status} '{example.Key}' → '{processed}'");
                if (processed != example.Value)
                {
                    logger.LogWarning($"   Attendu: '{example.Value}'");
                }
            }
            logger.LogInformation("=== FIN EXEMPLES CORRECTIONS ===");
        }

        /// <summary>
        /// Valide que le texte est compatible XML
        /// </summary>
        private void ValidateXmlCompatibility(string text)
        {
            foreach (char c in text)
            {
                if (IsInvalidXmlChar(c))
                {
                    _logger.LogWarning($"Caractère XML invalide détecté: U+{((int)c):X4} dans '{text}'");
                }
            }
        }

        /// <summary>
        /// Vérifie si un caractère est invalide en XML 1.0
        /// </summary>
        private bool IsInvalidXmlChar(char c)
        {
            return !(c == 0x09 || c == 0x0A || c == 0x0D ||
                    (c >= 0x20 && c <= 0xD7FF) ||
                    (c >= 0xE000 && c <= 0xFFFD));
        }

        /// <summary>
        /// Nettoyage basique en cas d'erreur
        /// </summary>
        private string CleanBasicText(string input, int? maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            var result = new StringBuilder();
            foreach (char c in input)
            {
                if (c >= 32 && c <= 126) // ASCII imprimable
                {
                    result.Append(c);
                }
                else if (c == ' ' || c == '\t')
                {
                    result.Append(' ');
                }
            }

            string cleaned = result.ToString().Trim();
            if (maxLength.HasValue && cleaned.Length > maxLength.Value)
            {
                cleaned = cleaned.Substring(0, maxLength.Value);
            }
            return cleaned;
        }

        /// <summary>
        /// Statistiques de traitement pour diagnostic
        /// </summary>
        public TextProcessingStats GetProcessingStats(string originalText, string processedText)
        {
            return new TextProcessingStats
            {
                OriginalLength = originalText?.Length ?? 0,
                ProcessedLength = processedText?.Length ?? 0,
                HasSpecialCharacters = _nonAsciiRegex.IsMatch(originalText ?? ""),
                TransformationApplied = originalText != processedText
            };
        }

        /// <summary>
        /// Méthode de test pour illustrer les transformations
        /// </summary>
        public void LogTransformationExamples(ILogger logger)
        {
            var examples = new Dictionary<string, string>
            {
                {"L'Oréal & Co", "L'Oreal et Co"},
                {"Beauté & Santé", "Beaute et Sante"},
                {"Shampoing & Soin", "Shampoing et Soin"},
                {"Crème hydratante", "Creme hydratante"},
                {"Sérum régénérant", "Serum regenerant"},
                {"Après-shampoing", "Apres-shampoing"},
                {"Démaquillant", "Demaquillant"},
                {"L'Occitane en Provence & Cie", "L'Occitane en Provence et Cie"},
                {"Garnier Fructis - Fortifiant & Réparateur", "Garnier Fructis - Fortifiant et Reparateur"},
                {"Nivea Crème & Huile Corporelle", "Nivea Creme et Huile Corporelle"}
            };

            logger.LogInformation("=== EXEMPLES DE TRANSFORMATIONS UTF-8 ===");
            foreach (var example in examples)
            {
                var processed = ProcessText(example.Key);
                var expected = example.Value;
                var status = processed == expected ? "✅" : "❌";

                logger.LogInformation($"{status} '{example.Key}' → '{processed}'");
                if (processed != expected)
                {
                    logger.LogWarning($"   Attendu: '{expected}'");
                }
            }
            logger.LogInformation("=== FIN EXEMPLES TRANSFORMATIONS ===");
        }
    }

    /// <summary>
    /// Statistiques de traitement de texte
    /// </summary>
    public class TextProcessingStats
    {
        public int OriginalLength { get; set; }
        public int ProcessedLength { get; set; }
        public bool HasSpecialCharacters { get; set; }
        public bool TransformationApplied { get; set; }
    }
}