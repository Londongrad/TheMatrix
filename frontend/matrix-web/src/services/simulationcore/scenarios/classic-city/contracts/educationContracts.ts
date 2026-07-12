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
