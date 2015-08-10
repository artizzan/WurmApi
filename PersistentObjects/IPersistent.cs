namespace AldursLab.PersistentObjects
{
    public interface IPersistent<out TEntity> where TEntity : Entity, new()
    {
        TEntity Entity { get; }
        void FlagAsChanged();
    }
}