using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Errors;
using Matrix.CityCore.Domain.Scenarios.ClassicCity.Events.Cities;
using Matrix.CityCore.Domain.Simulation;

namespace Matrix.CityCore.Domain.Scenarios.ClassicCity.Cities
{
    /// <summary>
    ///     City aggregate root. Owns the city's lifecycle and metadata.
    ///     Simulation/time is owned by a separate aggregate (SimulationClock) linked by CityId.
    /// </summary>
    public sealed class City : AggregateRoot<CityId>
    {
        public const int PopulationBootstrapFailureCodeMaxLength = 128;
        public const int EconomyBootstrapFailureCodeMaxLength = 128;

        private City(
            CityId id,
            CityName name,
            SimulationKind simulationKind,
            CityEnvironment environment,
            CityGenerationSeed generationSeed,
            CityGenerationProfile generationProfile,
            CityInitialWeatherProfile initialWeatherProfile,
            Guid? provisioningCorrelationId,
            CityStatus status,
            DateTimeOffset createdAtUtc,
            Guid populationBootstrapOperationId,
            Guid economyBootstrapOperationId,
            DateTimeOffset? populationBootstrapCompletedAtUtc,
            DateTimeOffset? economyBootstrapCompletedAtUtc,
            DateTimeOffset? populationBootstrapFailedAtUtc,
            DateTimeOffset? economyBootstrapFailedAtUtc,
            string? populationBootstrapFailureCode,
            string? economyBootstrapFailureCode,
            DateTimeOffset? archivedAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);
            EnsureUtc(populationBootstrapCompletedAtUtc);
            EnsureUtc(economyBootstrapCompletedAtUtc);
            EnsureUtc(populationBootstrapFailedAtUtc);
            EnsureUtc(economyBootstrapFailedAtUtc);
            EnsureUtc(archivedAtUtc);
            GuardHelper.AgainstEmptyGuid(
                id: populationBootstrapOperationId,
                propertyName: nameof(populationBootstrapOperationId));
            GuardHelper.AgainstEmptyGuid(
                id: economyBootstrapOperationId,
                propertyName: nameof(economyBootstrapOperationId));

            Name = name;
            SimulationKind = GuardHelper.AgainstInvalidEnum(
                value: simulationKind,
                propertyName: nameof(simulationKind));
            Environment = environment;
            GenerationSeed = generationSeed;
            GenerationProfile = generationProfile;
            InitialWeatherProfile = initialWeatherProfile;
            ProvisioningCorrelationId = provisioningCorrelationId;
            Status = status;
            CreatedAtUtc = createdAtUtc;
            PopulationBootstrapOperationId = populationBootstrapOperationId;
            EconomyBootstrapOperationId = economyBootstrapOperationId;
            PopulationBootstrapCompletedAtUtc = populationBootstrapCompletedAtUtc;
            EconomyBootstrapCompletedAtUtc = economyBootstrapCompletedAtUtc;
            PopulationBootstrapFailedAtUtc = populationBootstrapFailedAtUtc;
            EconomyBootstrapFailedAtUtc = economyBootstrapFailedAtUtc;
            PopulationBootstrapFailureCode = populationBootstrapFailureCode;
            EconomyBootstrapFailureCode = economyBootstrapFailureCode;
            ArchivedAtUtc = archivedAtUtc;
        }

        private City()
            : base(default(CityId))
        {
            Name = default(CityName);
            SimulationKind = SimulationKind.ClassicCity;
            Environment = null!;
            GenerationSeed = default(CityGenerationSeed);
            GenerationProfile = null!;
            InitialWeatherProfile = null!;
        }

        public CityName Name { get; private set; }
        public SimulationKind SimulationKind { get; }
        public CityEnvironment Environment { get; private set; }
        public CityGenerationSeed GenerationSeed { get; }
        public CityGenerationProfile GenerationProfile { get; }
        public CityInitialWeatherProfile InitialWeatherProfile { get; }
        public Guid? ProvisioningCorrelationId { get; }
        public CityStatus Status { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }
        public Guid PopulationBootstrapOperationId { get; private set; }
        public Guid EconomyBootstrapOperationId { get; private set; }
        public DateTimeOffset? PopulationBootstrapCompletedAtUtc { get; private set; }
        public DateTimeOffset? EconomyBootstrapCompletedAtUtc { get; private set; }
        public DateTimeOffset? PopulationBootstrapFailedAtUtc { get; private set; }
        public DateTimeOffset? EconomyBootstrapFailedAtUtc { get; private set; }
        public string? PopulationBootstrapFailureCode { get; private set; }
        public string? EconomyBootstrapFailureCode { get; private set; }
        public DateTimeOffset? ArchivedAtUtc { get; private set; }

