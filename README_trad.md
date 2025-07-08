# 🔄 DynamicsToXmlTranslator

## 📄 Description

**DynamicsToXmlTranslator** est un outil de traduction automatisé de données Dynamics 365 vers des fichiers XML compatibles WINDEV. Il extrait les articles depuis une base de données **SQL Server** et les convertit au format XML en mode complètement autonome.

## 🏗️ Architecture Technique

### **Technologies utilisées :**

- **.NET 6.0** (Console Application)
- **C#** pour la logique métier
- **SQL Server** pour le stockage de données (table JSON_IN)
- **XML Serialization** pour la génération des fichiers
- **Serilog** pour les logs
- **Microsoft.Extensions** pour la configuration

### **Fichiers de code principaux :**

**📁 Emplacement des fichiers :**

```
DynamicsToXmlTranslator/
├── Program.cs                    # ← Point d'entrée automatisé (sans menu)
├── Models/
│   ├── Article.cs               # ← Modèle des articles Dynamics
│   └── WinDevArticle.cs         # ← Structure XML WINDEV
├── Mappers/
│   └── ArticleMapper.cs         # ← Logique de transformation
├── Services/
│   ├── DatabaseService.cs       # ← Accès base de données SQL Server
│   └── XmlExportService.cs      # ← Génération fichiers XML
├── DynamicsToXmlTranslator.csproj   # ← Configuration projet
├── appsettings.json.example     # ← Template configuration
└── appsettings.json             # ← Configuration (à créer)
```

## ⚙️ Configuration

### **1. Fichier de configuration**

**📄 Fichier à créer :** `appsettings.json`

```json
{
  "Database": {
    "Host": "localhost\\SQLEXPRESS",
    "Name": "nom_de_votre_base_middleware",
    "User": "votre_utilisateur",
    "Password": "votre_mot_de_passe"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  },
  "XmlExport": {
    "OutputDirectory": "./exports",
    "BatchSize": 1000,
    "FileNamePrefix": "ARTICLE_COSMETIQUE"
  },
  "Export": {
    "TestMode": false
  }
}
```

### **2. Structure de base de données attendue**

**📄 Table requise :** `JSON_IN` (base Middleware SQL Server)

```sql
-- Table JSON_IN dans la base Middleware
-- Colonnes utilisées :
JSON_KEYU    (INT)         -- ID unique
JSON_DATA    (NVARCHAR)    -- Données JSON de l'article
JSON_HASH    (NVARCHAR)    -- Hash de contrôle
JSON_FROM    (NVARCHAR)    -- Source API
JSON_BKEY    (NVARCHAR)    -- Clé business (format: ART_XXXXXX)
JSON_CRDA    (DATETIME2)   -- Date de création
JSON_STAT    (NVARCHAR)    -- Statut ('ACTIVE')
JSON_CCLI    (NVARCHAR)    -- Code client ('BR')
JSON_TRTP    (INT)         -- Type de traitement (0=à exporter, 1=exporté)
JSON_TRDA    (DATETIME2)   -- Date de traitement
JSON_TREN    (NVARCHAR)    -- Entité de traitement ('SPEED')
```

## 🚀 Installation et Lancement

### **Prérequis**

- **.NET 6.0 SDK** installé
- **SQL Server** en fonctionnement
- **Base de données Middleware** avec table `JSON_IN` peuplée
- **Accès** en lecture/écriture à la base

### **1. Installation des dépendances**

```bash
cd DynamicsToXmlTranslator
dotnet restore
```

### **2. Configuration**

```bash
# Créer le fichier de configuration
cp appsettings.json.example appsettings.json

# Éditer la configuration avec vos paramètres SQL Server
nano appsettings.json
```

### **3. Compilation**

```bash
# Compilation simple
dotnet build --configuration Release

# Ou publication complète (recommandé)
dotnet publish --configuration Release --self-contained true --runtime win-x64 --output ./publish --property:PublishSingleFile=true
```

### **4. Lancement automatique**

```bash
# Mode production (nouveaux articles uniquement)
dotnet run
# ou
DynamicsToXmlTranslator.exe

# Mode test (tous les articles, sans marquage)
dotnet run test
# ou
DynamicsToXmlTranslator.exe test
```

## 📋 Fonctionnalités

### **Mode d'exécution automatisé :**

Le programme n'a plus de menu interactif. Il s'exécute directement selon le mode spécifié :

| **Mode** | **Commande** | **Description** |
|----------|-------------|----------------|
| **Production** | `DynamicsToXmlTranslator.exe` | Export des nouveaux articles uniquement |
| **Test** | `DynamicsToXmlTranslator.exe test` | Export de TOUS les articles sans marquage |

