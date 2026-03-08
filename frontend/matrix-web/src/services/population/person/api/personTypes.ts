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

export interface CityResidentHousingDto {
    householdId: string;
    housingStatus: string;
    residentialBuildingId?: string | null;
}

export interface CityResidentWorkplaceDto {
    workplaceId: string;
}

export interface CityResidentEducationInstitutionDto {
    institutionId: string;
    educationLevel: string;
}

export interface CityResidentDetailsDto extends PersonDto {
    currentSpouse?: PersonReferenceDto | null;
    currentHousing: CityResidentHousingDto;
    currentWorkplace?: CityResidentWorkplaceDto | null;
    currentEducationInstitution?: CityResidentEducationInstitutionDto | null;
}

export interface CityEmploymentCatalogDto {
    jobTitles: string[];
    currentWorkplaces: CityEmploymentWorkplaceDto[];
}

export interface CityEmploymentWorkplaceDto {
    workplaceId: string;
    jobTitle: string;
    residentCount: number;
}

export interface CityEducationCatalogDto {
    currentInstitutions: CityEducationInstitutionDto[];
}

export interface CityEducationInstitutionDto {
    institutionId: string;
    educationLevel: string;
    residentCount: number;
}

export interface CityCivilRegistryOperationResultDto {
    action: string;
    recordedAtUtc: string;
    firstResident: CityResidentDetailsDto;
    secondResident: CityResidentDetailsDto;
}

export interface CityEmploymentOperationResultDto {
    action: string;
    recordedAtUtc: string;
    resident: CityResidentDetailsDto;
}

export interface CityEducationOperationResultDto {
    action: string;
    recordedAtUtc: string;
    resident: CityResidentDetailsDto;
}