        public bool IsActive => Status == CityStatus.Active;
        public bool IsArchived => Status == CityStatus.Archived;
        public bool IsProvisioning => Status == CityStatus.Provisioning;

        public bool HasPopulationBootstrapFailure
            => Status == CityStatus.ProvisioningFailed && PopulationBootstrapFailedAtUtc.HasValue;

        public bool HasEconomyBootstrapFailure
            => Status == CityStatus.ProvisioningFailed && EconomyBootstrapFailedAtUtc.HasValue;

        public static City Create(
            CityName name,
            SimulationKind simulationKind,
            CityEnvironment environment,
            CityGenerationSeed generationSeed,
            CityGenerationProfile generationProfile,
            CityInitialWeatherProfile initialWeatherProfile,
            Guid? provisioningCorrelationId,
            bool requiresPopulationBootstrap,
            bool requiresEconomyBootstrap,
            DateTimeOffset createdAtUtc)
        {
            EnsureUtc(createdAtUtc);

            if (environment is null)
                throw ClassicCityDomainErrorsFactory.InvalidCityEnvironment(
                    reason: "City environment is required.",
                    propertyName: nameof(environment));

            if (generationSeed.Value is null)
                throw ClassicCityDomainErrorsFactory.CityGenerationSeedNullOrEmpty(
                    propertyName: nameof(generationSeed));

            if (generationProfile is null)
                throw ClassicCityDomainErrorsFactory.InvalidCityGenerationProfile(
                    reason: "City generation profile is required.",
                    propertyName: nameof(generationProfile));

            if (initialWeatherProfile is null)
                throw ClassicCityDomainErrorsFactory.InvalidCityEnvironment(
                    reason: "City initial weather profile is required.",
                    propertyName: nameof(initialWeatherProfile));

            var city = new City(
                id: CityId.New(),
                name: name,
                simulationKind: simulationKind,
                environment: environment,
                generationSeed: generationSeed,
                generationProfile: generationProfile,
                initialWeatherProfile: initialWeatherProfile,
                provisioningCorrelationId: provisioningCorrelationId,
                status: requiresPopulationBootstrap || requiresEconomyBootstrap
                    ? CityStatus.Provisioning
                    : CityStatus.Active,
                createdAtUtc: createdAtUtc,
                populationBootstrapOperationId: Guid.NewGuid(),
                economyBootstrapOperationId: Guid.NewGuid(),
                populationBootstrapCompletedAtUtc: requiresPopulationBootstrap
                    ? null
                    : createdAtUtc,
                economyBootstrapCompletedAtUtc: requiresEconomyBootstrap
                    ? null
                    : createdAtUtc,
                populationBootstrapFailedAtUtc: null,
                economyBootstrapFailedAtUtc: null,
                populationBootstrapFailureCode: null,
                economyBootstrapFailureCode: null,
                archivedAtUtc: null);

            city.AddDomainEvent(
                new CityCreatedDomainEvent(
                    CityId: city.Id,
                    Name: city.Name,
                    SimulationKind: city.SimulationKind,
                    Environment: city.Environment,
                    GenerationSeed: city.GenerationSeed,
                    GenerationProfile: city.GenerationProfile,
                    PopulationBootstrapOperationId: city.PopulationBootstrapOperationId,
                    CreatedAtUtc: city.CreatedAtUtc));

            return city;
        }

        public void Rename(CityName newName)
        {
            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (newName.Equals(Name))
                return;

            CityName from = Name;
            Name = newName;

            AddDomainEvent(
                new CityRenamedDomainEvent(
                    CityId: Id,
                    From: from,
                    To: newName));
        }

