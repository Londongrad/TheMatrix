import {useState} from "react";
import {Link, useNavigate} from "react-router-dom";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Button from "@shared/ui/controls/Button/Button";
import {useCityProvisioning} from "@services/citycore/scenarios/classic-city/hooks/useCityProvisioning";
import {
    CLASSIC_CITY_CLIMATE_OPTIONS,
    CLASSIC_CITY_DEVELOPMENT_OPTIONS,
    CLASSIC_CITY_DENSITY_OPTIONS,
    CLASSIC_CITY_HEMISPHERE_OPTIONS,
    CLASSIC_CITY_SIZE_TIER_OPTIONS,
    type SetupOption,
} from "@services/citycore/scenarios/classic-city/setupOptions";
import {
    getNowLocalDateTimeInputValue,
    localDateTimeToUtcIso,
} from "@services/citycore/simulation/utils/dateTime";
import {
    CITYCORE_SCENARIO_CATALOG_PATH,
    CLASSIC_CITY_SCENARIO,
    getClassicCityProvisioningPath,
} from "@services/citycore/scenarios/registry";
import "@services/citycore/scenarios/styles/scenario-setup.css";

type SetupStepId = "scenario" | "profile" | "environment" | "launch";

type ValidationErrors = {
    name?: string;
    startSimTimeLocal?: string;
    speedMultiplier?: string;
    utcOffsetMinutes?: string;
};

type SetupDraft = {
    name: string;
    startSimTimeLocal: string;
    speedMultiplier: string;
    climateZone: string;
    hemisphere: string;
    utcOffsetMinutes: string;
    generationSeed: string;
    sizeTier: string;
    urbanDensity: string;
    developmentLevel: string;
};

type OptionGridProps = {
    legend: string;
    options: SetupOption[];
    selectedValue: string;
    onSelect: (value: string) => void;
};

