// src/services/population/person/api/personTypes.ts
export interface PersonDto {
  id: string;
  fullName: string;
  sex: string;
  birthDate: string;
  deathDate: string;
  age: number;
  ageGroup: string;
  lifeStatus: string;
  maritalStatus: string;
  educationLevel: string;

  happiness: number;

  employmentStatus: string;
  jobTitle?: string | null;
}

export interface UpdateCitizenRequest {
  happiness?: number; // опционально
  fullName?: string; // опционально
  maritalStatus?: string; // опционально
  educationLevel?: string; // опционально
  employmentStatus?: string; // опционально
  jobTitle?: string | null; // опционально
}
