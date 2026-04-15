export interface PendingCityOperationView {
    focus: string;
    intensity: string;
    readyAtTickId: number;
}

export interface DispatchCityUtilityIncidentResponseRequest {
    focus?: string;
    intensity?: string;
    districtId?: string | null;
    emergencyOverride?: boolean;
}

export interface CityUtilityIncidentStatusView {
    cityId: string;
    requestedIntensity?: string | null;
    appliedIntensity?: string | null;
    budgetAuthorizationStatus?: string | null;
    budgetAuthorizationLevel?: string | null;
    budgetAvailableAmount?: number | null;
    budgetAuthorizedByEmergencyOverride?: boolean | null;
    budgetAuthorizedIntensity?: string | null;
    budgetAuthorizationSummary?: string | null;
    focusDistrictId?: string | null;
    pendingOperation?: PendingCityOperationView | null;
}

export interface DispatchCityResupplyRequest {
    focus?: number;
    intensity?: number;
    districtId?: string | null;
    emergencyOverride?: boolean;
}

export interface PendingResupplyView {
    focus: string;
    intensity: string;
    focusDistrictId?: string | null;
    readyAtTickId: number;
}

export interface DispatchCityResupplyView {
    status: string;
    cityId: string;
    requestedIntensity: string;
    budgetAuthorizedIntensity?: string | null;
    appliedIntensity?: string | null;
    pendingResupply?: PendingResupplyView | null;
    budgetPressureIndex: number;
    budgetAuthorizationStatus: string;
    budgetAuthorizationLevel: string;
    budgetAvailableAmount: number;
    budgetAuthorizedByEmergencyOverride: boolean;
    budgetAuthorizationSummary: string;
    supplyStressIndex: number;
}
