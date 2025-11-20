import type { PersonDto } from "./types";
import type { UpdateCitizenRequest } from "./types";

const POPULATION_API_BASE_URL =
  import.meta.env.VITE_POPULATION_API_URL ?? "https://localhost:7155";

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

// Helper
async function handleJson<T>(
  res: Response,
  defaultMessage: string
): Promise<T> {
  if (!res.ok) {
    let message = defaultMessage;

    try {
      const text = await res.text();
      if (text) {
        message = text;
      }
    } catch {
      // если text() упадёт, остаётся defaultMessage
    }

    throw new Error(message);
  }

  return res.json() as Promise<T>;
}

export async function initializePopulation(
  peopleCount: number,
  randomSeed?: number
): Promise<void> {
  const params = new URLSearchParams({
    peopleCount: peopleCount.toString(),
  });

  if (randomSeed !== undefined) {
    params.append("randomSeed", randomSeed.toString());
  }

  const res = await fetch(
    `${POPULATION_API_BASE_URL}/api/population/init?${params.toString()}`,
    { method: "POST" }
  );

  if (!res.ok) {
    throw new Error("Failed to initialize population");
  }
}

export async function getCitizensPage(
  page: number,
  pageSize: number
): Promise<PagedResult<PersonDto>> {
  const response = await fetch(
    `${POPULATION_API_BASE_URL}/api/population/citizens?pageNumber=${page}&pageSize=${pageSize}`
  );

  return handleJson<PagedResult<PersonDto>>(
    response,
    "Failed to load citizens"
  );
}

export async function killCitizen(id: string): Promise<PersonDto> {
  const res = await fetch(
    `${POPULATION_API_BASE_URL}/api/population/${id}/kill`,
    { method: "POST" }
  );

  return handleJson<PersonDto>(res, "Failed to kill citizen");
}

export async function resurrectCitizen(id: string): Promise<PersonDto> {
  const res = await fetch(
    `${POPULATION_API_BASE_URL}/api/population/${id}/resurrect`,
    { method: "POST" }
  );

  return handleJson<PersonDto>(res, "Failed to resurrect citizen");
}

export async function updateCitizen(
  id: string,
  payload: UpdateCitizenRequest
): Promise<PersonDto> {
  const res = await fetch(
    `${POPULATION_API_BASE_URL}/api/population/${id}/update`,
    {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    }
  );

  return handleJson<PersonDto>(res, "Failed to update citizen");
}
