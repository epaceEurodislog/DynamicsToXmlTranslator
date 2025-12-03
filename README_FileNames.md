# 📁 Guide de Configuration des Noms de Fichiers

## 🎯 Objectif

Ce document explique comment modifier les noms de fichiers générés par l'application **sans toucher au code** des services.

## 📝 Fichier de Configuration

**Fichier :** `FileNameConstants.cs`

Ce fichier centralise **TOUS** les noms de fichiers générés par l'application.

## 🔧 Comment Modifier un Nom de Fichier ?

### Exemple : Changer le préfixe des articles

**Avant :**
```csharp
public const string ARTICLE_PREFIX = "ARTICLE_COSMETIQUE";
```

**Après :**
```csharp
public const string ARTICLE_PREFIX = "ARTICLE_BEAUTY";
```

**Résultat :** Tous les fichiers d'articles seront maintenant nommés `ARTICLE_BEAUTY_YYYYMMDD_HHMMSS.XML`

---

## 📋 Liste des Constantes Disponibles

### Articles
| Constante | Valeur par défaut | Utilisation |
|-----------|------------------|-------------|
| `ARTICLE_PREFIX` | `ARTICLE_COSMETIQUE` | Fichiers XML simples d'articles |
| `ARTICLE_BATCH_PREFIX` | `ARTICLE_COSMETIQUE_LOT` | Fichiers XML d'articles par lot |

### Purchase Orders (Commandes d'achat)
| Constante | Valeur par défaut | Utilisation |
|-----------|------------------|-------------|
| `PURCHASE_ORDER_PREFIX` | `RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT` | Fichiers XML simples |
| `PURCHASE_ORDER_BATCH_PREFIX` | `RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_LOT` | Fichiers XML par lot |
| `PURCHASE_ORDER_TEST_EMPTY` | `RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_TEST_VIDE` | Fichiers de test vides |

### Return Orders (Commandes de retour)
| Constante | Valeur par défaut | Utilisation |
|-----------|------------------|-------------|
| `RETURN_ORDER_PREFIX` | `RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT` | Fichiers XML simples |
| `RETURN_ORDER_BATCH_PREFIX` | `RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT_LOT` | Fichiers XML par lot |
| `RETURN_ORDER_TEST_EMPTY` | `RECAT_COSMETIQUE_RETURN_ORDERS_API-IT-RCT_TEST_VIDE` | Fichiers de test vides |

### Transfer Orders (Ordres de transfert)
| Constante | Valeur par défaut | Utilisation |
|-----------|------------------|-------------|
| `TRANSFER_ORDER_PREFIX` | `RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT` | Fichiers XML simples |
| `TRANSFER_ORDER_BATCH_PREFIX` | `RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT_LOT` | Fichiers XML par lot |
| `TRANSFER_ORDER_TEST_EMPTY` | `RECAT_COSMETIQUE_TRANSFER_ORDERS_API-IT-RCT_TEST_VIDE` | Fichiers de test vides |

### Packing Slips (Bordereaux d'expédition)
| Constante | Valeur par défaut | Utilisation |
|-----------|------------------|-------------|
| `PACKING_SLIP_HEADER_PREFIX` | `CDEN_COSMETIQUE_API-IT-RCT` | Fichiers TXT d'en-têtes |
| `PACKING_SLIP_LINES_PREFIX` | `CDLG_COSMETIQUE_API-IT-RCT` | Fichiers TXT de lignes |

### Formats et Extensions
| Constante | Valeur par défaut | Utilisation |
|-----------|------------------|-------------|
| `DATE_FORMAT` | `yyyyMMdd` | Format de date dans les noms de fichiers |
| `TIME_FORMAT` | `HHmmss` | Format d'heure dans les noms de fichiers |
| `TIMESTAMP_FORMAT` | `yyyyMMdd_HHmmss` | Format timestamp complet |
| `XML_EXTENSION` | `.XML` | Extension des fichiers XML |
| `TXT_EXTENSION` | `.TXT` | Extension des fichiers TXT |

---

## 🔄 Workflow de Modification

1. **Ouvrir** `FileNameConstants.cs`
2. **Modifier** la valeur de la constante souhaitée
3. **Sauvegarder** le fichier
4. **Recompiler** l'application avec `dotnet build`
5. **Exécuter** l'application - les nouveaux noms seront automatiquement appliqués

---

## ⚠️ Points d'Attention

- **Ne pas supprimer** les constantes existantes (cela cassera le code)
- **Recompiler** après chaque modification
- **Tester** avec un petit jeu de données après modification
- Les formats de date/heure suivent les conventions .NET (`yyyyMMdd_HHmmss`)

---

## 🔍 Fichiers Utilisant ces Constantes

Les constantes sont utilisées dans les fichiers suivants :
- `Services/XmlExportService.cs` (Articles)
- `Services/PurchaseOrderXmlExportService.cs` (Purchase Orders)
- `Services/ReturnOrderXmlExportService.cs` (Return Orders)
- `Services/TransferOrderXmlExportService.cs` (Transfer Orders)
- `Services/PackingSlipTxtExportService.cs` (Packing Slips)
- `Program.cs` (Appels principaux)

---

## 📚 Exemples de Noms de Fichiers Générés

### Articles
- Simple : `ARTICLE_COSMETIQUE_20251203_143052.XML`
- Par lot : `ARTICLE_COSMETIQUE_LOT001_20251203_143052.XML`

### Purchase Orders
- Simple : `RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_20251203_143052.XML`
- Par lot : `RECAT_COSMETIQUE_PURCHASE_ORDERS_API-IT-RCT_LOT001_20251203_143052.XML`

### Packing Slips
- En-têtes : `CDEN_COSMETIQUE_API-IT-RCT_20251203_143052.TXT`
- Lignes : `CDLG_COSMETIQUE_API-IT-RCT_20251203_143052.TXT`

---

**Date de création :** 3 décembre 2025  
**Version :** 1.0
