import { useState, type FormEvent } from "react";
import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";
import { toIsoUtcFromDatetimeLocal } from "@services/citycore/utils/cityCoreFormatters";

interface JumpControlProps {
    disabled: boolean;
    isSubmitting: boolean;
    onSubmit: (newSimTimeUtc: string) => Promise<void>;
}

const JumpControl = ({ disabled, isSubmitting, onSubmit }: JumpControlProps) => {
    const [datetimeValue, setDatetimeValue] = useState("");
    const [validationError, setValidationError] = useState<string | null>(null);

    const submit = async (e: FormEvent) => {
        e.preventDefault();

        const iso = toIsoUtcFromDatetimeLocal(datetimeValue);
        if (!iso) {
            setValidationError("Provide a valid UTC date-time.");
            return;
        }

        setValidationError(null);
        await onSubmit(iso);
    };

    return (
        <Card title="Jump clock" subtitle="Move simulation time to exact UTC point">
            <form onSubmit={submit} className="citycore-form-inline">
                <input
                    type="datetime-local"
                    className="text-input"
                    value={datetimeValue}
                    onChange={(e) => setDatetimeValue(e.target.value)}
                    disabled={disabled || isSubmitting}
                />

                <Button
                    type="submit"
                    variant="primary"
                    disabled={disabled || isSubmitting}
                >
                    {isSubmitting ? "Jumping..." : "Jump"}
                </Button>
            </form>

            {validationError && <p className="error-text">{validationError}</p>}
            <p className="card-sub">Input is interpreted as UTC.</p>
        </Card>
    );
};

export default JumpControl;
