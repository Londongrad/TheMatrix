export interface PersonDto {
    id: string;
    fullName: string;
    sex: string;
    birthDate: string;
    deathDate?: string | null;
    age: number;
    ageGroup: string;
    lifeStatus: string;
    maritalStatus: string;
    educationLevel: string;
    health: number;
    happiness: number;
    energy: number;
    stress: number;
    socialNeed: number;
    employmentStatus: string;
    jobTitle?: string | null;
}

export interface PersonReferenceDto {
    id: string;
    fullName: string;
}

export interface CityResidentDetailsDto extends PersonDto {
    currentSpouse?: PersonReferenceDto | null;
}
