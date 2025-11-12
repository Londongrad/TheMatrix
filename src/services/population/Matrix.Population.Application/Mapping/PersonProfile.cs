using AutoMapper;
using Matrix.Population.Application.DTOs;
using Matrix.Population.Domain.Entities;

namespace Matrix.Population.Application.Mapping
{
    public sealed class PersonProfile : Profile
    {
        public PersonProfile()
        {
            CreateMap<Person, PersonDto>()
                .ForMember(d => d.Id,
                    opt => opt.MapFrom(s => s.Id.Value))
                .ForMember(d => d.FullName,
                    opt => opt.MapFrom(s => s.Name.ToString()))
                .ForMember(d => d.Sex,
                    opt => opt.MapFrom(s => s.Sex.ToString()))
                .ForMember(d => d.BirthDate,
                    opt => opt.MapFrom(s => s.BirthDate))

                // кастомные резолверы, потому что нужен currentDate
                .ForMember(d => d.Age,
                    opt => opt.MapFrom<AgeResolver>())
                .ForMember(d => d.AgeGroup,
                    opt => opt.MapFrom<AgeGroupResolver>())

                .ForMember(d => d.MaritalStatus,
                    opt => opt.MapFrom(s => s.MaritalStatus.ToString()))
                .ForMember(d => d.EducationLevel,
                    opt => opt.MapFrom(s => s.EducationLevel.ToString()))
                .ForMember(d => d.Happiness,
                    opt => opt.MapFrom(s => s.Happiness.Value))

                .ForMember(d => d.EmploymentStatus,
                    opt => opt.MapFrom(s => s.EmploymentStatus.ToString()))
                .ForMember(d => d.JobTitle,
                    opt => opt.MapFrom(s => s.Job != null ? s.Job.Title : null));
        }

        // --- резолвер возраста ---
        public sealed class AgeResolver : IValueResolver<Person, PersonDto, int>
        {
            public int Resolve(
                Person source,
                PersonDto destination,
                int destMember,
                ResolutionContext context)
            {
                if (!context.Items.TryGetValue("currentDate", out var value) ||
                    value is not DateOnly currentDate)
                {
                    throw new InvalidOperationException(
                        "currentDate must be provided in mapping options.");
                }

                return source.GetAge(currentDate).Years;
            }
        }

        // --- резолвер возрастной группы ---
        public sealed class AgeGroupResolver : IValueResolver<Person, PersonDto, string>
        {
            public string Resolve(
                Person source,
                PersonDto destination,
                string destMember,
                ResolutionContext context)
            {
                if (!context.Items.TryGetValue("currentDate", out var value) ||
                    value is not DateOnly currentDate)
                {
                    throw new InvalidOperationException(
                        "currentDate must be provided in mapping options.");
                }

                var group = source.GetAgeGroup(currentDate);
                return group.ToString();
            }
        }
    }
}
