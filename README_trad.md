# 🔄 DynamicsToXmlTranslator

## 📄 Description

**DynamicsToXmlTranslator** est un outil de traduction automatisé qui convertit les données Dynamics 365 en fichiers XML/TXT compatibles avec les systèmes WINDEV/SPEED. Il supporte l'export de 5 types d'entités depuis une base de données SQL Server.

### ✨ Fonctionnalités principales

- **Articles** → XML WINDEV (avec exclusion ART_STAT=3)
- **Purchase Orders** → XML CF_ATTENDUS_COSMETIQUE
- **Return Orders** → XML CF_ATTENDUS_COSMETIQUE
- **Transfer Orders** → XML CF_ATTENDUS_COSMETIQUE
- **Packing Slips** → 2 fichiers TXT SPEED (CDEN + CDLG)
- **Traitement UTF-8** automatique de tous les caractères spéciaux
- **Mode Test** et **Mode Production** avec marquage automatique
- **Export par lots** pour gros volumes
- **Logs complets** et traçabilité

## 🏗️ Architecture Technique

### **Technologies utilisées**

- **.NET 8.0** (Console Application)
- **C#** avec services d'injection de dépendances
- **SQL Server** comme source de données (table JSON_IN)
- **Serilog** pour les logs rotatifs
- **Traitement UTF-8** avancé avec Utf8TextProcessor

### **Structure du projet**

```
DynamicsToXmlTranslator/
├── Program.cs                           # Point d'entrée automatisé
├── Models/
│   ├── Article.cs                       # Modèle Articles Dynamics
│   ├── WinDevArticle.cs                 # Structure XML Articles WINDEV
│   ├── PurchaseOrder.cs                 # Modèle Purchase Orders
│   ├── WinDevPurchaseOrder.cs           # Structure XML Purchase Orders
│   ├── ReturnOrder.cs                   # Modèle Return Orders
│   ├── WinDevReturnOrder.cs             # Structure XML Return Orders
│   ├── TransferOrder.cs                 # Modèle Transfer Orders
│   ├── WinDevTransferOrder.cs           # Structure XML Transfer Orders
│   ├── PackingSlip.cs                   # Modèle Packing Slips
│   └── SpeedPackingSlip.cs              # Structures TXT SPEED
├── Services/
│   ├── DatabaseService.cs               # Accès BDD Articles
│   ├── XmlExportService.cs              # Export XML Articles
│   ├── PurchaseOrderDatabaseService.cs  # Accès BDD Purchase Orders
│   ├── PurchaseOrderXmlExportService.cs # Export XML Purchase Orders
│   ├── ReturnOrderDatabaseService.cs    # Accès BDD Return Orders
│   ├── ReturnOrderXmlExportService.cs   # Export XML Return Orders
│   ├── TransferOrderDatabaseService.cs  # Accès BDD Transfer Orders
│   ├── TransferOrderXmlExportService.cs # Export XML Transfer Orders
│   ├── PackingSlipDatabaseService.cs    # Accès BDD Packing Slips
│   ├── PackingSlipTxtExportService.cs   # Export TXT Packing Slips
│   └── Utf8TextProcessor.cs             # Traitement UTF-8 avancé
├── Mappers/
│   ├── ArticleMapper.cs                 # Mapping Articles + exclusions ART_STAT=3
│   ├── PurchaseOrderMapper.cs           # Mapping Purchase Orders
│   ├── ReturnOrderMapper.cs             # Mapping Return Orders
│   ├── TransferOrderMapper.cs           # Mapping Transfer Orders
│   └── PackingSlipMapper.cs             # Mapping Packing Slips + RG métier
├── DynamicsToXmlTranslator.csproj       # Configuration .NET 8.0
├── appsettings.json.example             # Template configuration
├── appsettings.json                     # Configuration (à créer)
├── build.bat                            # Script de compilation Windows
└── README.md                            # Cette documentation
```

## ⚙️ Configuration

### **1. Prérequis système**

- **.NET 8.0 SDK** installé
- **SQL Server** accessible (version 2016+)
- **Base Middleware** avec table JSON_IN peuplée
- **Permissions** lecture/écriture sur la base et dossier exports

### **2. Configuration appsettings.json**

Créer le fichier `appsettings.json` à la racine :

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

### **3. Structure base de données requise**

**Table principale :** `JSON_IN` (base Middleware SQL Server)

