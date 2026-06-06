import type {CityResidentDetailsDto} from "./residentContracts";

export interface CityEducationCatalogDto {
    currentInstitutions: CityEducationInstitutionDto[];
}

export interface CityEducationInstitutionDto {
    institutionId: string;
    educationLevel: string;
    residentCount: number;
}

export interface CityEducationOperationResultDto {
    action: string;
    recordedAtUtc: string;
    resident: CityResidentDetailsDto;
}
