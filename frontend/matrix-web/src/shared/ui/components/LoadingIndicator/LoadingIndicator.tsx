import "./loading-indicator.css";

interface LoadingIndicatorProps {
    label?: string;
}

const LoadingIndicator = ({label = "Loading..."}: LoadingIndicatorProps) => {
    return (
        <div className="mx-loading-indicator" role="status" aria-live="polite">
            <span className="mx-loading-indicator__visual" aria-hidden="true">
                <span className="mx-loading-indicator__halo"/>
                <span className="mx-loading-indicator__spinner"/>
                <span className="mx-loading-indicator__core"/>
            </span>
            <span className="mx-loading-indicator__copy">
                <span className="mx-loading-indicator__label">{label}</span>
                <span className="mx-loading-indicator__signal" aria-hidden="true">
                    <span/>
                    <span/>
                    <span/>
                    <span/>
                </span>
            </span>
        </div>
    );
};

export default LoadingIndicator;
