import {API_CLASSIC_CITY_CITIES_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {PagedResult} from "@shared/lib/paging/pagingTypes";
import type {PersonDto} from "@services/population/person/contracts/personContracts";
import type {CityCivilRegistryOperationResultDto} from "@services/simulationcore/scenarios/classic-city/contracts/civilRegistryContracts";
import type {
    CityEducationCatalogDto,
    CityEducationOperationResultDto,
    CityResidentEducationStatusDto,
} from "@services/simulationcore/scenarios/classic-city/contracts/educationContracts";
import type {
    CityEmploymentCatalogDto,
    CityEmploymentOperationResultDto,
} from "@services/simulationcore/scenarios/classic-city/contracts/employmentContracts";
import type {CityResidentDetailsDto} from "@services/simulationcore/scenarios/classic-city/contracts/residentContracts";

export function getCityResidentsPage(
    cityId: string,
    pageNumber: number,
    pageSize: number,
) {
    return apiRequest<PagedResult<PersonDto>>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/residents?pageNumber=${pageNumber}&pageSize=${pageSize}`,
        {
            method: "GET",
        },
    );
}

export function getCityResidentDetails(
    cityId: string,
    residentId: string,
    signal?: AbortSignal,
) {
    return apiRequest<CityResidentDetailsDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/residents/${residentId}`,
        {
            method: "GET",
            signal,
        },
    );
}

type CityCivilRegistryOperationPayload = {
    firstResidentId: string;
    secondResidentId: string;
};

type CityEmploymentOperationPayload = {
    residentId: string;
    jobTitle?: string | null;
    workplaceId?: string | null;
};

type EnrollCityResidentEducationPayload = {
    residentId: string;
    institutionId: string;
    stage: string;
};

type CityResidentEducationLifecyclePayload = {
    residentId: string;
};

export function getCityEmploymentCatalog(
    cityId: string,
    signal?: AbortSignal,
) {
    return apiRequest<CityEmploymentCatalogDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/employment/catalog`,
        {
            method: "GET",
            signal,
        },
    );
}

export function getCityEducationCatalog(
    cityId: string,
    signal?: AbortSignal,
) {
    return apiRequest<CityEducationCatalogDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/education/catalog`,
        {
            method: "GET",
            signal,
        },
    );
}

export function getCityResidentEducationStatus(
    cityId: string,
    residentId: string,
    signal?: AbortSignal,
) {
    return apiRequest<CityResidentEducationStatusDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/education/students/${residentId}`,
        {
            method: "GET",
            signal,
        },
    );
}

export function hireCityResident(
    cityId: string,
    payload: CityEmploymentOperationPayload,
) {
    return apiRequest<CityEmploymentOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/employment/hire`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function fireCityResident(
    cityId: string,
    payload: CityEmploymentOperationPayload,
) {
    return apiRequest<CityEmploymentOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/employment/fire`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function retireCityResident(
    cityId: string,
    payload: CityEmploymentOperationPayload,
) {
    return apiRequest<CityEmploymentOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/employment/retire`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function enrollCityResident(
    cityId: string,
    payload: EnrollCityResidentEducationPayload,
) {
    return apiRequest<CityEducationOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/education/enroll`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function graduateCityResident(
    cityId: string,
    payload: CityResidentEducationLifecyclePayload,
) {
    return apiRequest<CityEducationOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/education/graduate`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function withdrawCityResidentFromStudy(
    cityId: string,
    payload: CityResidentEducationLifecyclePayload,
) {
    return apiRequest<CityEducationOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/education/withdraw`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function registerCityMarriage(
    cityId: string,
    payload: CityCivilRegistryOperationPayload,
) {
    return apiRequest<CityCivilRegistryOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/civil-registry/marriages`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}

export function registerCityDivorce(
    cityId: string,
    payload: CityCivilRegistryOperationPayload,
) {
    return apiRequest<CityCivilRegistryOperationResultDto>(
        `${API_CLASSIC_CITY_CITIES_URL}/${cityId}/civil-registry/divorces`,
        {
            method: "POST",
            body: JSON.stringify(payload),
        },
    );
}
