export interface CityEducationCatalogDto {
    institutions: CityEducationInstitutionDto[];
}

export interface CityEducationInstitutionDto {
    institutionId: string;
    name: string;
    kind: string;
    locationAnchorId: string | null;
    capacity: number;
    currentEnrollmentCount: number;
    availableSeatCount: number;
}

export interface CityEducationOperationResultDto {
    status: string;
    enrollmentId: string | null;
    completedStage: string | null;
}

export interface CityResidentActiveEnrollmentDto {
    enrollmentId: string;
    institutionId: string;
    institutionName: string;
    institutionKind: string;
    locationAnchorId: string | null;
    stage: string;
    enrolledOn: string;
}

export interface CityResidentEducationStatusDto {
    residentId: string;
    profileAvailable: boolean;
    isAlive: boolean;
    isActive: boolean;
    completedStage: string | null;
    completedStageOn: string | null;
    activeEnrollment: CityResidentActiveEnrollmentDto | null;
}
