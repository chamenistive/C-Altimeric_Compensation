# 🧮 Système de Compensation Altimétrique C#

## 📋 Description

Système professionnel de **compensation altimétrique par moindres carrés** développé en C# avec Mono sur Ubuntu. Conversion du système Python original avec une précision garantie de **2 millimètres**.

## 🎯 Caractéristiques

- ✅ **Précision 2mm garantie** selon normes géodésiques
- 🔄 **Compensation par moindres carrés** avec validation statistique
- 📊 **Import Excel/CSV** avec validation automatique
- 🏗️ **Architecture modulaire** extensible
- 🐧 **Compatible Ubuntu/Linux** avec Mono
- 📝 **Pipeline complet** automatisé

## 🚀 Installation

### Prérequis
- Ubuntu 20.04+
- Mono 6.0+ (`sudo apt install mono-complete`)
- Git

### Installation
```bash
# Cloner le repository
git clone https://github.com/chamenistive/C-Altimeric_Compensation.git
cd C-Altimeric_Compensation

# Compilation
./scripts/build.sh

# Exécution
mono bin/CompensationAltimetrique.exe

