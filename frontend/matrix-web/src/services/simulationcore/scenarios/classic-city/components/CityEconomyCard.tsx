import {useEffect} from "react";
import {useSearchParams} from "react-router-dom";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import {usePermissions} from "@shared/permissions/usePermissions";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {useCityEconomyWorkspace} from "@services/economy/city/hooks/useCityEconomyWorkspace";
import type {CityBusinessDto, CityHouseholdAccountDto,} from "@services/economy/city/api/cityEconomyContracts";
import type {
    BudgetLedgerEntryDto,
    CityBusinessLedgerEntryDto,
    CityHouseholdAccountLedgerEntryDto,
} from "@services/economy/ledger/api/ledgerContracts";
import {
    useBudgetLedgerFeed,
    useBusinessLedgerFeed,
    useHouseholdAccountLedgerFeed,
} from "@services/economy/ledger/hooks/useEconomyLedgerFeed";
import "@services/simulationcore/scenarios/classic-city/styles/city-economy.css";

type Props = {
    cityId: string;
    cityName?: string;
    isArchived?: boolean;
};

type MetricTileProps = {
    label: string;
    value: string;
    note?: string;
    tone?: "default" | "success" | "warning" | "danger";
};

type LedgerFeedPanelProps<T> = {
    title: string;
    subtitle: string;
    entries: T[];
    error: string | null;
    isLoadingInitial: boolean;
    isLoadingMore: boolean;
    hasNext: boolean;
    onLoadMore: () => void;
    emptyTitle: string;
    emptyText: string;
    renderEntry: (entry: T) => React.ReactNode;
    right?: React.ReactNode;
};

type EconomyWorkspaceView = "all" | "budget" | "businesses" | "households";

const BUDGET_LEDGER_PAGE_SIZE = 24;
const ENTITY_LEDGER_PAGE_SIZE = 18;

function isEconomyWorkspaceView(value: string | null): value is EconomyWorkspaceView {
    return value === "all" || value === "budget" || value === "businesses" || value === "households";
}

function buildEconomySearchParams(
    currentSearchParams: URLSearchParams,
    updates: Record<string, string | null | undefined>,
) {
    const nextSearchParams = new URLSearchParams(currentSearchParams);

    Object.entries(updates).forEach(([key, value]) => {
        if (!value || value.trim().length === 0) {
            nextSearchParams.delete(key);
            return;
        }

        nextSearchParams.set(key, value);
    });

    return nextSearchParams;
}

function formatAmount(
    amount: number,
    unitSymbol?: string | null,
    unitCode?: string | null,
) {
    const formatted = new Intl.NumberFormat(document.documentElement.lang || undefined, {
        maximumFractionDigits: 2,
        minimumFractionDigits: 0,
    }).format(Math.abs(amount));
    const sign = amount < 0 ? "-" : "";

    if (unitSymbol?.trim()) {
        return `${sign}${unitSymbol}${formatted}`;
    }

    if (unitCode?.trim()) {
        return `${sign}${formatted} ${unitCode}`;
    }

    return `${sign}${formatted}`;
}

