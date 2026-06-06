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

export interface CityResidentRouteAccessDto {
    hasRouteData: boolean;
    isAccessible: boolean;
    accessibilityIndex: number;
    passabilityIndex: number;
    estimatedTravelTimeMinutes?: number | null;
}

export interface CityResidentWorkplaceDto {
    workplaceId: string;
    workplaceAnchorId?: string | null;
    routeAccess?: CityResidentRouteAccessDto | null;
}

export interface CityResidentEducationInstitutionDto {
    institutionId: string;
    institutionAnchorId?: string | null;
    educationLevel: string;
    routeAccess?: CityResidentRouteAccessDto | null;
}

export interface CityResidentHealthcareProviderDto {
    primaryCareAnchorId: string;
    routeAccess?: CityResidentRouteAccessDto | null;
}

export interface CityResidentActiveTripDto {
    subject: string;
    purpose: string;
    status: string;
    currentProgressIndex: number;
    startedAtSimTimeUtc: string;
    expectedArrivalAtSimTimeUtc: string;
    fromName: string;
    toName: string;
}

export interface CityResidentIllnessDto {
    kind: string;
    severity: string;
    diagnosedOn: string;
}

export interface CityResidentDetailsDto extends PersonDto {
    currentSpouse?: PersonReferenceDto | null;
    mother?: PersonReferenceDto | null;
    father?: PersonReferenceDto | null;
    children: PersonReferenceDto[];
    lastChildbirthDate?: string | null;
    currentIllness?: CityResidentIllnessDto | null;
    lastIllnessRecoveredOn?: string | null;
    currentHousing: CityResidentHousingDto;
    currentWorkplace?: CityResidentWorkplaceDto | null;
    currentEducationInstitution?: CityResidentEducationInstitutionDto | null;
    primaryHealthcareProvider?: CityResidentHealthcareProviderDto | null;
    currentActiveTrip?: CityResidentActiveTripDto | null;
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

export interface CityEducationOperationResultDto {
    action: string;
    recordedAtUtc: string;
    resident: CityResidentDetailsDto;
}
