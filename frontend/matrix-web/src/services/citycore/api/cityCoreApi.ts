import { API_CITY_URL } from "@shared/api/config";
import { apiRequest } from "@shared/api/http";
import type {
    CityClockResponseDto,
    JumpCityClockRequestDto,
    SetCityClockSpeedRequestDto,
} from "@services/citycore/api/cityCoreTypes";

function withAuth(token: string): HeadersInit {
    return { Authorization: `Bearer ${token}` };
}

export async function bootstrapCity(cityId: string, token: string): Promise<void> {
    await apiRequest<void>(`${API_CITY_URL}/${cityId}/bootstrap`, {
        method: "POST",
        headers: withAuth(token),
    });
}

export async function getClock(
    cityId: string,
    token: string,
): Promise<CityClockResponseDto> {
    return await apiRequest<CityClockResponseDto>(`${API_CITY_URL}/${cityId}/clock`, {
        method: "GET",
        headers: withAuth(token),
    });
}

export async function pause(cityId: string, token: string): Promise<void> {
    await apiRequest<void>(`${API_CITY_URL}/${cityId}/clock/pause`, {
        method: "POST",
        headers: withAuth(token),
    });
}

export async function resume(cityId: string, token: string): Promise<void> {
    await apiRequest<void>(`${API_CITY_URL}/${cityId}/clock/resume`, {
        method: "POST",
        headers: withAuth(token),
    });
}

export async function setSpeed(
    cityId: string,
    request: SetCityClockSpeedRequestDto,
    token: string,
): Promise<void> {
    await apiRequest<void>(`${API_CITY_URL}/${cityId}/clock/speed`, {
        method: "POST",
        headers: withAuth(token),
        body: JSON.stringify(request),
    });
}

export async function jump(
    cityId: string,
    request: JumpCityClockRequestDto,
    token: string,
): Promise<void> {
    await apiRequest<void>(`${API_CITY_URL}/${cityId}/clock/jump`, {
        method: "POST",
        headers: withAuth(token),
        body: JSON.stringify(request),
    });
}