        public void ChangeEnvironment(CityEnvironment newEnvironment)
        {
            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (newEnvironment is null)
                throw ClassicCityDomainErrorsFactory.InvalidCityEnvironment(
                    reason: "City environment is required.",
                    propertyName: nameof(newEnvironment));

            if (newEnvironment == Environment)
                return;

            CityEnvironment previousEnvironment = Environment;
            Environment = newEnvironment;

            AddDomainEvent(
                new CityEnvironmentChangedDomainEvent(
                    CityId: Id,
                    From: previousEnvironment,
                    To: newEnvironment));
        }

        public bool TryCompletePopulationBootstrap(
            Guid operationId,
            DateTimeOffset completedAtUtc)
        {
            EnsureUtc(completedAtUtc);
            GuardHelper.AgainstEmptyGuid(
                id: operationId,
                propertyName: nameof(operationId));

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (operationId != PopulationBootstrapOperationId)
                return false;

            if (Status == CityStatus.ProvisioningFailed)
                return false;

            if (HasPopulationBootstrapFailure)
                return false;

            if (IsActive)
                return true;

            PopulationBootstrapCompletedAtUtc = completedAtUtc;
            PopulationBootstrapFailedAtUtc = null;
            PopulationBootstrapFailureCode = null;
            TryActivate();

            AddDomainEvent(
                new CityPopulationBootstrapCompletedDomainEvent(
                    CityId: Id,
                    OperationId: operationId,
                    CompletedAtUtc: completedAtUtc));

            return true;
        }

        public bool TryCompleteEconomyBootstrap(
            Guid operationId,
            DateTimeOffset completedAtUtc)
        {
            EnsureUtc(completedAtUtc);
            GuardHelper.AgainstEmptyGuid(
                id: operationId,
                propertyName: nameof(operationId));

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (operationId != EconomyBootstrapOperationId)
                return false;

            if (Status == CityStatus.ProvisioningFailed)
                return false;

            if (HasEconomyBootstrapFailure)
                return false;

            if (EconomyBootstrapCompletedAtUtc.HasValue)
                return true;

            EconomyBootstrapCompletedAtUtc = completedAtUtc;
            EconomyBootstrapFailedAtUtc = null;
            EconomyBootstrapFailureCode = null;
            TryActivate();

            return true;
        }

        public bool TryFailPopulationBootstrap(
            Guid operationId,
            string failureCode,
            DateTimeOffset failedAtUtc)
        {
            EnsureUtc(failedAtUtc);
            GuardHelper.AgainstEmptyGuid(
                id: operationId,
                propertyName: nameof(operationId));

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (operationId != PopulationBootstrapOperationId)
                return false;

            if (Status == CityStatus.ProvisioningFailed && !HasPopulationBootstrapFailure)
                return false;

            if (IsActive)
                return false;

            if (HasPopulationBootstrapFailure)
                return true;

            string normalizedFailureCode = NormalizePopulationBootstrapFailureCode(failureCode);

            Status = CityStatus.ProvisioningFailed;
            PopulationBootstrapCompletedAtUtc = null;
            PopulationBootstrapFailedAtUtc = failedAtUtc;
            PopulationBootstrapFailureCode = normalizedFailureCode;
            EconomyBootstrapFailedAtUtc = null;
            EconomyBootstrapFailureCode = null;

            AddDomainEvent(
                new CityPopulationBootstrapFailedDomainEvent(
                    CityId: Id,
                    OperationId: operationId,
                    FailureCode: normalizedFailureCode,
                    FailedAtUtc: failedAtUtc));

            return true;
        }

        public bool TryFailEconomyBootstrap(
            Guid operationId,
            string failureCode,
            DateTimeOffset failedAtUtc)
        {
            EnsureUtc(failedAtUtc);
            GuardHelper.AgainstEmptyGuid(
                id: operationId,
                propertyName: nameof(operationId));

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (operationId != EconomyBootstrapOperationId)
                return false;

            if (Status == CityStatus.ProvisioningFailed && !HasEconomyBootstrapFailure)
                return false;

            if (IsActive)
                return false;

            if (HasEconomyBootstrapFailure)
                return true;

            string normalizedFailureCode = NormalizeFailureCode(
                failureCode: failureCode,
                maxLength: EconomyBootstrapFailureCodeMaxLength,
                propertyName: nameof(EconomyBootstrapFailureCode));

            Status = CityStatus.ProvisioningFailed;
            EconomyBootstrapCompletedAtUtc = null;
            EconomyBootstrapFailedAtUtc = failedAtUtc;
            EconomyBootstrapFailureCode = normalizedFailureCode;
            PopulationBootstrapFailedAtUtc = null;
            PopulationBootstrapFailureCode = null;

            return true;
        }

