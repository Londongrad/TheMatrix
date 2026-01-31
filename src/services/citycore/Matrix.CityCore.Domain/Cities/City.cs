using Matrix.BuildingBlocks.Domain;
using Matrix.BuildingBlocks.Domain.Common;
using Matrix.CityCore.Domain.Errors;
using Matrix.CityCore.Domain.Events.Cities;

namespace Matrix.CityCore.Domain.Cities
{
    /// <summary>
    ///     City aggregate root. Owns the city's lifecycle and metadata.
    ///     Simulation/time is owned by a separate aggregate (SimulationClock) linked by CityId.
    /// </summary>
    public sealed class City : AggregateRoot<CityId>
    {
        public const int PopulationBootstrapFailureCodeMaxLength = 128;

        private City(
            CityId id,
            CityName name,
            CityEnvironment environment,
            CityGenerationSeed generationSeed,
            CityGenerationProfile generationProfile,
            CityStatus status,
            DateTimeOffset createdAtUtc,
            Guid populationBootstrapOperationId,
            DateTimeOffset? populationBootstrapCompletedAtUtc,
            DateTimeOffset? populationBootstrapFailedAtUtc,
            string? populationBootstrapFailureCode,
            DateTimeOffset? archivedAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);
            EnsureUtc(populationBootstrapCompletedAtUtc);
            EnsureUtc(populationBootstrapFailedAtUtc);
            EnsureUtc(archivedAtUtc);
            GuardHelper.AgainstEmptyGuid(
                id: populationBootstrapOperationId,
                propertyName: nameof(populationBootstrapOperationId));

            Name = name;
            Environment = environment;
            GenerationSeed = generationSeed;
            GenerationProfile = generationProfile;
            Status = status;
            CreatedAtUtc = createdAtUtc;
            PopulationBootstrapOperationId = populationBootstrapOperationId;
            PopulationBootstrapCompletedAtUtc = populationBootstrapCompletedAtUtc;
            PopulationBootstrapFailedAtUtc = populationBootstrapFailedAtUtc;
            PopulationBootstrapFailureCode = populationBootstrapFailureCode;
            ArchivedAtUtc = archivedAtUtc;
        }

        private City()
            : base(default(CityId))
        {
            Name = default(CityName);
            Environment = null!;
            GenerationSeed = default(CityGenerationSeed);
            GenerationProfile = null!;
        }

        public CityName Name { get; private set; }
        public CityEnvironment Environment { get; private set; }
        public CityGenerationSeed GenerationSeed { get; }
        public CityGenerationProfile GenerationProfile { get; }
        public CityStatus Status { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; }
        public Guid PopulationBootstrapOperationId { get; private set; }
        public DateTimeOffset? PopulationBootstrapCompletedAtUtc { get; private set; }
        public DateTimeOffset? PopulationBootstrapFailedAtUtc { get; private set; }
        public string? PopulationBootstrapFailureCode { get; private set; }
        public DateTimeOffset? ArchivedAtUtc { get; private set; }

        public bool IsActive => Status == CityStatus.Active;
        public bool IsArchived => Status == CityStatus.Archived;
        public bool IsProvisioning => Status == CityStatus.Provisioning;
        public bool HasPopulationBootstrapFailure => Status == CityStatus.ProvisioningFailed;

