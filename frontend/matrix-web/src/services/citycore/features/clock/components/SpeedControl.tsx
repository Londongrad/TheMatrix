import { useState, type FormEvent } from "react";
import Button from "@shared/ui/controls/Button/Button";
import Card from "@shared/ui/controls/Card/Card";

interface SpeedControlProps {
    disabled: boolean;
    isSubmitting: boolean;
    onSubmit: (multiplier: number) => Promise<void>;
}

const SpeedControl = ({
                          disabled,
                          isSubmitting,
                          onSubmit,
                      }: SpeedControlProps) => {
    const [multiplierInput, setMultiplierInput] = useState("1");
    const [validationError, setValidationError] = useState<string | null>(null);

    const submit = async (e: FormEvent) => {
        e.preventDefault();

        const parsed = Number(multiplierInput);
        if (!Number.isFinite(parsed) || parsed <= 0 || parsed > 1000) {
            setValidationError("Multiplier must be a number in range (0, 1000].");
            return;
        }

        setValidationError(null);
        await onSubmit(parsed);
    };

    return (
        <Card title="Set speed" subtitle="Update simulation multiplier">
            <form onSubmit={submit} className="citycore-form-inline">
                <input
                    type="number"
                    className="text-input"
                    min="0.01"
                    max="1000"
                    step="0.01"
                    value={multiplierInput}
                    onChange={(e) => setMultiplierInput(e.target.value)}
                    disabled={disabled || isSubmitting}
                />

                <Button
                    type="submit"
                    variant="primary"
                    disabled={disabled || isSubmitting}
                >
                    {isSubmitting ? "Updating..." : "Apply"}
                </Button>
            </form>

            {validationError && <p className="error-text">{validationError}</p>}
        </Card>
    );
};

export default SpeedControl;
