import type { ReactNode } from "react";

interface KeyValueRowProps {
    label: string;
    value: ReactNode;
}

const KeyValueRow = ({ label, value }: KeyValueRowProps) => {
    return (
        <div className="citycore-kv-row">
            <span className="citycore-kv-row__label">{label}</span>
            <span className="citycore-kv-row__value">{value}</span>
        </div>
    );
};

export default KeyValueRow;
