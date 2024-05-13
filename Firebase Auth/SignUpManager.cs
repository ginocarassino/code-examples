using System.Collections;
using Firebase.Auth;
using UnityEngine;
using System;
using Firebase;
using TcgEngine;
using Firebase.Firestore;
using TMPro;

public class SignUpManager : MonoBehaviour
{
    public static SignUpManager Instance;

    [SerializeField] private TextMeshProUGUI signup_error;
    [Header("Verification")]
    [SerializeField] private VerificationEmailUI verificationUI;
    [SerializeField] private GameObject verificationPanel;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator SignUpCoroutine(string email, string username, string password, Action<FirebaseUser> onComplete)
    {
        var usernameCheck = FirebaseManager.Instance.CheckIfDisplayNameIsAvailable(username);
        yield return new WaitUntil(() => usernameCheck.IsCompleted);

        if (usernameCheck.Result.Equals(true))
        {
            Debug.Log("username disponibile");
            var registerTask = FirebaseAuth.DefaultInstance.CreateUserWithEmailAndPasswordAsync(email, password);
            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogError("Registration failed: " + registerTask.Exception);
                onComplete(null);
                yield break;
            }

            FirebaseUser user = registerTask.Result.User;

            if (user == null)
            {
                Debug.LogError("User is null");
                onComplete(null);
                yield break;
            }

            // Chiamata separata per aggiornare il nome utente
            yield return UpdateUsername(user, username);

            Debug.Log("Registration succeeded: " + user.Email);
            SaveToDB(user.UserId, username, user.Email);
            SendEmailForVerification(user);
            onComplete(user);
        }
        else
        {
            Debug.LogError("Username non disponibile");
            signup_error.text = "Username non disponibile";
        }       
    }

    private IEnumerator UpdateUsername(FirebaseUser user, string username)
    {
        UserProfile profile = new UserProfile { DisplayName = username };
        var profileUpdateTask = user.UpdateUserProfileAsync(profile);
        yield return new WaitUntil(() => profileUpdateTask.IsCompleted);

        if (profileUpdateTask.Exception != null)
        {
            Debug.LogError("Username update failed: " + profileUpdateTask.Exception);
            yield break;
        }
    }

    private void SaveToDB(string uid, string username, string email)
    {
        UserData userData = new UserData(uid, username, email);
        _ = FirebaseManager.Instance.SaveData(userData);  // Adesso utilizza il metodo di FirebaseManager
    }

    #region [Verification Email]
    public void SendEmailForVerification(FirebaseUser user)
    {
        StartCoroutine(SendEmailForVerificationAsync(user));
    }

    private IEnumerator SendEmailForVerificationAsync(FirebaseUser user)
    {
        if (user != null)
        {
            var sendEmailTask = user.SendEmailVerificationAsync();

            yield return new WaitUntil(() => sendEmailTask.IsCompleted);
            verificationPanel.SetActive(true);

            if (sendEmailTask.Exception != null)
            {
                FirebaseException firebaseException = sendEmailTask.Exception.GetBaseException() as FirebaseException;
                AuthError error = (AuthError)firebaseException.ErrorCode;

                string errorMessage = "Uknown Error: Please try again later ";

                switch (error)
                {
                    case AuthError.Cancelled:
                        errorMessage = "Email Verification was Cancelled.";
                        break;
                    case AuthError.TooManyRequests:
                        errorMessage = "Too Many Request.";
                        break;
                    case AuthError.InvalidRecipientEmail:
                        errorMessage = "The Email you entered is invalid..";
                        break;
                }

                verificationUI.ShowVerificationResponse(false, user.Email, errorMessage);
            }
            else
            {
                verificationUI.ShowVerificationResponse(true, user.Email, null);
                Debug.Log("Email has successfully sent.");
            }
        }
    }
    #endregion
}