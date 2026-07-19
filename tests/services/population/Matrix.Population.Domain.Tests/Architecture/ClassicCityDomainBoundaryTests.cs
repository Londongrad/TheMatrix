using System.Reflection;
using Matrix.ArchitectureTesting;
using Matrix.Population.Domain.Entities;
using Xunit;

namespace Matrix.Population.Domain.Tests.Architecture
{
    public sealed class ClassicCityDomainBoundaryTests
    {
        [Fact]
        public void ScenarioNeutralDomainTypes_DoNotDependOnClassicCity()
        {
            ScenarioDependencyRule.AssertScenarioNeutral(
                assembly: typeof(Person).Assembly,
                boundedContextNamespace: "Matrix.Population",
                scenarioName: "ClassicCity");
        }

        [Fact]
        public void Person_ExposesOnlyAuthoritativeVitalStateProjection()
        {
            string[] forbiddenMethods =
            [
                "ChangeHealth",
                "DiagnoseIllness",
                "ProgressIllness",
                "RecoverFromIllness"
            ];

            MethodInfo[] publicMethods = typeof(Person).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.DoesNotContain(
                publicMethods,
                method => forbiddenMethods.Contains(method.Name, StringComparer.Ordinal));
            Assert.Contains(
                publicMethods,
                method => string.Equals(
                    method.Name,
                    nameof(Person.TryApplyVitalStateProjection),
                    StringComparison.Ordinal));
        }

        [Fact]
        public void Person_DoesNotExposeEducationState()
        {
            string[] forbiddenMemberFragments =
            [
                "Education",
                "Enrollment",
                "School",
                "Student"
            ];

            MemberInfo[] publicMembers = typeof(Person).GetMembers(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            Assert.DoesNotContain(
                publicMembers,
                member => forbiddenMemberFragments.Any(fragment =>
                    member.Name.Contains(
                        fragment,
                        StringComparison.OrdinalIgnoreCase)));
        }
    }
}
