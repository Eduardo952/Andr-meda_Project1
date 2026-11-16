using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Ares.Infrastructure.Nasa
{
    public sealed partial class NasaSolService
    {
        // The Editor catalog and Scenario Director reuse this service, credentials, retry policy and deduplication.
        public Task<string> ReadArchiveBlockAsync(string kind,string start,string end,CancellationToken token=default)
        {
            if(kind!="FLR" && kind!="CMEAnalysis" && kind!="WSAEnlilSimulations")throw new ArgumentException("Unsupported archive source");
            MarsCalendar.DayMs(start);MarsCalendar.DayMs(end);
            return HttpJsonClient.Wait(Nasa("/DONKI/"+kind,"startDate="+start+"&endDate="+end+(kind=="CMEAnalysis"?"&mostAccurateOnly=true&catalog=ALL":""),kind=="FLR"?config.FlareTimeoutSeconds:config.HttpTimeoutSeconds),token);
        }
        public Task<string> ProbeHistoricalWindAsync(CancellationToken token=default)=>HttpJsonClient.Wait(Nasa("/insight_weather/","feedtype=json&ver=1.0",config.HttpTimeoutSeconds),token);
        public async Task<SolResponse> GetArchiveSolAsync(string mission,int sol,string earthDate,CancellationToken token=default)
        {
            mission=MissionPlanner.MissionId(mission,today());sol=Math.Max(1,Math.Min(5,sol));double day=MarsCalendar.DayMs(earthDate);
            if(day<MarsCalendar.DayMs("2012-08-07") || day>MarsCalendar.DayMs(today().ToUniversalTime().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture))-45*MarsCalendar.Day)throw new ArgumentOutOfRangeException(nameof(earthDate));
            string identity="archive-"+SecretRedactor.Hash(mission+"|"+earthDate).Substring(0,40);
            var hit=await cache.ReadAsync(identity,sol,config.CacheFreshSeconds).ConfigureAwait(false);
            if(hit!=null){hit=NasaJson.Read<SolResponse>(NasaJson.Write(hit));hit.mission=mission;return hit;}
            var plan=MissionPlanner.Plan(mission,sol,today());int curiosity=(int)Math.Floor((day-MarsCalendar.Landing)/MarsCalendar.SolMs);
            string prev=MarsCalendar.Iso(day-MarsCalendar.Day),next=MarsCalendar.Iso(day+MarsCalendar.Day);
            var values=await Task.WhenAll(
                ReadArchiveBlockAsync("FLR",earthDate,earthDate,token),ReadArchiveBlockAsync("CMEAnalysis",earthDate,earthDate,token),ReadArchiveBlockAsync("WSAEnlilSimulations",prev,next,token),
                Json(config.MarsWeatherBaseUrl.TrimEnd('/')+"/"+curiosity,config.HttpTimeoutSeconds),
                Nasa("/planetary/apod","start_date="+plan.apod_start+"&end_date="+plan.apod_end,config.HttpTimeoutSeconds),
                Json(config.ImageLibraryBaseUrl.TrimEnd('/')+"/search?q=Mars%20surface%20Curiosity&media_type=image&page_size=1&page="+plan.photo_page,config.HttpTimeoutSeconds)).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();bool any=false;foreach(var value in values)any|=value!=null;
            if(!any)return null;
            double ls=MarsCalendar.LsOf(day+MarsCalendar.Day/2),roll=DeterministicRng.Value(mission,"dust|"+sol);bool season=ls>=180 && ls<330;
            var window=new SelectedWindow {mission=mission,sol=sol,earth_date=earthDate,curiosity_sol=curiosity,d_prev=prev,d_next=next,
                ls=MarsCalendar.Round(ls*10)/10,dust_season=season,dust_storm_risk=season?(roll<.45?"high":"medium"):(roll<.08?"medium":"low"),
                flares_day=ApiModels.Many<DonkiFlare>(values[0]),mission_window=new MissionWindow {reason="curated_real_archive",earth_dates=new[]{earthDate},curiosity_sols=new[]{curiosity}}};
            var result=SolDataAssembler.Assemble(window,values[4],values[1],values[2],values[3],values[5]);
            result.is_fallback=values[0]==null && values[1]==null && values[2]==null && values[3]==null;
            result.data_dates.note="Five real days from NASA's archive, chosen to train different hazards. Curated dates are not consecutive calendar days. Weather is from the Curiosity Sol associated with this Earth date.";
            var stored=NasaJson.Read<SolResponse>(NasaJson.Write(result));stored.mission=identity;await cache.WriteAsync(stored).ConfigureAwait(false);return result;
        }
    }
}
