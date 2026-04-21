export interface BudgetLedgerEntryDto {
    entryId: string;
    occurredAtUtc: string;
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    kind: string;
    category: string;
    amount: number;
    title: string;
    description: string;
    source: string;
    referenceCode?: string | null;
}

export interface CityBusinessLedgerEntryDto {
    entryId: string;
    occurredAtUtc: string;
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    kind: string;
    amount: number;
    taxAmount: number;
    title: string;
    description: string;
    source: string;
    referenceCode?: string | null;
}

export interface CityHouseholdAccountLedgerEntryDto {
    entryId: string;
    occurredAtUtc: string;
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    kind: string;
    amount: number;
    title: string;
    description: string;
    source: string;
    referenceCode?: string | null;
}
