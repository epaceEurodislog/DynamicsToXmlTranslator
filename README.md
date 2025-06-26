# 🔄 DynamicsToXmlTranslator

## 📄 Description

**DynamicsToXmlTranslator** est un outil de traduction de données Dynamics 365 vers des fichiers XML compatibles WINDEV. Il extrait les articles depuis une base de données MySQL et les convertit au format XML.

## 🏗️ Architecture Technique

### **Technologies utilisées :**

- **.NET 6.0** (Console Application)
- **C#** pour la logique métier
- **MySQL** pour le stockage de données
- **XML Serialization** pour la génération des fichiers
- **Serilog** pour les logs
- **Microsoft.Extensions** pour la configuration

### **Fichiers de code principaux :**

**📁 Emplacement des fichiers :**

```
DynamicsToXmlTranslator/
├── Program.cs                    # ← Point d'entrée avec menu interactif
├── Models/
│   ├── Article.cs               # ← Modèle des articles Dynamics
│   └── WinDevArticle.cs         # ← Structure XML WINDEV
├── Mappers/
│   └── ArticleMapper.cs         # ← Logique de transformation
├── Services/
│   ├── DatabaseService.cs       # ← Accès base de données MySQL
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
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=dynamics_articles_db;Uid=root;Pwd=VOTRE_MDP;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Export": {
    "OutputDirectory": "./exports",
    "XmlFileName": "articles_windev_{date}.xml"
  }
}
```

### **2. Structure de base de données attendue**

**📄 Table requise :** `articles_raw`

```sql
CREATE TABLE articles_raw (
  id INT PRIMARY KEY AUTO_INCREMENT,
  json_data JSON NOT NULL,
  content_hash VARCHAR(255) NOT NULL,
  api_endpoint VARCHAR(255) DEFAULT 'BRINT34ReleasedProducts',
  item_id VARCHAR(50) GENERATED ALWAYS AS (JSON_UNQUOTE(JSON_EXTRACT(json_data, '$.ItemId'))) STORED,
  first_seen_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  last_updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  update_count INT DEFAULT 0
);
```

## 🚀 Installation et Lancement

### **Prérequis**

- **.NET 6.0 SDK** installé
- **MySQL/WAMP/XAMPP** en fonctionnement
- **Base de données** `dynamics_articles_db` créée
- **Table** `articles_raw` avec données articles

### **1. Installation des dépendances**

```bash
cd DynamicsToXmlTranslator
dotnet restore
```

### **2. Configuration**

```bash
# Copier le template de configuration
cp appsettings.json.example appsettings.json

# Éditer la configuration avec vos paramètres
nano appsettings.json
```

### **3. Compilation**

```bash
dotnet build
```

### **4. Lancement**

```bash
dotnet run
```

**Ou en mode Release :**

```bash
dotnet build --configuration Release
cd bin/Release/net6.0/
./DynamicsToXmlTranslator.exe
```

## 📋 Fonctionnalités

### **Menu interactif disponible :**

```
--- Menu Principal ---
1. Exporter tous les articles
2. Exporter les articles modifiés aujourd'hui
3. Exporter les articles modifiés depuis une date
4. Générer un fichier XML de test
5. Afficher les statistiques
0. Quitter
```

### **1. Export complet (Option 1)**

- Exporte **tous** les articles de la base
- Génère un fichier XML avec tous les articles disponibles
- **Usage :** Premier export ou synchronisation complète

### **2. Export incrémental journalier (Option 2)**

- Exporte uniquement les articles **modifiés aujourd'hui**
- Basé sur le champ `last_updated_at`
- **Usage :** Synchronisation quotidienne automatisée

### **3. Export depuis une date (Option 3)**

- Demande une date de début
- Exporte les articles modifiés depuis cette date
- **Usage :** Rattrapage après une panne ou période spécifique

### **4. Fichier de test (Option 4)**

- Génère un XML avec quelques articles échantillon
- **Usage :** Validation du formato et tests WINDEV

### **5. Statistiques (Option 5)**

- Affiche le nombre total d'articles
- Répartition par dates de modification
- **Usage :** Monitoring et contrôle qualité