```sql
-- Colonnes utilisées par le traducteur
JSON_KEYU    (INT)         -- ID unique (PK)
JSON_DATA    (NVARCHAR)    -- Données JSON de l'entité
JSON_HASH    (NVARCHAR)    -- Hash de contrôle
JSON_FROM    (NVARCHAR)    -- Endpoint source API
JSON_BKEY    (NVARCHAR)    -- Clé métier
JSON_CRDA    (DATETIME2)   -- Date création
JSON_STAT    (NVARCHAR)    -- Statut ('ACTIVE', 'DELETED')
JSON_CCLI    (NVARCHAR)    -- Code client ('BR')
JSON_TRTP    (INT)         -- Traitement (0=à exporter, 1=exporté)
JSON_TRDA    (DATETIME2)   -- Date traitement
JSON_TREN    (NVARCHAR)    -- Entité traitement ('SPEED', 'SPEED_PO', etc.)
JSON_SENT    (INT)         -- Envoyé (0/1)
```

**Endpoints API reconnus :**

- `data/BRINT34ReleasedProducts` → Articles
- `data/BRINT32PurchOrderTables` → Purchase Orders
- `data/BRINT32ReturnOrderTables` → Return Orders
- `data/BRINT32TransferOrderTables` → Transfer Orders
- `data/BRPackingSlipInterfaces` → Packing Slips

## 🚀 Installation et Utilisation

### **1. Installation**

```bash
# Cloner le projet
git clone <repository-url>
cd DynamicsToXmlTranslator

# Restaurer les dépendances
dotnet restore

# Créer la configuration
cp appsettings.json.example appsettings.json
# Éditer appsettings.json avec vos paramètres
```

### **2. Compilation**

```bash
# Compilation simple
dotnet build --configuration Release

# Publication complète (recommandé)
dotnet publish --configuration Release --self-contained true --runtime win-x64 --output ./publish --property:PublishSingleFile=true

# Ou utiliser le script Windows
build.bat
```

### **3. Modes d'exécution**

Le programme s'exécute automatiquement selon les arguments fournis :

#### **Mode Production (par défaut)**

```bash
# Tous les nouveaux éléments
DynamicsToXmlTranslator.exe

# Articles uniquement
DynamicsToXmlTranslator.exe articles

# Purchase Orders uniquement
DynamicsToXmlTranslator.exe purchaseorders
# ou
DynamicsToXmlTranslator.exe po

# Return Orders uniquement
DynamicsToXmlTranslator.exe returnorders
# ou
DynamicsToXmlTranslator.exe ro

# Transfer Orders uniquement
DynamicsToXmlTranslator.exe transferorders
# ou
DynamicsToXmlTranslator.exe to

# Packing Slips uniquement
DynamicsToXmlTranslator.exe packingslips
# ou
DynamicsToXmlTranslator.exe ps
```

#### **Mode Test**

```bash
# Tous les éléments (SANS marquage)
DynamicsToXmlTranslator.exe test

# Articles en mode test
DynamicsToXmlTranslator.exe test articles

# Purchase Orders en mode test
DynamicsToXmlTranslator.exe test po

# etc.
```

### **4. Fichiers générés**

#### **Articles**

- **Fichiers :** `ARTICLE_COSMETIQUE_YYYYMMDD_HHMMSS.XML`
- **Format :** XML WINDEV avec balises `<WINDEV_TABLE>` et `<Table>`
- **Règle spéciale :** Articles avec ART_STAT=3 automatiquement exclus

#### **Purchase/Return/Transfer Orders**

- **Fichiers :** `RECAT_COSMETIQUE_[TYPE]_ORDERS_YYYYMMDD_HHMMSS.XML`
- **Format :** XML avec balises `<CF_ATTENDUS_COSMETIQUE>` et `<LIGNE>`

#### **Packing Slips**

