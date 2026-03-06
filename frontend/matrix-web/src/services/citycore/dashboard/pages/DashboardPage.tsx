// src/services/citycore/pages/DashboardPage.tsx
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/dashboard/styles/dashboard.css";

const DashboardPage = () => {
    const handleTriggerStorm = () => {
        console.log("Trigger storm in district #1");
    };

    const handleTriggerBlackout = () => {
        console.log("Trigger blackout for 15 minutes");
    };

    return (
        <>
            <h1 className="page-title">Dashboard</h1>

            <section className="cards-grid">
                <div className="card">
                    <h2 className="card-title">Population</h2>
                    <p className="card-value">1 234 567</p>
                    <p className="card-sub">citizens in simulation</p>
                </div>

                <div className="card">
                    <h2 className="card-title">Active incidents</h2>
                    <p className="card-value">12</p>
                    <p className="card-sub">need operator attention</p>
                </div>

                <div className="card">
                    <h2 className="card-title">System status</h2>
                    <p className="card-value">Stable</p>
                    <p className="card-sub">no critical failures</p>
                </div>
            </section>

            <section className="actions">
                <h2 className="section-title">God actions</h2>
                <div className="actions-row">
                    <Button variant="primary" onClick={handleTriggerStorm}>
                        Trigger thunderstorm
                    </Button>
                    <Button variant="danger" onClick={handleTriggerBlackout}>
                        Trigger blackout
                    </Button>
                    <Button disabled>Spawn random event (soon)</Button>
                </div>
            </section>
        </>
    );
};

export default DashboardPage;
