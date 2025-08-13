# Description
Conversion du système Python de compensation altimétrique vers C#/Mono.

## Architecture
- **Core** : Modèles et services de base
- **Data** : Import/Export de données Excel/CSV
- **Calculations** : Algorithmes de compensation par moindres carrés
- **Console** : Interface en ligne de commande
- **GUI** : Interface graphique (future)

## Prérequis
- Mono 6.0+ (installé)
- Ubuntu 20.04+
- Git

## Compilation et Exécution
```bash
# Compilation
./scripts/build.sh

# Exécution
mono bin/CompensationAltimetrique.exe
