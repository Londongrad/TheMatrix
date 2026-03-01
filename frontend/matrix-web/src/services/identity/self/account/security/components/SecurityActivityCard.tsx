import type {SecurityActivityItem} from "@services/identity/api/self/account/accountTypes";
import {useSecurityActivity} from "../hooks/useSecurityActivity";
import "@services/identity/self/account/security/styles/security-card.css";

type Props = {
    token: string | null;
};

type ActivityPresentation = {
    title: string;
    description: string;
    tone: "success" | "danger" | "warning" | "neutral";
};

function formatExactUtc(value: string) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toUTCString();
}

function formatRelativeUtc(value: string) {
    const date = new Date(value);

    if (Number.isNaN(date.getTime()))
        return value;

    const diffMinutes = Math.round((date.getTime() - Date.now()) / 60000);
    const formatter = new Intl.RelativeTimeFormat("en", {numeric: "auto"});

    if (Math.abs(diffMinutes) < 1)
        return "Just now";

    if (Math.abs(diffMinutes) < 60)
        return formatter.format(diffMinutes, "minute");

    const diffHours = Math.round(diffMinutes / 60);

    if (Math.abs(diffHours) < 24)
        return formatter.format(diffHours, "hour");

    const diffDays = Math.round(diffHours / 24);

    if (Math.abs(diffDays) < 7)
        return formatter.format(diffDays, "day");

    return date.toLocaleString();
}

function describeActivity(item: SecurityActivityItem): ActivityPresentation {
    switch (item.eventType) {
        case "Login":
            return item.isSuccessful
                ? {
                    title: "Signed in",
                    description: "A session was opened for this account.",
                    tone: "success",
                }
                : {
                    title: "Failed sign-in attempt",
                    description: "A login attempt for this account did not succeed.",
                    tone: "danger",
                };
        case "EmailConfirmationRequested":
            return item.isSuccessful
                ? {
                    title: "Verification email sent",
                    description: "A confirmation link was issued for your email address.",
                    tone: "success",
                }
                : {
                    title: "Verification email request blocked",
                    description: "A confirmation email request was throttled or could not be completed.",
                    tone: "warning",
                };
        case "EmailConfirmed":
            return item.isSuccessful
                ? {
                    title: "Email confirmed",
                    description: "Your email address was successfully verified.",
                    tone: "success",
                }
                : {
                    title: "Email confirmation failed",
                    description: "A confirmation link was used unsuccessfully.",
                    tone: "danger",
                };
        case "PasswordResetRequested":
            return item.isSuccessful
                ? {
                    title: "Password reset email sent",
                    description: "A reset link was issued for this account.",
                    tone: "success",
                }
                : {
                    title: "Password reset request blocked",
                    description: "A reset request was throttled or could not be completed.",
                    tone: "warning",
                };
        case "PasswordResetCompleted":
            return item.isSuccessful
                ? {
                    title: "Password reset completed",
                    description: "The account password was replaced using a recovery flow.",
                    tone: "success",
                }
                : {
                    title: "Password reset failed",
                    description: "A password reset attempt did not succeed.",
                    tone: "danger",
                };
        case "Logout":
            return {
                title: item.isSuccessful ? "Signed out" : "Sign-out attempt failed",
                description: "A refresh token was revoked for this account.",
                tone: item.isSuccessful ? "neutral" : "warning",
            };
        case "SessionRevoked":
            return {
                title: item.isSuccessful ? "Session revoked" : "Session revocation failed",
                description: "One signed-in session was manually revoked.",
                tone: item.isSuccessful ? "neutral" : "warning",
            };
        case "AllSessionsRevoked":
            return {
                title: item.isSuccessful ? "All sessions revoked" : "Bulk session revoke failed",
                description: "All active sessions were revoked for this account.",
                tone: item.isSuccessful ? "neutral" : "warning",
            };
        case "UsernameChanged":
            return item.isSuccessful
                ? {
                    title: "Username changed",
                    description: "The primary login alias for this account was updated.",
                    tone: "warning",
                }
                : {
                    title: "Username change blocked",
                    description: "A username change attempt did not pass the current security rules.",
                    tone: "warning",
                };
        case "EmailChangeRequested":
            return item.isSuccessful
                ? {
                    title: "New email confirmation sent",
                    description: "A confirmation link was sent to the next email address for this account.",
                    tone: "success",
                }
                : {
                    title: "Email change request blocked",
                    description: "A request to replace the account email did not pass the current rules.",
                    tone: "warning",
                };
        case "EmailChanged":
            return item.isSuccessful
                ? {
                    title: "Email changed",
                    description: "The account email was replaced after confirmation.",
                    tone: "warning",
                }
                : {
                    title: "Email change confirmation failed",
                    description: "A pending email change could not be confirmed.",
                    tone: "danger",
                };
        case "AccountDeleted":
            return item.isSuccessful
                ? {
                    title: "Account soft-deleted",
                    description: "This account was disabled and all active sessions were revoked.",
                    tone: "danger",
                }
                : {
                    title: "Account deletion blocked",
                    description: "A request to delete this account did not pass the current security rules.",
                    tone: "warning",
                };
        case "AccountRestored":
            return item.isSuccessful
                ? {
                    title: "Account restored",
                    description: "Sign-in access for this account was restored.",
                    tone: "warning",
                }
                : {
                    title: "Account restore failed",
                    description: "A restore attempt for this account did not complete successfully.",
                    tone: "danger",
                };
        case "AccountRecoveryRequested":
            return item.isSuccessful
                ? {
                    title: "Account recovery email sent",
                    description: "A recovery link was issued for a deleted account tied to this email.",
                    tone: "warning",
                }
                : {
                    title: "Account recovery request blocked",
                    description: "A recovery request did not pass the current throttling or account state rules.",
                    tone: "warning",
                };
        default:
            return {
                title: item.eventType,
                description: "A security-related event was recorded for this account.",
                tone: item.isSuccessful ? "neutral" : "warning",
            };
    }
}

