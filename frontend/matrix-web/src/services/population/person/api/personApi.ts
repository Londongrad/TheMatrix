import { API_PERSON_URL } from "@shared/api/config";
import { apiRequest } from "@shared/api/http";
import type { PersonDto } from "./personTypes";

// Убить гражданина
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

// Воскрешение гражданина
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
