using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using System;
using Firebase.Extensions;
using TcgEngine;
using Firebase;
using System.Collections;
using Firebase.Firestore;
using Google;
using System.Threading.Tasks;

public class AuthManager : MonoBehaviour
{
    [Header("UI Input")]
    [SerializeField] private TMP_InputField in_email;
    [SerializeField] private TMP_InputField in_password;
    [SerializeField] private Toggle tog_Remember;

    [SerializeField] private TMP_InputField up_email;
    [SerializeField] private TMP_InputField up_username;
    [SerializeField] private TMP_InputField up_password;

    [Header("UI Error")]
    [SerializeField] private TextMeshProUGUI login_error;
    [SerializeField] private TextMeshProUGUI signup_error;

    [Header("UI Panels")]
    [SerializeField] private GameObject verifyEmail_Panel;

    [Header("Reset Password")]
    [SerializeField] private TMP_InputField resetEmailInputField;
    [SerializeField] private TextMeshProUGUI reset_message;

    [Header("Other")]
    public Action OnUserLoggedIn;
    public Action OnUserSignedUp;

    private FirebaseAuth auth;
    private FirebaseUser user;

    [SerializeField] private Loading loadScene;
    private bool isAuthenticated = false;
    private bool rememberMeActivated = false;

    // Google Sign In variables
    private string GoogleWebAPI = "***";
    private GoogleSignInConfiguration configuration;

    [Header("UI Username")]
    [SerializeField] public GameObject insertUsername_canvas;
    [SerializeField] public GameObject buttons_container;
    [SerializeField] private TMP_InputField insertUsername_input;
    [SerializeField] private Button acceptUsername_btn;
    [SerializeField] private TextMeshProUGUI usernameAlreadyTaken_text;

