import type { PersonDto, UpdateCitizenRequest } from "./populationTypes";
import { API_POPULATION_URL } from "../config";
import { apiRequest } from "../http";

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

// Инициализация населения
export async function initializePopulation(
  peopleCount: number,
  token: string,
  randomSeed?: number
): Promise<void> {
  const params = new URLSearchParams({
    peopleCount: peopleCount.toString(),
  });

  if (randomSeed !== undefined) {
    params.append("randomSeed", randomSeed.toString());
  }

  await apiRequest<void>(`${API_POPULATION_URL}/init?${params.toString()}`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}

// Получение страницы граждан
export async function getCitizensPage(
  page: number,
  pageSize: number,
  token: string
): Promise<PagedResult<PersonDto>> {
  return await apiRequest<PagedResult<PersonDto>>(
    `${API_POPULATION_URL}/citizens?pageNumber=${page}&pageSize=${pageSize}`,
    {
      method: "GET",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    }
  );
}

// Убить гражданина
export async function killCitizen(
  id: string,
  token: string
): Promise<PersonDto> {
  return await apiRequest<PersonDto>(`${API_POPULATION_URL}/${id}/kill`, {
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
  return await apiRequest<PersonDto>(`${API_POPULATION_URL}/${id}/resurrect`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
    },
  });
}

// Обновление гражданина
export async function updateCitizen(
  id: string,
  payload: UpdateCitizenRequest,
  token: string
): Promise<PersonDto> {
  return await apiRequest<PersonDto>(`${API_POPULATION_URL}/${id}/update`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(payload),
  });
}
