namespace CompensationAltimetrique.Calculations.Validation
{
    /// <summary>
    /// Résumé de validation complet
    /// </summary>
    public class ValidationSummary
    {
        public ValidationTestResult Chi2Validation { get; set; } = new ValidationTestResult();
        public ValidationTestResult PrecisionValidation { get; set; } = new ValidationTestResult();
        public ValidationTestResult BlunderValidation { get; set; } = new ValidationTestResult();
        public ValidationTestResult GeodeticValidation { get; set; } = new ValidationTestResult();
        
        /// <summary>
        /// Validation globale réussie
        /// </summary>
        public bool IsFullyValid => Chi2Validation.IsValid && 
                                   PrecisionValidation.IsValid && 
                                   BlunderValidation.IsValid &&
                                   GeodeticValidation.IsValid;
        
        /// <summary>
        /// Résumé exécutif de validation
        /// </summary>
        public string GetExecutiveSummary()
        {
            var status = IsFullyValid ? "✅ CONFORME" : "⚠️ ATTENTION REQUISE";
            
            return $"""
            🎯 RÉSUMÉ EXÉCUTIF DE VALIDATION
            ===============================
            Statut global: {status}
            
            📊 Tests statistiques:
            - Test χ²: {(Chi2Validation.IsValid ? "✅ VALIDÉ" : "❌ ÉCHOUÉ")}
            - Précision: {(PrecisionValidation.IsValid ? "✅ CONFORME" : "❌ DÉPASSEMENT")}
            - Fautes grossières: {(BlunderValidation.IsValid ? "✅ AUCUNE" : "⚠️ DÉTECTÉES")}
            - Validation géodésique: {(GeodeticValidation.IsValid ? "✅ CONFORME" : "⚠️ ANOMALIES")}
            """;
        }
    }
    
    /// <summary>
    /// Résultat d'un test de validation
    /// </summary>
    public class ValidationTestResult
    {
        public bool IsValid { get; set; } = true;
        public double Value { get; set; }
        public double CriticalValue { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}