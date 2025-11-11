import type { PersonDto } from "./types";

const POPULATION_API_BASE_URL =
  import.meta.env.VITE_POPULATION_API_URL ?? "https://localhost:7297";

export async function getPopulationPreview(
  count: number,
  seed?: number
): Promise<PersonDto[]> {
  const params = new URLSearchParams();
  params.set("peopleCount", count.toString());
  if (seed !== undefined) {
    params.set("randomSeed", seed.toString());
  }

  const url = `${POPULATION_API_BASE_URL}/api/population/simulation/init?${params.toString()}`;

  const response = await fetch(url, {
    method: "POST",
  });

  if (!response.ok) {
    throw new Error(`Failed to load population preview: ${response.status}`);
  }

  const data = (await response.json()) as PersonDto[];
  return data;
}
