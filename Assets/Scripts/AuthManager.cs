using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://sid-restapi.onrender.com/api";
    private string authToken;
    private string username;

    //Para el login register
    public TMP_InputField usernameField, passwordField;
    public TMP_Text feedbackText;
    public Button loginButton, registerButton;

    //Para enviar la data
    public Button sendScoreButton;
    public TMP_Text feedbackSendScore;
    
    //Para el scoreboard
    public Button updateScoreButton;
    public TMP_Text scoreBoardText, feedbackScoreboard;
    public ScoreManager scoreManager;

    //Para los paneles
    public GameObject panelAuth, panelMain, panelProfile;

    //Para no repetir tanto los colores
    private Color32 errorColor = new Color32(255, 61, 135, 255); // Color de errores
    private Color32 successColor = new Color32(37, 150, 28, 255); // Color de cosas buenas :D

    void Start()
    {
        //Se borra credenciales para que se empiece desde 0 siempre
        PlayerPrefs.DeleteKey("token");
        PlayerPrefs.DeleteKey("username");
        PlayerPrefs.Save();

        panelAuth.SetActive(true);
        panelMain.SetActive(false);
        panelMain.SetActive(false);

        updateScoreButton.onClick.AddListener(() => StartCoroutine(GetUsers()));
        sendScoreButton.onClick.AddListener(() => StartCoroutine(SendScoreToAPI()));
    }

    public void RegisterUser()
    {
        StartCoroutine(RegisterCoroutine());
    }

    private IEnumerator RegisterCoroutine()
    {
        string path = apiUrl + "/usuarios";
        string json = JsonUtility.ToJson(new Credentials { username = usernameField.text, password = passwordField.text });

        UnityWebRequest request = UnityWebRequest.Put(path, json);
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            SetFeedback(feedbackText, "Registrado con éxito. Inicia sesión.", successColor);
        }
        else
        {
            string errorMsg = ExtractErrorMessage(request.downloadHandler.text);
            SetFeedback(feedbackText, $"{errorMsg}", errorColor);
        }
    }

    public void LoginUser()
    {
        StartCoroutine(LoginCoroutine());
    }

    private IEnumerator LoginCoroutine()
    {
        string path = apiUrl + "/auth/login";
        string json = JsonUtility.ToJson(new Credentials { username = usernameField.text, password = passwordField.text });

        UnityWebRequest request = UnityWebRequest.Put(path, json);
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            authToken = response.token;
            username = response.usuario.username;

            PlayerPrefs.SetString("token", authToken);
            PlayerPrefs.SetString("username", username);

            Debug.Log($"[AuthManager] Login exitoso - Username: {username}, Token: {authToken}");

            ProfileManager profileManager = FindObjectOfType<ProfileManager>();
            if (profileManager != null)
            {
            Debug.Log($"[AuthManager] Enviando a ProfileManager - Username: {username}");
            profileManager.LoadUserProfile(username, authToken);
            }

            SetFeedback(feedbackText, "¡Login exitoso!", successColor);
            panelAuth.SetActive(false);
            panelMain.SetActive(true);

            StartCoroutine(GetUsers());
        }
        else
        {
            feedbackText.color = errorColor;
            feedbackText.text = "Error: " + request.error;
        }
    }

    private IEnumerator GetUserProfile()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "/usuarios/" + username);
        request.SetRequestHeader("x-token", authToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            panelAuth.SetActive(false);
            panelMain.SetActive(true);
            StartCoroutine(GetUsers());
        }
        else
        {
            feedbackText.color = errorColor;
            feedbackText.text = "Token vencido. Inicia sesión de nuevo.";
        }
    }

    /*
    private IEnumerator UpdateUserScore()
    {
        string url = apiUrl + "/usuarios";
        UnityWebRequest request = new UnityWebRequest(url, "PATCH");
        request.SetRequestHeader("x-token", authToken);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            feedbackMain.color = successColor;
            feedbackMain.text = "Puntaje actualizado";
        }
        else
        {
            feedbackMain.color = errorColor;
            feedbackMain.text = "Error al actualizar puntaje";
        }
    }
    */

    private IEnumerator GetUsers()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "/usuarios");
        request.SetRequestHeader("x-token", authToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UserList response = JsonUtility.FromJson<UserList>(request.downloadHandler.text);
            UserModel[] leaderboard = response.usuarios.OrderByDescending(u => u.data.score).Take(5).ToArray();

            scoreBoardText.text = "";
            foreach (var user in leaderboard)
            {
                scoreBoardText.text += $"<b>{user.username}</b> matriculó <b>{user.data.score}</b> crédito/s\n";
            }

            feedbackScoreboard.color = successColor;
            feedbackScoreboard.text = "Ranking actualizado";
        }
        else
        {
            feedbackScoreboard.color = errorColor;
            feedbackScoreboard.text = "Error al obtener ranking";
        }

        /*
        Debug.Log("Obteniendo ranking de usuarios...");
        Debug.Log("Respuesta del servidor: " + request.downloadHandler.text);
        */

    }

    private IEnumerator SendScoreToAPI()
    {
        int newScore = scoreManager.GetCurrentScore();
        string url = apiUrl + "/usuarios";

        ScoreUpdate updatedData = new ScoreUpdate
        {
            username = PlayerPrefs.GetString("username"),
            data = new DataUser { score = newScore }
        };

        string json = JsonUtility.ToJson(updatedData);
        UnityWebRequest request = new UnityWebRequest(url, "PATCH");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("x-token", authToken);

        /*Debug.Log("Username: " + PlayerPrefs.GetString("username"));
        Debug.Log("Score a enviar: " + newScore);
        Debug.Log("URL de la petición: " + url);
        Debug.Log("JSON enviado: " + json);*/

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            feedbackSendScore.color = successColor;
            feedbackSendScore.text = "Puntaje actualizado";
        }
        else
        {
            feedbackSendScore.color = errorColor;
            feedbackSendScore.text = "Error al actualizar";
        }
    }

    private void SetFeedback(TMP_Text feedbackLabel, string message, Color color)
    {
        feedbackLabel.text = message;
        feedbackLabel.color = color;
    }

    private string ExtractErrorMessage(string jsonResponse)
    {
        try { return JsonUtility.FromJson<ErrorResponse>(jsonResponse).msg; }
        catch { return "Ocurrió un error desconocido."; }
    }
    
}

public class ErrorResponse
{
    public string msg;
}

[System.Serializable]
public class Credentials
{
    public string username;
    public string password;
}

[System.Serializable]
public class AuthResponse
{
    public UserModel usuario;
    public string token;
}

[System.Serializable]
public class UserModel
{
    public string _id;
    public string username;
    public bool estado;
    public DataUser data;
}

[System.Serializable]
public class UserList
{
    public UserModel[] usuarios;
}

[System.Serializable]
public class DataUser
{
    public int score;
}

public class ScoreUpdate
{
    public string username;
    public DataUser data;
}
