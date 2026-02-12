import {Link} from "react-router-dom";
import {cityCoreScenarioRegistry} from "@services/citycore/scenarios/registry";
import "@services/citycore/scenarios/styles/scenario-catalog.css";

export default function ScenarioCatalogPage() {
    return (
        <section className="scenario-catalog">
            <header className="scenario-catalog__hero">
                <div className="scenario-catalog__eyebrow">CityCore</div>
                <h1 className="scenario-catalog__title">Scenario catalog</h1>
                <p className="scenario-catalog__subtitle">
                    Choose which simulation workspace to operate. New scenario modules can plug into this catalog
                    without reshaping the main application shell.
                </p>
            </header>

            <div className="scenario-catalog__grid">
                {cityCoreScenarioRegistry.map((scenario) => (
                    <article key={scenario.kind} className="scenario-card">
                        <div className="scenario-card__topline">
                            <span className="scenario-card__status">{scenario.availabilityLabel}</span>
                            <span className="scenario-card__kind">{scenario.kind}</span>
                        </div>

                        <div className="scenario-card__body">
                            <h2 className="scenario-card__title">{scenario.label}</h2>
                            <p className="scenario-card__summary">{scenario.summary}</p>
                            <p className="scenario-card__description">{scenario.description}</p>
                        </div>

                        <div className="scenario-card__footer">
                            <div className="scenario-card__route-chip">
                                Workspace: <span>{scenario.listPath}</span>
                            </div>

                            <Link className="scenario-card__action" to={scenario.listPath}>
                                Open workspace
                            </Link>
                        </div>
                    </article>
                ))}
            </div>
        </section>
    );
}
