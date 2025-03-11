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
        //Debug.Log($"[Profile] Realizando GET a: {url}");

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("x-token", token);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            //Debug.Log($"[Profile] Respuesta JSON del perfil: {request.downloadHandler.text}");

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
                    idText.text = $"{user._id}";
                    stateText.text = user.estado ? "Activo" : "Inactivo";
                    stateText.color = user.estado ? new Color32(37, 150, 28, 255) : new Color32(255, 61, 135, 255);


                    //Debug.Log($"[Profile] Perfil cargado correctamente: {user.username}");
                }
                else
                {
                    //Debug.LogError("[Profile] No se encontró el usuario correcto en la lista.");
                }
            }
            else
            {
                //Debug.LogError("[Profile] No se encontró el usuario en la base de datos.");
            }
        }
        else
        {
            //Debug.LogError($"[Profile] Error en la solicitud: {request.error}");
        }
    }
}