- **Fichiers :** 2 fichiers TXT par export
  - `CDEN_COSMETIQUE_YYYYMMDD_HHMMSS.TXT` (en-têtes de commandes)
  - `CDLG_COSMETIQUE_YYYYMMDD_HHMMSS.TXT` (lignes d'articles)
- **Format :** TXT délimité par `|` selon format SPEED
- **Encodage :** ISO-8859-1

## 🔧 Fonctionnalités Avancées

### **1. Traitement UTF-8 automatique**

Le service `Utf8TextProcessor` traite automatiquement :

- **Caractères accentués** : à, é, è, ç, etc. → a, e, e, c, etc.
- **Caractères spéciaux** : &, €, °, ©, etc. → et, EUR, deg, (C), etc.
- **Guillemets typographiques** : " " ' ' → " " ' '
- **Espaces insécables** et caractères de contrôle
- **Règle spéciale** : & → "et" (pour noms d'entreprises)

### **2. Règles métier intégrées**

#### **Articles (ART_STAT)**

- **ART_STAT=2** : ProductLifecycleStateId="Non" → Exportés
- **ART_STAT=3** : Autres valeurs → **Exclus automatiquement**
- **RG11** : UnitId vide → "UNITE"
- **RG21** : Durée de vie par défaut → 1620 jours

#### **Packing Slips (Règles RG1-RG4)**

- **RG1** : BTB → Utilise PurchOrderFormNum
- **RG2** : BTC → Utilise BRPortalOrderNumber
- **RG3** : CarrierCode vide → "A AFFECTER"
- **RG4** : CarrierServiceCode → Séparé par @ dans ALPHA40/ALPHA41

### **3. Export par lots automatique**

Si > 1000 éléments :

- **Articles** : `ARTICLE_COSMETIQUE_LOT001_YYYYMMDD_HHMMSS.XML`
- **Orders** : `RECAT_COSMETIQUE_[TYPE]_LOT001_YYYYMMDD_HHMMSS.XML`
- **Packing Slips** : Paires de fichiers `CDEN_LOT001_xxx.TXT` + `CDLG_LOT001_xxx.TXT`

### **4. Statistiques et monitoring**

```bash
# Le programme affiche automatiquement :
📊 === STATISTIQUES ARTICLES ===
📋 Total articles : 1250
✅ Articles ART_STAT=2 (exportables) : 1100
🚫 Articles ART_STAT=3 (exclus) : 150
📈 Pourcentage d'exclusion : 12.0%
```

## 📊 Monitoring et Logs

### **1. Logs applicatifs**

- **Localisation :** `logs/translator.log`
- **Rotation :** Quotidienne (30 jours conservés)
- **Taille max :** 10 MB par fichier
- **Format :** `[YYYY-MM-DD HH:MM:SS] [LEVEL] Message`

### **2. Tables de logs SQL**

- `xml_export_logs` → Logs exports Articles
- `xml_purchase_export_logs` → Logs Purchase Orders
- `xml_return_export_logs` → Logs Return Orders
- `xml_transfer_export_logs` → Logs Transfer Orders
- `txt_packingslip_export_logs` → Logs Packing Slips

### **3. Requêtes de surveillance**

```sql
-- Articles en attente
SELECT COUNT(*) as articles_en_attente
FROM dbo.JSON_IN
WHERE (JSON_TRTP = 0 OR JSON_TRTP IS NULL)
AND JSON_STAT = 'ACTIVE' AND JSON_CCLI = 'BR'
AND JSON_FROM = 'data/BRINT34ReleasedProducts';

-- Derniers exports
SELECT TOP 10 * FROM xml_export_logs ORDER BY export_date DESC;

-- Statistiques par type
SELECT
    JSON_FROM as type_entite,
    JSON_STAT as statut,
    JSON_TRTP as traitement,
    COUNT(*) as nb_elements
FROM dbo.JSON_IN
WHERE JSON_CCLI = 'BR'
GROUP BY JSON_FROM, JSON_STAT, JSON_TRTP
ORDER BY JSON_FROM, JSON_TRTP;
```

## 🔧 Développement et Extension

### **1. Ajouter un nouveau type d'entité**

1. **Créer le modèle** dans `Models/NouveauType.cs`
2. **Créer le modèle WINDEV** dans `Models/WinDevNouveauType.cs`
3. **Créer le service base** dans `Services/NouveauTypeDatabaseService.cs`
4. **Créer le service export** dans `Services/NouveauTypeXmlExportService.cs`
5. **Créer le mapper** dans `Mappers/NouveauTypeMapper.cs`
6. **Modifier Program.cs** pour ajouter les nouvelles méthodes d'export

### **2. Modifier les règles de transformation UTF-8**

**Fichier :** `Services/Utf8TextProcessor.cs`

```csharp
// Dans la méthode InitializeCharacterMapping()
TryAdd(mapping, "nouveau_caractère", "remplacement");
```

### **3. Ajouter des champs WINDEV**

**1. Dans le modèle WINDEV :**

```csharp
[XmlElement("NOUVEAU_CHAMP")]
public string NouveauChamp { get; set; } = "";
```

**2. Dans le mapper :**

```csharp
NouveauChamp = _textProcessor.ProcessText(dynamics.NouvelleProprieteDynamics),
```

### **4. Modifier les règles métier**

**Fichier :** `Mappers/[Type]Mapper.cs`

```csharp
// Exemple : Ajouter une nouvelle règle d'exclusion
public bool ShouldExcludeElement(Element element)
{
    // Votre logique ici
    return false;
}
```

## 🚨 Dépannage

### **Erreurs courantes**

#### **❌ Erreur de connexion SQL**

```bash
# Vérifier le service
services.msc → SQL Server

# Tester la connexion
sqlcmd -S localhost\SQLEXPRESS -U utilisateur -P password
```

#### **❌ Aucun élément à exporter**

- Normal si tous déjà exportés
- Utiliser mode test : `DynamicsToXmlTranslator.exe test`
- Vérifier `JSON_TRTP` dans la base

#### **❌ Fichiers non générés**

- Vérifier permissions dossier `exports/`
- Consulter `logs/translator.log`
- Vérifier espace disque

#### **❌ Caractères bizarres dans XML**

- Le traitement UTF-8 est automatique
- Vérifier que l'entrée SQL est bien en UTF-8
- Consulter les logs pour les transformations appliquées

### **Codes de retour**

| **Code** | **Signification** |
| -------- | ----------------- |
| `0`      | Export réussi     |
| `1`      | Erreur générale   |
| `2`      | Erreur fatale     |

## 🔄 Automatisation

### **1. Tâche planifiée Windows**

```bash
schtasks /create /tn "Export Dynamics" /tr "C:\Path\To\DynamicsToXmlTranslator.exe" /sc hourly
```

### **2. Script PowerShell**

```powershell
$ExePath = "C:\Path\To\DynamicsToXmlTranslator.exe"
$Process = Start-Process -FilePath $ExePath -Wait -PassThru -NoNewWindow

if ($Process.ExitCode -eq 0) {
    Write-Host "✅ Export réussi"
    # Traitement des fichiers...
} else {
    Write-Host "❌ Erreur export"
    # Gestion erreurs...
}
```

### **3. Intégration application**

```csharp
// Exemple C# pour appeler le traducteur
var startInfo = new ProcessStartInfo
{
    FileName = @"C:\Path\To\DynamicsToXmlTranslator.exe",
    Arguments = "articles", // articles, po, ro, to, ps
    UseShellExecute = false,
    RedirectStandardOutput = true
};

using var process = Process.Start(startInfo);
process.WaitForExit();
int exitCode = process.ExitCode; // 0 = succès
```

## 📝 Changelog

### **Version 3.0** (Actuelle)

- ✅ Support 5 types d'entités (Articles, PO, RO, TO, PS)
- ✅ Traitement UTF-8 automatique complet
- ✅ Exclusion automatique Articles ART_STAT=3
- ✅ Export Packing Slips en 2 fichiers TXT SPEED
- ✅ Règles métier intégrées (RG1-RG21)
- ✅ Mode Test et Production
- ✅ Export par lots automatique
- ✅ .NET 8.0 et SQL Server

### **Version 2.0**

- ✅ Migration SQL Server
- ✅ Support Purchase/Return/Transfer Orders
- ✅ Traitement UTF-8 basique

### **Version 1.0**

- ✅ Export Articles uniquement
- ✅ MySQL/MariaDB

## 🆘 Support

### **Diagnostics**

```bash
# Test complet
DynamicsToXmlTranslator.exe test

# Vérifier logs
tail -f logs/translator.log

# Statistiques base
SELECT JSON_FROM, COUNT(*) FROM JSON_IN GROUP BY JSON_FROM;
```

### **Fichiers de diagnostic**

- **Configuration :** `appsettings.json`
- **Logs :** `logs/translator.log`
- **Exports :** Dossier `exports/`
- **Base :** Tables `*_export_logs`

---

**🔧 Version :** 3.0 - Multi-entités avec UTF-8 et règles métier  
**📅 Dernière MAJ :** 2025  
**⚙️ Compatibilité :** .NET 8.0, SQL Server 2016+, WINDEV/SPEED
