namespace Matrix.BuildingBlocks.Infrastructure.Exceptions
{
    public class NotFoundException(string name, object key)
        : Exception($"Entity {name} with the key {key} not found");
}
