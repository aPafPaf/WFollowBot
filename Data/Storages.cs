namespace WFollowBot.Data;

public class Storages
{
    public StoragePath Path { get; private set; }
    public EntityStorage EntitiesToAttack { get; private set; }
    public EntityStorage EntitiesAreaTransitionOnScreen { get; private set; }
    public EntityStorage EntitiesInteractingOnScreen { get; private set; }

    public Storages()
    {
        Path = new StoragePath();

        EntitiesToAttack = new EntityStorage(248);
        EntitiesAreaTransitionOnScreen = new EntityStorage(16);
        EntitiesInteractingOnScreen = new EntityStorage(16);
    }
}