### **1. Mode Production (par défaut)**

- Exporte uniquement les **nouveaux articles** (`JSON_TRTP = 0`)
- **Marque automatiquement** les articles comme exportés (`JSON_TRTP = 1`)
- Génère des fichiers `ARTICLE_COSMETIQUE_YYYYMMDD_HHMMSS.XML`
- **Usage :** Production quotidienne, automatisation

### **2. Mode Test**

- Exporte **TOUS** les articles de la base
- **N'effectue AUCUN marquage** (réexécutable à volonté)
- Génère des fichiers `ARTICLE_TEST_COMPLET_YYYYMMDD_HHMMSS.XML`
- **Usage :** Tests, validation, débogage

### **3. Export par lots automatique**

- Si plus de 1000 articles : division automatique en plusieurs fichiers
- Format : `ARTICLE_COSMETIQUE_LOT001_YYYYMMDD_HHMMSS.XML`
- Marquage par lot pour traçabilité

### **4. Logs et traçabilité**

- Logs détaillés dans `logs/translator.log`
- Table de logs d'export : `xml_export_logs`
- Codes de retour pour automatisation

## 🔧 Architecture du Code

### **Program.cs - Point d'entrée automatisé**

**📄 Fonctions principales :**

```csharp
static async Task Main(string[] args)
{
    // Configuration automatique
    SetupConfiguration();
    SetupLogging();
    SetupServices();

    // Détection du mode (production/test)
    bool isTestMode = IsTestMode(args);
    
    // Exécution automatique
    if (isTestMode)
        await ExportAllArticlesTestMode();    // Tous articles, pas de marquage
    else
        await ExportNewArticlesOnly();        // Nouveaux articles, avec marquage
}
```

### **Models/Article.cs - Modèle adapté SQL Server**

**📄 Classes mises à jour :**

```csharp
public class Article
{
    public int Id { get; set; }                    // JSON_KEYU
    public string JsonData { get; set; }           // JSON_DATA
    public string ContentHash { get; set; }        // JSON_HASH
    public string? ApiEndpoint { get; set; }       // JSON_FROM
    public string? ItemId { get; set; }            // Extrait de JSON_BKEY
    public DateTime FirstSeenAt { get; set; }      // JSON_CRDA
    public DateTime LastUpdatedAt { get; set; }    // JSON_CRDA
    public DynamicsArticle? DynamicsData { get; set; }
}

public class DynamicsArticle
{
    // Nouveaux champs de l'API Dynamics mise à jour
    public string? ItemId { get; set; }
    public string? Name { get; set; }              // Remplace ItemName
    public string? Category { get; set; }          // Nouveau champ
    public string? ExternalItemId { get; set; }    // Nouveau champ
    public decimal GrossWeight { get; set; }
    public decimal Weight { get; set; }
    public string? itemBarCode { get; set; }       // Remplace BarcodeNumber
    public string? UnitId { get; set; }            // Remplace SalesUnitSymbol
    public string? ProductLifecycleStateId { get; set; }
    public string? ProducVersionAttribute { get; set; }
    public int FactorColli { get; set; }           // Nouveau champ
    public int FactorPallet { get; set; }          // Nouveau champ
    public int PdsShelfLife { get; set; }          // Remplace ShelfLifePeriodDays
    public int TrackingLot1 { get; set; }          // Nouveau champ
    public int TrackingLot2 { get; set; }          // Nouveau champ
    public int TrackingProoftag { get; set; }      // Nouveau champ
    public int TrackingDLCDDLUO { get; set; }      // Nouveau champ
    public string? OrigCountryRegionId { get; set; }
    public int HMIMIndicator { get; set; }
    public string? dataAreaId { get; set; }
    // + dimensions brutes/nettes
}
```

### **Services/DatabaseService.cs - Accès SQL Server**

**📄 Méthodes principales :**

```csharp
public class DatabaseService
{
    // CORRIGÉ : Requêtes sur table JSON_IN
    public async Task<List<Article>> GetAllArticlesAsync()           // Tous les articles
    public async Task<List<Article>> GetNonExportedArticlesAsync()   // JSON_TRTP = 0
    public async Task<List<Article>> GetArticlesSinceDateAsync()     // Depuis une date
    public async Task MarkArticlesAsExportedAsync()                  // JSON_TRTP = 1
    public async Task LogExportAsync()                               // Table xml_export_logs
}
```

### **Services/XmlExportService.cs - Export optimisé**

**📄 Nouvelles fonctionnalités :**