        public static City Create(
            CityName name,
            CityEnvironment environment,
            CityGenerationSeed generationSeed,
            CityGenerationProfile generationProfile,
            DateTimeOffset createdAtUtc)
        {
            EnsureUtc(createdAtUtc);

            if (environment is null)
                throw DomainErrorsFactory.InvalidCityEnvironment(
                    reason: "City environment is required.",
                    propertyName: nameof(environment));

            if (generationSeed == default(CityGenerationSeed?))
                throw DomainErrorsFactory.CityGenerationSeedNullOrEmpty(propertyName: nameof(generationSeed));

            if (generationProfile is null)
                throw DomainErrorsFactory.InvalidCityGenerationProfile(
                    reason: "City generation profile is required.",
                    propertyName: nameof(generationProfile));

            var city = new City(
                id: CityId.New(),
                name: name,
                environment: environment,
                generationSeed: generationSeed,
                generationProfile: generationProfile,
                status: CityStatus.Provisioning,
                createdAtUtc: createdAtUtc,
                populationBootstrapOperationId: Guid.NewGuid(),
                populationBootstrapCompletedAtUtc: null,
                populationBootstrapFailedAtUtc: null,
                populationBootstrapFailureCode: null,
                archivedAtUtc: null);

            city.AddDomainEvent(
                new CityCreatedDomainEvent(
                    CityId: city.Id,
                    Name: city.Name,
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
                errorFactory: DomainErrorsFactory.CityIsArchived);

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
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (newEnvironment is null)
                throw DomainErrorsFactory.InvalidCityEnvironment(
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
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (operationId != PopulationBootstrapOperationId)
                return false;

            if (HasPopulationBootstrapFailure)
                return false;

            if (IsActive)
                return true;

            Status = CityStatus.Active;
            PopulationBootstrapCompletedAtUtc = completedAtUtc;
            PopulationBootstrapFailedAtUtc = null;
            PopulationBootstrapFailureCode = null;

            AddDomainEvent(
                new CityPopulationBootstrapCompletedDomainEvent(
                    CityId: Id,
                    OperationId: operationId,
                    CompletedAtUtc: completedAtUtc));

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
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (operationId != PopulationBootstrapOperationId)
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

            AddDomainEvent(
                new CityPopulationBootstrapFailedDomainEvent(
                    CityId: Id,
                    OperationId: operationId,
                    FailureCode: normalizedFailureCode,
                    FailedAtUtc: failedAtUtc));

            return true;
        }

        public bool TryRestartPopulationBootstrap(
            DateTimeOffset restartedAtUtc,
            out Guid operationId)
        {
            EnsureUtc(restartedAtUtc);

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (!HasPopulationBootstrapFailure)
            {
                operationId = PopulationBootstrapOperationId;
                return false;
            }

            Guid previousOperationId = PopulationBootstrapOperationId;
            operationId = Guid.NewGuid();

            PopulationBootstrapOperationId = operationId;
            Status = CityStatus.Provisioning;
            PopulationBootstrapCompletedAtUtc = null;
            PopulationBootstrapFailedAtUtc = null;
            PopulationBootstrapFailureCode = null;

            AddDomainEvent(
                new CityPopulationBootstrapRestartedDomainEvent(
                    CityId: Id,
                    PreviousOperationId: previousOperationId,
                    OperationId: operationId,
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

        private static string NormalizePopulationBootstrapFailureCode(string failureCode)
        {
            string normalizedFailureCode = GuardHelper.AgainstNullOrWhiteSpace(
                    value: failureCode,
                    errorFactory: DomainErrorsFactory.CityPopulationBootstrapFailureCodeNullOrEmpty)
               .ToUpperInvariant();

            if (normalizedFailureCode.Length > PopulationBootstrapFailureCodeMaxLength)
                throw DomainErrorsFactory.CityPopulationBootstrapFailureCodeTooLong(
                    value: normalizedFailureCode,
                    max: PopulationBootstrapFailureCodeMaxLength,
                    propertyName: nameof(PopulationBootstrapFailureCode));

            bool isValid = normalizedFailureCode.All(symbol =>
                char.IsAsciiLetterOrDigit(symbol) || symbol == '_');

            if (!isValid)
                throw DomainErrorsFactory.CityPopulationBootstrapFailureCodeInvalid(
                    value: normalizedFailureCode,
                    propertyName: nameof(PopulationBootstrapFailureCode));

            return normalizedFailureCode;
        }

        private static void EnsureUtc(DateTimeOffset value)
        {
            GuardHelper.Ensure(
                condition: value.Offset == TimeSpan.Zero,
                value: value,
                errorFactory: DomainErrorsFactory.CityTimestampMustBeUtc);
        }

        private static void EnsureUtc(DateTimeOffset? value)
        {
            if (value.HasValue)
                EnsureUtc(value.Value);
        }
    }
}
