import React from "react";
import {PencilLine} from "lucide-react";
import type {PersonDto} from "@services/population/person/contracts/personContracts";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import "@services/population/person/styles/citizen-card.css";

interface CitizenCardProps {
    person: PersonDto;
    onOpen?: (person: PersonDto) => void;
}

const CitizenCard = ({person, onOpen}: CitizenCardProps) => {
    const handleOpen = (event: React.MouseEvent<HTMLButtonElement>) => {
        event.stopPropagation();
        onOpen?.(person);
    };

    const isDeceased = person.lifeStatus === "Deceased";

    return (
        <article
            className={`card citizen-card ${
                isDeceased ? "citizen-card--deceased" : ""
            }`}
        >
            <header className="citizen-card-header">
                <div>
                    <h3 className="citizen-card-title">{person.fullName}</h3>

                    <p className="card-sub citizen-card-sub">
                        {person.sex}, {person.age} y.o. ({person.ageGroup})
                        {isDeceased ? (
                            <span className="citizen-card-sub-status citizen-card-sub-status--deceased">
                                DECEASED {person.deathDate}
                            </span>
                        ) : (
                            <span className="citizen-card-sub-status citizen-card-sub-status--alive">
                                {person.lifeStatus}
                            </span>
                        )}
                    </p>
                </div>

                {onOpen ? (
                    <IconButton
                        size="sm"
                        aria-label="Open resident dossier"
                        title="Open resident dossier"
                        onClick={handleOpen}
                    >
                        <PencilLine size={14}/>
                    </IconButton>
                ) : null}
            </header>

            <section className="citizen-card-body">
                <p>
                    <strong>Marital:</strong> {person.maritalStatus}
                </p>
                <p>
                    <strong>Education:</strong> {person.educationLevel}
                </p>
                <p>
                    <strong>Employment:</strong> {person.employmentStatus}
                    {person.jobTitle ? ` (${person.jobTitle})` : ""}
                </p>
                <p>
                    <strong>Health / Happiness:</strong> {person.health} / {person.happiness}
                </p>
                <p>
                    <strong>Energy / Stress:</strong> {person.energy} / {person.stress}
                </p>
                <p>
                    <strong>Birth date:</strong> {person.birthDate}
                </p>
            </section>
        </article>
    );
};

export default CitizenCard;
