#!/bin/bash

# Créer une solution .NET
echo "📁 Création de la solution .NET..."

# Créer la solution principale
dotnet new sln -n CompensationAltimetrique

# Ajouter les projets à la solution
if [ -f "src/CompensationAltimetrique.Calculations/CompensationAltimetrique.Calculations.csproj" ]; then
    dotnet sln add src/CompensationAltimetrique.Calculations/CompensationAltimetrique.Calculations.csproj
    echo "✅ Projet Calculations ajouté"
fi

if [ -f "tests/CompensationAltimetrique.Tests/CompensationAltimetrique.Tests.csproj" ]; then
    dotnet sln add tests/CompensationAltimetrique.Tests/CompensationAltimetrique.Tests.csproj
    echo "✅ Projet Tests ajouté"
fi

echo "✅ Solution créée: CompensationAltimetrique.sln"
