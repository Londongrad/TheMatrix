import type {CityResidentDetailsDto} from "./residentContracts";

export interface CityCivilRegistryOperationResultDto {
    action: string;
    recordedAtUtc: string;
    firstResident: CityResidentDetailsDto;
    secondResident: CityResidentDetailsDto;
}
