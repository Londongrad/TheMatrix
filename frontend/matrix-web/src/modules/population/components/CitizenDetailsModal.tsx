import "../../../styles/population/citizen-details-modal.css";
import { useEffect, useState } from "react";
import type {
  PersonDto,
  UpdateCitizenRequest,
} from "../../../api/population/populationTypes";
import {
  killCitizen,
  resurrectCitizen,
  updateCitizen,
} from "../../../api/population/populationApi";
import { useAuth } from "../../../api/identity/AuthContext";

const MARITAL_STATUS_OPTIONS: string[] = [
  "Unknown",
  "Single",
  "Married",
  "Divorced",
  "Widowed",
];

const EMPLOYMENT_STATUS_OPTIONS: string[] = [
  "Unknown",
  "None",
  "Employed",
  "Student",
  "Unemployed",
  "Retired",
];

const EDUCATIONAL_LEVEL_OPTIONS: string[] = [
  "Unknown",
  "None",
  "Primary",
  "Secondary",
  "Vocational",
  "Higher",
  "Postgraduate",
];

// Helper methods
const normalizeEnumValue = (value: string, options: string[]): string =>
  options.includes(value) ? value : "Unknown";

const buildFormStateFromPerson = (p: PersonDto): CitizenFormState => ({
  fullName: p.fullName,
  happiness: p.happiness,

  maritalStatus: normalizeEnumValue(p.maritalStatus, MARITAL_STATUS_OPTIONS),

  educationLevel: normalizeEnumValue(
    p.educationLevel,
    EDUCATIONAL_LEVEL_OPTIONS
  ),

  employmentStatus: normalizeEnumValue(
    p.employmentStatus,
    EMPLOYMENT_STATUS_OPTIONS
  ),
  jobTitle: p.jobTitle ?? "",
});

interface CitizenDetailsModalProps {
  person: PersonDto | null;
  isOpen: boolean;
  onClose: () => void;
  onPersonUpdated?: (person: PersonDto) => void;
}

interface CitizenFormState {
  fullName: string;
  happiness: number;
  maritalStatus: string;
  educationLevel: string;
  employmentStatus: string;
  jobTitle: string;
}

