import {useEffect, useRef, useState} from "react";
import {Link, useNavigate, useParams} from "react-router-dom";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Button from "@shared/ui/controls/Button/Button";
import {
    createClassicCitySetupSession,
    getClassicCitySetupSession,
    launchClassicCitySetupSession,
    updateClassicCitySetupSession,
} from "@services/simulationcore/scenarios/classic-city/api/setupSessionsApi";
import type {
    ClassicCityEconomyProfile,
    ClassicCityInitialWeatherMode,
    ClassicCityInitialWeatherSeverity,
    ClassicCityInitialWeatherType,
    ClassicCityPopulationOccupancyProfile,
    ClassicCityPopulationTargetMode,
    ClassicCitySetupDraftView,
    ClassicCitySetupSessionView,
    ClassicCitySetupStepId,
} from "@services/simulationcore/scenarios/classic-city/contracts/setupSessionContracts";
import {
    CLASSIC_CITY_CLIMATE_OPTIONS,
    CLASSIC_CITY_DENSITY_OPTIONS,
    CLASSIC_CITY_DEVELOPMENT_OPTIONS,
    CLASSIC_CITY_ECONOMY_PROFILE_OPTIONS,
    CLASSIC_CITY_FORM_PRESET_OPTIONS,
    CLASSIC_CITY_HEMISPHERE_OPTIONS,
    CLASSIC_CITY_INITIAL_WEATHER_MODE_OPTIONS,
    CLASSIC_CITY_INITIAL_WEATHER_SEVERITY_OPTIONS,
    CLASSIC_CITY_INITIAL_WEATHER_TYPE_OPTIONS,
    CLASSIC_CITY_POPULATION_OCCUPANCY_OPTIONS,
    CLASSIC_CITY_POPULATION_TARGET_OPTIONS,
    CLASSIC_CITY_SIZE_TIER_OPTIONS,
    type SetupOption,
} from "@services/simulationcore/scenarios/classic-city/setupOptions";
import {getNowLocalDateTimeInputValue, localDateTimeToUtcIso,} from "@services/simulationcore/simulation/utils/dateTime";
import {
    buildPopulationPlanningEstimate,
    formatOccupancyRateRange,
    formatRange,
    getPopulationPressureLabel,
    getPopulationTargetModeLabel,
    hasMeaningfulRangeValue,
} from "@services/simulationcore/scenarios/classic-city/utils/populationPlanning";
import {
    SIMULATIONCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_SCENARIO,
    getClassicCitySetupProvisioningPath,
    getClassicCitySetupSessionPath,
} from "@services/simulationcore/scenarios/registry";
import "@services/simulationcore/scenarios/styles/scenario-setup.css";

type ValidationErrors = {
    name?: string;
    startSimTimeLocal?: string;
    speedMultiplier?: string;
    utcOffsetMinutes?: string;
    initialWeatherTemperatureC?: string;
    plannedPeopleCount?: string;
};

type SetupDraft = ClassicCitySetupDraftView;
type CityFormPresetValue =
    "CompactGrid"
    | "BalancedDistricts"
    | "VerticalCore"
    | "SprawlingSuburbs"
    | "PressureCooker"
    | "Custom";

type SessionSnapshot = {
    currentStepId: ClassicCitySetupStepId;
    draft: SetupDraft;
};

type OptionGridProps = {
    legend: string;
    options: SetupOption[];
    selectedValue: string;
    onSelect: (value: string) => void;
    disabled?: boolean;
};

const setupSteps: { id: ClassicCitySetupStepId; title: string; description: string }[] = [
    {
        id: "scenario",
        title: "Scenario",
        description: "Choose the simulation baseline and what the launch flow will provision.",
    },
    {
        id: "population",
        title: "Population",
        description: "Pick the opening headcount first, then let SimulationCore build a city form around that launch target.",
    },
    {
        id: "profile",
        title: "City profile",
        description: "Shape how the city should feel once topology is generated around the requested launch population.",
    },
    {
        id: "environment",
        title: "Environment",
        description: "Set the climate context that will drive weather planning and downstream bootstrap.",
    },
    {
        id: "launch",
        title: "Launch review",
        description: "Verify the launch contract before handing the setup off to backend provisioning.",
    },
];

const mutableSessionStatuses = new Set(["Draft", "LaunchFailed"]);
const runningSessionStatuses = new Set(["LaunchQueued", "CreatingCity", "BootstrappingPopulation"]);
const MAX_PLANNED_PEOPLE_COUNT = 1_000_000;
const MANUAL_WEATHER_TEMPERATURE_BY_CLIMATE: Record<string, string> = {
    Tropical: "28",
    Temperate: "12",
    Continental: "10",
    Arid: "22",
    Mountain: "6",
    Polar: "-12",
};

function createRandomGenerationSeed(): string {
    const raw = globalThis.crypto?.randomUUID?.().replace(/-/g, "")
        ?? `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 12)}`;

    return `cc-${raw.slice(0, 16)}`;
}

function formatGenerationSeedPreview(generationSeed: string): string {
    const normalized = generationSeed.trim();

    if (normalized.length <= 22) {
        return normalized;
    }

    return `${normalized.slice(0, 12)}...${normalized.slice(-8)}`;
}

function createDefaultDraft(): SetupDraft {
    const startSimTimeLocal = getNowLocalDateTimeInputValue();

    return {
        name: "",
        startSimTimeLocal,
        startSimTimeUtc: localDateTimeToUtcIso(startSimTimeLocal),
        speedMultiplier: "1",
        climateZone: "Temperate",
        hemisphere: "Northern",
        utcOffsetMinutes: String(-new Date().getTimezoneOffset()),
        generationSeed: createRandomGenerationSeed(),
        initialWeatherMode: "Random",
        initialWeatherType: "Clear",
        initialWeatherSeverity: "Mild",
        initialWeatherTemperatureC: getSuggestedManualWeatherTemperature("Temperate"),
        populationTargetMode: "Preset10K",
        sizeTier: "Medium",
        urbanDensity: "Balanced",
        developmentLevel: "Balanced",
        economyProfile: "Balanced",
        populationOccupancyProfile: "Balanced",
        plannedPeopleCount: "",
    };
}

function normalizeDraft(draft: SetupDraft): SetupDraft {
    return {
        ...draft,
        startSimTimeUtc: draft.startSimTimeUtc ?? localDateTimeToUtcIso(draft.startSimTimeLocal),
        initialWeatherMode: normalizeInitialWeatherMode(draft.initialWeatherMode),
        initialWeatherType: normalizeInitialWeatherType(draft.initialWeatherType),
        initialWeatherSeverity: normalizeInitialWeatherSeverity(draft.initialWeatherSeverity),
        initialWeatherTemperatureC: draft.initialWeatherTemperatureC?.trim() ?? "",
        populationTargetMode: normalizePopulationTargetMode(draft.populationTargetMode, draft.plannedPeopleCount, draft.sizeTier),
        economyProfile: normalizeEconomyProfile(draft.economyProfile),
        populationOccupancyProfile: normalizePopulationOccupancyProfile(draft.populationOccupancyProfile),
        plannedPeopleCount: draft.plannedPeopleCount?.trim() ?? "",
    };
}

