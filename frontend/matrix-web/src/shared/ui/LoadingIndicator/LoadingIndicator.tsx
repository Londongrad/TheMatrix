import "./loading-indicator.css";

interface LoadingIndicatorProps {
  label?: string;
}

const LoadingIndicator = ({ label = "Loading..." }: LoadingIndicatorProps) => {
  return (
    <div className="mx-loading-indicator" role="status" aria-live="polite">
      <span className="mx-loading-indicator__spinner" aria-hidden="true" />
      <span className="mx-loading-indicator__label">{label}</span>
    </div>
  );
};

export default LoadingIndicator;
