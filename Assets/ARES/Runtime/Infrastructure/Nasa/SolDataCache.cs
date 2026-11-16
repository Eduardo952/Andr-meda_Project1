using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ares.Infrastructure.Nasa
{
    [Serializable] public sealed class MissionBlock { public string mission, todayUtc; public DonkiFlare[] flares; }
    [Serializable] sealed class SolCacheEntry { public DateTime savedUtc; public SolResponse data; }
    public sealed class SolDataCache
    {
        readonly string folder;readonly object gate=new object();readonly Dictionary<string,SolCacheEntry> memory=new Dictionary<string,SolCacheEntry>();
        public SolDataCache(string folder){this.folder=folder;}
        string Key(string mission,int sol)=>SecretRedactor.Hash(mission+"|"+sol);
        public Task<SolResponse> ReadAsync(string mission,int sol,int maxAgeSeconds,bool allowStale=false)=>Task.Run(()=> {
            lock(gate) {
                string key=Key(mission,sol);
                if(!memory.TryGetValue(key,out var entry))entry=Read<SolCacheEntry>(Path.Combine(folder,key+".json"));
                if(entry?.data==null || entry.data.mission!=mission || entry.data.sol!=sol)return null;
                memory[key]=entry;
                return allowStale || (DateTime.UtcNow-entry.savedUtc).TotalSeconds<=maxAgeSeconds ? entry.data : null;
            }
        });
        public Task WriteAsync(SolResponse data)=>Task.Run(()=> {lock(gate){string key=Key(data.mission,data.sol);var entry=new SolCacheEntry {data=data,savedUtc=DateTime.UtcNow};memory[key]=entry;Write(Path.Combine(folder,key+".json"),entry);}});
        public Task<MissionBlock> ReadBlockAsync(string mission)=>Task.Run(()=> {lock(gate){var b=Read<MissionBlock>(Path.Combine(folder,"block-"+SecretRedactor.Hash(mission)+".json"));return b?.mission==mission?b:null;}});
        public Task WriteBlockAsync(MissionBlock block)=>Task.Run(()=> {lock(gate)Write(Path.Combine(folder,"block-"+SecretRedactor.Hash(block.mission)+".json"),block);});
        T Read<T>(string path) where T:class {try{return File.Exists(path)?NasaJson.Read<T>(File.ReadAllText(path)):null;}catch(IOException){return null;}catch(UnauthorizedAccessException){return null;}catch(Newtonsoft.Json.JsonException){return null;}}
        void Write(string path,object value)
        {
            try {Directory.CreateDirectory(folder);string temp=path+".tmp";File.WriteAllText(temp,NasaJson.Write(value));if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}
            catch(IOException){}catch(UnauthorizedAccessException){}
        }
    }
}
