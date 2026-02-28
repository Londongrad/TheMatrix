import "@services/identity/self/account/account/styles/account-card.css";

type Props = {
    username: string;
    email: string;
    isEmailConfirmed: boolean;
};

const AccountCard = ({
    username,
    email,
    isEmailConfirmed,
}: Props) => {
    return (
        <section className="settings-card settings-card--account">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Account identity</h2>
                    <p className="settings-card-description">
                        Core login and recovery identifiers for this operator account.
                    </p>
                </div>
                <span className="settings-pill">
                    {isEmailConfirmed ? "Email confirmed" : "Email pending"}
                </span>
            </div>

            <div className="settings-account-grid">
                <article className="settings-account-panel">
                    <div className="settings-label-row">
                        <span className="settings-label">Username</span>
                        <span>Login alias</span>
                    </div>
                    <div className="settings-account-value">
                        {username || "--"}
                    </div>
                </article>

                <article className="settings-account-panel">
                    <div className="settings-label-row">
                        <span className="settings-label">Email</span>
                        <span>Recovery and verification</span>
                    </div>
                    <div className="settings-account-value">
                        {email || "--"}
                    </div>
                </article>
            </div>

            <div className="settings-account-note">
                Username and email stay read-only for now. They should move through dedicated
                account commands instead of a fake form that pretends to save without backend
                support.
            </div>
        </section>
    );
};

export default AccountCard;
