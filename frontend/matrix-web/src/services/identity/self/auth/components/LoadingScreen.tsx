import "@services/identity/self/auth/styles/loading-screen.css";

export const LoadingScreen = () => {
    return (
        <div className="loading-overlay">
            <div className="loading-core">
                <div className="loading-ring">
                    <div className="loading-orb"/>
                </div>
                <div className="loading-title">Loading the workspace...</div>
                <div className="loading-subtitle">
                    Restoring your session and permissions.
                </div>
            </div>
        </div>
    );
};
