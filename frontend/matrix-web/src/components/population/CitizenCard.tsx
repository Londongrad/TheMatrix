import React from "react";
import type { PersonDto } from "../../api/population/types";

interface CitizenCardProps {
  person: PersonDto;
}

const CitizenCard: React.FC<CitizenCardProps> = ({ person }) => {
  return (
    <div className="card citizen-card">
      <h3 className="card-title">{person.fullName}</h3>
      <p className="card-sub">
        {person.sex}, {person.age} y.o. ({person.ageGroup})
      </p>

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
        <strong>Happiness:</strong> {person.happiness}
      </p>
    </div>
  );
};

export default CitizenCard;
