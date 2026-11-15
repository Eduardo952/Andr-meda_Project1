using System;
using System.Collections;
using Ares.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Ares.World
{
    /// <summary>Bounded APOD requests with a verified, attributed offline collection. No game rules.</summary>
    public sealed class ApodService : MonoBehaviour
    {
        readonly Texture2D[] downloaded = new Texture2D[5];
        readonly bool[] attempted = new bool[5];
        readonly bool[] loading = new bool[5];
        readonly string[] status = new string[5];
        readonly ApodRecord[] records = new ApodRecord[5];
        public event Action Changed;
        public ObservatoryCollection Collection { get; private set; }
        public bool OfflineOnly { get; set; }
        public int RequestCount { get; private set; }

        void Awake()
        {
            var text = Resources.Load<TextAsset>("ARES/Observatory/collection");
            Collection = JsonUtility.FromJson<ObservatoryCollection>(text.text.TrimStart('\uFEFF'));
            if (Collection.entries == null || Collection.entries.Length != 5)
                throw new InvalidOperationException("The observatory requires five curated APOD entries.");
        }

        public Texture2D Image(int day) => downloaded[day] != null ? downloaded[day]
            : Resources.Load<Texture2D>(Collection.entries[day].resource);
        public string Status(int day) => status[day] ?? "Imagem real do arquivo APOD · cópia local";
        public bool Loading(int day) => loading[day];
        public ApodRecord Record(int day) => records[day];

        public void Load(int day, bool retry = false)
        {
            if (day < 0 || day >= 5 || loading[day] || (attempted[day] && !retry)) return;
            attempted[day] = true;
            if (OfflineOnly) { status[day] = "Modo offline · imagem real do arquivo APOD"; Changed?.Invoke(); return; }
            StartCoroutine(Fetch(day));
        }

        IEnumerator Fetch(int day)
        {
            loading[day] = true;
            status[day] = "Consultando a NASA… Você já pode explorar a cópia local.";
            Changed?.Invoke();
            string key = Environment.GetEnvironmentVariable("NASA_API_KEY");
            if (string.IsNullOrWhiteSpace(key)) key = "DEMO_KEY";
            string endpoint = Collection.endpoint;
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var address) || address.Scheme != "https")
            { Finish(day, "Conexão indisponível · imagem real do arquivo APOD"); yield break; }
            string query = endpoint + (endpoint.Contains("?") ? "&" : "?") + "date=" + Collection.entries[day].date;
            // A configured proxy owns its credentials. Never send the NASA key to another host.
            if (address.Host == "api.nasa.gov") query += "&api_key=" + UnityWebRequest.EscapeURL(key);
            ApodRecord record = null;
            using (var request = UnityWebRequest.Get(query))
            {
                RequestCount++;
                request.timeout = 12;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try { record = JsonUtility.FromJson<ApodRecord>(request.downloadHandler.text); }
                    catch (ArgumentException) { }
                }
            }
            if (record == null || !record.ValidFor(Collection.entries[day].date))
            { Finish(day, "A NASA não enviou uma foto válida. Mostrando o arquivo verificado."); yield break; }

            using (var request = UnityWebRequestTexture.GetTexture(record.ImageUrl, true))
            {
                request.timeout = 15;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                { Finish(day, "Não foi possível baixar a foto. A cópia local continua disponível."); yield break; }
                var texture = DownloadHandlerTexture.GetContent(request);
                if (texture.width < 128 || texture.height < 128 || texture.width > 8192 || texture.height > 8192)
                { Destroy(texture); Finish(day, "Imagem inválida · mostrando o arquivo verificado."); yield break; }
                if (downloaded[day] != null) Destroy(downloaded[day]);
                downloaded[day] = texture;
                records[day] = record;
            }
            Finish(day, "Imagem recebida pela API APOD da NASA · " + record.date);
        }

        void Finish(int day, string message)
        {
            loading[day] = false;
            status[day] = message;
            Changed?.Invoke();
        }

        void OnDestroy()
        {
            StopAllCoroutines();
            foreach (var texture in downloaded) if (texture != null) Destroy(texture);
        }
    }
}
