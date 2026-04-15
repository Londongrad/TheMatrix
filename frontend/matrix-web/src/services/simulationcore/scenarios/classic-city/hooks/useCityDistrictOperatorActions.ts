import {useCallback, useMemo, useState} from "react";
import {HttpError} from "@shared/api/http";
import {
    dispatchDistrictResupply,
    dispatchDistrictUtilityResponse,
} from "@services/simulationcore/scenarios/classic-city/api/citiesApi";
import type {
    CityUtilityIncidentStatusView,
    DispatchCityResupplyView,
} from "@services/simulationcore/scenarios/classic-city/contracts/operatorContracts";

type OperatorActionKind = "utility-response" | "resupply";

type OperatorNotice = {
    districtId: string;
    kind: OperatorActionKind;
    tone: "success" | "warning" | "danger";
    title: string;
    detail: string;
};

function isObjectRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null;
}

function isUtilityIncidentStatusView(value: unknown): value is CityUtilityIncidentStatusView {
    return isObjectRecord(value) && typeof value.cityId === "string";
}

function isDispatchCityResupplyView(value: unknown): value is DispatchCityResupplyView {
    return isObjectRecord(value) && typeof value.cityId === "string" && typeof value.status === "string";
}

function buildUtilityNotice(
    districtId: string,
    view: CityUtilityIncidentStatusView,
    tone: "success" | "warning",
): OperatorNotice {
    const readyTick = view.pendingOperation?.readyAtTickId;
    const appliedIntensity = view.appliedIntensity ?? view.budgetAuthorizedIntensity ?? view.requestedIntensity ?? "Standard";

    return {
        districtId,
        kind: "utility-response",
        tone,
        title: tone === "success" ? "Utility response queued" : "Utility response constrained",
        detail: readyTick
            ? `${view.budgetAuthorizationSummary ?? "Dispatch accepted."} Ready around tick ${readyTick} with ${appliedIntensity} intensity.`
            : (view.budgetAuthorizationSummary ?? "Dispatch evaluated."),
    };
}

function buildResupplyNotice(
    districtId: string,
    view: DispatchCityResupplyView,
    tone: "success" | "warning",
): OperatorNotice {
    const readyTick = view.pendingResupply?.readyAtTickId;
    const appliedIntensity = view.appliedIntensity ?? view.budgetAuthorizedIntensity ?? view.requestedIntensity;

    return {
        districtId,
        kind: "resupply",
        tone,
        title: tone === "success" ? "District resupply queued" : "District resupply constrained",
        detail: readyTick
            ? `${view.budgetAuthorizationSummary} Ready around tick ${readyTick} with ${appliedIntensity} intensity.`
            : view.budgetAuthorizationSummary,
    };
}

export function useCityDistrictOperatorActions(
    cityId: string,
    onSettled?: () => void | Promise<void>,
) {
    const [pendingAction, setPendingAction] = useState<{ districtId: string; kind: OperatorActionKind } | null>(null);
    const [notice, setNotice] = useState<OperatorNotice | null>(null);
    const [error, setError] = useState<string | null>(null);

    const utilityResponse = useCallback(async (districtId: string) => {
        setPendingAction({districtId, kind: "utility-response"});
        setError(null);

        try {
            const view = await dispatchDistrictUtilityResponse(cityId, {
                focus: "Balanced",
                intensity: "Standard",
                districtId,
                emergencyOverride: false,
            });

            setNotice(buildUtilityNotice(districtId, view, "success"));
            await onSettled?.();
        } catch (error: unknown) {
            if (error instanceof HttpError && error.status === 409 && isUtilityIncidentStatusView(error.payload)) {
                setNotice(buildUtilityNotice(districtId, error.payload, "warning"));
                await onSettled?.();
                return;
            }

            setError(error instanceof Error ? error.message : "Failed to dispatch district utility response.");
        } finally {
            setPendingAction(null);
        }
    }, [cityId, onSettled]);

    const resupply = useCallback(async (districtId: string) => {
        setPendingAction({districtId, kind: "resupply"});
        setError(null);

        try {
            const view = await dispatchDistrictResupply(cityId, {
                focus: 1,
                intensity: 2,
                districtId,
                emergencyOverride: false,
            });

            setNotice(buildResupplyNotice(districtId, view, "success"));
            await onSettled?.();
        } catch (error: unknown) {
            if (error instanceof HttpError && error.status === 409 && isDispatchCityResupplyView(error.payload)) {
                setNotice(buildResupplyNotice(districtId, error.payload, "warning"));
                await onSettled?.();
                return;
            }

            setError(error instanceof Error ? error.message : "Failed to dispatch district resupply.");
        } finally {
            setPendingAction(null);
        }
    }, [cityId, onSettled]);

    return useMemo(
        () => ({
            pendingAction,
            notice,
            error,
            clearError: () => setError(null),
            utilityResponse,
            resupply,
        }),
        [error, notice, pendingAction, resupply, utilityResponse],
    );
}
