using System;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace Ares.Editor
{
    public static class ReferenceProvenance
    {
        [Serializable] sealed class Manifest { public string reference; public Hash[] sourceHashes, outputHashes; }
        [Serializable] sealed class Hash { public string file, sha256; }
        public const string Reference = "casa-marte-missao-n8n";
        public static void Validate()
        {
            const string import = "Assets/ARES/Import";
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(import + "/manifest.json"));
            if (manifest.reference != Reference || manifest.sourceHashes == null || manifest.outputHashes == null)
                throw new InvalidOperationException("Export casa-marte-missao-n8n with node Tools/AresImport/export.mjs before importing.");
            Verify(Reference + "/src", manifest.sourceHashes);
            Verify(import, manifest.outputHashes);
        }
        static void Verify(string directory, Hash[] hashes)
        {
            string root = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
            using (var sha = SHA256.Create()) foreach (var hash in hashes)
            {
                string file = Path.GetFullPath(Path.Combine(root, hash.file));
                if (!file.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid manifest path.");
                if (!File.Exists(file)) throw new InvalidOperationException("Missing reference data: " + hash.file);
                string actual;
                using (var stream = File.OpenRead(file)) actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                if (actual != hash.sha256) throw new InvalidOperationException("Reference/export changed: " + hash.file + ". Run node Tools/AresImport/export.mjs again.");
            }
        }
    }
}