```csharp
public class XmlExportService
{
    // Export en un seul fichier avec marquage optionnel
    public async Task<string?> ExportToXmlAsync(List<WinDevArticle> articles, List<int>? originalIds)
    
    // Export par lots pour gros volumes
    public async Task<List<string>> ExportInBatchesAsync(List<WinDevArticle> articles, List<int>? originalIds, int batchSize)
    
    // Génération de fichier de test
    public async Task<string?> GenerateTestXmlAsync()
}
```

### **Mappers/ArticleMapper.cs - Règles métier**

**📄 Mapping selon Excel de correspondance :**

```csharp
public class ArticleMapper
{
    public WinDevArticle? MapToWinDev(Article article)
    {
        // Transformation selon le document Excel fourni
        // Application des règles métier (RG1 à RG21)
        // Gestion des valeurs par défaut
        // Transformation des catégories
    }
}
```

## 📊 Format XML Généré

### **Structure du fichier de sortie :**

```xml
<?xml version="1.0" encoding="ISO-8859-1"?>
<WINDEV_TABLE>
    <Table>
        <ACT_CODE>COSMETIQUE</ACT_CODE>
        <ART_CCLI>BR</ART_CCLI>
        <ART_CODE>BRSHSEBO500</ART_CODE>
        <ART_CODC>SHSEBO500</ART_CODC>
        <ART_DESL>SHAMPOING SEBORREGULATRICE 500ML</ART_DESL>
        <ART_ALPHA2>ML</ART_ALPHA2>
        <ART_EANU>3401360016484</ART_EANU>
        <ART_ALPHA17>PF</ART_ALPHA17>
        <ART_ALPHA3>FR</ART_ALPHA3>
        <ART_STAT>3</ART_STAT>
        <ART_POIU>500.000</ART_POIU>
        <!-- + tous les autres champs selon Excel -->
    </Table>
    <!-- Répétition pour chaque article -->
</WINDEV_TABLE>
```

### **Correspondance des champs (selon Excel) :**

| **Dynamics 365 (API)**   | **WINDEV XML** | **Règle** | **Description** |
|--------------------------|---------------|-----------|-----------------|
| `dataAreaId`             | `ART_CCLI`    | Direct    | Code client/activité |
| `"BR" + ItemId`          | `ART_CODE`    | Concat    | Code article complet |
| `ItemId`                 | `ART_CODC`    | Direct    | Code article source |
| `Name`                   | `ART_DESL`    | Direct    | Désignation article |
| `UnitId`                 | `ART_ALPHA2`  | RG11      | UNITE si vide |
| `itemBarCode`            | `ART_EANU`    | Direct    | Code-barres EAN |
| `Category`               | `ART_ALPHA17` | Transform | PF/Machine/Echantillon/Autres |
| `OrigCountryRegionId`    | `ART_ALPHA3`  | Direct    | Pays d'origine |
| `ProductLifecycleStateId`| `ART_STAT`    | RG21      | "Non"="2", autres="3" |
| `GrossWeight`            | `ART_POIU`    | Direct    | Poids brut |
| `FactorColli`            | `ART_QTEC`    | Direct    | Facteur colis |
| `FactorPallet`           | `ART_QTEP`    | RG8       | 0 si pas géré |
| `PdsShelfLife`           | `ART_NUM19`   | RG10      | 1620 par défaut |

## 🔄 Automatisation et Intégration

### **1. Lancement via programme externe**

```csharp
// Exemple C# pour appeler le traducteur
ProcessStartInfo startInfo = new ProcessStartInfo
{
    FileName = @"C:\Path\To\DynamicsToXmlTranslator.exe",
    Arguments = "", // Mode production
    UseShellExecute = false,
    CreateNoWindow = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true
};

using (Process process = Process.Start(startInfo))
{
    process.WaitForExit();
    int exitCode = process.ExitCode; // 0 = succès, autre = erreur
}
```

### **2. Tâche planifiée Windows**

```bash
# Créer une tâche qui s'exécute toutes les heures
schtasks /create /tn "Export Dynamics XML" /tr "C:\Path\To\DynamicsToXmlTranslator.exe" /sc hourly
```

### **3. Script PowerShell d'automatisation**

```powershell
# Script de lancement avec gestion d'erreurs
$ExePath = "C:\Path\To\DynamicsToXmlTranslator.exe"
$Process = Start-Process -FilePath $ExePath -Wait -PassThru -NoNewWindow

if ($Process.ExitCode -eq 0) {
    Write-Host "✅ Export réussi"
    # Traitement des fichiers XML générés...
} else {
    Write-Host "❌ Erreur lors de l'export"
    # Gestion des erreurs...
}
```

