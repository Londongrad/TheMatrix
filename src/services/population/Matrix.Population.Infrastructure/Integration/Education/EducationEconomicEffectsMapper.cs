using System.Text.Json;
using Matrix.Education.Contracts.Events;
using Matrix.Population.Application.Integration;
using Matrix.Population.Domain.Models;

namespace Matrix.Population.Infrastructure.Integration.Education;

internal static class EducationEconomicEffectsMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static ResidentExternalEconomicProfile FromContract(EducationEconomicEffectsV1 effects)
    {
        ArgumentNullException.ThrowIfNull(effects);
        ArgumentNullException.ThrowIfNull(effects.TransferIncome);
        if (effects.TransferIncome.Count is < 1 or > 128 || effects.TransferIncome.Any(band => band is null))
            throw new ArgumentException("Economic effects require between 1 and 128 valid age bands.", nameof(effects));
        return new ResidentExternalEconomicProfile(
            ResidentAgeIncomeSchedule.Create(effects.TransferIncome.Select(band => (band.MinimumAge, band.DailyIncome)).ToArray()),
            effects.EmploymentIncomeBonus, effects.EmploymentOpportunityBonus, effects.EmploymentAvailabilityFactor,
            effects.RetailStoreSpendShareAdjustment, effects.ServiceSpendShareAdjustment, effects.MunicipalSpendShareAdjustment);
    }

    internal static string Serialize(ResidentExternalEconomicProfile profile) => JsonSerializer.Serialize(
        new EducationEconomicEffectsV1(
            profile.TransferIncome.Bands.Select(band => new EducationAgeIncomeBandV1(band.MinimumAge, band.DailyIncome)).ToArray(),
            profile.EmploymentIncomeBonus, profile.EmploymentOpportunityBonus, profile.EmploymentAvailabilityFactor,
            profile.RetailStoreSpendShareAdjustment, profile.ServiceSpendShareAdjustment, profile.MunicipalSpendShareAdjustment), JsonOptions);

    internal static ResidentExternalEconomicProfile Deserialize(string json) => FromContract(
        JsonSerializer.Deserialize<EducationEconomicEffectsV1>(json, JsonOptions)
        ?? throw new InvalidOperationException("Stored education economic effects cannot be null."));
}
