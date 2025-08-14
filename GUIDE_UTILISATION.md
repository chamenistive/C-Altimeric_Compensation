# 🚀 Guide d'Utilisation Rapide

## Utilisation avec vos données

### 1. Préparer vos données
Convertissez votre fichier Excel en CSV avec le format :Matricule,AR 1,AV 1,DIST 1,AR 2,AV 2,DIST 2
AM2,1.0038,,,1.01262,,
1,0.97702,1.99728,20.234,0.98953,2.00614,20.234
### 2. Lancer la compensation
```bash
# Mode automatique
mono bin/CompensationAltimetrique.exe votre_fichier.csv

# Mode interactif (pour tester)
mono bin/CompensationAltimetrique.exe
