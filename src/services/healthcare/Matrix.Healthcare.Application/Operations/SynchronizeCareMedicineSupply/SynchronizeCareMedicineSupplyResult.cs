namespace Matrix.Healthcare.Application.Operations.SynchronizeCareMedicineSupply;

public sealed record SynchronizeCareMedicineSupplyResult(
    SynchronizeCareMedicineSupplyStatus Status,
    bool StateCreated,
    bool StateUpdated);