    private void Awake()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = GoogleWebAPI,
            RequestIdToken = true
        };

    }

    private void Start()
    {
        StartCoroutine(CheckAndFixDependenciesAsync());
    }

    public IEnumerator CheckAndFixDependenciesAsync()
    {
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();

        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        if (dependencyTask.Result == DependencyStatus.Available)
            InitializeFirebase();
        else
            Debug.LogError("Could not resolve all firebase dependencies: " + dependencyTask.Result);
    }

    private static bool IsInitialized = false;
    public void InitializeFirebase()
    {
        if (!IsInitialized)
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            FirebaseManager.Instance.db = FirebaseFirestore.GetInstance(app);
            FirebaseManager.Instance.db.Settings.PersistenceEnabled = false;
            IsInitialized = true;
        }

        auth = FirebaseAuth.DefaultInstance;

        CheckRembemberMeOption();

        OnUserLoggedIn += LoadSceneAfterLogin;
        OnUserSignedUp += LoadSceneAfterSignup;
        auth.StateChanged += HandleAuthStateChanged;
        HandleAuthStateChanged(this, null);

        //faccio il refresh della data
        FirebaseManager.Instance.GetCurrentDate();
    }

    private void CheckRembemberMeOption()
    {
        rememberMeActivated = PlayerPrefs.GetInt("rememberMe") == 1;
        if (FirebaseAuth.DefaultInstance.CurrentUser != null && rememberMeActivated)
        {
            isAuthenticated = true;
            loadScene.ActiveOrDisableLoadingScreen(true, 0f);
        }else
            loadScene.ActiveOrDisableLoadingScreen(false, 1f);
    }

    private async void HandleAuthStateChanged(object sender, EventArgs e)
    {
        if (auth.CurrentUser != null)
            await auth.CurrentUser.ReloadAsync();

        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            Debug.Log("HandleAuthStateChanged called");
            if (!signedIn && user != null)
            {
                Debug.Log("User disconnected");
            }

            user = auth.CurrentUser;

            if (signedIn && isAuthenticated)
            {
                Debug.Log($"User connected: {auth.CurrentUser.UserId}");
                OnLoginComplete(user);
            }
        }
    }

    #region GOOGLE LOGIN

    FirebaseUser googleUser;

    public void GoogleSignInClick()
    {
        if (GoogleSignIn.Configuration == null)
            GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;
        GoogleSignIn.Configuration.RequestEmail = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleAuthenticationFinished);
    }

    private void OnGoogleAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
        }
        else if (task.IsCanceled)
        {
        }
        else
        {
            Firebase.Auth.Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(task.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                }
                else if (task.IsCanceled)
                {
                }
                else
                {
                    // Selezione account andata a buon fine
                    googleUser = task.Result;

                    TryLogin();
                }
            });
        }
    }

    private void TryLogin()
    {
        CollectionReference dbCollection_Users = FirebaseManager.Instance.db.Collection("users");
        DocumentReference documentReference = dbCollection_Users.Document(googleUser.UserId);

        FirebaseManager.Instance.db.Collection("users").Document(googleUser.UserId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot user = task.Result;
            if (user.Exists)
            {
                // l'id esiste nel DB
                // vai dentro la scena menu

                OnLoginComplete(googleUser);
            }
            else
            {
                // id non esiste nel DB

                // apri pannello per inserire username
                insertUsername_canvas.SetActive(true);
                // hide buttons
                buttons_container.SetActive(false);
            }
        });
    }

    // Accept Username button
    public void NewUserFromGoogleAccount()
    {
        string usernameFromInput = insertUsername_input.text;

        FirebaseManager.Instance.db.Collection("displayNames").Document(usernameFromInput).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            DocumentSnapshot user = task.Result;
            if (user.Exists)
            {
                // Mostra testo di warning per cambiare username
                usernameAlreadyTaken_text.gameObject.SetActive(true);
            }
            else
            {
                UserProfile profile = new UserProfile { DisplayName = usernameFromInput };
                googleUser.UpdateUserProfileAsync(profile).ContinueWithOnMainThread(async task =>
                {
                    UserData userData = new UserData(googleUser.UserId, googleUser.DisplayName, googleUser.Email);
                    await FirebaseManager.Instance.SaveData(userData);

                    if (task.IsCanceled)
                    {
                    }
                    else if (task.IsFaulted)
                    {
                    }
                    else
                    {
                        OnLoginComplete(googleUser);
                    }
                });
            }
        });
    }

    #endregion

    #region LOGIN

    public void Login()
    {
        if (AreFieldsValid(new TMP_InputField[] { in_email, in_password }))
        {
            //registro la posizione del toggle del login
            if (tog_Remember != null)
            {
                if (tog_Remember.isOn)
                    PlayerPrefs.SetInt("rememberMe", 1);
                else
                    PlayerPrefs.SetInt("rememberMe", 0);
            }

            StartCoroutine(LoginManager.Instance.LoginCoroutine(in_email.text, in_password.text, OnLoginComplete, OnLoginFailed));
        }
    }

    async void OnLoginComplete(FirebaseUser user)
    {
        try
        {
            await user.ReloadAsync();
            if (user.IsEmailVerified || user.IsAnonymous)
            {
                isAuthenticated = true;
                var task = FirebaseManager.Instance.UpdateUserInfo(user.UserId);
                //var task = FirebaseManager.Instance.FetchUserData();
                await task;
                if (task.IsCompletedSuccessfully)
                {
                    Debug.Log("User info loaded after login.");
                    //questa istruzione serve per passare i valori di login alla scena del menu
                    if (user.IsAnonymous)
                        await Authenticator.Get().Login(user.DisplayName, "");
                    else
                        await Authenticator.Get().Login(user.DisplayName, in_password.text);
                    OnUserLoggedIn?.Invoke();
                }
            }
            else
            {
                isAuthenticated = false;
                OnLoginFailed();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to reload user: {e}");
            isAuthenticated = false;
            OnLoginFailed();
        }
    }

    void OnLoginFailed()
    {
        isAuthenticated = false;
        login_error.text = "Wrong Email or Password, or verify your email before logging in";
    }

    void LoadSceneAfterLogin()
    {
        if (FirebaseManager.Instance.GetUserData().HasReward("tutorialDone"))
        {
            loadScene.FadeAndLoadLevel();
        }
        else
        {
            loadScene.FadeAndLoadLevel(true);
        }
    }
    #endregion

    #region ANONYMOUS LOGIN

    public void LoginAnonymously()
    {
        if (auth == null)
            auth = FirebaseAuth.DefaultInstance;

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                OnLoginFailed();
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                OnLoginFailed();
                return;
            }

            AuthResult result = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                result.User.DisplayName, result.User.UserId);

            OnAnonymousLoginComplete(result.User);
        });
    }

    public async void OnAnonymousLoginComplete(FirebaseUser User)
    {
        if (await FirebaseManager.Instance.CheckIfUserIdExisting(User.UserId))
            OnLoginComplete(User);
        else
        {
            bool usernameCheck;
            int index = 0;
            string username = "Player" + UnityEngine.Random.Range(1, 999999);

            do
            {
                usernameCheck = await FirebaseManager.Instance.CheckIfDisplayNameIsAvailable(username);
                index++;
            } while (!usernameCheck && index < 20);

            if (index >= 20)
            {
                Debug.LogError("Timeout creazione username random");
                OnLoginFailed();
            }

            UserData userData = new UserData(User.UserId, username, "");
            await FirebaseManager.Instance.SaveData(userData);

            UserProfile profile = new UserProfile();
            profile.DisplayName = username;
            await user.UpdateUserProfileAsync(profile);

            //serve per capire se l'utente ha già cambiato username
            PlayerPrefs.SetInt(userData.username, 0);

            OnLoginComplete(User);
        }
    }

    #endregion

    #region SIGNUP
    public void SignUp()
    {
        if (AreFieldsValid(new TMP_InputField[] { up_email, up_username, up_password }))
        {
            StartCoroutine(SignUpManager.Instance.SignUpCoroutine(up_email.text, up_username.text, up_password.text, OnSignUpComplete));
        }
    }

    void OnSignUpComplete(FirebaseUser user)
    {
        isAuthenticated = false;
    }

    void LoadSceneAfterSignup()
    {
        verifyEmail_Panel.SetActive(true);
    }
    #endregion

    #region RESET PASSWORD
    public void SendPasswordResetEmail()
    {
        reset_message.text = "";
        string emailAddress = resetEmailInputField.text;

        if (!String.IsNullOrEmpty(emailAddress))
        {
            auth.SendPasswordResetEmailAsync(emailAddress).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    reset_message.text = "Email per il reset della password inviato.";
                }
                else
                {
                    Debug.LogError("Non � stato possibile inviare l'email per il reset della password: " + task.Exception);
                    reset_message.text = "Non � stato possibile inviare l'email per il reset della password";
                }
            });
        }
        else
            reset_message.text = "Campo vuoto.";
    }
    #endregion

    public bool AreFieldsValid(TMP_InputField[] fields)
    {
        login_error.text = "";
        signup_error.text = "";

        foreach (var field in fields)
        {
            if (string.IsNullOrEmpty(field.text))
            {
                Debug.LogError($"The {field.name} field is empty.");
                login_error.text = $"The {field.name} field is empty.";
                signup_error.text = $"The {field.name} field is empty.";
                return false;
            }
        }
        return true;
    }
}