// src/services/population/api/populationApi.ts
import type { PersonDto } from "./populationTypes";
import { API_POPULATION_URL, API_PERSON_URL } from "@shared/api/config";
import { apiRequest } from "@shared/api/http";
import { type PagedResult } from "@shared/lib/paging/pagingTypes";

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
