import {API_PERSON_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";
import type {PersonDto, UpdatePersonRequest} from "./personTypes";

export async function updateCitizen(
    id: string,
    request: UpdatePersonRequest,
    token: string
): Promise<PersonDto> {
    return await apiRequest<PersonDto>(`${API_PERSON_URL}/${id}`, {
        method: "PUT",
        headers: {
            Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(request),
    });
}

export async function killCitizen(
    id: string,
    token: string
): Promise<PersonDto> {
    return await apiRequest<PersonDto>(`${API_PERSON_URL}/${id}/kill`, {
        method: "POST",
        headers: {
            Authorization: `Bearer ${token}`,
        },
    });
}

export async function resurrectCitizen(
    id: string,
    token: string
): Promise<PersonDto> {
    return await apiRequest<PersonDto>(`${API_PERSON_URL}/${id}/resurrect`, {
        method: "POST",
        headers: {
            Authorization: `Bearer ${token}`,
        },
    });
}
