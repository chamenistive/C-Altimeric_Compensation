#!/usr/bin/env python3
"""
Convertisseur Excel vers CSV pour le système C#
"""
import pandas as pd
import sys

def convert_excel_to_csv(excel_file, csv_file):
    try:
        # Lecture du fichier Excel
        df = pd.read_excel(excel_file)
        
        # Affichage de la structure
        print(f"📊 Fichier Excel lu: {excel_file}")
        print(f"   Colonnes: {list(df.columns)}")
        print(f"   Lignes: {len(df)}")
        print("\n📋 Aperçu des données:")
        print(df.head())
        
        # Sauvegarde en CSV
        df.to_csv(csv_file, index=False)
        print(f"\n✅ Conversion réussie: {csv_file}")
        
    except Exception as e:
        print(f"❌ Erreur: {e}")

if __name__ == "__main__":
    if len(sys.argv) != 3:
        print("Usage: python3 excel_to_csv.py fichier.xlsx sortie.csv")
        sys.exit(1)
    
    convert_excel_to_csv(sys.argv[1], sys.argv[2])