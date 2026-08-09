public class GameManager : Singleton<GameManager>
{
    public float clearRatio;
    public int deathCount;
    public bool isDead;
    public bool stageClear;

    public void Reset()
    {
        clearRatio = 80f;
        deathCount = 3;
        isDead = false;
        stageClear = false;
    }
}
