import {Link} from "react-router";
import {simulationCoreScenarioRegistry} from "@services/simulationcore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import "@services/simulationcore/scenarios/styles/scenario-catalog.css";

function canComposeScenario(kind: string, can: (permission: string) => boolean): boolean {
    switch (kind) {
        case "ClassicCity":
            return can(PermissionKeys.SimulationCoreClassicCityCreate);
        default:
            return false;
    }
}

function canOpenScenario(kind: string, can: (permission: string) => boolean): boolean {
    switch (kind) {
        case "ClassicCity":
            return can(PermissionKeys.SimulationCoreClassicCityRead);
        default:
            return false;
    }
}

export default function ScenarioCatalogPage() {
    const {can} = usePermissions();
    const primaryScenario = simulationCoreScenarioRegistry[0];
    const canComposePrimary = primaryScenario ? canComposeScenario(primaryScenario.kind, can) : false;
    const canOpenPrimary = primaryScenario ? canOpenScenario(primaryScenario.kind, can) : false;

    return (
        <section className="scenario-catalog">
            <header className="scenario-catalog__hero">
                <div className="scenario-catalog__eyebrow">Scenario registry</div>
                <div className="scenario-catalog__hero-grid">
                    <div className="scenario-catalog__hero-copy">
                        <h1 className="scenario-catalog__title">Compose scenario</h1>
                        <p className="scenario-catalog__subtitle">
                            Choose the simulation flow first, launch it through a dedicated setup wizard, and only then
                            hand the finished host off to a live workspace. Scenario modules can grow independently
                            without turning the registry into a maze of inline forms.
                        </p>

                        <div className="scenario-catalog__hero-actions">
                            {primaryScenario && canComposePrimary ? (
                                <Link
                                    className="scenario-catalog__hero-link scenario-catalog__hero-link--primary"
                                    to={primaryScenario.setupPath}
                                >
                                    Compose {primaryScenario.label}
                                </Link>
                            ) : null}

                            {primaryScenario && canOpenPrimary ? (
                                <Link className="scenario-catalog__hero-link" to={primaryScenario.listPath}>
                                    Open {primaryScenario.label} registry
                                </Link>
                            ) : null}
                        </div>
                    </div>

                    <div className="scenario-catalog__hero-panel" aria-hidden="true">
                        <span className="scenario-catalog__hero-orbit scenario-catalog__hero-orbit--one"/>
                        <span className="scenario-catalog__hero-orbit scenario-catalog__hero-orbit--two"/>
                        <span className="scenario-catalog__hero-orbit scenario-catalog__hero-orbit--three"/>
                    </div>
                </div>
            </header>

            <div className="scenario-catalog__grid">
                {simulationCoreScenarioRegistry.map((scenario, index) => {
                    const canCompose = canComposeScenario(scenario.kind, can);
                    const canOpen = canOpenScenario(scenario.kind, can);

                    return (
                        <article key={scenario.kind} className="scenario-card">
                            <div className="scenario-card__topline">
                                <span className="scenario-card__status">{scenario.availabilityLabel}</span>
                                <span className="scenario-card__kind">{scenario.kind}</span>
                            </div>

                            <div className="scenario-card__media" aria-hidden="true">
                                <div className={`scenario-card__pulse scenario-card__pulse--${index % 3}`}/>
                                <div className="scenario-card__media-label">{scenario.label}</div>
                            </div>

                            <div className="scenario-card__body">
                                <h2 className="scenario-card__title">{scenario.label}</h2>
                                <p className="scenario-card__summary">{scenario.summary}</p>
                                <p className="scenario-card__description">{scenario.description}</p>

                                <div className="scenario-card__highlight-list">
                                    {scenario.highlights.map((item) => (
                                        <div key={item} className="scenario-card__highlight">
                                            {item}
                                        </div>
                                    ))}
                                </div>
                            </div>

                            <div className="scenario-card__footer">
                                <div className="scenario-card__route-list">
                                    <div className="scenario-card__route-chip">
                                        Setup: <span>{scenario.setupPath}</span>
                                    </div>
                                    <div className="scenario-card__route-chip">
                                        Registry: <span>{scenario.listPath}</span>
                                    </div>
                                </div>

                                <div className="scenario-card__actions">
                                    {canCompose ? (
                                        <Link className="scenario-card__action" to={scenario.setupPath}>
                                            Compose scenario
                                        </Link>
                                    ) : (
                                        <span className="scenario-card__action scenario-card__action--disabled">
                                            Create permission required
                                        </span>
                                    )}

                                    {canOpen ? (
                                        <Link className="scenario-card__action scenario-card__action--secondary"
                                              to={scenario.listPath}>
                                            Open registry
                                        </Link>
                                    ) : (
                                        <span className="scenario-card__action scenario-card__action--disabled">
                                            Read permission required
                                        </span>
                                    )}
                                </div>
                            </div>
                        </article>
                    );
                })}
            </div>
        </section>
    );
}
