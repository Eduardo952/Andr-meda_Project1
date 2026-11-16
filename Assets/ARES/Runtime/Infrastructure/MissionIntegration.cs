using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ares.Domain;
using Ares.Infrastructure.Nasa;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Ares.Infrastructure
{
    /// <summary>Native NASA/Groq facade. Network responses never award resources or mutate mission rules.</summary>
    public sealed class MissionIntegration : MonoBehaviour
    {
        public NasaConfig Configuration { get; private set; } = new NasaConfig();
        public NasaSolService Nasa { get; private set; }
        public BubbleNarrator Narrator { get; private set; }
        public ImageLoader Images { get; private set; }
        public Task Ready { get; private set; }
        public string MissionId { get; private set; } = Guid.NewGuid().ToString("N");
        public SolResponse CurrentSol { get; private set; }
        public bool Busy { get; private set; }
        public string Message { get; private set; } = "Explore a imagem e consulte o assistente.";
        public string Mood { get; private set; } = "happy";
        public event Action Changed;
        readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        CancellationTokenSource mission = new CancellationTokenSource();
        HttpJsonClient http; float nextAllowed; int loadedSol; bool destroyed;
        PromptBuilder promptBuilder;
        public Ares.Infrastructure.Hazards.MissionScenarioService Scenarios {get;private set;}
        public Ares.Domain.Hazards.ScenarioResult Scenario {get;private set;}
        Task<Ares.Domain.Hazards.ScenarioResult> scenarioTask; Campaign boundCampaign;int boundSol;
        void Awake() { Ready = InitializeAsync(); }
        async Task InitializeAsync()
        {
            string assets = Application.streamingAssetsPath, disk = Application.persistentDataPath;
            var transport = new UnityHttpTransport(this);
            string system = Resources.Load<TextAsset>("ARES/Narrator/bubble-system").text;
            var hazardPrompt=Resources.Load<TextAsset>("ARES/Hazards/narrator-rules");if(hazardPrompt!=null)system+="\n"+hazardPrompt.text;
            string questions = Resources.Load<TextAsset>("ARES/Narrator/questions").text;
            var fallbackImage = Resources.Load<Texture2D>("ARES/Observatory/day1");
            try { Configuration = await Task.Run(() => NasaConfig.Load(Path.Combine(assets, "integrations.json")), lifetime.Token); }
            catch (OperationCanceledException) { return; }
            catch { Configuration = new NasaConfig(); Message = "Confira integrations.json; usando as configurações padrão."; }
            bool development = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            development = true;
            if (string.IsNullOrWhiteSpace(Configuration.GroqProxyBaseUrl)) Debug.LogWarning("ARES: modo privado direto do Groq. Credenciais locais não são exibidas; use um proxy antes de distribuir publicamente.");
#endif
            ApiCredentials credentials;
            try { credentials = await new LocalSecretsProvider(Path.Combine(assets, "secrets.local.json"), Path.Combine(disk, "secrets.local.json"), development).LoadAsync(lifetime.Token); }
            catch (OperationCanceledException) { return; }
            if (destroyed) return;
            http = new HttpJsonClient(transport, Configuration.RetryCount);
            Nasa = new NasaSolService(Configuration, credentials, http, new SolDataCache(Path.Combine(disk, "NasaSolCache")), legacyFolder: Path.Combine(disk, "LegacyN8nResponses"));
            var catalog=Resources.Load<TextAsset>("ARES/Hazards/catalog");var snapshots=Resources.Load<TextAsset>("ARES/Hazards/snapshots");
            var hazardProfile=Resources.Load<Ares.Infrastructure.Hazards.ScenarioProfile>("ARES/Hazards/ScenarioProfile");
            Scenarios=new Ares.Infrastructure.Hazards.MissionScenarioService(Nasa,catalog==null?null:NasaJson.Read<Ares.Domain.Hazards.HazardCatalog>(catalog.text),snapshots==null?null:NasaJson.Read<Ares.Domain.Hazards.ScenarioResult[]>(snapshots.text),Path.Combine(disk,"HazardScenarios"),hazardProfile?.provisionalBalance,hazardProfile?.slots);
            var prompts = new PromptBuilder(system, questions);
            promptBuilder = prompts;
            Images = new ImageLoader(this, Path.Combine(disk, "NasaImages"), fallbackImage, Configuration.ImageMemoryLimit);
            var names = new Dictionary<string,string[]>();
            var definition = GetComponent<Ares.World.ReferenceExplorer>().definition;
            var en = new Ares.UI.Localization("en"); var pt = new Ares.UI.Localization("pt-BR");
            var templates = new LocalNarratorTemplates((lang,key)=>(lang=="en"?en:pt).T(key));
            Narrator = new BubbleNarrator(prompts, new GroqLlmClient(http, Configuration, credentials, prompts), templates.For);
            foreach (var sol in definition.campaign.sols) foreach (var task in sol.tasks) names[task.id] = new[]{en.T("mini."+task.id+".name"),pt.T("mini."+task.id+".name")};
            NarrationValidator.TaskNames = names;
        }
        public void ResetMission() => ResumeMission(Guid.NewGuid().ToString("N"));
        public void ResumeMission(string missionId)
        {
            mission.Cancel(); mission.Dispose(); mission = new CancellationTokenSource();
            MissionId = MissionPlanner.MissionId(missionId, DateTime.UtcNow); loadedSol = 0; CurrentSol = null;
            Scenario=null;scenarioTask=null;boundCampaign=null;boundSol=0;
            Busy = false; nextAllowed = 0; Message = "Explore a imagem e consulte o assistente."; Changed?.Invoke();
        }
        public async Task<SolResponse> GetSolAsync(int sol, CancellationToken cancellation = default)
        {
            string id = MissionId; var owner = mission.Token;
            await Ready; owner.ThrowIfCancellationRequested(); cancellation.ThrowIfCancellationRequested();
            if (Nasa == null) return NasaSolService.Offline(id, sol);
            if(boundCampaign?.NativeHazards==true) {
                if(scenarioTask==null)scenarioTask=Scenarios.GetAsync(id,owner);
                var result=await HttpJsonClient.Wait(scenarioTask,cancellation);Scenario=result;
                return result.sols[Math.Max(0,Math.Min(4,sol-1))];
            }
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(owner, cancellation, lifetime.Token))
                return await Nasa.GetSolAsync(id, sol, linked.Token);
        }
        public async Task<SolImagery> GetImageryAsync(int sol,CancellationToken cancellation=default,string[] excludeUrls=null,bool retry=false)
        {
            string id=MissionId;var owner=mission.Token;await Ready;owner.ThrowIfCancellationRequested();
            using(var linked=CancellationTokenSource.CreateLinkedTokenSource(owner,cancellation,lifetime.Token)){
                var images=await Nasa.GetImageryAsync(id,sol,linked.Token,excludeUrls,retry);
                if(id==MissionId && CurrentSol?.sol==sol){CurrentSol.apod=images.apod;CurrentSol.photo=images.photo;if(CurrentSol.data_dates!=null)CurrentSol.data_dates.apod=images.apod?.date;}
                return images;
            }
        }
        public void ObserveSol(int sol, bool offlineOnly = false)
        {
            if (offlineOnly || sol == loadedSol) return; loadedSol = sol; _ = LoadCurrent(sol, MissionId);
        }
        public void BindNativeCampaign(Campaign campaign,bool offlineOnly=false)
        {
            if(campaign?.NativeHazards!=true)return;
            if(boundCampaign==campaign && boundSol==campaign.SolIndex+1)return;
            boundCampaign=campaign;boundSol=campaign.SolIndex+1;_ = ApplyScenario(campaign,boundSol,offlineOnly,MissionId);
        }
        async Task ApplyScenario(Campaign campaign,int sol,bool offlineOnly,string id)
        {
            try {
                await Ready;
                if(id!=MissionId)return;
                if(offlineOnly){Scenario=Scenarios.Snapshot(id);scenarioTask=Task.FromResult(Scenario);}
                var response=await GetSolAsync(sol);
                if(id!=MissionId || campaign.SolIndex+1!=sol)return;
                CurrentSol=response;campaign.ApplyNativeSol(response,Scenario?.provenance??"sample",Scenario?.mode??"Consecutive");Changed?.Invoke();
            }catch(OperationCanceledException){}catch{if(id==MissionId)campaign.ApplyNativeSol(NasaSolService.Offline(id,sol),"sample","Consecutive");}
        }
        async Task LoadCurrent(int sol, string id)
        {
            try { var result = await GetSolAsync(sol); if (!destroyed && id == MissionId && loadedSol == sol) { CurrentSol = result; Changed?.Invoke(); } }
            catch (OperationCanceledException) { }
            catch { if (!destroyed && id == MissionId) { CurrentSol = NasaSolService.Offline(id, sol); Changed?.Invoke(); } }
        }
        // Compatibility for existing project checks; the active path supplies the canonical SolResponse.
        public static string Payload(Campaign campaign, string language, string formula) => NasaJson.Write(BuildPayload(campaign, language, formula, "hint", null));
        public static JObject BuildPayload(Campaign campaign, string language, string formula, string phase, SolResponse data)
        {
            var facts = data?.facts ?? new SolFacts {severity="calm"};
            var payloadFacts=JObject.FromObject(facts);
            payloadFacts["game"]=JObject.FromObject(new {
                dose=campaign.Dose,energy_pool_kwh=campaign.EnergyPool,panel_dust_pct=campaign.PanelDust.Average(),
                cleaned=campaign.CleanedThisSol,shelter_missed=campaign.ShelterMissed,
                simulated_storm=campaign.Demands.DustInAir,storm_source=campaign.Demands.DustInAir?"training":"none",
                consequences=campaign.Events.Select(e=>new {id=e.id,resource=e.resource,severity=e.severity,
                    allocated=e.cause.hasAllocation?(float?)e.cause.allocated:null,demand=e.cause.hasAllocation?(float?)e.cause.demand:null,
                    missing=e.cause.hasAllocation?(float?)e.cause.missing:null,dose=e.effects.dose,energy_loss_pct=e.cause.energyLossPct})
            });
            if(campaign.NativeHazards){var game=(JObject)payloadFacts["game"];game["hazards"]=JArray.FromObject(campaign.NativePlan?.events??Array.Empty<Ares.Domain.Hazards.HazardEvent>());game["condition"]=campaign.Condition.Stage.ToString();game["condition_cause"]=campaign.Condition.Cause.ToString();game["battery_kwh"]=campaign.Solar.ReserveKwh;game["oxygen_percent"]=campaign.Oxygen;game["cold_exposure_seconds"]=campaign.ColdExposure;game["medical_evacuations"]=campaign.Condition.CollapseCount;game["med_kits"]=campaign.MedKits;game["oxygen_canisters"]=campaign.OxygenCanisters;game["data_provenance"]=campaign.NativePlan?.dataProvenance;game["scenario_mode"]=campaign.NativePlan?.mode;game["storm_source"]=campaign.NativePlan?.Has(Ares.Domain.Hazards.HazardKind.Dust)==true?"derived-seasonal-risk":"none";}
            // No synthetic NASA wind or invented formula/FP contract. Existing gameplay calculations stay authoritative.
            return new JObject {
                ["lang"]=language, ["sol"]=campaign.SolIndex+1, ["facts"]=payloadFacts,
                ["effects"]=data?.effects==null?JValue.CreateNull():JToken.FromObject(data.effects),
                ["data_dates"]=data?.data_dates==null?JValue.CreateNull():JToken.FromObject(data.data_dates),
                ["demand_kwh"]=JToken.Parse(JsonUtility.ToJson(campaign.Current.demand)),
                ["player_kwh"]=JToken.Parse(JsonUtility.ToJson(campaign.Allocation)),
                ["result"]=campaign.Outcome==null?JValue.CreateNull():JObject.FromObject(new {shield=campaign.Outcome.shield.status,life=campaign.Outcome.life.status,greenhouse=campaign.Outcome.greenhouse.status}),
                // TODO(confirm): no FP schema or formula version is supplied by Appendix A.
                ["formula_version"]=string.IsNullOrWhiteSpace(formula)?JValue.CreateNull():new JValue(formula),
                ["phase"]=phase,["question_id"]=JValue.CreateNull(),["question_text"]=JValue.CreateNull(),
                ["assigned_tasks"]=new JArray(campaign.Current.tasks.Select(t=>t.id)),
                ["task_results"]=new JArray(campaign.SuccessfulTaskResults.Select(r=>JObject.FromObject(r)))
            };
        }
        public void Ask(Campaign campaign, string language) => AskPhase(campaign, language, "hint");
        public void AskQuestion(Campaign campaign, string language, string id) => AskPhase(campaign, language, "answer", id);
        public void AskFreeQuestion(Campaign campaign, string language, string text) => AskPhase(campaign, language, "free_question", null, text);
        public void AskPhase(Campaign campaign, string language, string phase, string questionId = null, string questionText = null)
        {
            if(campaign?.RadioBlackout==true){Message=new Ares.UI.Localization(language).T("hazard.radio");Changed?.Invoke();return;}
            if (Busy || Time.unscaledTime < nextAllowed) return;
            if (campaign == null || (language != "en" && language != "pt-BR") || !PromptBuilder.Phases.Contains(phase)) { Message = "Pedido inválido para o assistente."; Changed?.Invoke(); return; }
            // Sanitization is repeated in PromptBuilder, before the LLM transport sees the request.
            if (phase == "free_question") questionText = PromptBuilder.Sanitize(questionText);
            if (phase == "free_question" && questionText.Length < 3) { Message = "Escreva uma pergunta com pelo menos três caracteres."; Changed?.Invoke(); return; }
            _ = Send(campaign, language, phase, questionId, questionText, MissionId);
        }
        async Task Send(Campaign campaign, string language, string phase, string questionId, string questionText, string id)
        {
            Busy = true; nextAllowed = Time.unscaledTime + 5; Message = "Consultando o assistente…"; Changed?.Invoke();
            int sol = campaign.SolIndex + 1;
            try {
                await Ready;
                if (id != MissionId || campaign.SolIndex + 1 != sol || destroyed) return;
                var validated=promptBuilder.Prepare(new JObject { ["lang"]=language,["sol"]=sol,["facts"]=new JObject(),["phase"]=phase,["question_id"]=questionId,["question_text"]=questionText });
                questionText=(string)validated["question_text"];
                var response = await GetSolAsync(sol);
                if (id != MissionId || campaign.SolIndex + 1 != sol || destroyed) return;
                var payload = BuildPayload(campaign, language, Configuration.formulaVersion, phase, response);
                payload["question_id"] = questionId == null ? JValue.CreateNull() : new JValue(questionId);
                payload["question_text"] = questionText == null ? JValue.CreateNull() : new JValue(questionText);
                var answer = await Narrator.SayAsync(payload, mission.Token);
                if (id != MissionId || campaign.SolIndex + 1 != sol || destroyed) return;
                Message = "Bubble · " + Ares.Data.ApodRecord.PlainText(answer.text); Mood = answer.mood;
            }
            catch (OperationCanceledException) { }
            catch (ArgumentException) { Message = "Pergunta inválida. Escolha um tema sobre a missão."; }
            catch { Message = language == "en" ? "Radio interference. Please try again, astronaut." : NarratorResponseParser.Fallback; }
            finally { if (!destroyed && id == MissionId) { Busy = false; Changed?.Invoke(); } }
        }
        void OnDestroy() { destroyed = true; lifetime.Cancel(); mission.Cancel(); Nasa?.Dispose(); http?.Dispose(); Images?.Dispose(); }
    }
}
