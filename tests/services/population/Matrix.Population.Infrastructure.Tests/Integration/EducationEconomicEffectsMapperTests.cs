using Matrix.Education.Contracts.Events;
using Matrix.Population.Infrastructure.Integration.Education;
using Xunit;

namespace Matrix.Population.Infrastructure.Tests.Integration;

public sealed class EducationEconomicEffectsMapperTests
{
    [Fact]
    public void JsonRoundTrip_PreservesProviderTermsWithoutCityDefaults()
    {
        var effects = new EducationEconomicEffectsV1([new(0, 3m), new(21, 99m)], 7m, 0.1d, 0.8d, -0.1m, 0.03m, 0.07m);
        var profile = EducationEconomicEffectsMapper.FromContract(effects);
        var restored = EducationEconomicEffectsMapper.Deserialize(EducationEconomicEffectsMapper.Serialize(profile));
        Assert.Equal(3m, restored.TransferIncome.Resolve(20));
        Assert.Equal(99m, restored.TransferIncome.Resolve(21));
        Assert.Equal(7m, restored.EmploymentIncomeBonus);
        Assert.Equal(0.1d, restored.EmploymentOpportunityBonus);
        Assert.Equal(0.8d, restored.EmploymentAvailabilityFactor);
        Assert.Equal(-0.1m, restored.RetailStoreSpendShareAdjustment);
        Assert.Equal(0.03m, restored.ServiceSpendShareAdjustment);
        Assert.Equal(0.07m, restored.MunicipalSpendShareAdjustment);
    }

    [Fact]
    public void FromContract_RejectsMalformedEffects()
    {
        var valid = new EducationEconomicEffectsV1([new(0, 0m)], 0m, 0d, 1d, 0m, 0m, 0m);
        Assert.Throws<ArgumentNullException>(() => EducationEconomicEffectsMapper.FromContract(valid with { TransferIncome = null! }));
        Assert.Throws<ArgumentException>(() => EducationEconomicEffectsMapper.FromContract(valid with { TransferIncome = [] }));
        Assert.Throws<ArgumentException>(() => EducationEconomicEffectsMapper.FromContract(valid with { TransferIncome = [null!] }));
        Assert.Throws<ArgumentException>(() => EducationEconomicEffectsMapper.FromContract(valid with { TransferIncome = [new(1, 4m)] }));
        Assert.Throws<ArgumentException>(() => EducationEconomicEffectsMapper.FromContract(valid with { TransferIncome = [new(0, 4m), new(0, 10m)] }));
        Assert.Throws<ArgumentException>(() => EducationEconomicEffectsMapper.FromContract(valid with
        { TransferIncome = Enumerable.Range(0, 129).Select(age => new EducationAgeIncomeBandV1(age, 1m)).ToArray() }));
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationEconomicEffectsMapper.FromContract(valid with { EmploymentAvailabilityFactor = double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationEconomicEffectsMapper.FromContract(valid with { EmploymentOpportunityBonus = double.PositiveInfinity }));
        Assert.Throws<ArgumentException>(() => EducationEconomicEffectsMapper.FromContract(valid with { MunicipalSpendShareAdjustment = 0.1m }));
        Assert.Throws<InvalidOperationException>(() => EducationEconomicEffectsMapper.Deserialize("null"));
    }
}