function createSnapshotSignature(snapshot: SessionSnapshot): string {
    return JSON.stringify({
        currentStepId: snapshot.currentStepId,
        draft: normalizeDraft(snapshot.draft),
    });
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

function validateProfile(draft: SetupDraft): ValidationErrors {
    const errors: ValidationErrors = {};

    if (!draft.name.trim()) {
        errors.name = "City name is required.";
    }

    if (!draft.startSimTimeLocal.trim()) {
        errors.startSimTimeLocal = "Start simulation time is required.";
    } else if (!localDateTimeToUtcIso(draft.startSimTimeLocal)) {
        errors.startSimTimeLocal = "Invalid date/time value.";
    }

    const speed = Number(draft.speedMultiplier);
    if (!draft.speedMultiplier.trim()) {
        errors.speedMultiplier = "Speed multiplier is required.";
    } else if (!Number.isFinite(speed)) {
        errors.speedMultiplier = "Speed multiplier must be a number.";
    } else if (speed <= 0) {
        errors.speedMultiplier = "Speed multiplier must be greater than 0.";
    }

    return errors;
}

function validateEnvironment(draft: SetupDraft): ValidationErrors {
    const errors: ValidationErrors = {};
    const utcOffsetMinutes = Number(draft.utcOffsetMinutes);

    if (!draft.utcOffsetMinutes.trim()) {
        errors.utcOffsetMinutes = "UTC offset is required.";
    } else if (!Number.isInteger(utcOffsetMinutes)) {
        errors.utcOffsetMinutes = "UTC offset must be a whole number of minutes.";
    } else if (utcOffsetMinutes < -14 * 60 || utcOffsetMinutes > 14 * 60) {
        errors.utcOffsetMinutes = "UTC offset must stay between -840 and 840 minutes.";
    }

    if (draft.initialWeatherMode === "Manual" && draft.initialWeatherTemperatureC.trim()) {
        const initialWeatherTemperatureC = Number(draft.initialWeatherTemperatureC);

        if (!Number.isFinite(initialWeatherTemperatureC)) {
            errors.initialWeatherTemperatureC = "Initial weather temperature must be a number.";
        } else if (initialWeatherTemperatureC < -100 || initialWeatherTemperatureC > 80) {
            errors.initialWeatherTemperatureC = "Initial weather temperature must stay between -100 and 80 Celsius.";
        }
    }

    return errors;
}

function validatePopulation(draft: SetupDraft): ValidationErrors {
    const errors: ValidationErrors = {};

    if (draft.populationTargetMode !== "Manual") {
        return errors;
    }

    const plannedPeopleCount = Number(draft.plannedPeopleCount);

    if (!draft.plannedPeopleCount.trim()) {
        errors.plannedPeopleCount = "Exact resident target is required when override is enabled.";
    } else if (!Number.isInteger(plannedPeopleCount)) {
        errors.plannedPeopleCount = "Planned people count must be a whole number.";
    } else if (plannedPeopleCount < 0) {
        errors.plannedPeopleCount = "Planned people count cannot be negative.";
    } else if (plannedPeopleCount > MAX_PLANNED_PEOPLE_COUNT) {
        errors.plannedPeopleCount = `Planned people count must stay below ${MAX_PLANNED_PEOPLE_COUNT.toLocaleString()}.`;
    }

    return errors;
}

function mergeErrors(...items: ValidationErrors[]): ValidationErrors {
    return Object.assign({}, ...items);
}

function getStepIndex(stepId: ClassicCitySetupStepId): number {
    return Math.max(0, setupSteps.findIndex((step) => step.id === stepId));
}

function hasReachedStep(currentStepIndex: number, stepId: ClassicCitySetupStepId): boolean {
    return currentStepIndex >= getStepIndex(stepId);
}

function formatUtcOffset(minutesText: string): string {
    const minutes = Number(minutesText);
    if (!Number.isFinite(minutes)) {
        return "--";
    }

    const sign = minutes >= 0 ? "+" : "-";
    const absoluteMinutes = Math.abs(minutes);
    const hours = Math.floor(absoluteMinutes / 60)
        .toString()
        .padStart(2, "0");
    const mins = (absoluteMinutes % 60)
        .toString()
        .padStart(2, "0");

    return `UTC ${sign}${hours}:${mins}`;
}

function formatDateTime(value?: string | null): string {
    if (!value) {
        return "--";
    }

    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

function normalizePopulationOccupancyProfile(value?: string): ClassicCityPopulationOccupancyProfile {
    return value === "Light" || value === "High" ? value : "Balanced";
}

function normalizeEconomyProfile(value?: string): ClassicCityEconomyProfile {
    return value === "Struggling" || value === "Affluent" ? value : "Balanced";
}

function normalizeInitialWeatherMode(value?: string): ClassicCityInitialWeatherMode {
    return value === "Manual" ? "Manual" : "Random";
}

function normalizeInitialWeatherType(value?: string): ClassicCityInitialWeatherType {
    switch (value) {
        case "Overcast":
        case "Rain":
        case "Snow":
        case "Storm":
        case "Fog":
        case "Windy":
        case "Heatwave":
        case "ColdSnap":
            return value;
        default:
            return "Clear";
    }
}

function normalizeInitialWeatherSeverity(value?: string): ClassicCityInitialWeatherSeverity {
    switch (value) {
        case "Calm":
        case "Moderate":
        case "Severe":
        case "Extreme":
            return value;
        default:
            return "Mild";
    }
}

function getSuggestedManualWeatherTemperature(climateZone: string): string {
    return MANUAL_WEATHER_TEMPERATURE_BY_CLIMATE[climateZone] ?? MANUAL_WEATHER_TEMPERATURE_BY_CLIMATE.Temperate;
}

function normalizePopulationTargetMode(
    value: string | undefined,
    plannedPeopleCount: string | undefined,
    sizeTier: string | undefined,
): ClassicCityPopulationTargetMode {
    switch (value) {
        case "Random":
        case "Preset1K":
        case "Preset10K":
        case "Preset100K":
        case "Manual":
            return value;
        default:
            if ((plannedPeopleCount?.trim().length ?? 0) > 0) {
                return "Manual";
            }

            return sizeTier === "Small"
                ? "Preset1K"
                : sizeTier === "Large"
                    ? "Preset100K"
                    : "Preset10K";
    }
}

function getPopulationPlanLabel(
    draft: SetupDraft,
    targetPopulation: number | null,
): string {
    return getPopulationTargetModeLabel(draft.populationTargetMode, targetPopulation);
}

function getPopulationPlanDescription(
    draft: SetupDraft,
    targetPopulation: number | null,
): string {
    if (draft.populationTargetMode === "Manual") {
        return targetPopulation === null
            ? "Manual resident target requires a whole-number headcount."
            : `${targetPopulation.toLocaleString()} residents requested explicitly before topology generation starts.`;
    }

    if (draft.populationTargetMode === "Random") {
        return targetPopulation === null
            ? "Randomized launch target will be derived from the current generation seed."
            : `The current generation seed resolves this launch to ${targetPopulation.toLocaleString()} residents before the city form is generated around that headcount.`;
    }

    return `${getPopulationPressureLabel(draft.populationOccupancyProfile)} will shape how much housing slack or launch pressure the generated city keeps around ${targetPopulation?.toLocaleString() ?? "--"} residents.`;
}

function getInitialWeatherPlanLabel(draft: SetupDraft): string {
    if (draft.initialWeatherMode === "Random") {
        return "Seeded random opening weather";
    }

    const temperature = draft.initialWeatherTemperatureC.trim();
    return temperature
        ? `${draft.initialWeatherType} / ${draft.initialWeatherSeverity} / ${temperature}C`
        : `${draft.initialWeatherType} / ${draft.initialWeatherSeverity}`;
}

function getInitialWeatherPlanDescription(draft: SetupDraft): string {
    if (draft.initialWeatherMode === "Random") {
        return "SimulationCore derives the first weather block from the launch seed, climate zone, hemisphere, and start time, so the same seed plus the same launch contract reproduces the same opening weather.";
    }

    const temperature = draft.initialWeatherTemperatureC.trim();
    return temperature
        ? `SimulationCore will start the city under a manually pinned ${draft.initialWeatherType.toLowerCase()} state at ${temperature}C before normal weather simulation takes over.`
        : `SimulationCore will start the city under a manually pinned ${draft.initialWeatherType.toLowerCase()} state. If no temperature is supplied, it will derive one from the current climate profile.`;
}

function inferCityFormPreset(draft: SetupDraft): CityFormPresetValue {
    if (draft.sizeTier === "Small" && draft.urbanDensity === "Balanced" && draft.developmentLevel === "Balanced") {
        return "CompactGrid";
    }

    if (draft.sizeTier === "Medium" && draft.urbanDensity === "Balanced" && draft.developmentLevel === "Balanced") {
        return "BalancedDistricts";
    }

    if (draft.sizeTier === "Medium" && draft.urbanDensity === "Dense" && draft.developmentLevel === "Advanced") {
        return "VerticalCore";
    }

    if (draft.sizeTier === "Large" && draft.urbanDensity === "Sparse" && draft.developmentLevel === "Balanced") {
        return "SprawlingSuburbs";
    }

    if (draft.sizeTier === "Medium" && draft.urbanDensity === "Dense" && draft.developmentLevel === "Struggling") {
        return "PressureCooker";
    }

    return "Custom";
}

function applyCityFormPreset(
    draft: SetupDraft,
    preset: CityFormPresetValue,
): SetupDraft {
    switch (preset) {
        case "CompactGrid":
            return {
                ...draft,
                sizeTier: "Small",
                urbanDensity: "Balanced",
                developmentLevel: "Balanced",
            };
        case "BalancedDistricts":
            return {
                ...draft,
                sizeTier: "Medium",
                urbanDensity: "Balanced",
                developmentLevel: "Balanced",
            };
        case "VerticalCore":
            return {
                ...draft,
                sizeTier: "Medium",
                urbanDensity: "Dense",
                developmentLevel: "Advanced",
            };
        case "SprawlingSuburbs":
            return {
                ...draft,
                sizeTier: "Large",
                urbanDensity: "Sparse",
                developmentLevel: "Balanced",
            };
        case "PressureCooker":
            return {
                ...draft,
                sizeTier: "Medium",
                urbanDensity: "Dense",
                developmentLevel: "Struggling",
            };
        default:
            return draft;
    }
}

function getCityFormPresetLabel(preset: CityFormPresetValue): string {
    switch (preset) {
        case "CompactGrid":
            return "Compact grid";
        case "BalancedDistricts":
            return "Balanced districts";
        case "VerticalCore":
            return "Vertical core";
        case "SprawlingSuburbs":
            return "Sprawling suburbs";
        case "PressureCooker":
            return "Pressure cooker";
        default:
            return "Custom city form";
    }
}

function getCityFormDescription(draft: SetupDraft): string {
    const preset = inferCityFormPreset(draft);
    const presetOption = CLASSIC_CITY_FORM_PRESET_OPTIONS.find((option) => option.value === preset);

    if (presetOption) {
        return presetOption.description;
    }

    return `${draft.sizeTier} footprint with ${draft.urbanDensity.toLowerCase()} density and ${draft.developmentLevel.toLowerCase()} development.`;
}

function getEconomyProfileOption(profile: ClassicCityEconomyProfile): SetupOption {
    return CLASSIC_CITY_ECONOMY_PROFILE_OPTIONS.find((option) => option.value === profile)
        ?? CLASSIC_CITY_ECONOMY_PROFILE_OPTIONS[1];
}

function formatSessionStatusLabel(status?: string | null): string {
    switch (status) {
        case "Draft":
            return "Draft";
        case "LaunchQueued":
            return "Launch queued";
        case "CreatingCity":
            return "Creating city";
        case "BootstrappingPopulation":
            return "Bootstrapping population";
        case "Ready":
            return "Ready";
        case "ProvisioningFailed":
            return "Provisioning failed";
        case "LaunchFailed":
            return "Launch failed";
        default:
            return "Preparing session";
    }
}

function getSessionStatusTone(status?: string | null): "draft" | "running" | "ready" | "failed" {
    if (status === "Ready") {
        return "ready";
    }

    if (status === "LaunchFailed" || status === "ProvisioningFailed") {
        return "failed";
    }

    if (status && runningSessionStatuses.has(status)) {
        return "running";
    }

    return "draft";
}

function isMutableSessionStatus(status?: string | null): boolean {
    return status ? mutableSessionStatuses.has(status) : false;
}

function isRunningSessionStatus(status?: string | null): boolean {
    return status ? runningSessionStatuses.has(status) : false;
}

function getSessionStatusDescription(session: ClassicCitySetupSessionView | null): string {
    switch (session?.status) {
        case "LaunchQueued":
            return "The launch request is queued in Gateway and will survive page refresh or tab closure.";
        case "CreatingCity":
            return "SimulationCore is creating topology, clock, and initial environment for the requested launch contract.";
        case "BootstrappingPopulation":
            return "Population bootstrap is running downstream. The city will hand off to provisioning as soon as a host id is available.";
        case "LaunchFailed":
            return session.failureMessage ?? "Launch orchestration failed before the city host was created.";
        case "ProvisioningFailed":
            return session.failureMessage ?? "Population bootstrap failed after city creation and requires operator review.";
        case "Ready":
            return "The setup session completed successfully and is ready to hand off to monitoring.";
        default:
            return "Draft changes are saved to a backend setup session so the wizard can be resumed after refresh.";
    }
}

function OptionGrid({legend, options, selectedValue, onSelect, disabled = false}: OptionGridProps) {
    return (
        <div className="scenario-setup__field">
            <div className="scenario-setup__label">{legend}</div>
            <div className="scenario-setup__option-grid">
                {options.map((option) => {
                    const isSelected = option.value === selectedValue;

                    return (
                        <button
                            key={option.value}
                            type="button"
                            className={`scenario-setup__option-card ${isSelected ? "scenario-setup__option-card--selected" : ""}`}
                            onClick={() => onSelect(option.value)}
                            disabled={disabled}
                        >
                            <span className="scenario-setup__option-title">{option.label}</span>
                            <span className="scenario-setup__option-text">{option.description}</span>
                        </button>
                    );
                })}
            </div>
        </div>
    );
}

export default function ClassicCitySetupPage() {
    const params = useParams<{ sessionId?: string }>();
    const routeSessionId = params.sessionId ?? null;
    const navigate = useNavigate();
    const [session, setSession] = useState<ClassicCitySetupSessionView | null>(null);
    const [draft, setDraft] = useState<SetupDraft>(createDefaultDraft);
    const [currentStepId, setCurrentStepId] = useState<ClassicCitySetupStepId>("scenario");
    const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
    const [isInitializing, setIsInitializing] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [isLaunching, setIsLaunching] = useState(false);
    const [isAdvancedProfileOpen, setIsAdvancedProfileOpen] = useState(false);
    const [pageError, setPageError] = useState<string | null>(null);
    const [saveError, setSaveError] = useState<string | null>(null);
    const [lastSavedAtUtc, setLastSavedAtUtc] = useState<string | null>(null);
    const saveTimeoutRef = useRef<number | null>(null);
    const saveAbortRef = useRef<AbortController | null>(null);
    const setupRootRef = useRef<HTMLElement | null>(null);
    const lastSyncedSignatureRef = useRef<string | null>(null);
    const previousStepIdRef = useRef<ClassicCitySetupStepId>("scenario");
    const latestSnapshotRef = useRef<SessionSnapshot>({
        currentStepId: "scenario",
        draft: createDefaultDraft(),
    });

    latestSnapshotRef.current = {
        currentStepId,
        draft,
    };

    const currentStepIndex = getStepIndex(currentStepId);
    const currentStep = setupSteps[currentStepIndex];
    const populationPlanningEstimate = buildPopulationPlanningEstimate(draft);
    const resolvedPopulationTarget = populationPlanningEstimate.targetPopulation;
    const cityFormPreset = inferCityFormPreset(draft);
    const economyProfileOption = getEconomyProfileOption(draft.economyProfile);
    const showAdvancedProfile = isAdvancedProfileOpen || cityFormPreset === "Custom";
    const hasProfileSummary = hasReachedStep(currentStepIndex, "profile");
    const hasEnvironmentSummary = hasReachedStep(currentStepIndex, "environment");
    const hasPopulationSummary = hasReachedStep(currentStepIndex, "population");
    const sessionStatusLabel = formatSessionStatusLabel(session?.status);
    const sessionStatusTone = getSessionStatusTone(session?.status);
    const canEditSession = isMutableSessionStatus(session?.status);
    const isLaunchRunning = isRunningSessionStatus(session?.status);
    const isBusy = isInitializing || isLaunching;

    function adoptSession(nextSession: ClassicCitySetupSessionView, syncLocalState = true) {
        const normalizedDraft = normalizeDraft(nextSession.draft);

        setSession(nextSession);
        setLastSavedAtUtc(nextSession.updatedAtUtc);
        lastSyncedSignatureRef.current = createSnapshotSignature({
            currentStepId: nextSession.currentStepId,
            draft: normalizedDraft,
        });

        if (syncLocalState) {
            setDraft(normalizedDraft);
            setCurrentStepId(nextSession.currentStepId);
        }
    }

    function clearPendingAutosave() {
        if (saveTimeoutRef.current) {
            window.clearTimeout(saveTimeoutRef.current);
            saveTimeoutRef.current = null;
        }
    }

    async function persistDraftNow(force = false): Promise<ClassicCitySetupSessionView | null> {
        if (!session?.sessionId || !canEditSession) {
            return session;
        }

        const snapshot = latestSnapshotRef.current;
        const signature = createSnapshotSignature(snapshot);

        if (!force && signature === lastSyncedSignatureRef.current) {
            return session;
        }

        clearPendingAutosave();
        saveAbortRef.current?.abort();

        const abortController = new AbortController();
        saveAbortRef.current = abortController;

        try {
            setIsSaving(true);
            setSaveError(null);

            const updatedSession = await updateClassicCitySetupSession(
                session.sessionId,
                {
                    currentStepId: snapshot.currentStepId,
                    draft: snapshot.draft,
                },
                abortController.signal,
            );

            if (abortController.signal.aborted) {
                return null;
            }

            const normalizedDraft = normalizeDraft(updatedSession.draft);
            const liveSignature = createSnapshotSignature(latestSnapshotRef.current);

            setSession(updatedSession);
            setLastSavedAtUtc(updatedSession.updatedAtUtc);
            lastSyncedSignatureRef.current = createSnapshotSignature({
                currentStepId: updatedSession.currentStepId,
                draft: normalizedDraft,
            });

            if (liveSignature === signature) {
                setDraft(normalizedDraft);
                setCurrentStepId(updatedSession.currentStepId);
            }

            return updatedSession;
        } catch (error: unknown) {
            if (abortController.signal.aborted) {
                return null;
            }

            setSaveError(getErrorMessage(error, "Failed to save setup session."));
            return null;
        } finally {
            if (!abortController.signal.aborted) {
                setIsSaving(false);
            }

            if (saveAbortRef.current === abortController) {
                saveAbortRef.current = null;
            }
        }
    }

    function updateDraft<K extends keyof SetupDraft>(key: K, value: SetupDraft[K]) {
        setDraft((current) => {
            const next = {
                ...current,
                [key]: value,
            };

            if (key === "startSimTimeLocal") {
                next.startSimTimeUtc = localDateTimeToUtcIso(String(value));
            }

            if (key === "climateZone" &&
                next.initialWeatherMode === "Manual" &&
                !next.initialWeatherTemperatureC.trim()) {
                next.initialWeatherTemperatureC = getSuggestedManualWeatherTemperature(String(value));
            }

            if (key === "initialWeatherMode" &&
                value === "Manual" &&
                !next.initialWeatherTemperatureC.trim()) {
                next.initialWeatherTemperatureC = getSuggestedManualWeatherTemperature(next.climateZone);
            }

            return next;
        });

        setValidationErrors((current) => {
            if ((key === "populationTargetMode" && value !== "Manual") ||
                (key === "initialWeatherMode" && value !== "Manual")) {
                const next = {...current};

                if (key === "populationTargetMode") {
                    delete next.plannedPeopleCount;
                }

                if (key === "initialWeatherMode") {
                    delete next.initialWeatherTemperatureC;
                }

                return next;
            }

            if (!(key in current)) {
                return current;
            }

            const next = {...current};
            delete next[key as keyof ValidationErrors];
            return next;
        });
        setPageError(null);
        setSaveError(null);
    }

    function updateCityFormPreset(preset: CityFormPresetValue) {
        setDraft((current) => applyCityFormPreset(current, preset));
        setIsAdvancedProfileOpen(false);
        setPageError(null);
        setSaveError(null);
    }

    function moveToStep(stepId: ClassicCitySetupStepId) {
        setCurrentStepId(stepId);
        setPageError(null);
        setSaveError(null);
    }

    function goNext() {
        if (currentStep.id === "profile") {
            const errors = validateProfile(draft);
            setValidationErrors(errors);
            if (Object.keys(errors).length > 0) {
                return;
            }
        }

        if (currentStep.id === "environment") {
            const errors = validateEnvironment(draft);
            setValidationErrors(errors);
            if (Object.keys(errors).length > 0) {
                return;
            }
        }

        if (currentStep.id === "population") {
            const errors = validatePopulation(draft);
            setValidationErrors(errors);
            if (Object.keys(errors).length > 0) {
                return;
            }
        }

        moveToStep(setupSteps[Math.min(currentStepIndex + 1, setupSteps.length - 1)].id);
    }

    function goBack() {
        moveToStep(setupSteps[Math.max(currentStepIndex - 1, 0)].id);
    }

    async function handleLaunch() {
        const errors = mergeErrors(
            validateProfile(draft),
            validateEnvironment(draft),
            validatePopulation(draft),
        );
        setValidationErrors(errors);

        if (Object.keys(errors).length > 0) {
            return;
        }

        if (!session?.sessionId) {
            setPageError("Setup session is still being prepared. Please try again.");
            return;
        }

        setPageError(null);
        setSaveError(null);
        setIsLaunching(true);

        try {
            const persisted = await persistDraftNow(true);
            if (!persisted) {
                return;
            }

            const launchedSession = await launchClassicCitySetupSession(session.sessionId);
            adoptSession(launchedSession);
            navigate(getClassicCitySetupProvisioningPath(launchedSession.sessionId), {
                replace: true,
                state: {
                    session: launchedSession,
                },
            });
        } catch (error: unknown) {
            setPageError(getErrorMessage(error, "Failed to queue Classic City launch."));
        } finally {
            setIsLaunching(false);
        }
    }

    useEffect(() => {
        let isDisposed = false;
        const abortController = new AbortController();

        async function initialize() {
            if (routeSessionId && session?.sessionId === routeSessionId) {
                setIsInitializing(false);
                return;
            }

            setIsInitializing(true);
            setPageError(null);

            try {
                if (routeSessionId) {
                    const loadedSession = await getClassicCitySetupSession(routeSessionId, abortController.signal);
                    if (isDisposed || abortController.signal.aborted) {
                        return;
                    }

                    adoptSession(loadedSession);
                    return;
                }

                const initialDraft = createDefaultDraft();
                const createdSession = await createClassicCitySetupSession({
                    currentStepId: "scenario",
                    draft: initialDraft,
                });

                if (isDisposed) {
                    return;
                }

                adoptSession(createdSession);
                navigate(getClassicCitySetupSessionPath(createdSession.sessionId), {replace: true});
            } catch (error: unknown) {
                if (abortController.signal.aborted || isDisposed) {
                    return;
                }

                setPageError(getErrorMessage(error, "Failed to prepare Classic City setup session."));
            } finally {
                if (!isDisposed) {
                    setIsInitializing(false);
                }
            }
        }

        void initialize();

        return () => {
            isDisposed = true;
            abortController.abort();
        };
    }, [navigate, routeSessionId, session?.sessionId]);

    useEffect(() => {
        if (!session?.sessionId || !canEditSession || isInitializing || isLaunching) {
            return;
        }

        const signature = createSnapshotSignature(latestSnapshotRef.current);
        if (signature === lastSyncedSignatureRef.current) {
            return;
        }

        clearPendingAutosave();
        saveTimeoutRef.current = window.setTimeout(() => {
            void persistDraftNow();
        }, 700);

        return () => {
            clearPendingAutosave();
        };
    }, [canEditSession, currentStepId, draft, isInitializing, isLaunching, session?.sessionId]);

    useEffect(() => {
        if (!session?.sessionId || !isLaunchRunning) {
            return;
        }

        const timer = window.setTimeout(async () => {
            try {
                const refreshedSession = await getClassicCitySetupSession(session.sessionId);
                adoptSession(refreshedSession, true);
            } catch (error: unknown) {
                setPageError(getErrorMessage(error, "Failed to refresh setup session status."));
            }
        }, 2500);

        return () => {
            window.clearTimeout(timer);
        };
    }, [isLaunchRunning, session?.sessionId, session?.status]);

    useEffect(() => {
        if (!session?.sessionId || canEditSession) {
            return;
        }

        navigate(getClassicCitySetupProvisioningPath(session.sessionId), {replace: true});
    }, [canEditSession, navigate, session?.sessionId]);

    useEffect(() => {
        return () => {
            clearPendingAutosave();
            saveAbortRef.current?.abort();
        };
    }, []);

    useEffect(() => {
        if (previousStepIdRef.current === currentStepId) {
            return;
        }

        previousStepIdRef.current = currentStepId;

        const scrollContainer = setupRootRef.current?.closest(".mx-shell__content");
        if (scrollContainer instanceof HTMLElement) {
            scrollContainer.scrollTo({
                top: 0,
                left: 0,
            });
            return;
        }

        window.scrollTo({
            top: 0,
            left: 0,
        });
    }, [currentStepId]);

    if (isInitializing && !session) {
        return (
            <section className="scenario-setup" ref={setupRootRef}>
                <div className="scenario-setup__panel">
                    <LoadingIndicator label="Preparing Classic City setup session..."/>
                </div>
            </section>
        );
    }

    return (
        <section className="scenario-setup" ref={setupRootRef}>
            <header className="scenario-setup__hero">
                <div className="scenario-setup__eyebrow">Compose scenario</div>
                <div className="scenario-setup__hero-grid">
                    <div className="scenario-setup__hero-copy">
                        <div className="scenario-setup__status-row">
                            <span
                                className={`scenario-setup__status-chip scenario-setup__status-chip--${sessionStatusTone}`}>
                                {sessionStatusLabel}
                            </span>
                            {session?.sessionId ? (
                                <span className="scenario-setup__status-meta">
                                    Session {session.sessionId.slice(0, 8)}
                                </span>
                            ) : null}
                        </div>

                        <h1 className="scenario-setup__title">Classic City setup</h1>
                        <p className="scenario-setup__subtitle">
                            Build the launch contract in steps, keep the draft in a backend setup session, and hand
                            the city off to provisioning only after the orchestration flow has actually started.
                        </p>
                    </div>

                    <div className="scenario-setup__hero-art" aria-hidden="true">
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--one"/>
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--two"/>
                        <span className="scenario-setup__hero-orbit scenario-setup__hero-orbit--three"/>
                    </div>
                </div>

                <div className="scenario-setup__stepper" aria-label="Classic City setup progress">
                    {setupSteps.map((step, index) => {
                        const isCurrent = index === currentStepIndex;
                        const isComplete = index < currentStepIndex;

                        return (
                            <div
                                key={step.id}
                                className={`scenario-setup__step ${isCurrent ? "scenario-setup__step--current" : ""} ${isComplete ? "scenario-setup__step--complete" : ""}`}
                            >
                                <span className="scenario-setup__step-index">{index + 1}</span>
                                <div className="scenario-setup__step-copy">
                                    <span className="scenario-setup__step-title">{step.title}</span>
                                    <span className="scenario-setup__step-text">{step.description}</span>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </header>

            <div className="scenario-setup__layout">
                <div className="scenario-setup__panel">
                    <div className="scenario-setup__panel-header">
                        <div>
                            <div className="scenario-setup__panel-eyebrow">{currentStep.title}</div>
                            <h2 className="scenario-setup__panel-title">{currentStep.description}</h2>
                        </div>

                        <Link className="scenario-setup__ghost-link" to={SIMULATIONCORE_SCENARIO_CATALOG_PATH}>
                            Back to catalog
                        </Link>
                    </div>

                    {pageError ? (
                        <div className="scenario-setup__error-banner" role="alert">
                            {pageError}
                        </div>
                    ) : null}

                    {saveError ? (
                        <div className="scenario-setup__error-banner" role="alert">
                            {saveError}
                        </div>
                    ) : null}

                    {session?.failureMessage && session.status === "LaunchFailed" ? (
                        <div className="scenario-setup__error-banner" role="alert">
                            {session.failureMessage}
                        </div>
                    ) : null}

                    {currentStep.id === "scenario" ? (
                        <div className="scenario-setup__stack">
                            <article className="scenario-setup__scenario-card">
                                <div className="scenario-setup__scenario-topline">
                                    <span className="scenario-setup__scenario-chip">{CLASSIC_CITY_SCENARIO.kind}</span>
                                    <span
                                        className="scenario-setup__scenario-chip scenario-setup__scenario-chip--accent">
                                        {CLASSIC_CITY_SCENARIO.availabilityLabel}
                                    </span>
                                </div>

                                <h3 className="scenario-setup__scenario-title">{CLASSIC_CITY_SCENARIO.label}</h3>
                                <p className="scenario-setup__scenario-summary">{CLASSIC_CITY_SCENARIO.summary}</p>
                                <p className="scenario-setup__scenario-description">{CLASSIC_CITY_SCENARIO.description}</p>

                                <div className="scenario-setup__highlight-list">
                                    {CLASSIC_CITY_SCENARIO.highlights.map((item) => (
                                        <div key={item} className="scenario-setup__highlight-item">
                                            {item}
                                        </div>
                                    ))}
                                </div>
                            </article>

                            <div className="scenario-setup__note">
                                The launch contract now lives in a real setup session resource. You can refresh,
                                navigate away, and come back without losing the authoring state or launch lifecycle.
                            </div>
                        </div>
                    ) : null}

                    {currentStep.id === "profile" ? (
                        <div className="scenario-setup__stack">
                            <div className="scenario-setup__form-grid">
                                <div className="scenario-setup__field scenario-setup__field--wide">
                                    <label className="scenario-setup__label" htmlFor="classic-city-name">
                                        City name
                                    </label>
                                    <input
                                        id="classic-city-name"
                                        className="scenario-setup__input"
                                        value={draft.name}
                                        maxLength={128}
                                        placeholder="New Amsterdam"
                                        onChange={(event) => updateDraft("name", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.name ? (
                                        <div className="scenario-setup__error">{validationErrors.name}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <label className="scenario-setup__label" htmlFor="classic-city-start-time">
                                        Start simulation time
                                    </label>
                                    <input
                                        id="classic-city-start-time"
                                        className="scenario-setup__input"
                                        type="datetime-local"
                                        value={draft.startSimTimeLocal}
                                        onChange={(event) => updateDraft("startSimTimeLocal", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.startSimTimeLocal ? (
                                        <div
                                            className="scenario-setup__error">{validationErrors.startSimTimeLocal}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <label className="scenario-setup__label" htmlFor="classic-city-speed">
                                        Speed multiplier
                                    </label>
                                    <input
                                        id="classic-city-speed"
                                        className="scenario-setup__input"
                                        type="number"
                                        min="0.1"
                                        step="0.1"
                                        value={draft.speedMultiplier}
                                        onChange={(event) => updateDraft("speedMultiplier", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.speedMultiplier ? (
                                        <div className="scenario-setup__error">{validationErrors.speedMultiplier}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field scenario-setup__field--wide">
                                    <label className="scenario-setup__label" htmlFor="classic-city-seed">
                                        Generation seed
                                    </label>
                                    <input
                                        id="classic-city-seed"
                                        className="scenario-setup__input"
                                        value={draft.generationSeed}
                                        onChange={(event) => updateDraft("generationSeed", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    <div className="scenario-setup__hint">
                                        Setup sessions now persist an explicit seed. Reusing the same seed together with
                                        the same launch contract reproduces the same topology, population bootstrap, and
                                        initial weather snapshot.
                                    </div>
                                    <Button
                                        type="button"
                                        variant="default"
                                        onClick={() => updateDraft("generationSeed", createRandomGenerationSeed())}
                                        disabled={!canEditSession}
                                    >
                                        Regenerate seed
                                    </Button>
                                </div>
                            </div>

                            <OptionGrid
                                legend="City form preset"
                                options={CLASSIC_CITY_FORM_PRESET_OPTIONS}
                                selectedValue={cityFormPreset === "Custom" ? "" : cityFormPreset}
                                onSelect={(value) => updateCityFormPreset(value as CityFormPresetValue)}
                                disabled={!canEditSession}
                            />

                            <div className="scenario-setup__fact-card">
                                <strong>{getCityFormPresetLabel(cityFormPreset)}</strong>
                                <span>
                                    {getCityFormDescription(draft)}
                                </span>
                                <span>
                                    Current topology shape: {draft.sizeTier.toLowerCase()} footprint, {draft.urbanDensity.toLowerCase()} density, {draft.developmentLevel.toLowerCase()} development.
                                </span>
                                <Button
                                    type="button"
                                    variant={showAdvancedProfile ? "danger" : "default"}
                                    onClick={() => setIsAdvancedProfileOpen((current) => !current)}
                                    disabled={!canEditSession}
                                >
                                    {showAdvancedProfile ? "Hide advanced tuning" : "Fine-tune city form"}
                                </Button>
                            </div>

                            {showAdvancedProfile ? (
                                <div className="scenario-setup__stack">
                                    <div className="scenario-setup__note">
                                        Advanced tuning stays available when you want to move beyond presets. If the
                                        combination no longer matches a named preset, the launch review will label it as
                                        a custom city form.
                                    </div>

                                    <OptionGrid
                                        legend="City size"
                                        options={CLASSIC_CITY_SIZE_TIER_OPTIONS}
                                        selectedValue={draft.sizeTier}
                                        onSelect={(value) => updateDraft("sizeTier", value)}
                                        disabled={!canEditSession}
                                    />

                                    <OptionGrid
                                        legend="Urban density"
                                        options={CLASSIC_CITY_DENSITY_OPTIONS}
                                        selectedValue={draft.urbanDensity}
                                        onSelect={(value) => updateDraft("urbanDensity", value)}
                                        disabled={!canEditSession}
                                    />

                                    <OptionGrid
                                        legend="Development level"
                                        options={CLASSIC_CITY_DEVELOPMENT_OPTIONS}
                                        selectedValue={draft.developmentLevel}
                                        onSelect={(value) => updateDraft("developmentLevel", value)}
                                        disabled={!canEditSession}
                                    />
                                </div>
                            ) : null}

                            <OptionGrid
                                legend="Economy profile"
                                options={CLASSIC_CITY_ECONOMY_PROFILE_OPTIONS}
                                selectedValue={draft.economyProfile}
                                onSelect={(value) => updateDraft("economyProfile", value as ClassicCityEconomyProfile)}
                                disabled={!canEditSession}
                            />

                            <div className="scenario-setup__fact-card">
                                <strong>{economyProfileOption.label}</strong>
                                <span>
                                    {economyProfileOption.description}
                                </span>
                                <span>
                                    This profile becomes part of the launch contract and is published to `Economy`, which uses it to seed the city's initial treasury reserve and category allocations.
                                </span>
                            </div>
                        </div>
                    ) : null}

                    {currentStep.id === "environment" ? (
                        <div className="scenario-setup__stack">
                            <OptionGrid
                                legend="Climate zone"
                                options={CLASSIC_CITY_CLIMATE_OPTIONS}
                                selectedValue={draft.climateZone}
                                onSelect={(value) => updateDraft("climateZone", value)}
                                disabled={!canEditSession}
                            />

                            <OptionGrid
                                legend="Hemisphere"
                                options={CLASSIC_CITY_HEMISPHERE_OPTIONS}
                                selectedValue={draft.hemisphere}
                                onSelect={(value) => updateDraft("hemisphere", value)}
                                disabled={!canEditSession}
                            />

                            <OptionGrid
                                legend="Opening weather"
                                options={CLASSIC_CITY_INITIAL_WEATHER_MODE_OPTIONS}
                                selectedValue={draft.initialWeatherMode}
                                onSelect={(value) => updateDraft("initialWeatherMode", value as ClassicCityInitialWeatherMode)}
                                disabled={!canEditSession}
                            />

                            <div className="scenario-setup__form-grid">
                                <div className="scenario-setup__field">
                                    <label className="scenario-setup__label" htmlFor="classic-city-utc-offset">
                                        UTC offset (minutes)
                                    </label>
                                    <input
                                        id="classic-city-utc-offset"
                                        className="scenario-setup__input"
                                        type="number"
                                        step="15"
                                        value={draft.utcOffsetMinutes}
                                        onChange={(event) => updateDraft("utcOffsetMinutes", event.target.value)}
                                        disabled={!canEditSession}
                                    />
                                    {validationErrors.utcOffsetMinutes ? (
                                        <div className="scenario-setup__error">{validationErrors.utcOffsetMinutes}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <div className="scenario-setup__label">Weather bootstrap</div>
                                    <div className="scenario-setup__fact-card">
                                        <strong>{getInitialWeatherPlanLabel(draft)}</strong>
                                        <span>
                                            {getInitialWeatherPlanDescription(draft)}
                                        </span>
                                    </div>
                                </div>
                            </div>

                            {draft.initialWeatherMode === "Manual" ? (
                                <div className="scenario-setup__stack">
                                    <OptionGrid
                                        legend="Manual weather type"
                                        options={CLASSIC_CITY_INITIAL_WEATHER_TYPE_OPTIONS}
                                        selectedValue={draft.initialWeatherType}
                                        onSelect={(value) => updateDraft("initialWeatherType", value as ClassicCityInitialWeatherType)}
                                        disabled={!canEditSession}
                                    />

                                    <OptionGrid
                                        legend="Manual severity"
                                        options={CLASSIC_CITY_INITIAL_WEATHER_SEVERITY_OPTIONS}
                                        selectedValue={draft.initialWeatherSeverity}
                                        onSelect={(value) => updateDraft("initialWeatherSeverity", value as ClassicCityInitialWeatherSeverity)}
                                        disabled={!canEditSession}
                                    />

                                    <div className="scenario-setup__form-grid">
                                        <div className="scenario-setup__field">
                                            <label className="scenario-setup__label"
                                                   htmlFor="classic-city-initial-weather-temperature">
                                                Manual temperature (C)
                                            </label>
                                            <input
                                                id="classic-city-initial-weather-temperature"
                                                className="scenario-setup__input"
                                                type="number"
                                                step="0.1"
                                                value={draft.initialWeatherTemperatureC}
                                                onChange={(event) => updateDraft("initialWeatherTemperatureC", event.target.value)}
                                                disabled={!canEditSession}
                                            />
                                            {validationErrors.initialWeatherTemperatureC ? (
                                                <div
                                                    className="scenario-setup__error">{validationErrors.initialWeatherTemperatureC}</div>
                                            ) : null}
                                            <div className="scenario-setup__hint">
                                                Leave the suggested value or override it. This temperature becomes part
                                                of the launch contract and stays stable across retries.
                                            </div>
                                        </div>

                                        <div className="scenario-setup__field">
                                            <div className="scenario-setup__label">Manual weather note</div>
                                            <div className="scenario-setup__fact-card">
                                                <strong>{draft.initialWeatherType} / {draft.initialWeatherSeverity}</strong>
                                                <span>
                                                    SimulationCore will derive cloud cover, wind, pressure, and precipitation coherently from this manual opening state instead of asking you to fill every low-level weather scalar by hand.
                                                </span>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            ) : null}
                        </div>
                    ) : null}

                    {currentStep.id === "population" ? (
                        <div className="scenario-setup__stack">
                            <OptionGrid
                                legend="Opening headcount"
                                options={CLASSIC_CITY_POPULATION_TARGET_OPTIONS}
                                selectedValue={draft.populationTargetMode}
                                onSelect={(value) => updateDraft("populationTargetMode", value as ClassicCityPopulationTargetMode)}
                                disabled={!canEditSession}
                            />

                            <div className="scenario-setup__fact-card">
                                <strong>{getPopulationPlanLabel(draft, resolvedPopulationTarget)}</strong>
                                <span>
                                    {draft.populationTargetMode === "Random"
                                        ? "The current generation seed resolves a deterministic launch headcount. Share the same seed and launch settings, and another operator will get the same opening population target."
                                        : "SimulationCore will generate topology around this launch headcount first, then Population will bootstrap households and residents against the resulting housing stock."}
                                </span>
                            </div>

                            {draft.populationTargetMode === "Manual" ? (
                                <div className="scenario-setup__form-grid">
                                    <div className="scenario-setup__field">
                                        <label className="scenario-setup__label"
                                               htmlFor="classic-city-planned-people-count">
                                            Exact resident target
                                        </label>
                                        <input
                                            id="classic-city-planned-people-count"
                                            className="scenario-setup__input"
                                            type="number"
                                            min="0"
                                            step="1"
                                            value={draft.plannedPeopleCount}
                                            onChange={(event) => updateDraft("plannedPeopleCount", event.target.value)}
                                            disabled={!canEditSession}
                                        />
                                        {validationErrors.plannedPeopleCount ? (
                                            <div
                                                className="scenario-setup__error">{validationErrors.plannedPeopleCount}</div>
                                        ) : null}
                                        <div className="scenario-setup__hint">
                                            This headcount is persisted in the setup session and becomes part of the
                                            launch contract, so retries keep the same opening population target.
                                        </div>
                                    </div>
                                </div>
                            ) : null}

                            <OptionGrid
                                legend="Housing pressure"
                                options={CLASSIC_CITY_POPULATION_OCCUPANCY_OPTIONS}
                                selectedValue={draft.populationOccupancyProfile}
                                onSelect={(value) => updateDraft("populationOccupancyProfile", value as ClassicCityPopulationOccupancyProfile)}
                                disabled={!canEditSession}
                            />

                            <div className="scenario-setup__fact-card">
                                <strong>{getPopulationPressureLabel(draft.populationOccupancyProfile)}</strong>
                                <span>
                                    This does not change the requested headcount. It changes how much housing slack or pressure SimulationCore will build around that same target, which is what later drives housed versus homeless outcomes.
                                </span>
                            </div>

                            <div className="scenario-setup__stats-grid">
                                <article className="scenario-setup__stat-card">
                                    <span className="scenario-setup__review-label">Target population</span>
                                    <strong
                                        className="scenario-setup__stat-value">{resolvedPopulationTarget?.toLocaleString() ?? "--"}</strong>
                                    <span className="scenario-setup__review-text">
                                        {draft.populationTargetMode === "Random"
                                            ? "Deterministic from the current seed."
                                            : "This is the opening population SimulationCore and Population will provision against."}
                                    </span>
                                </article>

                                <article className="scenario-setup__stat-card">
                                    <span className="scenario-setup__review-label">Estimated districts</span>
                                    <strong
                                        className="scenario-setup__stat-value">{formatRange(populationPlanningEstimate.districtRange)}</strong>
                                    <span className="scenario-setup__review-text">
                                        Includes the central district and profile-driven expansion around the requested headcount.
                                    </span>
                                </article>

                                <article className="scenario-setup__stat-card">
                                    <span className="scenario-setup__review-label">Residential buildings</span>
                                    <strong
                                        className="scenario-setup__stat-value">{formatRange(populationPlanningEstimate.residentialBuildingRange)}</strong>
                                    <span className="scenario-setup__review-text">
                                        Estimated from the population target plus the chosen city footprint, density, and development profile.
                                    </span>
                                </article>

                                <article className="scenario-setup__stat-card">
                                    <span className="scenario-setup__review-label">Housing capacity</span>
                                    <strong
                                        className="scenario-setup__stat-value">{formatRange(populationPlanningEstimate.capacityRange)}</strong>
                                    <span className="scenario-setup__review-text">
                                        Expected housing coverage: {formatOccupancyRateRange(populationPlanningEstimate.housingCoverageRange)} of launch target once topology is generated.
                                        {hasMeaningfulRangeValue(populationPlanningEstimate.housingHeadroomRange)
                                            ? ` Extra headroom: ${formatOccupancyRateRange(populationPlanningEstimate.housingHeadroomRange)} above launch target.`
                                            : ""}
                                    </span>
                                </article>
                            </div>

                            <div className="scenario-setup__form-grid">
                                <div className="scenario-setup__field">
                                    <div className="scenario-setup__label">City footprint</div>
                                    <div className="scenario-setup__fact-card">
                                        <strong>{getCityFormPresetLabel(cityFormPreset)}</strong>
                                        <span>
                                            {getCityFormDescription(draft)}
                                        </span>
                                        <span>
                                            Under the hood: {draft.sizeTier.toLowerCase()} footprint, {draft.urbanDensity.toLowerCase()} density, {draft.developmentLevel.toLowerCase()} development.
                                        </span>
                                    </div>
                                </div>

                                <div className="scenario-setup__field">
                                    <div className="scenario-setup__label">Launch behavior</div>
                                    <div className="scenario-setup__fact-card">
                                        <strong>Population comes before topology</strong>
                                        <span>
                                            This launch flow now plans people first and generates housing second. Homelessness can emerge naturally if housing pressure is tight enough, but the city no longer starts from an arbitrary empty shell.
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </div>
                    ) : null}

                    {currentStep.id === "launch" ? (
                        <div className="scenario-setup__stack">
                            <div className="scenario-setup__review-grid">
                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">City identity</span>
                                    <strong
                                        className="scenario-setup__review-value">{draft.name || "Unnamed city"}</strong>
                                    <span className="scenario-setup__review-text">
                                        {getCityFormPresetLabel(cityFormPreset)}. {getCityFormDescription(draft)}
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Timeline</span>
                                    <strong
                                        className="scenario-setup__review-value">{draft.startSimTimeLocal || "--"}</strong>
                                    <span className="scenario-setup__review-text">
                                        Local launch input is persisted in the setup session together with the derived UTC timestamp.
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Environment</span>
                                    <strong className="scenario-setup__review-value">
                                        {draft.climateZone} / {draft.hemisphere}
                                    </strong>
                                    <span className="scenario-setup__review-text">
                                        {formatUtcOffset(draft.utcOffsetMinutes)}. {getInitialWeatherPlanDescription(draft)}
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Population bootstrap</span>
                                    <strong
                                        className="scenario-setup__review-value">{getPopulationPlanLabel(draft, resolvedPopulationTarget)}</strong>
                                    <span className="scenario-setup__review-text">
                                        {getPopulationPlanDescription(draft, resolvedPopulationTarget)}
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Economy</span>
                                    <strong
                                        className="scenario-setup__review-value">{economyProfileOption.label}</strong>
                                    <span className="scenario-setup__review-text">
                                        {economyProfileOption.description}
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Generation seed</span>
                                    <strong
                                        className="scenario-setup__review-value scenario-setup__review-value--seed"
                                        title={draft.generationSeed}
                                    >
                                        {formatGenerationSeedPreview(draft.generationSeed)}
                                    </strong>
                                    <span className="scenario-setup__review-text">
                                        The full seed is preserved for launch. Share it together with the launch
                                        settings if you want another operator to reproduce the same starting world.
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Estimated opening</span>
                                    <strong className="scenario-setup__review-value">
                                        {resolvedPopulationTarget?.toLocaleString() ?? "--"} residents
                                    </strong>
                                    <span className="scenario-setup__review-text">
                                        Capacity preview: {formatRange(populationPlanningEstimate.capacityRange)} residents across {formatRange(populationPlanningEstimate.residentialBuildingRange)} residential buildings and {formatRange(populationPlanningEstimate.districtRange)} districts.
                                    </span>
                                </article>
                            </div>

                            <div className="scenario-setup__note">
                                Launch is queued against the setup session and processed asynchronously by Gateway. If
                                anything fails before a city exists, the wizard stays resumable. If the city exists,
                                handoff moves to provisioning instead of pretending the launch was instantly complete.
                            </div>
                        </div>
                    ) : null}

                    <div className="scenario-setup__actions">
                        {currentStep.id === "scenario" ? (
                            <Link className="scenario-setup__secondary-link" to={SIMULATIONCORE_SCENARIO_CATALOG_PATH}>
                                Choose another scenario
                            </Link>
                        ) : (
                            <Button
                                type="button"
                                variant="default"
                                onClick={goBack}
                                disabled={isBusy || !canEditSession}
                            >
                                Back
                            </Button>
                        )}

                        {currentStep.id !== "launch" ? (
                            <Button
                                type="button"
                                variant="primary"
                                onClick={goNext}
                                disabled={isBusy || !canEditSession}
                            >
                                Continue
                            </Button>
                        ) : (
                            <Button
                                type="button"
                                variant="success"
                                onClick={() => void handleLaunch()}
                                disabled={isBusy || !canEditSession}
                            >
                                {isLaunching ? "Queueing launch..." : "Launch Classic City"}
                            </Button>
                        )}
                    </div>
                </div>

                <aside className="scenario-setup__aside">
                    <div
                        className={`scenario-setup__aside-card scenario-setup__aside-card--status-${sessionStatusTone}`}>
                        <div className="scenario-setup__aside-label">Setup session</div>
                        <div className="scenario-setup__aside-value">{sessionStatusLabel}</div>
                        <div className="scenario-setup__aside-list">
                            <div className="scenario-setup__aside-item">
                                <span>Draft</span>
                                <strong>{draft.name || "Classic City launch"}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Profile</span>
                                <strong>{hasProfileSummary ? getCityFormPresetLabel(cityFormPreset) : "Not configured yet"}</strong>
                                <span>
                                    {hasProfileSummary
                                        ? getCityFormDescription(draft)
                                        : "City form, launch timeline, generation seed, and economy stay neutral until the profile step is completed."}
                                </span>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Environment</span>
                                <strong>{hasEnvironmentSummary ? `${draft.climateZone} / ${draft.hemisphere}` : "Not configured yet"}</strong>
                                <span>
                                    {hasEnvironmentSummary
                                        ? getInitialWeatherPlanLabel(draft)
                                        : "Climate, hemisphere, UTC offset, and initial weather are only confirmed on the environment step."}
                                </span>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Population</span>
                                <strong>{hasPopulationSummary ? getPopulationPlanLabel(draft, resolvedPopulationTarget) : "Not configured yet"}</strong>
                                <span>
                                    {hasPopulationSummary
                                        ? getPopulationPlanDescription(draft, resolvedPopulationTarget)
                                        : "Headcount target, occupancy pressure, and housing preview appear after the population step is configured."}
                                </span>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Economy</span>
                                <strong>{hasProfileSummary ? economyProfileOption.label : "Not configured yet"}</strong>
                                <span>
                                    {hasProfileSummary
                                        ? economyProfileOption.description
                                        : "Economy baseline is chosen together with the city profile instead of being assumed up front."}
                                </span>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Clock</span>
                                <strong>{hasProfileSummary ? `${draft.speedMultiplier}x at ${formatUtcOffset(draft.utcOffsetMinutes)}` : "Not configured yet"}</strong>
                            </div>
                        </div>
                    </div>

                    <div className="scenario-setup__aside-card scenario-setup__aside-card--accent">
                        <div className="scenario-setup__aside-label">Persistence</div>
                        <p className="scenario-setup__aside-copy">
                            {isInitializing
                                ? "Preparing setup session..."
                                : isLaunching
                                    ? "Queueing launch request..."
                                    : isSaving
                                        ? "Saving draft to backend session..."
                                        : saveError
                                            ? "Autosave needs attention before launch can proceed cleanly."
                                            : lastSavedAtUtc
                                                ? `Last saved ${formatDateTime(lastSavedAtUtc)}`
                                                : "Draft has not been saved yet."}
                        </p>

                        {session?.launchQueuedAtUtc ? (
                            <div className="scenario-setup__aside-item">
                                <span>Launch queued</span>
                                <strong>{formatDateTime(session.launchQueuedAtUtc)}</strong>
                            </div>
                        ) : null}

                        {session?.startedAtUtc ? (
                            <div className="scenario-setup__aside-item">
                                <span>Provisioning started</span>
                                <strong>{formatDateTime(session.startedAtUtc)}</strong>
                            </div>
                        ) : null}

                        {session?.completedAtUtc ? (
                            <div className="scenario-setup__aside-item">
                                <span>Last completion</span>
                                <strong>{formatDateTime(session.completedAtUtc)}</strong>
                            </div>
                        ) : null}
                    </div>

                    <div className="scenario-setup__aside-card">
                        <div className="scenario-setup__aside-label">Operational note</div>
                        <p className="scenario-setup__aside-copy">
                            {getSessionStatusDescription(session)}
                        </p>

                        {isLaunchRunning ? (
                            <LoadingIndicator label="Refreshing setup session status..."/>
                        ) : null}
                    </div>
                </aside>
            </div>
        </section>
    );
}
