public interface IEntity
{
    bool IsBusy { get; }
    bool CanFire();
    void Fire();
}