using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class ProfileManager : MonoBehaviour
{
    [SerializeField] private string apiUrl = "https://sid-restapi.onrender.com/api/usuarios";
    public TMP_Text usernameText;
    public TMP_Text idText;
    public TMP_Text stateText;

    public void LoadUserProfile(string username, string token)
    {
        StartCoroutine(GetUserProfile(username, token));
    }

    private IEnumerator GetUserProfile(string username, string token)
    {
        string url = $"{apiUrl}?username={username}";
        //Debug.Log($"[ProfileManager] Realizando GET a: {url}");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("x-token", token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            //Debug.Log($"[ProfileManager] Respuesta JSON del perfil: {request.downloadHandler.text}");

            UserList response = JsonUtility.FromJson<UserList>(request.downloadHandler.text);

            if (response.usuarios.Length > 0)
            {
                // Buscar el usuario correcto en la lista
                UserModel user = null;
                foreach (UserModel u in response.usuarios)
                {
                    if (u.username == username)
                    {
                        user = u;
                        break; // Detener el loop cuando encuentra el usuario correcto
                    }
                }

                if (user != null)
                {
                    usernameText.text = $"<b>{user.username}</b>";
                    idText.text = $"Id: <b>{user._id}</b>";
                    stateText.text = user.estado ? "Activo" : "Inactivo";

                    //Debug.Log($"[ProfileManager] Perfil cargado correctamente: {user.username}");
                }
                else
                {
                    //Debug.LogError("[ProfileManager] No se encontró el usuario correcto en la lista.");
                }
            }
            else
            {
                //Debug.LogError("[ProfileManager] No se encontró el usuario en la base de datos.");
            }
        }
        else
        {
            //Debug.LogError($"[ProfileManager] Error en la solicitud: {request.error}");
        }
    }
}
