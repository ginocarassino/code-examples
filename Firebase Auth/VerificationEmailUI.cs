using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VerificationEmailUI : MonoBehaviour
{
    [SerializeField] private GameObject signUpPanel;
    [SerializeField] private GameObject verificationPanel;
    [SerializeField] private TextMeshProUGUI emailVerificationText;

    public void ShowVerificationResponse(bool isEmailSent, string emailID, string errorMessage)
    {
        signUpPanel.SetActive(false);
        verificationPanel.SetActive(true);

        if (isEmailSent)
        {
            emailVerificationText.text = $"Please verify your email address \n Verification email has been sent to {emailID}";
        }
        else
        {
            emailVerificationText.text = $"Couldn't sent email: {errorMessage}";
        }
    }
}