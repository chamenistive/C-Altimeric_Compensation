#!/bin/bash

echo "🔨 Compilation du projet Compensation Altimétrique..."

# Créer le répertoire bin s'il n'existe pas
mkdir -p bin

# Compilation avec Mono
echo "📦 Compilation des modules..."
# Trouver tous les fichiers .cs
find src -name "*.cs" > /tmp/cs_files.txt

if [ -s /tmp/cs_files.txt ]; then
    mcs -out:bin/CompensationAltimetrique.exe \
        -target:exe \
        -reference:System.Data.dll \
        -reference:System.Xml.dll \
        -reference:System.Core.dll \
        $(cat /tmp/cs_files.txt | tr '\n' ' ') \
        2>/dev/null
    rm /tmp/cs_files.txt
else
    echo "❌ Aucun fichier .cs trouvé"
    exit 1
fi

if [ $? -eq 0 ]; then
    echo "✅ Compilation réussie !"
    echo "💡 Exécution : mono bin/CompensationAltimetrique.exe"
    echo "📊 Taille : $(du -h bin/CompensationAltimetrique.exe | cut -f1)"
else
    echo "❌ Erreur de compilation - Vérifiez les fichiers source"
    echo "💡 Astuce : Les fichiers .cs doivent être créés dans les modules"
fi