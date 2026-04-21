import {apiRequest} from "@shared/api/http";
import type {CursorPagedResult} from "@shared/lib/paging/cursorPagingTypes";
import {
    API_ECONOMY_BUDGET_URL,
    API_ECONOMY_BUSINESS_URL,
    API_ECONOMY_HOUSEHOLD_ACCOUNTS_URL,
} from "@shared/api/config";
import type {
    BudgetLedgerEntryDto,
    CityBusinessLedgerEntryDto,
    CityHouseholdAccountLedgerEntryDto,
} from "@services/economy/ledger/api/ledgerContracts";

function buildFeedUrl(
    baseUrl: string,
    ownerId: string,
    cursor: string | null,
    pageSize: number,
) {
    const params = new URLSearchParams();
    params.set("pageSize", String(pageSize));

    if (cursor) {
        params.set("cursor", cursor);
    }

    return `${baseUrl}/${ownerId}/ledger-feed?${params.toString()}`;
}

export function getBudgetLedgerFeed(
    cityId: string,
    cursor: string | null,
    pageSize: number,
    signal?: AbortSignal,
) {
    return apiRequest<CursorPagedResult<BudgetLedgerEntryDto>>(
        buildFeedUrl(`${API_ECONOMY_BUDGET_URL}/cities`, cityId, cursor, pageSize),
        {method: "GET", signal},
    );
}

export function getBusinessLedgerFeed(
    businessId: string,
    cursor: string | null,
    pageSize: number,
    signal?: AbortSignal,
) {
    return apiRequest<CursorPagedResult<CityBusinessLedgerEntryDto>>(
        buildFeedUrl(API_ECONOMY_BUSINESS_URL, businessId, cursor, pageSize),
        {method: "GET", signal},
    );
}

export function getHouseholdAccountLedgerFeed(
    householdAccountId: string,
    cursor: string | null,
    pageSize: number,
    signal?: AbortSignal,
) {
    return apiRequest<CursorPagedResult<CityHouseholdAccountLedgerEntryDto>>(
        buildFeedUrl(API_ECONOMY_HOUSEHOLD_ACCOUNTS_URL, householdAccountId, cursor, pageSize),
        {method: "GET", signal},
    );
}