## 🐛 Résolution de Problèmes

### **Erreur de connexion SQL Server**

```bash
# Vérifier le service SQL Server
services.msc

# Tester la connexion
sqlcmd -S localhost\SQLEXPRESS -U utilisateur -P motdepasse
```

### **Table JSON_IN inexistante ou vide**

```sql
-- Vérifier l'existence de la table
SELECT COUNT(*) FROM dbo.JSON_IN WHERE JSON_STAT = 'ACTIVE' AND JSON_CCLI = 'BR';

-- Vérifier les nouveaux articles
SELECT COUNT(*) FROM dbo.JSON_IN WHERE JSON_TRTP = 0 OR JSON_TRTP IS NULL;
```

### **Aucun nouvel article à exporter**

- Normal si tous les articles ont déjà été exportés
- Utiliser le mode test pour vérifier : `DynamicsToXmlTranslator.exe test`
- Vérifier les logs pour comprendre le filtrage

### **Fichiers XML non générés**

- Vérifier les permissions d'écriture du dossier `exports/`
- Consulter `logs/translator.log` pour les détails
- Vérifier l'espace disque disponible

### **Erreur de mapping des articles**

- Activer le mode test pour diagnostic
- Vérifier la structure JSON dans `JSON_DATA`
- Consulter les logs pour les articles en erreur

## 📈 Monitoring et Maintenance

### **Surveillance via requêtes SQL**

```sql
-- Articles en attente d'export
SELECT COUNT(*) as articles_en_attente
FROM dbo.JSON_IN
WHERE (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
AND JSON_STAT = 'ACTIVE' AND JSON_CCLI = 'BR';

-- Derniers exports
SELECT TOP 10 * FROM xml_export_logs ORDER BY export_date DESC;

-- Articles exportés aujourd'hui
SELECT COUNT(*) as exportes_aujourd_hui
FROM dbo.JSON_IN
WHERE CAST(JSON_TRDA AS DATE) = CAST(GETDATE() AS DATE);
```

### **Nettoyage automatique**

```powershell
# Supprimer les XML de plus de 30 jours
Get-ChildItem -Path ".\exports" -Filter "*.XML" | 
Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-30)} | 
Remove-Item -Force
```

### **Script de surveillance**

```bash
#!/bin/bash
# Script de surveillance pour Linux/WSL

LOG_FILE="/path/to/logs/translator.log"
ERROR_COUNT=$(grep -c "ERROR" "$LOG_FILE" | tail -100)

if [ $ERROR_COUNT -gt 0 ]; then
    echo "⚠️ $ERROR_COUNT erreurs détectées dans les logs"
    # Envoyer alerte email/notification
fi
```

## 🔄 Développement et Extension

### **Ajouter un nouveau mode d'export**

**1. Dans Program.cs :**

```csharp
// Ajouter dans IsTestMode()
if (arg == "custom") {
    return true;
}

// Ajouter la méthode
private static async Task ExportCustomMode() {
    // Logique spécifique
}
```

### **Modifier les règles de transformation**

**Fichier :** `Mappers/ArticleMapper.cs`

```csharp
// Modifier la méthode TransformCategory()
private string TransformCategory(string? category)
{
    // Ajouter de nouvelles règles de transformation
}
```

### **Ajouter des champs WINDEV**

**1. Dans Models/WinDevArticle.cs :**

```csharp
[XmlElement("NOUVEAU_CHAMP")]
public string NouveauChamp { get; set; } = "";
```

**2. Dans Mappers/ArticleMapper.cs :**

```csharp
ArtNouveauChamp = dynamics.NouvelleProprieté ?? "valeur_par_defaut",
```

## 📋 Codes de Retour

| **Code** | **Signification** |
|----------|------------------|
| `0` | Export réussi |
| `1` | Erreur générale |
| `2` | Erreur fatale |

## 📞 Support Technique

### **Fichiers de diagnostic :**

- **Logs applicatifs :** `logs/translator.log`
- **Logs d'export :** Table `xml_export_logs`
- **Configuration :** `appsettings.json`

### **Commandes de diagnostic :**

```bash
# Test rapide
DynamicsToXmlTranslator.exe test

# Vérifier les logs
tail -f logs/translator.log

# Statistiques SQL
SELECT 
    JSON_STAT as statut,
    JSON_TRTP as traitement,
    COUNT(*) as nb_articles
FROM dbo.JSON_IN 
GROUP BY JSON_STAT, JSON_TRTP;
```

---

**Version :** 2.0 - Mode automatisé avec SQL Server  
**Dernière mise à jour :** 2025  
**Compatibilité :** .NET 8.0, SQL Server 2016+