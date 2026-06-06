import type {CityResidentDetailsDto} from "./residentContracts";

export interface CityEmploymentCatalogDto {
    jobTitles: string[];
    currentWorkplaces: CityEmploymentWorkplaceDto[];
}

export interface CityEmploymentWorkplaceDto {
    workplaceId: string;
    jobTitle: string;
    residentCount: number;
}

export interface CityEmploymentOperationResultDto {
    action: string;
    recordedAtUtc: string;
    resident: CityResidentDetailsDto;
}