const setupSteps: { id: SetupStepId; title: string; description: string }[] = [
    {
        id: "scenario",
        title: "Scenario",
        description: "Choose the simulation baseline and what the launch flow will provision.",
    },
    {
        id: "profile",
        title: "City profile",
        description: "Define the city identity, timeline, and generation profile.",
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

function createDefaultDraft(): SetupDraft {
    return {
        name: "",
        startSimTimeLocal: getNowLocalDateTimeInputValue(),
        speedMultiplier: "1",
        climateZone: "Temperate",
        hemisphere: "Northern",
        utcOffsetMinutes: String(-new Date().getTimezoneOffset()),
        generationSeed: "",
        sizeTier: "Medium",
        urbanDensity: "Balanced",
        developmentLevel: "Balanced",
    };
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

    return errors;
}

function mergeErrors(...items: ValidationErrors[]): ValidationErrors {
    return Object.assign({}, ...items);
}

function OptionGrid({legend, options, selectedValue, onSelect}: OptionGridProps) {
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

export default function ClassicCitySetupPage() {
    const navigate = useNavigate();
    const provisioning = useCityProvisioning();
    const [draft, setDraft] = useState<SetupDraft>(createDefaultDraft);
    const [currentStepIndex, setCurrentStepIndex] = useState(0);
    const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

    const currentStep = setupSteps[currentStepIndex];

    function updateDraft<K extends keyof SetupDraft>(key: K, value: SetupDraft[K]) {
        setDraft((current) => ({...current, [key]: value}));
        setValidationErrors((current) => {
            if (!(key in current)) {
                return current;
            }

            const next = {...current};
            delete next[key as keyof ValidationErrors];
            return next;
        });
        provisioning.clearError();
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

        setCurrentStepIndex((index) => Math.min(index + 1, setupSteps.length - 1));
    }

    function goBack() {
        setCurrentStepIndex((index) => Math.max(index - 1, 0));
    }

    async function handleLaunch() {
        const errors = mergeErrors(
            validateProfile(draft),
            validateEnvironment(draft),
        );
        setValidationErrors(errors);

        if (Object.keys(errors).length > 0) {
            return;
        }

        const startSimTimeUtc = localDateTimeToUtcIso(draft.startSimTimeLocal);
        if (!startSimTimeUtc) {
            setValidationErrors((current) => ({
                ...current,
                startSimTimeLocal: "Invalid date/time value.",
            }));
            return;
        }

        const result = await provisioning.launch({
            name: draft.name.trim(),
            simulationKind: CLASSIC_CITY_SCENARIO.kind,
            startSimTimeUtc,
            speedMultiplier: Number(draft.speedMultiplier),
            climateZone: draft.climateZone,
            hemisphere: draft.hemisphere,
            utcOffsetMinutes: Number(draft.utcOffsetMinutes),
            generationSeed: draft.generationSeed.trim() || null,
            sizeTier: draft.sizeTier,
            urbanDensity: draft.urbanDensity,
            developmentLevel: draft.developmentLevel,
        });

        if (!result) {
            return;
        }

        navigate(getClassicCityProvisioningPath(result.cityId), {
            state: {
                provisioning: result,
                launchedFromSetup: true,
            },
        });
    }

    return (
        <section className="scenario-setup">
            <header className="scenario-setup__hero">
                <div className="scenario-setup__eyebrow">Compose scenario</div>
                <div className="scenario-setup__hero-grid">
                    <div className="scenario-setup__hero-copy">
                        <h1 className="scenario-setup__title">Classic City setup</h1>
                        <p className="scenario-setup__subtitle">
                            Build the launch contract in steps, validate the world profile before provisioning, and
                            hand off the finished city to monitoring instead of dropping operators into a half-built
                            workspace.
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

                        <Link className="scenario-setup__ghost-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                            Back to catalog
                        </Link>
                    </div>

                    {currentStep.id === "scenario" ? (
                        <div className="scenario-setup__stack">
                            <article className="scenario-setup__scenario-card">
                                <div className="scenario-setup__scenario-topline">
                                    <span className="scenario-setup__scenario-chip">{CLASSIC_CITY_SCENARIO.kind}</span>
                                    <span className="scenario-setup__scenario-chip scenario-setup__scenario-chip--accent">
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
                                CityCore remains the owner of topology, weather, and clock state. Population bootstrap
                                stays downstream and is reported back as launch outcome instead of being hidden behind
                                a silent redirect.
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
                                    />
                                    {validationErrors.startSimTimeLocal ? (
                                        <div className="scenario-setup__error">{validationErrors.startSimTimeLocal}</div>
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
                                        placeholder="Leave empty to derive a deterministic seed from launch inputs"
                                        onChange={(event) => updateDraft("generationSeed", event.target.value)}
                                    />
                                    <div className="scenario-setup__hint">
                                        Leaving this empty keeps the launch deterministic while still deriving the seed
                                        from the configured city profile.
                                    </div>
                                </div>
                            </div>

                            <OptionGrid
                                legend="City size"
                                options={CLASSIC_CITY_SIZE_TIER_OPTIONS}
                                selectedValue={draft.sizeTier}
                                onSelect={(value) => updateDraft("sizeTier", value)}
                            />

                            <OptionGrid
                                legend="Urban density"
                                options={CLASSIC_CITY_DENSITY_OPTIONS}
                                selectedValue={draft.urbanDensity}
                                onSelect={(value) => updateDraft("urbanDensity", value)}
                            />

                            <OptionGrid
                                legend="Development level"
                                options={CLASSIC_CITY_DEVELOPMENT_OPTIONS}
                                selectedValue={draft.developmentLevel}
                                onSelect={(value) => updateDraft("developmentLevel", value)}
                            />
                        </div>
                    ) : null}

                    {currentStep.id === "environment" ? (
                        <div className="scenario-setup__stack">
                            <OptionGrid
                                legend="Climate zone"
                                options={CLASSIC_CITY_CLIMATE_OPTIONS}
                                selectedValue={draft.climateZone}
                                onSelect={(value) => updateDraft("climateZone", value)}
                            />

                            <OptionGrid
                                legend="Hemisphere"
                                options={CLASSIC_CITY_HEMISPHERE_OPTIONS}
                                selectedValue={draft.hemisphere}
                                onSelect={(value) => updateDraft("hemisphere", value)}
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
                                    />
                                    {validationErrors.utcOffsetMinutes ? (
                                        <div className="scenario-setup__error">{validationErrors.utcOffsetMinutes}</div>
                                    ) : null}
                                </div>

                                <div className="scenario-setup__field">
                                    <div className="scenario-setup__label">Weather bootstrap</div>
                                    <div className="scenario-setup__fact-card">
                                        <strong>Automatic from climate profile</strong>
                                        <span>
                                            Initial weather is still generated by CityCore from the climate setup and
                                            start simulation time. Manual weather tuning is the next follow-up slice,
                                            not a fake frontend-only field.
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
                                    <strong className="scenario-setup__review-value">{draft.name || "Unnamed city"}</strong>
                                    <span className="scenario-setup__review-text">
                                        {draft.sizeTier} city, {draft.urbanDensity.toLowerCase()} density, {draft.developmentLevel.toLowerCase()} development.
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Timeline</span>
                                    <strong className="scenario-setup__review-value">{draft.startSimTimeLocal || "--"}</strong>
                                    <span className="scenario-setup__review-text">
                                        Local launch input converted to UTC before the request leaves the browser.
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Environment</span>
                                    <strong className="scenario-setup__review-value">
                                        {draft.climateZone} / {draft.hemisphere}
                                    </strong>
                                    <span className="scenario-setup__review-text">
                                        {formatUtcOffset(draft.utcOffsetMinutes)}
                                    </span>
                                </article>

                                <article className="scenario-setup__review-card">
                                    <span className="scenario-setup__review-label">Population bootstrap</span>
                                    <strong className="scenario-setup__review-value">Automatic</strong>
                                    <span className="scenario-setup__review-text">
                                        Population is initialized downstream after CityCore creates topology, weather,
                                        and simulation clock state.
                                    </span>
                                </article>
                            </div>

                            <div className="scenario-setup__note">
                                Launch result is handled as provisioning outcome, not as an implicit redirect. If
                                Population bootstrap fails, the handoff page will show that explicitly and offer retry
                                instead of dumping you into a confusing half-ready city.
                            </div>

                            {provisioning.error ? (
                                <div className="scenario-setup__error-banner" role="alert">
                                    {provisioning.error}
                                </div>
                            ) : null}
                        </div>
                    ) : null}

                    <div className="scenario-setup__actions">
                        {currentStep.id === "scenario" ? (
                            <Link className="scenario-setup__secondary-link" to={CITYCORE_SCENARIO_CATALOG_PATH}>
                                Choose another scenario
                            </Link>
                        ) : (
                            <Button
                                type="button"
                                variant="default"
                                onClick={goBack}
                                disabled={provisioning.isSubmitting}
                            >
                                Back
                            </Button>
                        )}

                        {currentStep.id !== "launch" ? (
                            <Button
                                type="button"
                                variant="primary"
                                onClick={goNext}
                                disabled={provisioning.isSubmitting}
                            >
                                Continue
                            </Button>
                        ) : (
                            <Button
                                type="button"
                                variant="success"
                                onClick={() => void handleLaunch()}
                                disabled={provisioning.isSubmitting}
                            >
                                {provisioning.isSubmitting ? "Launching..." : "Launch Classic City"}
                            </Button>
                        )}
                    </div>
                </div>

                <aside className="scenario-setup__aside">
                    <div className="scenario-setup__aside-card">
                        <div className="scenario-setup__aside-label">Launch summary</div>
                        <div className="scenario-setup__aside-value">{draft.name || "Classic City launch"}</div>
                        <div className="scenario-setup__aside-list">
                            <div className="scenario-setup__aside-item">
                                <span>Profile</span>
                                <strong>{draft.sizeTier} / {draft.urbanDensity} / {draft.developmentLevel}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Environment</span>
                                <strong>{draft.climateZone} / {draft.hemisphere}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Clock</span>
                                <strong>{draft.speedMultiplier}x at {formatUtcOffset(draft.utcOffsetMinutes)}</strong>
                            </div>
                            <div className="scenario-setup__aside-item">
                                <span>Population</span>
                                <strong>Auto-bootstrap after city creation</strong>
                            </div>
                        </div>
                    </div>

                    <div className="scenario-setup__aside-card scenario-setup__aside-card--accent">
                        <div className="scenario-setup__aside-label">Operational note</div>
                        <p className="scenario-setup__aside-copy">
                            This flow deliberately separates authoring, provisioning, and monitoring. The city is
                            launched as a backend setup operation first and handed off to the live workspace only after
                            the provisioning outcome is known.
                        </p>

                        {provisioning.isSubmitting ? (
                            <LoadingIndicator label="Provisioning city and population bootstrap..."/>
                        ) : null}
                    </div>
                </aside>
            </div>
        </section>
    );
}