function buildMeta(item: SecurityActivityItem) {
    const parts = [
        item.deviceName,
        item.ipAddress ? `IP ${item.ipAddress}` : null,
    ].filter(Boolean) as string[];

    return parts;
}

export default function SecurityActivityCard({token}: Props) {
    const {items, isLoading, error, reload} = useSecurityActivity(token);

    return (
        <section className="settings-card settings-card--security-activity">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Recent security activity</h2>
                    <p className="settings-card-description">
                        Review recent sign-ins, recovery flows, and session actions for your account.
                    </p>
                </div>

                <button
                    type="button"
                    className="settings-button settings-button--secondary"
                    onClick={() => void reload()}
                    disabled={!token || isLoading}
                >
                    {isLoading ? "Loading..." : "Refresh"}
                </button>
            </div>

            {!token ? (
                <p className="settings-muted">
                    Sign in to review security activity for this account.
                </p>
            ) : error ? (
                <div className="settings-alert settings-alert--error">{error}</div>
            ) : isLoading ? (
                <div className="settings-security-activity__skeleton">
                    <div className="settings-security-activity__skeletonLine"/>
                    <div className="settings-security-activity__skeletonLine"/>
                    <div className="settings-security-activity__skeletonLine"/>
                </div>
            ) : items.length === 0 ? (
                <p className="settings-muted">
                    No security activity has been recorded yet.
                </p>
            ) : (
                <div className="settings-security-activity__list">
                    {items.map((item, index) => {
                        const presentation = describeActivity(item);
                        const meta = buildMeta(item);
                        const exactUtc = formatExactUtc(item.occurredAtUtc);

                        return (
                            <article
                                key={`${item.eventType}-${item.occurredAtUtc}-${index}`}
                                className={`settings-security-activity__item settings-security-activity__item--${presentation.tone}`}
                            >
                                <div className="settings-security-activity__itemTop">
                                    <div>
                                        <div className="settings-security-activity__title">
                                            {presentation.title}
                                        </div>
                                        <div className="settings-security-activity__description">
                                            {presentation.description}
                                        </div>
                                    </div>

                                    <div
                                        className="settings-security-activity__time"
                                        title={exactUtc}
                                    >
                                        <div className="settings-security-activity__timePrimary">
                                            {formatRelativeUtc(item.occurredAtUtc)}
                                        </div>
                                        <div className="settings-security-activity__timeSecondary">
                                            {exactUtc}
                                        </div>
                                    </div>
                                </div>

                                <div className="settings-security-activity__meta">
                                    {meta.map((value) => (
                                        <span
                                            key={value}
                                            className="settings-security-activity__chip"
                                        >
                                            {value}
                                        </span>
                                    ))}

                                    <span className="settings-security-activity__chip">
                                        {item.isSuccessful ? "Successful" : "Attention needed"}
                                    </span>
                                </div>

                                {item.userAgent ? (
                                    <div className="settings-security-activity__userAgent">
                                        {item.userAgent}
                                    </div>
                                ) : null}
                            </article>
                        );
                    })}
                </div>
            )}
        </section>
    );
}
