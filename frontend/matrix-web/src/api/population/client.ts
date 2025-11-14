import type { PersonDto } from "./types";

const POPULATION_API_BASE_URL =
  import.meta.env.VITE_POPULATION_API_URL ?? "https://localhost:7155";

export interface PagedResult<T> {
  items: T[];
  total: number;
}

// новый метод для CitizensPage
export async function getCitizensPage(
  page: number,
  pageSize: number
): Promise<PagedResult<PersonDto>> {
  const response = await fetch(
    `${POPULATION_API_BASE_URL}/api/population/citizens?page=${page}&pageSize=${pageSize}`
  );

  if (!response.ok) {
    throw new Error("Failed to load citizens");
  }

  return await response.json();
}
