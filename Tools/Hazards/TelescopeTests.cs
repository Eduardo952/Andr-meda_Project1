using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Ares.Domain;
using Ares.Infrastructure.Nasa;

public static class TelescopeTests
{
    static int assertions;
    static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
    public static async Task<string> Run()
    {
        assertions=0;int[] previous=null;var seen=new List<int>();var firstFive=new HashSet<int>();
        for(int mission=0;mission<50;mission++){
            var order=DiscoveryDeck.FreshOrder(5,mission+901,previous,seen.ToArray());
            Check(order.Distinct().Count()==5,"Duplicate within offline mission");
            if(previous!=null)Check(Enumerable.Range(0,5).All(i=>previous[i]!=order[i]),"Repeated Sol slot");
            if(mission<5)Check(firstFive.Add(order[0]),"First image repeated before exhausting collection");
            seen.Remove(order[0]);seen.Add(order[0]);previous=order;
        }
        for(int mission=0;mission<50;mission++){
            var order=DiscoveryDeck.FreshOrder(5,mission+1701,previous,seen.ToArray());
            Check(Enumerable.Range(0,5).All(i=>previous[i]!=order[i]),"Full-mission restart preserved a slot");
            foreach(int image in order){seen.Remove(image);seen.Add(image);}previous=order;
        }
        string folder="Temp/TelescopeUnitCache";
        var transport=new Fake();using(var http=new HttpJsonClient(transport,0))using(var nasa=new NasaSolService(new NasaConfig(),new ApiCredentials {NASA_API_KEY="mock-only"},http,new SolDataCache(folder))){
            var first=await nasa.GetImageryAsync("telescope-mission-a",1);var same=await nasa.GetImageryAsync("telescope-mission-a",1);var second=await nasa.GetImageryAsync("telescope-mission-b",1);
            Check(first.apod.url==same.apod.url&&transport.calls==4,"Same Sol cache/request count");Check(first.apod.url!=second.apod.url,"New mission image seed");
        }
        transport=new Fake {repeat="https://apod.nasa.gov/apod/image/already-viewed.jpg",repeatCount=1};
        using(var http=new HttpJsonClient(transport,0))using(var nasa=new NasaSolService(new NasaConfig(),new ApiCredentials {NASA_API_KEY="mock-only"},http,new SolDataCache(folder))){
            var fresh=await nasa.GetImageryAsync("telescope-mission-c",1,default,new[]{transport.repeat});Check(fresh.apod!=null&&fresh.apod.url!=transport.repeat&&transport.calls==4,"Known online repeat was not replaced");
        }
        transport=new Fake {fail=true};using(var http=new HttpJsonClient(transport,0))using(var nasa=new NasaSolService(new NasaConfig(),new ApiCredentials {NASA_API_KEY="mock-only"},http,new SolDataCache(folder))){
            var failed=await nasa.GetImageryAsync("telescope-retry-a",1);Check(failed.apod==null&&failed.photo==null,"Failure fixture");transport.fail=false;
            var recovered=await nasa.GetImageryAsync("telescope-retry-a",1,default,null,true);Check(recovered.apod!=null&&transport.calls==4,"Failed metadata cached permanently despite retry");
        }
        return "PASS: "+assertions+" telescope assertions; 100 mission restarts, offline exhaustion/order, stable same Sol, fresh mission, online repeat rejection and failed-request retry.";
    }
    sealed class Fake:IHttpTransport
    {
        public int calls,repeatCount;public bool fail;public string repeat;
        public Task<HttpResponse> SendAsync(HttpRequest request,CancellationToken token){
            calls++;if(fail)return Task.FromResult(HttpResponse.Error("test_unavailable"));
            string date=Regex.Match(request.Url,@"start_date=([0-9-]+)").Groups[1].Value;
            string url=repeatCount-->0?repeat:"https://apod.nasa.gov/apod/image/"+date+".jpg";
            string json=date.Length==0?"{}":NasaJson.Write(new[]{new{date=date,title="Synthetic telescope fixture",media_type="image",url=url}});
            return Task.FromResult(new HttpResponse {Status=200,Data=Encoding.UTF8.GetBytes(json)});
        }
    }
}
