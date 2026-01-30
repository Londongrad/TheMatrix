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
        public const int PopulationBootstrapErrorMaxLength = 1024;

        private City(
            CityId id,
            CityName name,
            CityEnvironment environment,
            CityGenerationSeed generationSeed,
            CityGenerationProfile generationProfile,
            CityStatus status,
            DateTimeOffset createdAtUtc,
            DateTimeOffset? populationBootstrapCompletedAtUtc,
            DateTimeOffset? populationBootstrapFailedAtUtc,
            string? populationBootstrapError,
            DateTimeOffset? archivedAtUtc)
            : base(id)
        {
            EnsureUtc(createdAtUtc);
            EnsureUtc(populationBootstrapCompletedAtUtc);
            EnsureUtc(populationBootstrapFailedAtUtc);
            EnsureUtc(archivedAtUtc);

            Name = name;
            Environment = environment;
            GenerationSeed = generationSeed;
            GenerationProfile = generationProfile;
            Status = status;
            CreatedAtUtc = createdAtUtc;
            PopulationBootstrapCompletedAtUtc = populationBootstrapCompletedAtUtc;
            PopulationBootstrapFailedAtUtc = populationBootstrapFailedAtUtc;
            PopulationBootstrapError = populationBootstrapError;
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
        public DateTimeOffset? PopulationBootstrapCompletedAtUtc { get; private set; }
        public DateTimeOffset? PopulationBootstrapFailedAtUtc { get; private set; }
        public string? PopulationBootstrapError { get; private set; }
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
                populationBootstrapCompletedAtUtc: null,
                populationBootstrapFailedAtUtc: null,
                populationBootstrapError: null,
                archivedAtUtc: null);

            city.AddDomainEvent(
                new CityCreatedDomainEvent(
                    CityId: city.Id,
                    Name: city.Name,
                    Environment: city.Environment,
                    GenerationSeed: city.GenerationSeed,
                    GenerationProfile: city.GenerationProfile,
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

        public void CompletePopulationBootstrap(DateTimeOffset completedAtUtc)
        {
            EnsureUtc(completedAtUtc);

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (IsActive)
                return;

            Status = CityStatus.Active;
            PopulationBootstrapCompletedAtUtc = completedAtUtc;
            PopulationBootstrapFailedAtUtc = null;
            PopulationBootstrapError = null;

            AddDomainEvent(
                new CityPopulationBootstrapCompletedDomainEvent(
                    CityId: Id,
                    CompletedAtUtc: completedAtUtc));
        }

        public void FailPopulationBootstrap(
            string error,
            DateTimeOffset failedAtUtc)
        {
            EnsureUtc(failedAtUtc);

            GuardHelper.Ensure(
                condition: !IsArchived,
                value: Status,
                errorFactory: DomainErrorsFactory.CityIsArchived);

            if (IsActive)
                return;

            string normalizedError = NormalizePopulationBootstrapError(error);

            Status = CityStatus.ProvisioningFailed;
            PopulationBootstrapCompletedAtUtc = null;
            PopulationBootstrapFailedAtUtc = failedAtUtc;
            PopulationBootstrapError = normalizedError;

            AddDomainEvent(
                new CityPopulationBootstrapFailedDomainEvent(
                    CityId: Id,
                    Error: normalizedError,
                    FailedAtUtc: failedAtUtc));
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

        private static string NormalizePopulationBootstrapError(string error)
        {
            string normalizedError = error?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(normalizedError))
                throw DomainErrorsFactory.CityPopulationBootstrapErrorNullOrEmpty(
                    propertyName: nameof(PopulationBootstrapError));

            if (normalizedError.Length > PopulationBootstrapErrorMaxLength)
                throw DomainErrorsFactory.CityPopulationBootstrapErrorTooLong(
                    value: normalizedError,
                    max: PopulationBootstrapErrorMaxLength,
                    propertyName: nameof(PopulationBootstrapError));

            return normalizedError;
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