## 🔧 Architecture du Code

### **Program.cs - Point d'entrée**

**📄 Fonctions principales :**

```csharp
static async Task Main(string[] args)
{
    // Configuration et initialisation des services
    SetupConfiguration();
    SetupLogging();
    SetupServices();

    // Menu interactif
    // Gestion des choix utilisateur
}

// Méthodes d'export principales
static async Task ExportAllArticles()
static async Task ExportTodayArticles()
static async Task ExportArticlesSinceDate()
static async Task GenerateTestXml()
```

### **Models/Article.cs - Modèle de données**

**📄 Classes principales :**

```csharp
public class Article
{
    public int Id { get; set; }
    public string JsonData { get; set; }           // JSON brut depuis l'API
    public string ContentHash { get; set; }        // Hash pour détecter modifications
    public string ItemId { get; set; }             // ID unique article
    public DateTime LastUpdatedAt { get; set; }    // Date de modification
    public DynamicsArticle DynamicsData { get; set; } // Objet désérialisé
}

public class DynamicsArticle
{
    public string ItemId { get; set; }             // Code article
    public string Name { get; set; }               // Nom article
    public string Category { get; set; }           // Catégorie
    public string itemBarCode { get; set; }        // Code-barres
    public string UnitId { get; set; }             // Unité
    public string dataAreaId { get; set; }         // Zone de données
    // + tous les autres champs Dynamics
}
```

### **Models/WinDevArticle.cs - Structure XML**

**📄 Mapping vers WINDEV :**

```csharp
[XmlRoot("WINDEV_TABLE")]
public class WinDevTable
{
    [XmlElement("Table")]
    public List<WinDevArticle> Articles { get; set; }
}

public class WinDevArticle
{
    [XmlElement("ACT_CODE")]
    public string ActCode { get; set; } = "COSMETIQUE";    // Fixe

    [XmlElement("ART_CCLI")]
    public string ArtCcli { get; set; }                    // dataAreaId

    [XmlElement("ART_CODE")]
    public string ArtCode { get; set; }                    // "BR" + ItemId

    [XmlElement("ART_DESL")]
    public string ArtDesl { get; set; }                    // Name

    [XmlElement("ART_EANU")]
    public string ArtEanu { get; set; }                    // itemBarCode

    // + tous les champs requis par WINDEV
}
```

### **Mappers/ArticleMapper.cs - Logique de transformation**

**📄 Fonctions de mapping :**

```csharp
public class ArticleMapper
{
    public WinDevArticle MapToWinDevArticle(Article article)
    {
        // Transformation des données Dynamics vers structure WINDEV
        // Application des règles métier spécifiques
        // Gestion des valeurs par défaut
    }

    public List<WinDevArticle> MapArticleList(List<Article> articles)
    {
        // Traitement en lot des articles
        // Gestion des erreurs de mapping
    }
}
```

### **Services/DatabaseService.cs - Accès données**

**📄 Méthodes d'accès aux données :**

```csharp
public class DatabaseService
{
    public async Task<List<Article>> GetAllArticlesAsync()
    public async Task<List<Article>> GetArticlesModifiedTodayAsync()
    public async Task<List<Article>> GetArticlesModifiedSinceAsync(DateTime since)
    public async Task<ArticleStatistics> GetStatisticsAsync()
    public async Task CreateTablesIfNotExistsAsync()
}
```

### **Services/XmlExportService.cs - Génération XML**

**📄 Méthodes d'export :**

```csharp
public class XmlExportService
{
    public async Task<string> ExportToXmlAsync(List<WinDevArticle> articles, string fileName)
    public async Task<bool> ValidateXmlStructure(string xmlPath)
    public string GenerateFileName(string prefix)
}
```

## 📊 Format XML Généré

### **Structure du fichier de sortie :**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<WINDEV_TABLE>
    <Table>
        <ACT_CODE>COSMETIQUE</ACT_CODE>
        <ART_CCLI>BRE</ART_CCLI>
        <ART_CODE>BRSHSEBO500</ART_CODE>
        <ART_CODC>SHSEBO500</ART_CODC>
        <ART_DESL>SHAMPOING SEBORREGULATRICE 500ML</ART_DESL>
        <ART_ALPHA2>ML</ART_ALPHA2>
        <ART_EANU>3401360016484</ART_EANU>
        <ART_ALPHA17>CAPILLAIRE</ART_ALPHA17>
        <ART_ALPHA3>FR</ART_ALPHA3>
    </Table>
    <!-- Répétition pour chaque article -->