const CitizenDetailsModal = ({
  person,
  isOpen,
  onClose,
  onPersonUpdated,
}: CitizenDetailsModalProps) => {
  const [form, setForm] = useState<CitizenFormState | null>(null);
  const [initialForm, setInitialForm] = useState<CitizenFormState | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  const { token } = useAuth(); // берем access токен

  // когда открываем модалку для нового person — заполняем форму
  useEffect(() => {
    if (!person) {
      setForm(null);
      setInitialForm(null);
      setError(null);
      setIsEditing(false);
      return;
    }

    const state = buildFormStateFromPerson(person); // Собираем форму по общей константе

    setForm(state);
    setInitialForm(state);
    setError(null);
    setIsEditing(false);
  }, [person]);

  const updateFormField = <K extends keyof CitizenFormState>(
    field: K,
    value: CitizenFormState[K]
  ) => {
    setForm((prev) => (prev ? { ...prev, [field]: value } : prev));
  };

  if (!isOpen || !person || !form) return null;

  const isDead = person.lifeStatus === "Deceased";

  const handleKill = async () => {
    if (isDead) return;

    if (!token) {
      setError("Not authenticated.");
      return;
    }

    const confirm = window.confirm(
      `Are you sure you want to kill ${person.fullName}?`
    );
    if (!confirm) return;

    try {
      setIsBusy(true);
      setError(null);

      const updated = await killCitizen(person.id, token);
      onPersonUpdated?.(updated);
      // parent обновит person, useEffect перезабьёт форму
    } catch (e) {
      console.error(e);
      setError("Failed to kill citizen.");
    } finally {
      setIsBusy(false);
    }
  };

  const handleResurrect = async () => {
    if (!isDead) return;

    if (!token) {
      setError("Not authenticated.");
      return;
    }

    try {
      setIsBusy(true);
      setError(null);

      const updated = await resurrectCitizen(person.id, token);
      onPersonUpdated?.(updated);

      const state = buildFormStateFromPerson(updated); // Собираем форму по общей константе

      setForm(state);
      setInitialForm(state);
      setIsEditing(false);
    } catch (e) {
      console.error(e);
      setError("Failed to resurrect citizen.");
    } finally {
      setIsBusy(false);
    }
  };

  const handleHappinessChange = (value: number) => {
    const clamped = Math.min(100, Math.max(0, value));
    updateFormField("happiness", clamped);
  };

  const handleResetHappiness = () => {
    if (!initialForm) return;
    setForm((prev) =>
      prev ? { ...prev, happiness: initialForm.happiness } : prev
    );
  };

  const buildPayload = (): UpdateCitizenRequest => {
    if (!initialForm) return {};
    const payload: UpdateCitizenRequest = {};

    if (initialForm.fullName !== form.fullName) {
      payload.fullName = form.fullName;
    }
    if (initialForm.happiness !== form.happiness) {
      payload.happiness = form.happiness;
    }
    if (initialForm.maritalStatus !== form.maritalStatus) {
      payload.maritalStatus = form.maritalStatus;
    }
    if (initialForm.educationLevel !== form.educationLevel) {
      payload.educationLevel = form.educationLevel;
    }

    const employmentStatusChanged =
      initialForm.employmentStatus !== form.employmentStatus;
    const jobTitleChanged = initialForm.jobTitle !== form.jobTitle;

    // Если изменилось что-то в паре EmploymentStatus/JobTitle — шлём оба поля
    if (employmentStatusChanged || jobTitleChanged) {
      payload.employmentStatus = form.employmentStatus;
      payload.jobTitle =
        form.jobTitle.trim() === "" ? null : form.jobTitle.trim();
    }

    return payload;
  };

  // При отмене редактирования - сбрасываем любые изменения
  const handleToggleEditing = () => {
    setIsEditing((prev) => {
      const next = !prev;

      // Выходим из Edit Mode → откатываем все поля к initialForm
      if (prev && !next && initialForm) {
        setForm(initialForm);
        setError(null);
      }

      return next;
    });
  };

  // Кнопка сохранения
  const handleSave = async () => {
    if (isDead) return; // мёртвых не редактируем
    if (!form) return;
    if (!token) {
      setError("Not authenticated.");
      return;
    }

    const payload = buildPayload();
    if (Object.keys(payload).length === 0) {
      // ничего не изменилось
      setError(null);
      return;
    }

    try {
      setIsBusy(true);
      setError(null);

      const updated = await updateCitizen(person.id, payload, token);
      onPersonUpdated?.(updated);

      const state = buildFormStateFromPerson(person);
      setForm(state);
      setInitialForm(state);
      setIsEditing(false);
    } catch (e) {
      console.error(e);
      setError("Failed to save changes.");
    } finally {
      setIsBusy(false);
    }
  };

  // --- Различные флаги ---
  const hasHappinessChanges = // Для кнопки Reset
    initialForm !== null && initialForm.happiness !== form.happiness;

  const hasFormChanges =
    initialForm !== null &&
    (initialForm.fullName !== form.fullName ||
      initialForm.happiness !== form.happiness ||
      initialForm.maritalStatus !== form.maritalStatus ||
      initialForm.educationLevel !== form.educationLevel ||
      initialForm.employmentStatus !== form.employmentStatus ||
      initialForm.jobTitle !== form.jobTitle);

  // Проверка связанных полей EmploymentStatus & JobTitle
  const isJobTitleEmpty = form.jobTitle.trim() === "";
  const employmentRequiresJobTitle = form.employmentStatus === "Employed";
  const hasEmploymentJobTitleError =
    (employmentRequiresJobTitle && isJobTitleEmpty) ||
    (!employmentRequiresJobTitle && !isJobTitleEmpty);

  const hasErrors = hasEmploymentJobTitleError;

  // Для кнопки SaveChanges
  const cannotSave = !hasFormChanges || isDead || isBusy || hasErrors;

  return (
    <div className="citizens-page-modal-backdrop">
      <div
        className={
          "citizens-page-modal" + (isDead ? " citizens-page-modal--dead" : "")
        }
        onClick={(e) => e.stopPropagation()}
      >
        <header className="citizens-page-modal-header">
          <div>
            <div className="citizens-page-modal-title-row">
              {/* Edit name (hideable) */}
              {isEditing ? (
                <input
                  type="text"
                  className="citizens-page-modal-title-edit-input citizens-page-modal-input-text"
                  disabled={isBusy || isDead}
                  value={form.fullName}
                  onChange={(e) => updateFormField("fullName", e.target.value)}
                />
              ) : (
                <h2 className="citizens-page-modal-title">{form.fullName}</h2>
              )}
            </div>

            <p className="citizens-page-modal-subtitle">
              {person.sex}, {person.age} y.o. ({person.ageGroup})
            </p>
            <p className="citizens-page-modal-subtitle">
              Status: {person.lifeStatus}
              {person.deathDate && ` • Died: ${person.deathDate}`}
            </p>
          </div>

          {/* Action buttons section (edit/close modal) */}
          <div>
            {!isDead && (
              <button
                type="button"
                className="icon-btn"
                disabled={isBusy}
                onClick={handleToggleEditing}
              >
                ✏️
              </button>
            )}

            <button className="icon-btn" onClick={onClose}>
              ✕
            </button>
          </div>
        </header>

        <section className="citizens-page-modal-body">
          <div className="citizens-page-modal-grid">
            <div>
              <h3 className="citizens-page-modal-section-title">Personal</h3>

              {/* Marital status */}
              <div className="citizens-page-modal-field">
                <div className="citizens-page-modal-field-label">Marital</div>
                <select
                  className="citizens-page-modal-select"
                  disabled={isDead || isBusy || !isEditing}
                  value={form.maritalStatus}
                  onChange={(e) =>
                    updateFormField("maritalStatus", e.target.value)
                  }
                >
                  {MARITAL_STATUS_OPTIONS.map((value) => (
                    <option key={value} value={value}>
                      {value}
                    </option>
                  ))}
                </select>
              </div>

              {/* Education */}
              <div className="citizens-page-modal-field">
                <div className="citizens-page-modal-field-label">Education</div>
                <select
                  className="citizens-page-modal-select"
                  disabled={isDead || isBusy || !isEditing}
                  value={form.educationLevel}
                  onChange={(e) =>
                    updateFormField("educationLevel", e.target.value)
                  }
                >
                  {EDUCATIONAL_LEVEL_OPTIONS.map((value) => (
                    <option key={value} value={value}>
                      {value}
                    </option>
                  ))}
                </select>
              </div>

              {/* Happinness */}
              <div className="citizens-page-modal-field">
                <div className="citizens-page-modal-field-row">
                  <div className="citizens-page-modal-field-label">
                    Happiness
                  </div>
                </div>
                <div className="citizens-page-modal-happiness-row">
                  {form.happiness}

                  {/* Edit happiness (hideable) */}
                  {isEditing && (
                    <>
                      <input
                        type="range"
                        min={0}
                        max={100}
                        disabled={isDead || isBusy}
                        value={form.happiness}
                        onChange={(e) =>
                          handleHappinessChange(Number(e.target.value))
                        }
                      />

                      <input
                        type="number"
                        min={0}
                        max={100}
                        disabled={isDead || isBusy}
                        value={form.happiness}
                        onChange={(e) =>
                          handleHappinessChange(Number(e.target.value))
                        }
                        className="citizens-page-modal-input-number"
                      />

                      <button
                        type="button"
                        className="btn btn-sm"
                        disabled={isDead || isBusy || !hasHappinessChanges}
                        onClick={handleResetHappiness}
                      >
                        Reset
                      </button>
                    </>
                  )}
                </div>
              </div>
            </div>

            {/* Employment */}
            <div>
              <h3 className="citizens-page-modal-section-title">Employment</h3>

              <div className="citizens-page-modal-field">
                <div className="citizens-page-modal-field-label">
                  Employment status
                </div>
                <select
                  className="citizens-page-modal-select"
                  disabled={isDead || isBusy || !isEditing}
                  value={form.employmentStatus}
                  onChange={(e) =>
                    updateFormField("employmentStatus", e.target.value)
                  }
                >
                  {EMPLOYMENT_STATUS_OPTIONS.map((value) => (
                    <option key={value} value={value}>
                      {value}
                    </option>
                  ))}
                </select>
              </div>

              {/* Job title */}
              <div className="citizens-page-modal-field">
                <div className="citizens-page-modal-field-label">Job title</div>
                <input
                  type="text"
                  className="citizens-page-modal-input-text"
                  disabled={isDead || isBusy || !isEditing}
                  value={form.jobTitle}
                  onChange={(e) => updateFormField("jobTitle", e.target.value)}
                  placeholder="—"
                />
                {/* Hideable validation error */}
                {hasEmploymentJobTitleError && (
                  <p className="citizens-page-modal-error">
                    {employmentRequiresJobTitle &&
                      "Job title is required when employment status is Employed."}
                    {!employmentRequiresJobTitle && "Job title must be empty."}
                  </p>
                )}
              </div>
            </div>
          </div>

          {/* Errors */}
          {error && <p className="error-text">{error}</p>}
        </section>

        {/* Footer with kill, resurrect and save actions */}
        <footer className="citizens-page-modal-footer">
          <div className="citizens-page-modal-footer-group">
            {!isDead && (
              <button
                className="btn btn-danger btn-sm"
                disabled={isBusy}
                onClick={handleKill}
              >
                Kill citizen
              </button>
            )}

            {isDead && (
              <button
                className="btn btn-success btn-sm"
                disabled={isBusy}
                onClick={handleResurrect}
              >
                Resurrect citizen
              </button>
            )}
          </div>

          <div className="citizens-page-modal-footer-group">
            {isEditing && (
              <button
                className="btn btn-sm"
                disabled={cannotSave}
                onClick={handleSave}
              >
                Save changes
              </button>
            )}
          </div>
        </footer>
      </div>
    </div>
  );
};

export default CitizenDetailsModal;
