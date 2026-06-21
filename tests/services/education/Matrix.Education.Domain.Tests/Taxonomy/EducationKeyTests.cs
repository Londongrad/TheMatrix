using Matrix.BuildingBlocks.Domain.Exceptions;
using Matrix.Education.Domain.Institutions;
using Matrix.Education.Domain.Programs;
using Xunit;

namespace Matrix.Education.Domain.Tests.Taxonomy
{
    public sealed class EducationKeyTests
    {
        [Fact]
        public void Keys_NormalizeStableExternalValues()
        {
            var stage = new EducationStageKey("  Upper-Secondary ");
            var institutionKind = new EducationInstitutionKindKey(" Research-Institute ");

            Assert.Equal("upper-secondary", stage.Value);
            Assert.Equal("research-institute", institutionKind.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Keys_RejectMissingValues(string value)
        {
            Assert.Throws<DomainException>(() => new EducationStageKey(value));
        }

        [Theory]
        [InlineData("-primary")]
        [InlineData("primary-")]
        [InlineData("primary_school")]
        [InlineData("primary school")]
        public void Keys_RejectUnstableFormats(string value)
        {
            Assert.Throws<ArgumentException>(() => new EducationStageKey(value));
        }
    }
}