</WINDEV_TABLE>
```

### **Correspondance des champs :**

| **Dynamics 365**      | **WINDEV XML** | **Description**      |
| --------------------- | -------------- | -------------------- |
| `dataAreaId`          | `ART_CCLI`     | Code client/activité |
| `"BR" + ItemId`       | `ART_CODE`     | Code article complet |
| `ItemId`              | `ART_CODC`     | Code article source  |
| `Name`                | `ART_DESL`     | Désignation article  |
| `UnitId`              | `ART_ALPHA2`   | Unité de vente       |
| `itemBarCode`         | `ART_EANU`     | Code-barres EAN      |
| `Category`            | `ART_ALPHA17`  | Catégorie produit    |
| `OrigCountryRegionId` | `ART_ALPHA3`   | Pays d'origine       |

## 🐛 Résolution de Problèmes

### **Erreur de connexion MySQL**

```bash
# Vérifier que MySQL fonctionne
services.msc  # Vérifier service MySQL

# Tester la connexion
mysql -u root -p -h localhost -P 3306
```

### **Table articles_raw inexistante**

```sql
-- L'outil crée automatiquement les tables manquantes
-- Sinon, créer manuellement avec le script fourni
```

### **Fichier XML vide ou corrompu**

- Vérifier que des articles existent dans la base
- Consulter les logs dans le dossier `logs/`
- Vérifier les permissions d'écriture du dossier `exports/`

### **Erreur de format XML**

- Valider le XML généré avec un outil en ligne
- Vérifier l'encodage UTF-8
- Contrôler les caractères spéciaux dans les données

### **Performance lente**

```sql
-- Optimiser les requêtes avec des index
CREATE INDEX idx_last_updated ON articles_raw(last_updated_at);
CREATE INDEX idx_item_id ON articles_raw(item_id);
```

## 📈 Monitoring et Maintenance

### **Vérification du bon fonctionnement**

```sql
-- Nombre total d'articles
SELECT COUNT(*) as total_articles FROM articles_raw;

-- Articles modifiés aujourd'hui
SELECT COUNT(*) as modified_today
FROM articles_raw
WHERE DATE(last_updated_at) = CURDATE();

-- Dernière synchronisation
SELECT MAX(last_updated_at) as derniere_sync FROM articles_raw;
```

### **Nettoyage des anciens exports**

```bash
# Supprimer les XML de plus de 30 jours
find ./exports -name "*.xml" -mtime +30 -delete
```

### **Sauvegarde automatique**

```bash
# Script pour automatiser l'export quotidien
#!/bin/bash
cd /path/to/DynamicsToXmlTranslator
echo "2" | dotnet run  # Option 2 = export du jour
```

## 🔄 Développement et Extension

### **Ajouter un nouveau champ WINDEV**

**1. Dans WinDevArticle.cs :**

```csharp
[XmlElement("NOUVEAU_CHAMP")]
public string NouveauChamp { get; set; } = "";
```

**2. Dans ArticleMapper.cs :**

```csharp
nouveauArticleWinDev.NouveauChamp = article.DynamicsData?.NouvelleProprieteDynamics ?? "DEFAUT";
```

### **Modifier la logique de transformation**

- **Fichier :** `Mappers/ArticleMapper.cs`
- Modifier la méthode `MapToWinDevArticle()`
- Ajouter des règles métier spécifiques

### **Ajouter de nouveaux types d'export**

- **Fichier :** `Program.cs`
- Ajouter une nouvelle option au menu
- Créer la méthode correspondante

## 📞 Support Technique

- **Logs :** Consultez le dossier `logs/` pour les détails d'erreur
- **Performance :** Surveiller la taille de la table `articles_raw`
- **Intégrité :** Vérifier régulièrement la cohérence des hash de contenu

---
