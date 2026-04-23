import {useEffect, useState} from "react";
import {Link, useSearchParams} from "react-router-dom";
import {getErrorMessage} from "@shared/lib/errors/getErrorMessage";
import {confirmEmailChange} from "@services/identity/api/self/auth/authApi";
import {useAuth} from "@services/identity/api/self/auth/useAuth";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

type ConfirmStatus = "pending" | "success" | "error";

export const ConfirmEmailChangePage = () => {
    const [searchParams] = useSearchParams();
    const {reloadMe} = useAuth();
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");
    const hasValidLink = Boolean(userId && token);

    const [status, setStatus] = useState<ConfirmStatus>(hasValidLink ? "pending" : "error");
    const [message, setMessage] = useState(
        hasValidLink ? "Confirming your new email..." : "This email change link is incomplete or invalid.",
    );

    useEffect(() => {
        if (!userId || !token) {
            return;
        }

        let cancelled = false;

        void (async () => {
            try {
                await confirmEmailChange({userId, token});
                if (cancelled) {
                    return;
                }

                setStatus("success");
                setMessage("Your new email has been confirmed and is now active.");
                void reloadMe();
            } catch (error: unknown) {
                if (cancelled) {
                    return;
                }

                setStatus("error");
                setMessage(getErrorMessage(error, "Failed to confirm the new email."));
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [reloadMe, token, userId]);

    return (
        <AuthShell>
            <AuthCard
                side={
                    <>
                        <AuthLogo/>
                        <h2 className="auth-heading">
                            Confirm your next <span>Matrix</span> contact route
                        </h2>
                        <p className="auth-text">
                            Email replacement stays inactive until this confirmation step succeeds.
                        </p>
                    </>
                }
            >
                <h1 className="auth-title">Email change confirmation</h1>
                <p className="auth-subtitle">
                    {hasValidLink ? message : "This email change link is incomplete or invalid."}
                </p>

                {status === "pending" ? (
                    <button className="auth-button" type="button" disabled>
                        <span className="auth-spinner" aria-hidden="true"/>
                        <span>Confirming...</span>
                    </button>
                ) : null}

                {status !== "pending" ? (
                    <div className="auth-switch">
                        <Link to="/login">Back to login</Link>
                        {" · "}
                        <Link to="/userSettings/security">Security settings</Link>
                    </div>
                ) : null}
            </AuthCard>
        </AuthShell>
    );
};
