export interface PersonDto {
  id: string;
  fullName: string;
  sex: string;
  birthDate: string; // DateOnly из .NET придет как строка "YYYY-MM-DD"
  age: number;
  ageGroup: string;

  maritalStatus: string;
  educationLevel: string;

  happiness: number;

  employmentStatus: string;
  jobTitle?: string | null;
}
