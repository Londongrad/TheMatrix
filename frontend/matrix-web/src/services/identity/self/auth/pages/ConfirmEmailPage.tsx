import {useEffect, useState} from "react";
import {Link, useSearchParams} from "react-router";
import {getErrorMessage} from "@shared/lib/errors/getErrorMessage";
import {confirmEmail} from "@services/identity/api/self/auth/authApi";
import {useAuth} from "@services/identity/api/self/auth/useAuth";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

type ConfirmStatus = "pending" | "success" | "error";

export const ConfirmEmailPage = () => {
    const [searchParams] = useSearchParams();
    const {reloadMe} = useAuth();
    const userId = searchParams.get("userId");
    const token = searchParams.get("token");
    const hasValidLink = Boolean(userId && token);

    const [status, setStatus] = useState<ConfirmStatus>(hasValidLink ? "pending" : "error");
    const [message, setMessage] = useState(
        hasValidLink ? "Confirming your email..." : "This confirmation link is incomplete or invalid.",
    );

    useEffect(() => {
        if (!userId || !token) {
            return;
        }

        let cancelled = false;

        void (async () => {
            try {
                await confirmEmail({userId, token});
                if (cancelled) {
                    return;
                }

                setStatus("success");
                setMessage("Your email has been confirmed.");
                void reloadMe();
            } catch (error: unknown) {
                if (cancelled) {
                    return;
                }

                setStatus("error");
                setMessage(getErrorMessage(error, "Failed to confirm email."));
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
                            Verify your <span>Matrix</span> identity
                        </h2>
                        <p className="auth-text">
                            Email confirmation keeps recovery and notification flows tied
                            to the right operator account.
                        </p>
                    </>
                }
            >
                <h1 className="auth-title">Email confirmation</h1>
                <p className="auth-subtitle">
                    {hasValidLink ? message : "This confirmation link is incomplete or invalid."}
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
