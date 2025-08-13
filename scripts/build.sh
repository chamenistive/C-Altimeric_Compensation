#!/bin/bash

echo "🔨 Compilation du projet Compensation Altimétrique..."

# Créer le répertoire bin s'il n'existe pas
mkdir -p bin

# Compilation avec Mono
echo "📦 Compilation des modules..."
mcs -out:bin/CompensationAltimetrique.exe \
    -target:exe \
    -reference:System.Data.dll \
    -reference:System.Xml.dll \
    -reference:System.Core.dll \
    src/CompensationAltimetrique.Console/*.cs \
    src/CompensationAltimetrique.Core/**/*.cs \
    src/CompensationAltimetrique.Data/**/*.cs \
    src/CompensationAltimetrique.Calculations/**/*.cs \
    2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ Compilation réussie !"
    echo "💡 Exécution : mono bin/CompensationAltimetrique.exe"
    echo "📊 Taille : $(du -h bin/CompensationAltimetrique.exe | cut -f1)"
else
    echo "❌ Erreur de compilation - Vérifiez les fichiers source"
    echo "💡 Astuce : Les fichiers .cs doivent être créés dans les modules"
fi