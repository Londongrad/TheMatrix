namespace Matrix.Population.Application.DTOs
{
    public sealed class PersonDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = default!;
        public string Sex { get; set; } = default!;
        public DateOnly BirthDate { get; set; }
        public int Age { get; set; }
        public string AgeGroup { get; set; } = default!;

        public string MaritalStatus { get; set; } = default!;
        public string EducationLevel { get; set; } = default!;

        public int Happiness { get; set; }

        public string EmploymentStatus { get; set; } = default!;
        public string? JobTitle { get; set; }
    }
}
