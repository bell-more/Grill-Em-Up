using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }
    public MainMenu MainMenuRef { get; set; }
    public static bool showConnectionLostOnLoad = false;
    private NetworkManager NetManager => NetworkManager.Singleton;
    private bool isLeavingIntentionally = false;
    private ISession session;

    public event Action OnSessionChange;

    private const string GAME_SCENE = "2.LevelSelector";
    private const string KEY_MATCH_TYPE = "MatchType";
    private const string KEY_PLAYER_NAME = "PlayerName";
    private const string KEY_PLAYER_READY = "PlayerReady";

    private bool isStartingGame;

    public ISession GetSession() => session;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        if (NetManager != null)
        {
            NetManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
            NetManager.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            NetManager.OnServerStarted += NetworkManager_OnServerStarted;
            NetManager.OnServerStopped += NetworkManager_OnServerStopped;
        }

        await InitializeUGS();
    }

    private async Task<bool> InitializeUGS()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public async Task InitializeLobbyAsync(string hostName = default, int maxPlayers = 4, bool isPrivate = true, MatchType matchType = MatchType.CookingCoop)
    {
        try
        {
            if (!await InitializeUGS()) return;

            hostName = "Chef 1";

            var options = new SessionOptions
            {
                Name = hostName,
                MaxPlayers = maxPlayers,
                IsPrivate = isPrivate
            };

            session = await MultiplayerService.Instance.CreateSessionAsync(options);
            SuscribeEvents();

            if (session is IHostSession hostSession)
            {
                hostSession.SetProperties(new Dictionary<string, SessionProperty>
                {
                    { KEY_MATCH_TYPE, new SessionProperty(matchType.ToString(), VisibilityPropertyOptions.Public) }
                });

                hostSession.CurrentPlayer.SetProperty(KEY_PLAYER_NAME, new PlayerProperty(hostName, VisibilityPropertyOptions.Public));
                await hostSession.SaveCurrentPlayerDataAsync();
                await hostSession.SavePropertiesAsync();
            }

            MainMenuRef?.ShowLobbyPanel();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task JoinLobbyWithCode(string joinCode, string playerName = default)
    {
        try
        {
            if (!await InitializeUGS()) return;

            session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
            SuscribeEvents();

            int playerNumber = session.Players.Count;
            playerName = "Chef " + playerNumber;

            session.CurrentPlayer.SetProperty(KEY_PLAYER_NAME, new PlayerProperty(playerName, VisibilityPropertyOptions.Public));
            await session.SaveCurrentPlayerDataAsync();

            MainMenuRef?.ShowLobbyPanel();
        }
        catch
        {
            MainMenuRef?.JoinError();
        }
    }

    public async Task SetReadyAsync(bool isReady = true)
    {
        if (session == null) return;

        session.CurrentPlayer.SetProperty(KEY_PLAYER_READY, new PlayerProperty(isReady ? "True" : "False", VisibilityPropertyOptions.Public));
        await session.SaveCurrentPlayerDataAsync();

        OnSessionChange?.Invoke();
    }

    private void SuscribeEvents()
    {
        if (session != null)
        {
            session.Changed += Session_Changed;
            session.Deleted += Session_Deleted; 
        }
    }

    private void DeSuscribeEvents()
    {
        if (session != null)
        {
            session.Changed -= Session_Changed;
            session.Deleted -= Session_Deleted;
        }
    }

    private async void Session_Changed()
    {
        OnSessionChange?.Invoke();

        if (session == null) return;
        if (SceneManager.GetActiveScene().buildIndex != 0) return;

        int readyCount = 0;
        int totalPlayers = session.Players.Count;

        foreach (var player in session.Players)
        {
            if (player.Properties.TryGetValue(KEY_PLAYER_READY, out var r) && r.Value == "True")
            {
                readyCount++;
            }
        }

        if (readyCount >= totalPlayers && totalPlayers > 0)
        {
            MainMenuRef?.ShowLoadingScreen();

            if (session.IsHost && !isStartingGame)
            {
                isStartingGame = true;

                try
                {
                    var hostSession = session.AsHost();
                    hostSession.IsLocked = true;
                    await hostSession.SavePropertiesAsync();

                    await hostSession.Network.StartRelayNetworkAsync(new RelayNetworkOptions());

                    var status = NetManager.SceneManager.LoadScene(GAME_SCENE, LoadSceneMode.Single);
                }
                catch (Exception e)
                {
                    Debug.Log("Error starting game "+e);
                    isStartingGame = false;
                }
            }
        }
    }

    private void Session_Deleted()
    {
        if (isLeavingIntentionally) return;
        ReturnToMainMenu();
    }
    public async Task LeaveSessionAsync()
    {
        if (session != null)
        {
            try
            {
                isLeavingIntentionally = true;
                isStartingGame = false;
                DeSuscribeEvents();
                if (session.IsHost)
                {
                    var hostSession = session.AsHost();
                    if (hostSession != null) await hostSession.DeleteAsync();
                }
                else
                {
                    await session.LeaveAsync();
                }

                session = null;
                isStartingGame = false;

                if (NetManager != null)
                {
                    NetManager.Shutdown();
                }

                if (SceneManager.GetActiveScene().buildIndex != 0)
                {
                    SceneManager.LoadScene(0);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                Invoke(nameof(ResetLeavingFlag), 1f);
            }
        }
    }
    private void ResetLeavingFlag()
    {
        isLeavingIntentionally = false;
    }
    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
        {
            Debug.Log("host Connected");
        }
        else
        {
            Debug.Log("client Connected");
        }
    }

    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (isLeavingIntentionally) return;

        if (clientId == NetworkManager.ServerClientId)
        {
            ReturnToMainMenu();
        }
    }

    private void ReturnToMainMenu()
    {
        session = null;
        isStartingGame = false;

        if (NetManager != null)
        {
            NetManager.Shutdown();
        }

        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            showConnectionLostOnLoad = true;
            SceneManager.LoadScene(0);
        }
        else
        {
            MainMenuRef?.ConnectionLostPanel();
        }
    }
    private void NetworkManager_OnServerStarted() { }
    private void NetworkManager_OnServerStopped(bool didTimeout) { }

    private void OnDestroy()
    {
        DeSuscribeEvents();

        if (NetManager != null)
        {
            NetManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
            NetManager.OnClientDisconnectCallback -= NetworkManager_OnClientDisconnectCallback;
            NetManager.OnServerStarted -= NetworkManager_OnServerStarted;
            NetManager.OnServerStopped -= NetworkManager_OnServerStopped;
        }
    }
}

public enum MatchType
{
    FlagCapture,
    CookingCoop
}