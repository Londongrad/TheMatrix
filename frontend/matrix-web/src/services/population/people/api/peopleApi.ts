import {API_POPULATION_URL} from "@shared/api/config";
import {apiRequest} from "@shared/api/http";

export async function initializePopulation(
    peopleCount: number,
    token: string,
    randomSeed?: number,
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
