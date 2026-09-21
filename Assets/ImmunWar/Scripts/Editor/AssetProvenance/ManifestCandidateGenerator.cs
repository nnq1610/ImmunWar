using System;
using System.IO;
using ImmunWar.Editor.AssetProvenance;
using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor
{
    [Serializable] internal sealed class ManifestCandidateMap { public ManifestCandidateEntry[] assets; }
    [Serializable] internal sealed class ManifestCandidateEntry { public string assetId; public string category; public string name; public string priority; public string runtimePath; }

    public static class ManifestCandidateGenerator
    {
        public static void Generate()
        {
            var map = JsonUtility.FromJson<ManifestCandidateMap>(File.ReadAllText("Docs/AssetProvenance/manifest-path-map.json"));
            foreach (var entry in map.assets ?? Array.Empty<ManifestCandidateEntry>())
            {
                if (!File.Exists(entry.runtimePath))
                {
                    if (entry.runtimePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) WriteTone(entry.runtimePath, entry.assetId);
                    else WriteTexture(entry.runtimePath, entry.assetId, entry.category);
                }
                var source = $"AssetSource/Working/ManifestCandidates/{entry.assetId}/{Path.GetFileName(entry.runtimePath)}"; Directory.CreateDirectory(Path.GetDirectoryName(source)); File.Copy(entry.runtimePath, source, true);
                if (FindExistingRecord(entry.assetId) != null) continue;
                var hash = AssetRecordValidator.ComputeSha256(entry.runtimePath); var record = AssetRecord.CreateInHouse(entry.assetId, entry.runtimePath, hash); record.manifestCategory = entry.category; record.purpose = entry.name; record.releaseCritical = entry.priority == "P0"; record.status = "Review"; record.sourceType = "in_house_procedural_candidate"; record.creatorOrProvider = "Immune War procedural candidate generator"; record.sourceUrlOrJobId = "ManifestCandidateGenerator/2026-09-18"; record.evidence.snapshotPath = "Docs/AssetProvenance/evidence/in-house-procedural-declaration.md"; record.review.reviewer = "Codex automated intake"; record.review.notes = "Technical candidate generated and hashed. Human visual/audio/similarity/release approval remains required.";
                var recordPath = $"Docs/AssetProvenance/records/ManifestCandidates/{entry.assetId}.json"; Directory.CreateDirectory(Path.GetDirectoryName(recordPath)); File.WriteAllText(recordPath, JsonUtility.ToJson(record, true));
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); ConfigureTextures(); Debug.Log("IMMUNEWAR_MANIFEST_CANDIDATES_OK");
        }

        private static string FindExistingRecord(string id) { if (!Directory.Exists("Docs/AssetProvenance/records")) return null; foreach (var path in Directory.GetFiles("Docs/AssetProvenance/records", "*.json", SearchOption.AllDirectories)) if (File.ReadAllText(path).Contains($"\"assetId\": \"{id}\"")) return path; return null; }
        private static void WriteTexture(string path, string id, string category)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); var size = category == "Environment" ? 512 : 256; var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); var pixels = new Color32[size * size]; var seed = StableHash(id); var color = new Color32((byte)(70 + seed % 150), (byte)(70 + seed / 7 % 150), (byte)(70 + seed / 17 % 150), 255); var lobes = 4 + seed % 9;
            for (var y = 0; y < size; y++) for (var x = 0; x < size; x++) { var dx=x-size*.5f;var dy=y-size*.5f;var angle=Mathf.Atan2(dy,dx);var radius=size*(.3f+.04f*Mathf.Sin(angle*lobes));var d=Mathf.Sqrt(dx*dx+dy*dy);pixels[y*size+x]=d<radius?new Color32((byte)Mathf.Clamp(color.r+(1-d/radius)*35,0,255),color.g,color.b,255):new Color32(0,0,0,0); }
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        }
        private static void WriteTone(string path, string id)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)); const int rate=48000;var count=rate/2;var bytes=new byte[44+count*2];void Text(int o,string v){for(var i=0;i<v.Length;i++)bytes[o+i]=(byte)v[i];}void Int(int o,int v)=>Buffer.BlockCopy(BitConverter.GetBytes(v),0,bytes,o,4);void Short(int o,short v)=>Buffer.BlockCopy(BitConverter.GetBytes(v),0,bytes,o,2);Text(0,"RIFF");Int(4,36+count*2);Text(8,"WAVE");Text(12,"fmt ");Int(16,16);Short(20,1);Short(22,1);Int(24,rate);Int(28,rate*2);Short(32,2);Short(34,16);Text(36,"data");Int(40,count*2);var frequency=180+StableHash(id)%720;for(var i=0;i<count;i++){var t=i/(float)count;var envelope=Mathf.Sin(Mathf.PI*t);Short(44+i*2,(short)(Math.Sin(Math.PI*2*frequency*i/rate)*envelope*9000));}File.WriteAllBytes(path,bytes);
        }
        private static int StableHash(string value){unchecked{var h=17;foreach(var c in value)h=h*31+c;return Math.Abs(h==int.MinValue?0:h);}}
        private static void ConfigureTextures(){foreach(var path in AssetDatabase.GetAllAssetPaths()){if(!path.StartsWith("Assets/ImmunWar/")||!path.EndsWith(".png",StringComparison.OrdinalIgnoreCase))continue;if(AssetImporter.GetAtPath(path)is TextureImporter importer){importer.textureType=TextureImporterType.Sprite;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.SaveAndReimport();}}}
    }
}