        public bool TryRestartPopulationBootstrap(
            DateTimeOffset restartedAtUtc,
            out Guid populationOperationId,
            out Guid economyOperationId)
        {
            EnsureUtc(restartedAtUtc);

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: ClassicCityDomainErrorsFactory.CityIsArchived);

            if (Status != CityStatus.ProvisioningFailed)
            {
                populationOperationId = PopulationBootstrapOperationId;
                economyOperationId = EconomyBootstrapOperationId;
                return false;
            }

            Guid previousOperationId = PopulationBootstrapOperationId;
            populationOperationId = Guid.NewGuid();
            economyOperationId = Guid.NewGuid();

            PopulationBootstrapOperationId = populationOperationId;
            EconomyBootstrapOperationId = economyOperationId;
            Status = CityStatus.Provisioning;
            PopulationBootstrapCompletedAtUtc = null;
            EconomyBootstrapCompletedAtUtc = null;
            PopulationBootstrapFailedAtUtc = null;
            EconomyBootstrapFailedAtUtc = null;
            PopulationBootstrapFailureCode = null;
            EconomyBootstrapFailureCode = null;

            AddDomainEvent(
                new CityPopulationBootstrapRestartedDomainEvent(
                    CityId: Id,
                    PreviousOperationId: previousOperationId,
                    OperationId: populationOperationId,
                    RestartedAtUtc: restartedAtUtc));

            return true;
        }

        public void Archive(DateTimeOffset archivedAtUtc)
        {
            EnsureUtc(archivedAtUtc);

            if (IsArchived)
                return;

            Status = CityStatus.Archived;
            ArchivedAtUtc = archivedAtUtc;

            AddDomainEvent(
                new CityArchivedDomainEvent(
                    CityId: Id,
                    ArchivedAtUtc: archivedAtUtc));
        }

        private void TryActivate()
        {
            if (PopulationBootstrapCompletedAtUtc.HasValue &&
                EconomyBootstrapCompletedAtUtc.HasValue)
                Status = CityStatus.Active;
        }

        private static string NormalizePopulationBootstrapFailureCode(string failureCode)
        {
            return NormalizeFailureCode(
                failureCode: failureCode,
                maxLength: PopulationBootstrapFailureCodeMaxLength,
                propertyName: nameof(PopulationBootstrapFailureCode));
        }

        private static string NormalizeFailureCode(
            string failureCode,
            int maxLength,
            string propertyName)
        {
            string normalizedFailureCode = GuardHelper.AgainstNullOrWhiteSpace(
                    value: failureCode,
                    errorFactory: ClassicCityDomainErrorsFactory.CityPopulationBootstrapFailureCodeNullOrEmpty)
               .ToUpperInvariant();

            if (normalizedFailureCode.Length > maxLength)
                throw ClassicCityDomainErrorsFactory.CityPopulationBootstrapFailureCodeTooLong(
                    value: normalizedFailureCode,
                    max: maxLength,
                    propertyName: propertyName);

            bool isValid = normalizedFailureCode.All(symbol =>
                char.IsAsciiLetterOrDigit(symbol) || symbol == '_');

            if (!isValid)
                throw ClassicCityDomainErrorsFactory.CityPopulationBootstrapFailureCodeInvalid(
                    value: normalizedFailureCode,
                    propertyName: propertyName);

            return normalizedFailureCode;
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: ClassicCityDomainErrorsFactory.CityTimestampMustBeUtc);
        }

        private static void EnsureUtc(DateTimeOffset? value)
        {
            if (value.HasValue)
                EnsureUtc(value.Value);
        }
    }
}
