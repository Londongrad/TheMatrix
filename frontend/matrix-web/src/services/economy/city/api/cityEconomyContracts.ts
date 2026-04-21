export interface EconomySummaryDto {
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    balance: number;
    totalTaxIncome: number;
    totalIncomeTaxIncome: number;
    totalSalesTaxIncome: number;
    totalDirectRevenue: number;
    totalCityExpenses: number;
    totalRetailTurnover: number;
    totalGrossPayroll: number;
    totalNetPayroll: number;
}

export interface CityOperationalBudgetPressureDto {
    cityId: string;
    effectiveTickId: number;
    effectivePhase: string;
    effectiveAtUtc?: string | null;
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    balance: number;
    totalCityExpenses: number;
    municipalOperationsExpenses: number;
    infrastructureOperationsExpenses: number;
    emergencyOperationsExpenses: number;
    generalAvailableAmount: number;
    operationsAvailableAmount: number;
    infrastructureAvailableAmount: number;
    healthcareAvailableAmount: number;
    generalAuthorizationLevel: string;
    operationsAuthorizationLevel: string;
    infrastructureAuthorizationLevel: string;
    healthcareAuthorizationLevel: string;
    lastMunicipalExpenseAtUtc?: string | null;
    pressureIndex: number;
}

export interface CityBusinessDto {
    businessId: string;
    cityId: string;
    createdAtUtc: string;
    name: string;
    kind: string;
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    balance: number;
    taxReserve: number;
    totalCapitalInjections: number;
    totalRetailTurnover: number;
    totalNetSalesRevenue: number;
    totalOperatingExpenses: number;
    totalTaxRemitted: number;
}

export interface CityHouseholdAccountDto {
    householdAccountId: string;
    cityId: string;
    createdAtUtc: string;
    name: string;
    externalReferenceCode?: string | null;
    unitKind: string;
    unitCode: string;
    unitDisplayName: string;
    unitSymbol: string;
    balance: number;
    totalOpeningBalance: number;
    totalPayrollIncome: number;
    totalConsumerSpending: number;
}
