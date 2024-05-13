using System.Collections;
using Firebase.Auth;
using UnityEngine;
using System;
using Firebase;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator LoginCoroutine(string email, string password, Action<FirebaseUser> onComplete, Action onEmailNotVerified)
    {
        var loginTask = FirebaseAuth.DefaultInstance.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError("Login failed: " + loginTask.Exception);
            
            onComplete(null);
            yield break;
        }

        FirebaseUser user = loginTask.Result.User;

        if (user.IsEmailVerified)
        {
            Debug.Log("Login succeeded: " + user.Email);
            onComplete(user);
        }
        else
        {
            Debug.LogError("Email not verified, please verify first.");
            onEmailNotVerified?.Invoke();
        }
    }

}