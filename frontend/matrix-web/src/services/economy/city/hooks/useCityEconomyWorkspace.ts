import {useEffect, useRef, useState} from "react";
import {
    getCityBudgetSummary,
    getCityBusinesses,
    getCityHouseholdAccounts,
    getCityOperationalBudgetPressure,
} from "@services/economy/city/api/cityEconomyApi";
import type {
    CityBusinessDto,
    CityHouseholdAccountDto,
    CityOperationalBudgetPressureDto,
    EconomySummaryDto,
} from "@services/economy/city/api/cityEconomyContracts";
import {getErrorMessage} from "@shared/lib/errors/getErrorMessage";

interface UseCityEconomyWorkspaceOptions {
    includeBudget?: boolean;
    includeBusinesses?: boolean;
    includeHouseholds?: boolean;
}

interface CityEconomyWorkspaceState {
    budgetSummary: EconomySummaryDto | null;
    budgetPressure: CityOperationalBudgetPressureDto | null;
    businesses: CityBusinessDto[];
    householdAccounts: CityHouseholdAccountDto[];
    budgetError: string | null;
    businessesError: string | null;
    householdAccountsError: string | null;
}

const EMPTY_STATE: CityEconomyWorkspaceState = {
    budgetSummary: null,
    budgetPressure: null,
    businesses: [],
    householdAccounts: [],
    budgetError: null,
    businessesError: null,
    householdAccountsError: null,
};

export function useCityEconomyWorkspace(
    cityId: string,
    options: UseCityEconomyWorkspaceOptions = {},
) {
    const {
        includeBudget = true,
        includeBusinesses = true,
        includeHouseholds = true,
    } = options;

    const [state, setState] = useState<CityEconomyWorkspaceState>(EMPTY_STATE);
    const [isLoading, setIsLoading] = useState(false);
    const [isRefreshing, setIsRefreshing] = useState(false);
    const [reloadToken, setReloadToken] = useState(0);
    const [loadedKey, setLoadedKey] = useState<string | null>(null);
    const loadedKeyRef = useRef<string | null>(null);

    const shouldLoad =
        cityId.length > 0 && (includeBudget || includeBusinesses || includeHouseholds);
    const requestKey = JSON.stringify({
        cityId,
        includeBudget,
        includeBusinesses,
        includeHouseholds,
    });

    useEffect(() => {
        if (!shouldLoad) {
            void (async () => {
                loadedKeyRef.current = null;
                setLoadedKey(null);
                setState(EMPTY_STATE);
                setIsLoading(false);
                setIsRefreshing(false);
            })();
            return;
        }

        const abortController = new AbortController();
        const isBackgroundRefresh = loadedKeyRef.current === requestKey;

        async function load() {
            if (isBackgroundRefresh) {
                setIsRefreshing(true);
            } else {
                setIsLoading(true);
            }

            const [
                summaryResult,
                pressureResult,
                businessesResult,
                householdsResult,
            ] = await Promise.allSettled([
                includeBudget
                    ? getCityBudgetSummary(cityId, abortController.signal)
                    : Promise.resolve(null),
                includeBudget
                    ? getCityOperationalBudgetPressure(cityId, abortController.signal)
                    : Promise.resolve(null),
                includeBusinesses
                    ? getCityBusinesses(cityId, abortController.signal)
                    : Promise.resolve([]),
                includeHouseholds
                    ? getCityHouseholdAccounts(cityId, abortController.signal)
                    : Promise.resolve([]),
            ]);

            if (abortController.signal.aborted) {
                return;
            }

            setState({
                budgetSummary:
                    includeBudget && summaryResult.status === "fulfilled"
                        ? summaryResult.value
                        : null,
                budgetPressure:
                    includeBudget && pressureResult.status === "fulfilled"
                        ? pressureResult.value
                        : null,
                businesses:
                    includeBusinesses && businessesResult.status === "fulfilled"
                        ? businessesResult.value
                        : [],
                householdAccounts:
                    includeHouseholds && householdsResult.status === "fulfilled"
                        ? householdsResult.value
                        : [],
                budgetError:
                    includeBudget && (summaryResult.status === "rejected" || pressureResult.status === "rejected")
                        ? getErrorMessage(
                            summaryResult.status === "rejected"
                                ? summaryResult.reason
                                : pressureResult.status === "rejected"
                                    ? pressureResult.reason
                                    : null,
                            "Failed to load city budget snapshot.",
                        )
                        : null,
                businessesError:
                    includeBusinesses && businessesResult.status === "rejected"
                        ? getErrorMessage(businessesResult.reason, "Failed to load city businesses.")
                        : null,
                householdAccountsError:
                    includeHouseholds && householdsResult.status === "rejected"
                        ? getErrorMessage(
                            householdsResult.reason,
                            "Failed to load city household accounts.",
                        )
                        : null,
            });
            loadedKeyRef.current = requestKey;
            setLoadedKey(requestKey);
            setIsLoading(false);
            setIsRefreshing(false);
        }

        void load();

        return () => {
            abortController.abort();
        };
    }, [
        cityId,
        includeBudget,
        includeBusinesses,
        includeHouseholds,
        reloadToken,
        requestKey,
        shouldLoad,
    ]);

    const refetch = () => {
        setReloadToken((value) => value + 1);
    };

    const visibleState = shouldLoad && loadedKey === requestKey ? state : EMPTY_STATE;

    return {
        ...visibleState,
        isLoading: shouldLoad ? isLoading : false,
        isRefreshing: shouldLoad && loadedKey === requestKey ? isRefreshing : false,
        refetch,
    };
}
