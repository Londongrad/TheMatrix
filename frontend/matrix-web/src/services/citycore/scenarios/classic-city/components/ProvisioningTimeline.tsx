export type ProvisioningTimelineItem = {
    id: string;
    title: string;
    description: string;
    status: "pending" | "current" | "complete" | "failed";
    meta?: string;
};

type ProvisioningTimelineProps = {
    items: ProvisioningTimelineItem[];
};

export default function ProvisioningTimeline({items}: ProvisioningTimelineProps) {
    return (
        <div className="scenario-setup__timeline" aria-label="Provisioning progress timeline">
            {items.map((item) => (
                <article
                    key={item.id}
                    className={`scenario-setup__timeline-step scenario-setup__timeline-step--${item.status}`}
                >
                    <div className="scenario-setup__timeline-rail" aria-hidden="true">
                        <span className="scenario-setup__timeline-dot"/>
                        <span className="scenario-setup__timeline-line"/>
                    </div>

                    <div className="scenario-setup__timeline-card">
                        <div className="scenario-setup__timeline-header">
                            <span className="scenario-setup__timeline-title">{item.title}</span>
                            <span
                                className={`scenario-setup__timeline-status scenario-setup__timeline-status--${item.status}`}>
                                {item.status === "complete"
                                    ? "Complete"
                                    : item.status === "current"
                                        ? "In progress"
                                        : item.status === "failed"
                                            ? "Failed"
                                            : "Pending"}
                            </span>
                        </div>

                        <p className="scenario-setup__timeline-description">{item.description}</p>

                        {item.meta ? (
                            <div className="scenario-setup__timeline-meta">{item.meta}</div>
                        ) : null}
                    </div>
                </article>
            ))}
        </div>
    );
}
