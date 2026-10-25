# KiloKilo — carnet d'approvisionnement au marché

Application mobile (.NET MAUI) + API (ASP.NET Core) pour enregistrer les achats au marché et suivre
la dépense et le prix payé par produit dans le temps.

```
KiloKilo.sln
├── KiloKilo.Shared/   Contrats (DTO) et calculs communs : prix par produit, statistiques, catégories
├── KiloKilo.Api/      API REST : ASP.NET Core (minimal APIs), EF Core + SQLite, JWT
└── KiloKilo.App/      Application .NET MAUI (Android, iOS, Windows) : MVVM, Shell, cache JSON hors ligne
```

## Prérequis
- SDK .NET 10 et charge de travail MAUI (`dotnet workload install maui`) ; Visual Studio 2022/2026 recommandé.
- Pour une autre version de .NET : changer `TargetFramework(s)` dans les trois `.csproj` et les versions des paquets NuGet.

## 1. Démarrer l'API
```
cd KiloKilo.Api
dotnet run
```
- Écoute sur `http://0.0.0.0:5080` (voir `appsettings.json` → `Urls`). Santé : `GET /health`.
- La base SQLite `kilokilo.db` est créée au premier lancement.
- En **Development** (profil `http` de `launchSettings.json`, utilisé par `dotnet run` et Visual Studio), des données de démonstration sont créées :
  compte **+237690000000 / PIN 1234**. La documentation OpenAPI est exposée sur `/openapi/v1.json`.
- **Production** : remplacer `Jwt:Key` (≥ 32 caractères, secret) par variable d'environnement ou secret, servir en HTTPS,
  brancher un fournisseur SMS/e-mail via `INotificationSender` (aujourd'hui le code de réinitialisation est seulement journalisé).

## 2. Lancer l'application
```
dotnet build KiloKilo.App -t:Run -f net10.0-android
dotnet build KiloKilo.App -t:Run -f net10.0-windows10.0.19041.0
```
Adresse du serveur (modifiable dans l'app : *Connexion → Adresse du serveur* ou *Profil → Paramètres*) :
- émulateur Android : `http://10.0.2.2:5080` (valeur par défaut) ;
- Windows / simulateur iOS : `http://localhost:5080` (valeur par défaut) ;
- téléphone réel : `http://<IP-de-votre-PC>:5080` (même réseau Wi-Fi, pare-feu autorisant le port 5080).

## Fonctionnement hors ligne
- Toutes les lectures/écritures passent par un **cache JSON** par utilisateur (`AppDataDirectory/cache/<id>/`) :
  `courses.json`, `marches.json`, `profil.json`, `outbox.json` (modifications en attente), `meta.json`.
- Chaque modification est ajoutée à la **file d'attente** puis envoyée en arrière-plan (au démarrage, à la reprise,
  au retour du réseau, après chaque modification, ou via « Synchroniser maintenant »).
- Les identifiants de courses sont générés par l'application : les envois (`PUT`) sont **idempotents**.
- Conflits : la dernière modification reçue par le serveur l'emporte. Les suppressions sont propagées (marqueurs).
- Les jetons sont dans le stockage sécurisé du système ; le PIN n'est jamais conservé, seulement une empreinte salée
  (PBKDF2) pour la connexion hors ligne.

## Principaux endpoints (`Authorization: Bearer <jeton>`)
| Méthode | Route | Rôle |
|---|---|---|
| POST | `/api/auth/register`, `/login`, `/refresh`, `/logout` | Comptes et sessions (jeton d'accès 60 min, rafraîchissement 60 j, rotation) |
| POST | `/api/auth/forgot`, `/reset` | Réinitialisation du PIN par code à 6 chiffres (10 min) |
| GET/PUT | `/api/profil` | Profil, budget, seuil d'alerte |
| GET/PUT/DELETE | `/api/marches[/{id}]` | Marchés suivis |
| GET | `/api/courses`, `/api/courses/sync?since=` | Liste complète / changements incrémentaux |
| PUT/DELETE | `/api/courses/{id}` | Création-modification idempotente / suppression logique |
| GET | `/api/stats?periode=Semaine\|Mois\|Année`, `/api/produits` | Statistiques et historique de prix |

Sécurité : PIN haché PBKDF2-SHA256 ; verrouillage 15 min après 5 échecs ; chaque requête est limitée aux données
de l'utilisateur connecté ; validation des entrées côté serveur.
