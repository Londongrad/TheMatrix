import {useCursorFeed} from "@shared/lib/paging/useCursorFeed";
import {
    getBudgetLedgerFeed,
    getBusinessLedgerFeed,
    getHouseholdAccountLedgerFeed,
} from "@services/economy/ledger/api/ledgerApi";

interface UseLedgerFeedOptions {
    enabled?: boolean;
    pageSize?: number;
}

export function useBudgetLedgerFeed(
    cityId: string | null | undefined,
    options: UseLedgerFeedOptions = {},
) {
    const {enabled = true, pageSize} = options;

    return useCursorFeed(
        (cursor, size, signal) => getBudgetLedgerFeed(cityId!, cursor, size, signal),
        [cityId],
        {
            enabled: enabled && Boolean(cityId),
            pageSize,
            errorMessage: "Failed to load budget ledger feed.",
        },
    );
}

export function useBusinessLedgerFeed(
    businessId: string | null | undefined,
    options: UseLedgerFeedOptions = {},
) {
    const {enabled = true, pageSize} = options;

    return useCursorFeed(
        (cursor, size, signal) => getBusinessLedgerFeed(businessId!, cursor, size, signal),
        [businessId],
        {
            enabled: enabled && Boolean(businessId),
            pageSize,
            errorMessage: "Failed to load business ledger feed.",
        },
    );
}

export function useHouseholdAccountLedgerFeed(
    householdAccountId: string | null | undefined,
    options: UseLedgerFeedOptions = {},
) {
    const {enabled = true, pageSize} = options;

    return useCursorFeed(
        (cursor, size, signal) => getHouseholdAccountLedgerFeed(householdAccountId!, cursor, size, signal),
        [householdAccountId],
        {
            enabled: enabled && Boolean(householdAccountId),
            pageSize,
            errorMessage: "Failed to load household ledger feed.",
        },
    );
}
