import type {
    CityPopulationBootstrapView,
    CityProvisioningStatusView,
} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

export type BootstrapOutcome = "completed" | "failed" | "skipped" | "pending";

export function formatProvisioningDateTime(value?: string | null): string {
    if (!value) {
        return "--";
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

export function getBootstrapOutcome(
    bootstrap?: CityPopulationBootstrapView | null,
    provisioning?: CityProvisioningStatusView | null,
): BootstrapOutcome {
    const bootstrapStatus = bootstrap?.status?.toLowerCase();
    const cityStatus = provisioning?.status?.toLowerCase();

    if (cityStatus === "provisioning") {
        return "pending";
    }

    if (bootstrapStatus === "completed" || cityStatus === "active") {
        return "completed";
    }

    if (bootstrapStatus === "failed" || cityStatus === "provisioningfailed") {
        return "failed";
    }

    if (bootstrapStatus === "skipped") {
        return "skipped";
    }

    return "pending";
}
