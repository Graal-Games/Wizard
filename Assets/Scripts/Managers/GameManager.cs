using Singletons;
using System.Diagnostics;

public class GameManager : NetworkSingleton<GameManager>
{
    public static GameManager gameManager { get; private set; }

    public PlayerHealth _playerHealth = new PlayerHealth(100f, 100f);

    void Awake()
    {
        if (gameManager != null && gameManager != this)
        {
            Destroy(this);
        }
        else
        {
            gameManager = this;
        }
    }

    private void Start()
    {
        print("Game manager active");
    }


    public string CallTest()
    {
        return "Game Manager is referenced";
    }
}