function formatDateTime(value: string | null | undefined) {
    if (!value) {
        return "--";
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

function humanize(value: string | null | undefined) {
    if (!value) {
        return "--";
    }

    return value
        .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
        .replace(/[_-]+/g, " ")
        .trim()
        .replace(/\b\w/g, (match) => match.toUpperCase());
}

function getAmountTone(amount: number) {
    if (amount > 0) {
        return "success";
    }

    if (amount < 0) {
        return "danger";
    }

    return "default";
}

function getPressureTone(pressureIndex: number) {
    if (pressureIndex >= 0.9) {
        return "danger";
    }

    if (pressureIndex >= 0.65) {
        return "warning";
    }

    return "success";
}

function MetricTile({
                        label,
                        value,
                        note,
                        tone = "default",
                    }: MetricTileProps) {
    return (
        <article className={`city-economy__metric city-economy__metric--${tone}`}>
            <span className="city-economy__metric-label">{label}</span>
            <strong className="city-economy__metric-value">{value}</strong>
            {note ? <span className="city-economy__metric-note">{note}</span> : null}
        </article>
    );
}

function LedgerFeedPanel<T>({
                                title,
                                subtitle,
                                entries,
                                error,
                                isLoadingInitial,
                                isLoadingMore,
                                hasNext,
                                onLoadMore,
                                emptyTitle,
                                emptyText,
                                renderEntry,
                                right,
                            }: LedgerFeedPanelProps<T>) {
    return (
        <Card title={title} subtitle={subtitle} right={right}>
            {error ? (
                <div className="simulationcore-error-banner" role="alert">
                    <span>{error}</span>
                </div>
            ) : null}

            {isLoadingInitial && entries.length === 0 ? (
                <div className="city-economy__empty" role="status">
                    <strong>Loading ledger feed</strong>
                    <span>Pulling the newest entries through the cursor-based feed.</span>
                </div>
            ) : null}

            {!isLoadingInitial && entries.length === 0 ? (
                <div className="city-economy__empty" role="status">
                    <strong>{emptyTitle}</strong>
                    <span>{emptyText}</span>
                </div>
            ) : null}

            {entries.length > 0 ? (
                <div className="city-economy__ledger-list">
                    {entries.map((entry) => renderEntry(entry))}
                </div>
            ) : null}

            {hasNext ? (
                <div className="city-economy__load-more">
                    <Button
                        type="button"
                        variant="primary"
                        onClick={onLoadMore}
                        disabled={isLoadingInitial || isLoadingMore}
                    >
                        {isLoadingMore ? "Loading more..." : "Load more"}
                    </Button>
                </div>
            ) : null}
        </Card>
    );
}

function BudgetLedgerEntryRow({entry}: { entry: BudgetLedgerEntryDto }) {
    return (
        <article className="city-economy__ledger-entry">
            <div className="city-economy__ledger-topline">
                <div className="city-economy__ledger-heading">
                    <strong>{entry.title}</strong>
                    <span className="city-economy__ledger-meta">
                        {formatDateTime(entry.occurredAtUtc)}
                        {" / "}
                        {humanize(entry.kind)}
                        {" / "}
                        {humanize(entry.category)}
                    </span>
                </div>

                <span
                    className={`city-economy__ledger-amount city-economy__ledger-amount--${getAmountTone(entry.amount)}`}>
                    {formatAmount(entry.amount, entry.unitSymbol, entry.unitCode)}
                </span>
            </div>

            <p className="city-economy__ledger-copy">{entry.description || "No extra narration for this budget movement."}</p>

            <div className="city-economy__ledger-tags">
                <span className="city-economy__chip">{humanize(entry.source)}</span>
                <span className="city-economy__chip">{entry.unitDisplayName}</span>
                {entry.referenceCode ? (
                    <span className="city-economy__chip city-economy__chip--muted">
                        Ref {entry.referenceCode}
                    </span>
                ) : null}
            </div>
        </article>
    );
}

function BusinessLedgerEntryRow({entry}: { entry: CityBusinessLedgerEntryDto }) {
    return (
        <article className="city-economy__ledger-entry">
            <div className="city-economy__ledger-topline">
                <div className="city-economy__ledger-heading">
                    <strong>{entry.title}</strong>
                    <span className="city-economy__ledger-meta">
                        {formatDateTime(entry.occurredAtUtc)}
                        {" / "}
                        {humanize(entry.kind)}
                    </span>
                </div>

                <span
                    className={`city-economy__ledger-amount city-economy__ledger-amount--${getAmountTone(entry.amount)}`}>
                    {formatAmount(entry.amount, entry.unitSymbol, entry.unitCode)}
                </span>
            </div>

            <p className="city-economy__ledger-copy">{entry.description || "No extra narration for this business movement."}</p>

            <div className="city-economy__ledger-tags">
                <span className="city-economy__chip">{humanize(entry.source)}</span>
                <span className="city-economy__chip city-economy__chip--muted">
                    Tax {formatAmount(entry.taxAmount, entry.unitSymbol, entry.unitCode)}
                </span>
                {entry.referenceCode ? (
                    <span className="city-economy__chip city-economy__chip--muted">
                        Ref {entry.referenceCode}
                    </span>
                ) : null}
            </div>
        </article>
    );
}

function HouseholdLedgerEntryRow({entry}: { entry: CityHouseholdAccountLedgerEntryDto }) {
    return (
        <article className="city-economy__ledger-entry">
            <div className="city-economy__ledger-topline">
                <div className="city-economy__ledger-heading">
                    <strong>{entry.title}</strong>
                    <span className="city-economy__ledger-meta">
                        {formatDateTime(entry.occurredAtUtc)}
                        {" / "}
                        {humanize(entry.kind)}
                    </span>
                </div>

                <span
                    className={`city-economy__ledger-amount city-economy__ledger-amount--${getAmountTone(entry.amount)}`}>
                    {formatAmount(entry.amount, entry.unitSymbol, entry.unitCode)}
                </span>
            </div>

            <p className="city-economy__ledger-copy">{entry.description || "No extra narration for this household movement."}</p>

            <div className="city-economy__ledger-tags">
                <span className="city-economy__chip">{humanize(entry.source)}</span>
                {entry.referenceCode ? (
                    <span className="city-economy__chip city-economy__chip--muted">
                        Ref {entry.referenceCode}
                    </span>
                ) : null}
            </div>
        </article>
    );
}

function BusinessRoster({
                            businesses,
                            selectedBusinessId,
                            onSelect,
                        }: {
    businesses: CityBusinessDto[];
    selectedBusinessId: string;
    onSelect: (businessId: string) => void;
}) {
    return (
        <div className="city-economy__roster-list">
            {businesses.map((business) => {
                const isSelected = selectedBusinessId === business.businessId;

                return (
                    <article
                        key={business.businessId}
                        className={`city-economy__roster-item${isSelected ? " city-economy__roster-item--selected" : ""}`}
                    >
                        <div className="city-economy__roster-copy">
                            <div className="city-economy__roster-title-row">
                                <strong>{business.name}</strong>
                                <span className="city-economy__chip">{humanize(business.kind)}</span>
                            </div>

                            <div className="city-economy__roster-facts">
                                <span>Balance {formatAmount(business.balance, business.unitSymbol, business.unitCode)}</span>
                                <span>Tax reserve {formatAmount(business.taxReserve, business.unitSymbol, business.unitCode)}</span>
                                <span>Turnover {formatAmount(business.totalRetailTurnover, business.unitSymbol, business.unitCode)}</span>
                            </div>
                        </div>

                        <Button
                            type="button"
                            size="sm"
                            variant={isSelected ? "success" : "default"}
                            disabled={isSelected}
                            onClick={() => onSelect(business.businessId)}
                        >
                            {isSelected ? "Inspecting" : "Inspect ledger"}
                        </Button>
                    </article>
                );
            })}
        </div>
    );
}

function HouseholdRoster({
                             householdAccounts,
                             selectedHouseholdAccountId,
                             onSelect,
                         }: {
    householdAccounts: CityHouseholdAccountDto[];
    selectedHouseholdAccountId: string;
    onSelect: (householdAccountId: string) => void;
}) {
    return (
        <div className="city-economy__roster-list">
            {householdAccounts.map((householdAccount) => {
                const isSelected = selectedHouseholdAccountId === householdAccount.householdAccountId;

                return (
                    <article
                        key={householdAccount.householdAccountId}
                        className={`city-economy__roster-item${isSelected ? " city-economy__roster-item--selected" : ""}`}
                    >
                        <div className="city-economy__roster-copy">
                            <div className="city-economy__roster-title-row">
                                <strong>{householdAccount.name}</strong>
                                {householdAccount.externalReferenceCode ? (
                                    <span className="city-economy__chip city-economy__chip--muted">
                                        {householdAccount.externalReferenceCode}
                                    </span>
                                ) : null}
                            </div>

                            <div className="city-economy__roster-facts">
                                <span>Balance {formatAmount(householdAccount.balance, householdAccount.unitSymbol, householdAccount.unitCode)}</span>
                                <span>Payroll {formatAmount(householdAccount.totalPayrollIncome, householdAccount.unitSymbol, householdAccount.unitCode)}</span>
                                <span>Spending {formatAmount(householdAccount.totalConsumerSpending, householdAccount.unitSymbol, householdAccount.unitCode)}</span>
                            </div>
                        </div>

                        <Button
                            type="button"
                            size="sm"
                            variant={isSelected ? "success" : "default"}
                            disabled={isSelected}
                            onClick={() => onSelect(householdAccount.householdAccountId)}
                        >
                            {isSelected ? "Inspecting" : "Inspect ledger"}
                        </Button>
                    </article>
                );
            })}
        </div>
    );
}

export function CityEconomyCard({
                                    cityId,
                                    cityName,
                                    isArchived = false,
                                }: Props) {
    const [searchParams, setSearchParams] = useSearchParams();
    const {can} = usePermissions();
    const canReadBudget = can(PermissionKeys.EconomyBudgetRead);
    const canReadBusinesses = can(PermissionKeys.EconomyBusinessesRead);
    const canReadHouseholds = can(PermissionKeys.EconomyHouseholdAccountsRead);
    const hasAnyEconomyAccess = canReadBudget || canReadBusinesses || canReadHouseholds;
    const searchParamsKey = searchParams.toString();
    const requestedEconomyView = searchParams.get("economyView");
    const requestedBusinessId = searchParams.get("businessId") ?? "";
    const requestedHouseholdAccountId = searchParams.get("householdAccountId") ?? "";
    const activeEconomyView: EconomyWorkspaceView = isEconomyWorkspaceView(requestedEconomyView)
        ? requestedEconomyView
        : "all";

    const overviewQuery = useCityEconomyWorkspace(cityId, {
        includeBudget: canReadBudget,
        includeBusinesses: canReadBusinesses,
        includeHouseholds: canReadHouseholds,
    });

    const activeBusinessId = canReadBusinesses && overviewQuery.businesses.some((business) => business.businessId === requestedBusinessId)
        ? requestedBusinessId
        : canReadBusinesses
            ? overviewQuery.businesses[0]?.businessId ?? ""
            : "";
    const activeHouseholdAccountId = canReadHouseholds && overviewQuery.householdAccounts.some((account) => account.householdAccountId === requestedHouseholdAccountId)
        ? requestedHouseholdAccountId
        : canReadHouseholds
            ? overviewQuery.householdAccounts[0]?.householdAccountId ?? ""
            : "";
    const showBudget = canReadBudget && (activeEconomyView === "all" || activeEconomyView === "budget");
    const showBusinesses = canReadBusinesses && (activeEconomyView === "all" || activeEconomyView === "businesses");
    const showHouseholds = canReadHouseholds && (activeEconomyView === "all" || activeEconomyView === "households");
    const budgetFeed = useBudgetLedgerFeed(cityId, {
        enabled: showBudget,
        pageSize: BUDGET_LEDGER_PAGE_SIZE,
    });
    const businessFeed = useBusinessLedgerFeed(activeBusinessId || null, {
        enabled: showBusinesses && Boolean(activeBusinessId),
        pageSize: ENTITY_LEDGER_PAGE_SIZE,
    });
    const householdFeed = useHouseholdAccountLedgerFeed(activeHouseholdAccountId || null, {
        enabled: showHouseholds && Boolean(activeHouseholdAccountId),
        pageSize: ENTITY_LEDGER_PAGE_SIZE,
    });
    const selectedBusiness = activeBusinessId
        ? overviewQuery.businesses.find((business) => business.businessId === activeBusinessId) ?? null
        : null;
    const selectedHouseholdAccount = activeHouseholdAccountId
        ? overviewQuery.householdAccounts.find((account) => account.householdAccountId === activeHouseholdAccountId) ?? null
        : null;
    const isWorkspaceInitialLoading =
        overviewQuery.isLoading &&
        !overviewQuery.budgetSummary &&
        !overviewQuery.budgetPressure &&
        overviewQuery.businesses.length === 0 &&
        overviewQuery.householdAccounts.length === 0;

    useEffect(() => {
        const normalizedEconomyView =
            activeEconomyView === "all" ||
            (activeEconomyView === "budget" && !canReadBudget) ||
            (activeEconomyView === "businesses" && !canReadBusinesses) ||
            (activeEconomyView === "households" && !canReadHouseholds)
                ? null
                : activeEconomyView;
        const nextSearchParams = buildEconomySearchParams(searchParams, {
            economyView: normalizedEconomyView,
            businessId: canReadBusinesses ? activeBusinessId : null,
            householdAccountId: canReadHouseholds ? activeHouseholdAccountId : null,
        });

        if (nextSearchParams.toString() !== searchParamsKey) {
            setSearchParams(nextSearchParams, {replace: true});
        }
    }, [
        activeBusinessId,
        activeEconomyView,
        activeHouseholdAccountId,
        canReadBudget,
        canReadBusinesses,
        canReadHouseholds,
        searchParams,
        searchParamsKey,
        setSearchParams,
    ]);

    function handleEconomyViewChange(nextView: EconomyWorkspaceView) {
        const nextSearchParams = buildEconomySearchParams(searchParams, {
            economyView: nextView === "all" ? null : nextView,
        });
        setSearchParams(nextSearchParams, {replace: false});
    }

    function handleSelectBusiness(businessId: string) {
        const nextSearchParams = buildEconomySearchParams(searchParams, {
            businessId,
            economyView: activeEconomyView === "households" ? "businesses" : activeEconomyView === "all" ? null : activeEconomyView,
        });
        setSearchParams(nextSearchParams, {replace: false});
    }

    function handleSelectHouseholdAccount(householdAccountId: string) {
        const nextSearchParams = buildEconomySearchParams(searchParams, {
            householdAccountId,
            economyView: activeEconomyView === "businesses" ? "households" : activeEconomyView === "all" ? null : activeEconomyView,
        });
        setSearchParams(nextSearchParams, {replace: false});
    }

    function refreshWorkspace() {
        overviewQuery.refetch();
        budgetFeed.reset();
        businessFeed.reset();
        householdFeed.reset();
    }

    if (!hasAnyEconomyAccess) {
        return (
            <Card
                title="Economy"
                subtitle="Budget, business, and household financial signals for the current city."
            >
                <div className="city-state-banner city-state-banner--archived">
                    <div className="city-state-banner__title">Economy workspace is unavailable</div>
                    <div className="city-state-banner__text">
                        Your current role does not include economy read access for this city host.
                    </div>
                </div>
            </Card>
        );
    }

    return (
        <div className="city-economy">
            <Card
                title="Economic posture"
                subtitle="Budget pressure, revenue mix, and entity coverage for the active city economy."
                right={(
                    <Button
                        size="sm"
                        onClick={refreshWorkspace}
                        disabled={overviewQuery.isLoading}
                    >
                        {overviewQuery.isRefreshing ? "Refreshing..." : overviewQuery.isLoading ? "Loading..." : "Refresh"}
                    </Button>
                )}
            >
                {isWorkspaceInitialLoading ? (
                    <div className="city-economy__empty" role="status">
                        <strong>Loading city economy</strong>
                        <span>Pulling budget posture, business network, and household coverage into the workspace.</span>
                    </div>
                ) : null}

                {canReadBudget && overviewQuery.budgetError ? (
                    <div className="simulationcore-error-banner" role="alert">
                        <span>{overviewQuery.budgetError}</span>
                    </div>
                ) : null}

                {canReadBudget && overviewQuery.budgetSummary ? (
                    <section className="city-economy__hero">
                        <div className="city-economy__hero-copy">
                            <div className="city-economy__hero-topline">
                                <span className="city-economy__eyebrow">City treasury</span>
                                <span className="city-economy__chip">
                                    {isArchived ? "Archived snapshot" : cityName ? `${cityName} live budget` : "Live budget"}
                                </span>
                            </div>

                            <h3 className="city-economy__hero-balance">
                                {formatAmount(
                                    overviewQuery.budgetSummary.balance,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}
                            </h3>

                            <p className="city-economy__hero-summary">
                                {overviewQuery.budgetPressure
                                    ? `Pressure index ${overviewQuery.budgetPressure.pressureIndex.toFixed(2)} with ${humanize(overviewQuery.budgetPressure.generalAuthorizationLevel)} authorization currently active.`
                                    : "Live budget posture is available, but no operational-pressure snapshot has been reported yet."}
                            </p>
                        </div>

                        <div className="city-economy__hero-side">
                            <span className="city-economy__hero-side-label">Pressure phase</span>
                            <strong className="city-economy__hero-side-value">
                                {overviewQuery.budgetPressure
                                    ? humanize(overviewQuery.budgetPressure.effectivePhase)
                                    : "Awaiting pressure"}
                            </strong>
                            <span className="city-economy__hero-side-note">
                                {overviewQuery.budgetPressure?.effectiveAtUtc
                                    ? `Updated ${formatDateTime(overviewQuery.budgetPressure.effectiveAtUtc)}`
                                    : "No pressure timestamp yet"}
                            </span>
                        </div>
                    </section>
                ) : null}

                <section className="city-economy__focus-strip" aria-label="Economy focus filters">
                    <Button
                        type="button"
                        size="sm"
                        variant={activeEconomyView === "all" ? "primary" : "default"}
                        onClick={() => handleEconomyViewChange("all")}
                    >
                        All surfaces
                    </Button>

                    {canReadBudget ? (
                        <Button
                            type="button"
                            size="sm"
                            variant={activeEconomyView === "budget" ? "primary" : "default"}
                            onClick={() => handleEconomyViewChange("budget")}
                        >
                            Budget ledger
                        </Button>
                    ) : null}

                    {canReadBusinesses ? (
                        <Button
                            type="button"
                            size="sm"
                            variant={activeEconomyView === "businesses" ? "primary" : "default"}
                            onClick={() => handleEconomyViewChange("businesses")}
                        >
                            Businesses ({overviewQuery.businesses.length})
                        </Button>
                    ) : null}

                    {canReadHouseholds ? (
                        <Button
                            type="button"
                            size="sm"
                            variant={activeEconomyView === "households" ? "primary" : "default"}
                            onClick={() => handleEconomyViewChange("households")}
                        >
                            Households ({overviewQuery.householdAccounts.length})
                        </Button>
                    ) : null}
                </section>

                <section className="city-economy__metric-grid">
                    {canReadBudget && overviewQuery.budgetSummary ? (
                        <>
                            <MetricTile
                                label="Tax intake"
                                value={formatAmount(
                                    overviewQuery.budgetSummary.totalTaxIncome,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}
                                note={`Income ${formatAmount(
                                    overviewQuery.budgetSummary.totalIncomeTaxIncome,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )} / Sales ${formatAmount(
                                    overviewQuery.budgetSummary.totalSalesTaxIncome,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}`}
                                tone="success"
                            />
                            <MetricTile
                                label="Direct revenue"
                                value={formatAmount(
                                    overviewQuery.budgetSummary.totalDirectRevenue,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}
                                note={`City expenses ${formatAmount(
                                    overviewQuery.budgetSummary.totalCityExpenses,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}`}
                                tone={overviewQuery.budgetSummary.totalDirectRevenue >= overviewQuery.budgetSummary.totalCityExpenses ? "success" : "warning"}
                            />
                            <MetricTile
                                label="Retail turnover"
                                value={formatAmount(
                                    overviewQuery.budgetSummary.totalRetailTurnover,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}
                                note={`Gross payroll ${formatAmount(
                                    overviewQuery.budgetSummary.totalGrossPayroll,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}`}
                            />
                            <MetricTile
                                label="Net payroll"
                                value={formatAmount(
                                    overviewQuery.budgetSummary.totalNetPayroll,
                                    overviewQuery.budgetSummary.unitSymbol,
                                    overviewQuery.budgetSummary.unitCode,
                                )}
                                note={overviewQuery.budgetPressure
                                    ? `Ops available ${formatAmount(
                                        overviewQuery.budgetPressure.operationsAvailableAmount,
                                        overviewQuery.budgetPressure.unitSymbol,
                                        overviewQuery.budgetPressure.unitCode,
                                    )}`
                                    : "Awaiting pressure envelope"}
                            />
                        </>
                    ) : null}

                    {canReadBudget && overviewQuery.budgetPressure ? (
                        <>
                            <MetricTile
                                label="Pressure index"
                                value={overviewQuery.budgetPressure.pressureIndex.toFixed(2)}
                                note={`General ${humanize(overviewQuery.budgetPressure.generalAuthorizationLevel)}`}
                                tone={getPressureTone(overviewQuery.budgetPressure.pressureIndex)}
                            />
                            <MetricTile
                                label="Emergency spend"
                                value={formatAmount(
                                    overviewQuery.budgetPressure.emergencyOperationsExpenses,
                                    overviewQuery.budgetPressure.unitSymbol,
                                    overviewQuery.budgetPressure.unitCode,
                                )}
                                note={`Municipal ${formatAmount(
                                    overviewQuery.budgetPressure.municipalOperationsExpenses,
                                    overviewQuery.budgetPressure.unitSymbol,
                                    overviewQuery.budgetPressure.unitCode,
                                )}`}
                                tone={overviewQuery.budgetPressure.emergencyOperationsExpenses > 0 ? "warning" : "default"}
                            />
                        </>
                    ) : null}

                    {canReadBusinesses ? (
                        <MetricTile
                            label="Businesses"
                            value={String(overviewQuery.businesses.length)}
                            note={selectedBusiness ? `${selectedBusiness.name} selected` : "No business selected yet"}
                        />
                    ) : null}

                    {canReadHouseholds ? (
                        <MetricTile
                            label="Household accounts"
                            value={String(overviewQuery.householdAccounts.length)}
                            note={selectedHouseholdAccount ? `${selectedHouseholdAccount.name} selected` : "No household account selected yet"}
                        />
                    ) : null}
                </section>
            </Card>

            {showBudget ? (
                <LedgerFeedPanel
                    title="City budget ledger"
                    subtitle="Latest municipal budget movements in stable cursor order."
                    entries={budgetFeed.items}
                    error={budgetFeed.error}
                    isLoadingInitial={budgetFeed.isLoadingInitial}
                    isLoadingMore={budgetFeed.isLoadingMore}
                    hasNext={budgetFeed.hasNext}
                    onLoadMore={() => {
                        void budgetFeed.loadMore();
                    }}
                    emptyTitle="No budget activity yet"
                    emptyText="The city budget ledger will appear here once bootstrap or runtime budget events are recorded."
                    renderEntry={(entry) => <BudgetLedgerEntryRow key={entry.entryId} entry={entry}/>}
                    right={(
                        <span className="city-economy__chip city-economy__chip--muted">
                            {budgetFeed.items.length} loaded
                        </span>
                    )}
                />
            ) : null}

            <div className="city-economy__workspace-grid">
                {showBusinesses ? (
                    <Card
                        title="Businesses"
                        subtitle="Registered city businesses available for cursor-based ledger inspection."
                        right={(
                            <span className="city-economy__chip city-economy__chip--muted">
                                {overviewQuery.businesses.length}
                            </span>
                        )}
                    >
                        {overviewQuery.businessesError ? (
                            <div className="simulationcore-error-banner" role="alert">
                                <span>{overviewQuery.businessesError}</span>
                            </div>
                        ) : null}

                        {overviewQuery.isLoading && overviewQuery.businesses.length === 0 && !overviewQuery.businessesError ? (
                            <div className="city-economy__empty" role="status">
                                <strong>Loading businesses</strong>
                                <span>Pulling the current business operator network for this city.</span>
                            </div>
                        ) : null}

                        {overviewQuery.businesses.length > 0 ? (
                            <BusinessRoster
                                businesses={overviewQuery.businesses}
                                selectedBusinessId={activeBusinessId}
                                onSelect={handleSelectBusiness}
                            />
                        ) : !overviewQuery.isLoading ? (
                            <div className="city-economy__empty" role="status">
                                <strong>No businesses registered yet</strong>
                                <span>Business ledgers will light up here once the city economy starts registering operators.</span>
                            </div>
                        ) : null}
                    </Card>
                ) : null}

                {showBusinesses ? (
                    <LedgerFeedPanel
                        title={selectedBusiness ? `${selectedBusiness.name} ledger` : "Business ledger"}
                        subtitle={selectedBusiness
                            ? `Live feed for ${humanize(selectedBusiness.kind)} business movements.`
                            : "Select a business on the left to inspect its ledger."}
                        entries={businessFeed.items}
                        error={businessFeed.error}
                        isLoadingInitial={businessFeed.isLoadingInitial}
                        isLoadingMore={businessFeed.isLoadingMore}
                        hasNext={businessFeed.hasNext}
                        onLoadMore={() => {
                            void businessFeed.loadMore();
                        }}
                        emptyTitle={selectedBusiness ? "No business movements yet" : "No business selected"}
                        emptyText={selectedBusiness
                            ? "This business does not have ledger movements yet."
                            : "Choose a business from the roster to load its cursor feed."}
                        renderEntry={(entry) => <BusinessLedgerEntryRow key={entry.entryId} entry={entry}/>}
                        right={selectedBusiness ? (
                            <span className="city-economy__chip">
                                {humanize(selectedBusiness.kind)}
                            </span>
                        ) : undefined}
                    />
                ) : null}

                {showHouseholds ? (
                    <Card
                        title="Household accounts"
                        subtitle="Household money surfaces available for direct ledger inspection."
                        right={(
                            <span className="city-economy__chip city-economy__chip--muted">
                                {overviewQuery.householdAccounts.length}
                            </span>
                        )}
                    >
                        {overviewQuery.householdAccountsError ? (
                            <div className="simulationcore-error-banner" role="alert">
                                <span>{overviewQuery.householdAccountsError}</span>
                            </div>
                        ) : null}

                        {overviewQuery.isLoading && overviewQuery.householdAccounts.length === 0 && !overviewQuery.householdAccountsError ? (
                            <div className="city-economy__empty" role="status">
                                <strong>Loading household accounts</strong>
                                <span>Pulling current household money surfaces for this city.</span>
                            </div>
                        ) : null}

                        {overviewQuery.householdAccounts.length > 0 ? (
                            <HouseholdRoster
                                householdAccounts={overviewQuery.householdAccounts}
                                selectedHouseholdAccountId={activeHouseholdAccountId}
                                onSelect={handleSelectHouseholdAccount}
                            />
                        ) : !overviewQuery.isLoading ? (
                            <div className="city-economy__empty" role="status">
                                <strong>No household accounts registered yet</strong>
                                <span>Household ledgers will appear here once residents start participating in the city economy.</span>
                            </div>
                        ) : null}
                    </Card>
                ) : null}

                {showHouseholds ? (
                    <LedgerFeedPanel
                        title={selectedHouseholdAccount ? `${selectedHouseholdAccount.name} ledger` : "Household ledger"}
                        subtitle={selectedHouseholdAccount
                            ? "Cursor-based household balance changes, newest first."
                            : "Select a household account on the left to inspect its ledger."}
                        entries={householdFeed.items}
                        error={householdFeed.error}
                        isLoadingInitial={householdFeed.isLoadingInitial}
                        isLoadingMore={householdFeed.isLoadingMore}
                        hasNext={householdFeed.hasNext}
                        onLoadMore={() => {
                            void householdFeed.loadMore();
                        }}
                        emptyTitle={selectedHouseholdAccount ? "No household movements yet" : "No household account selected"}
                        emptyText={selectedHouseholdAccount
                            ? "This household account does not have ledger movements yet."
                            : "Choose a household account from the roster to load its cursor feed."}
                        renderEntry={(entry) => <HouseholdLedgerEntryRow key={entry.entryId} entry={entry}/>}
                        right={selectedHouseholdAccount?.externalReferenceCode ? (
                            <span className="city-economy__chip city-economy__chip--muted">
                                {selectedHouseholdAccount.externalReferenceCode}
                            </span>
                        ) : undefined}
                    />
                ) : null}
            </div>
        </div>
    );
}
