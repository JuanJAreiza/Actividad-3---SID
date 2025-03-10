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

    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_Text feedbackText;
    public Button loginButton;
    public Button registerButton;

    public Button updateScoreButton;
    public TMP_Text scoreBoardText;
    public TMP_Text feedbackMain;

    public GameObject panelAuth;
    public GameObject panelMain;

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

        loginButton.onClick.AddListener(LoginUser);
        registerButton.onClick.AddListener(RegisterUser);
        updateScoreButton.onClick.AddListener(() => StartCoroutine(UpdateUserScore()));
    }

    public void RegisterUser()
    {
        StartCoroutine(RegisterCoroutine());
    }

    private IEnumerator RegisterCoroutine()
    {
        var credentials = new Credentials
        {
            username = usernameField.text,
            password = passwordField.text
        };

        string json = JsonUtility.ToJson(credentials);
        UnityWebRequest request = new UnityWebRequest(apiUrl + "/usuarios", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
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
        var credentials = new Credentials
        {
            username = usernameField.text,
            password = passwordField.text
        };

        string json = JsonUtility.ToJson(credentials);
        UnityWebRequest request = new UnityWebRequest(apiUrl + "/auth/login", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AuthResponse response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
            authToken = response.token;
            PlayerPrefs.SetString("token", response.token);
            PlayerPrefs.SetString("username", response.usuario.username);

            feedbackText.color = successColor;
            feedbackText.text = "¡Login exitoso!";
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

    private IEnumerator GetUsers()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl + "/usuarios");
        request.SetRequestHeader("x-token", authToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            UserList response = JsonUtility.FromJson<UserList>(request.downloadHandler.text);
            UserModel[] leaderboard = response.usuarios.OrderByDescending(u => u.data.score).Take(5).ToArray();

            scoreBoardText.text = "Top 5 Jugadores:\n";
            foreach (var user in leaderboard)
            {
                scoreBoardText.text += $"{user.username} | {user.data.score}\n";
            }

            feedbackMain.color = successColor;
            feedbackMain.text = "Ranking actualizado";
        }
        else
        {
            feedbackMain.color = errorColor;
            feedbackMain.text = "Error al obtener ranking";
        }
    }

    private void SetFeedback(TMP_Text feedbackLabel, string message, Color color)
    {
        feedbackLabel.text = message;
        feedbackLabel.color = color;
    }

    private string ExtractErrorMessage(string jsonResponse)
    {
        try
        {
            ErrorResponse errorResponse = JsonUtility.FromJson<ErrorResponse>(jsonResponse);
            return errorResponse.msg; //Errores de la API
        }
        catch
        {
            return "Ocurrió un error desconocido.";
        }
    }

    public class ErrorResponse
    {
        public string msg;
    }
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
