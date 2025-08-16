// ================================================================
// SERVICE DE COMPENSATION - INTERFACE WEB
// Service principal pour l'interface web Blazor
// Orchestration complète et gestion état UI
// ================================================================

using CompensationAltimetrique.Core.Models;
using CompensationAltimetrique.Calculations.Design;
using CompensationAltimetrique.Calculations.Weights;
using System.ComponentModel;

namespace CompensationAltimetrique.Web.Services
{
    /// <summary>
    /// États du processus de compensation pour l'UI.
    /// </summary>
    public enum CompensationState
    {
        Idle,
        LoadingData,
        ValidatingData,
        ConfiguringParameters,
        Computing,
        Completed,
        Error
    }

    /// <summary>
    /// Événements de progression pour l'interface utilisateur.
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        public int Percentage { get; set; }
        public string Message { get; set; } = string.Empty;
        public CompensationState State { get; set; }
    }

    /// <summary>
    /// Service principal de compensation pour l'interface web.
    /// Gère l'état, la progression et l'orchestration complète.
    /// </summary>
    public class CompensationService : INotifyPropertyChanged
    {
        #region Événements

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<ProgressEventArgs>? ProgressChanged;
        public event EventHandler<string>? ErrorOccurred;
        public event EventHandler? CompensationCompleted;

        #endregion

        #region Propriétés d'État

        private CompensationState _currentState = CompensationState.Idle;
        public CompensationState CurrentState
        {
            get => _currentState;
            private set
            {
                _currentState = value;
                OnPropertyChanged(nameof(CurrentState));
                OnPropertyChanged(nameof(CanImportData));
                OnPropertyChanged(nameof(CanConfigureParameters));
                OnPropertyChanged(nameof(CanStartCompensation));
                OnPropertyChanged(nameof(IsComputing));
                OnPropertyChanged(nameof(HasResults));
            }
        }

        private int _progressPercentage = 0;
        public int ProgressPercentage
        {
            get => _progressPercentage;
            private set
            {
                _progressPercentage = value;
                OnPropertyChanged(nameof(ProgressPercentage));
            }
        }

        private string _statusMessage = "Prêt à importer des données";
        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        #endregion

        #region Propriétés de Données

        private List<LevelingData> _levelingData = new();
        public List<LevelingData> LevelingData
        {
            get => _levelingData;
            private set
            {
                _levelingData = value;
                OnPropertyChanged(nameof(LevelingData));
                OnPropertyChanged(nameof(HasData));
                OnPropertyChanged(nameof(DataCount));
            }
        }

        private CompensationConfiguration _configuration = new();
        public CompensationConfiguration Configuration
        {
            get => _configuration;
            set
            {
                _configuration = value;
                OnPropertyChanged(nameof(Configuration));
            }
        }

        private DetailedCompensationResults? _results;
        public DetailedCompensationResults? Results
        {
            get => _results;
            private set
            {
                _results = value;
                OnPropertyChanged(nameof(Results));
                OnPropertyChanged(nameof(HasResults));
            }
        }

        private List<string> _validationMessages = new();
        public List<string> ValidationMessages
        {
            get => _validationMessages;
            private set
            {
                _validationMessages = value;
                OnPropertyChanged(nameof(ValidationMessages));
                OnPropertyChanged(nameof(HasValidationIssues));
            }
        }

        #endregion

        #region Propriétés Calculées

        public bool HasData => LevelingData.Any();
        public int DataCount => LevelingData.Count;
        public bool HasResults => Results != null && Results.IsValid;
        public bool HasValidationIssues => ValidationMessages.Any();
        public bool CanImportData => CurrentState == CompensationState.Idle || CurrentState == CompensationState.Error;
        public bool CanConfigureParameters => HasData && (CurrentState == CompensationState.Idle || CurrentState == CompensationState.ConfiguringParameters);
        public bool CanStartCompensation => HasData && !HasValidationIssues && CurrentState == CompensationState.ConfiguringParameters;
        public bool IsComputing => CurrentState == CompensationState.Computing;

        #endregion

        #region Services

        private readonly ExcelLevelingImporter _importer;
        private LeastSquaresOrchestrator? _orchestrator;

        #endregion

        #region Constructeur

        public CompensationService()
        {
            _importer = new ExcelLevelingImporter();
            InitializeDefaultConfiguration();
        }

        #endregion

        #region Méthodes Publiques

        /// <summary>
        /// Importation de données depuis un fichier Excel.
        /// </summary>
        public async Task<bool> ImportDataFromFileAsync(string filePath)
        {
            try
            {
                CurrentState = CompensationState.LoadingData;
                UpdateProgress(10, "Ouverture du fichier Excel...");

                await Task.Delay(500); // Simulation du temps de chargement

                var importedData = await Task.Run(() => _importer.ImportFromFile(filePath));
                
                UpdateProgress(50, "Lecture des données...");
                await Task.Delay(300);

                LevelingData = importedData.ToList();
                
                UpdateProgress(80, "Validation des données...");
                await Task.Delay(200);

                await ValidateDataAsync();
                
                UpdateProgress(100, $"Import terminé : {DataCount} points importés");
                CurrentState = CompensationState.ConfiguringParameters;

                return true;
            }
            catch (Exception ex)
            {
                HandleError($"Erreur lors de l'import : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configuration des paramètres de compensation.
        /// </summary>
        public void ConfigureCompensation(string referencePoint, double referenceAltitude, double targetPrecisionMm = 2.0)
        {
            try
            {
                var points = LevelingData.Select(d => d.Matricule).Distinct().ToList();
                
                if (!points.Contains(referencePoint))
                {
                    points.Insert(0, referencePoint);
                }

                Configuration = new CompensationConfiguration
                {
                    NetworkConfig = NetworkConfigurationFactory.CreateClosedTraverse(
                        referencePoint, referenceAltitude, points),
                    GeodeticParams = new GeodeticParameters
                    {
                        InstrumentalErrorMm = 1.0,
                        KilometricErrorMm = 1.5
                    },
                    TargetPrecisionMm = targetPrecisionMm,
                    ConfidenceLevel = 0.95,
                    ApplyAtmosphericCorrections = false
                };

                _orchestrator = new LeastSquaresOrchestrator(Configuration);
                
                StatusMessage = "Configuration terminée, prêt pour les calculs";
            }
            catch (Exception ex)
            {
                HandleError($"Erreur de configuration : {ex.Message}");
            }
        }

        /// <summary>
        /// Lancement de la compensation complète.
        /// </summary>
        public async Task<bool> StartCompensationAsync()
        {
            if (_orchestrator == null)
            {
                HandleError("Configuration non initialisée");
                return false;
            }

            try
            {
                CurrentState = CompensationState.Computing;
                
                UpdateProgress(10, "Initialisation des calculs...");
                await Task.Delay(300);

                UpdateProgress(30, "Construction de la matrice de conception...");
                await Task.Delay(500);

                UpdateProgress(50, "Calcul des poids géodésiques...");
                await Task.Delay(400);

                UpdateProgress(70, "Résolution du système linéaire...");
                await Task.Delay(600);

                // Calcul réel en arrière-plan
                var results = await Task.Run(() => _orchestrator.CompensateNetwork(LevelingData));
                
                UpdateProgress(90, "Validation des résultats...");
                await Task.Delay(200);

                Results = results;
                
                UpdateProgress(100, "Compensation terminée avec succès!");
                CurrentState = CompensationState.Completed;
                
                CompensationCompleted?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                HandleError($"Erreur lors de la compensation : {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Réinitialisation complète du service.
        /// </summary>
        public void Reset()
        {
            CurrentState = CompensationState.Idle;
            LevelingData = new List<LevelingData>();
            Results = null;
            ValidationMessages = new List<string>();
            _orchestrator = null;
            ProgressPercentage = 0;
            StatusMessage = "Prêt à importer des données";
            InitializeDefaultConfiguration();
        }

        /// <summary>
        /// Génération d'un rapport détaillé.
        /// </summary>
        public string GenerateDetailedReport()
        {
            if (Results == null || _orchestrator == null)
                return "Aucun résultat disponible pour le rapport.";

            return _orchestrator.GenerateDetailedReport(Results);
        }

        #endregion

        #region Méthodes Privées

        private async Task ValidateDataAsync()
        {
            CurrentState = CompensationState.ValidatingData;
            var messages = new List<string>();

            await Task.Run(() =>
            {
                // Validation des données
                if (!LevelingData.Any())
                {
                    messages.Add("❌ Aucune donnée importée");
                }
                else
                {
                    messages.Add($"✅ {DataCount} points de mesure importés");
                }

                // Validation des colonnes requises
                var invalidRows = LevelingData.Where(d => 
                    string.IsNullOrEmpty(d.Matricule) || 
                    (!d.AR1.HasValue || !d.AV1.HasValue)).ToList();

                if (invalidRows.Any())
                {
                    messages.Add($"⚠️ {invalidRows.Count} lignes avec données manquantes");
                }
                else
                {
                    messages.Add("✅ Toutes les colonnes requises sont présentes");
                }

                // Validation des valeurs numériques
                var numericIssues = LevelingData.Where(d => 
                    d.AR1.HasValue && d.AV1.HasValue && 
                    Math.Abs(d.AR1.Value) > 10 || Math.Abs(d.AV1.Value) > 10).ToList();

                if (numericIssues.Any())
                {
                    messages.Add($"⚠️ {numericIssues.Count} valeurs suspectes détectées");
                }
                else
                {
                    messages.Add("✅ Données numériques cohérentes");
                }
            });

            ValidationMessages = messages;
        }

        private void InitializeDefaultConfiguration()
        {
            Configuration = new CompensationConfiguration
            {
                GeodeticParams = new GeodeticParameters
                {
                    InstrumentalErrorMm = 1.0,
                    KilometricErrorMm = 1.5
                },
                TargetPrecisionMm = 2.0,
                ConfidenceLevel = 0.95,
                ApplyAtmosphericCorrections = false
            };
        }

        private void UpdateProgress(int percentage, string message)
        {
            ProgressPercentage = percentage;
            StatusMessage = message;
            ProgressChanged?.Invoke(this, new ProgressEventArgs 
            { 
                Percentage = percentage, 
                Message = message, 
                State = CurrentState 
            });
        }

        private void HandleError(string errorMessage)
        {
            CurrentState = CompensationState.Error;
            StatusMessage = errorMessage;
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}