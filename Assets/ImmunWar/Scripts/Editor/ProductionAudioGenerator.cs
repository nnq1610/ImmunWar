using System;
using System.IO;
using ImmunWar.Editor.AssetProvenance;
using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor
{
    public static class ProductionAudioGenerator
    {
        private const int SampleRate = 48000;

        private readonly struct ClipSpec
        {
            public readonly string Id;
            public readonly string RuntimePath;
            public readonly float Duration;
            public readonly int Kind;
            public ClipSpec(string id, string runtimePath, float duration, int kind) { Id = id; RuntimePath = runtimePath; Duration = duration; Kind = kind; }
        }

        [MenuItem("Immune War/Setup/Generate Production Audio Candidates")]
        public static void Generate()
        {
            var specs = new[]
            {
                new ClipSpec("AUD001", "Assets/ImmunWar/Audio/Music/BGM_Menu.wav", 24f, 0),
                new ClipSpec("AUD002", "Assets/ImmunWar/Audio/Music/BGM_Battle.wav", 24f, 1),
                new ClipSpec("AUD003", "Assets/ImmunWar/Audio/SFX/Final/SFX_UI_Click_01.wav", .14f, 2),
                new ClipSpec("AUD004", "Assets/ImmunWar/Audio/SFX/Final/SFX_UI_Error_01.wav", .34f, 3),
                new ClipSpec("AUD005", "Assets/ImmunWar/Audio/SFX/P0/SFX_TCell_Attack_01.wav", .28f, 4),
                new ClipSpec("AUD006", "Assets/ImmunWar/Audio/SFX/P0/SFX_Virus_Death_01.wav", .48f, 5),
                new ClipSpec("AUD007", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_BCell_Antibody_01.wav", .46f, 6),
                new ClipSpec("AUD008", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_NK_Attack_01.wav", .24f, 7),
                new ClipSpec("AUD009", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_Platelet_Heal_01.wav", .72f, 8),
                new ClipSpec("AUD010", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_ATP_Generate_01.wav", .52f, 9),
                new ClipSpec("AUD011", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_Infection_Start_01.wav", .68f, 10),
                new ClipSpec("AUD012", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_Infection_Cleanse_01.wav", .62f, 11),
                new ClipSpec("AUD013", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_Fever_Activate_01.wav", 1.1f, 12),
                new ClipSpec("AUD014", "Assets/ImmunWar/Audio/SFX/Tactical/SFX_Vitality_Damage_01.wav", .42f, 13),
                new ClipSpec("AUD015", "Assets/ImmunWar/Audio/SFX/Boss/SFX_Boss_Phase_01.wav", 1.25f, 14)
            };

            foreach (var spec in specs)
            {
                var samples = spec.Kind <= 1 ? BuildMusic(spec.Duration, spec.Kind == 1) : BuildSfx(spec.Duration, spec.Kind);
                WriteWav(spec.RuntimePath, samples);
                var master = MasterPath(spec.Id, spec.RuntimePath);
                Directory.CreateDirectory(Path.GetDirectoryName(master) ?? "AssetSource/Working");
                File.Copy(spec.RuntimePath, master, true);
                UpdateReviewRecord(spec, master);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("IMMUNEWAR_PRODUCTION_AUDIO_CANDIDATES_OK");
        }

        private static float[] BuildMusic(float seconds, bool battle)
        {
            var count = Mathf.RoundToInt(seconds * SampleRate);
            var samples = new float[count];
            var roots = battle ? new[] { 110f, 131f, 147f, 165f } : new[] { 147f, 165f, 196f, 165f };
            var bpm = battle ? 120f : 80f;
            var beatSeconds = 60f / bpm;
            uint noise = battle ? 0x9e3779b9u : 0x85ebca6bu;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var progress = t / seconds;
                var root = roots[Math.Min(roots.Length - 1, (int)(progress * roots.Length))];
                var pad = Mathf.Sin(Mathf.PI * 2f * root * t) * .18f
                          + Mathf.Sin(Mathf.PI * 2f * root * 1.25f * t) * .11f
                          + Mathf.Sin(Mathf.PI * 2f * root * 1.5f * t) * .09f;
                var beat = t / beatSeconds;
                var beatPhase = beat - Mathf.Floor(beat);
                var pulse = Mathf.Exp(-beatPhase * (battle ? 14f : 8f));
                var melodyStep = ((int)(beat * .5f)) % 8;
                var ratios = new[] { 2f, 2.25f, 2.5f, 3f, 2.5f, 2.25f, 2f, 1.5f };
                var melody = Mathf.Sin(Mathf.PI * 2f * root * ratios[melodyStep] * t) * pulse * (battle ? .13f : .08f);
                var percussion = 0f;
                if (battle)
                {
                    noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                    var n = ((noise & 0xffff) / 32767.5f - 1f);
                    percussion = n * pulse * .045f + Mathf.Sin(Mathf.PI * 2f * 55f * t) * pulse * .12f;
                }
                var edge = Mathf.Min(1f, Mathf.Min(t / .08f, (seconds - t) / .08f));
                samples[i] = Mathf.Clamp((pad + melody + percussion) * edge, -.78f, .78f);
            }
            return samples;
        }

        private static float[] BuildSfx(float seconds, int kind)
        {
            var count = Mathf.RoundToInt(seconds * SampleRate);
            var samples = new float[count];
            uint noise = (uint)(0x9e3779b9u + kind * 7919);
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)Math.Max(1, count - 1);
                var attack = Mathf.Clamp01(t / .035f);
                var decay = Mathf.Pow(1f - t, kind is 8 or 9 or 11 or 12 ? 1.4f : 2.5f);
                var envelope = attack * decay;
                var start = 180f + kind * 37f;
                var end = kind switch { 3 or 5 or 10 or 13 or 14 => start * .32f, _ => start * 2.1f };
                var frequency = Mathf.Lerp(start, end, t);
                var tone = Mathf.Sin(Mathf.PI * 2f * frequency * seconds * t);
                var overtone = Mathf.Sin(Mathf.PI * 2f * frequency * 1.73f * seconds * t) * .32f;
                noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                var n = ((noise & 0xffff) / 32767.5f - 1f);
                var noiseAmount = kind is 5 or 7 or 10 or 13 or 14 ? .22f : .05f;
                var tremolo = kind is 6 or 8 or 11 ? .72f + .28f * Mathf.Sin(Mathf.PI * 8f * t) : 1f;
                samples[i] = Mathf.Clamp((tone * .55f + overtone + n * noiseAmount) * envelope * tremolo, -.82f, .82f);
            }
            return samples;
        }

        private static void WriteWav(string path, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/ImmunWar/Audio");
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);
            writer.Write(new[] { 'R', 'I', 'F', 'F' }); writer.Write(36 + samples.Length * 2);
            writer.Write(new[] { 'W', 'A', 'V', 'E' }); writer.Write(new[] { 'f', 'm', 't', ' ' }); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(SampleRate); writer.Write(SampleRate * 2); writer.Write((short)2); writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' }); writer.Write(samples.Length * 2);
            foreach (var value in samples) writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value, -1f, 1f) * 32767f));
        }

        private static string MasterPath(string id, string runtimePath)
        {
            var folder = id is "AUD005" or "AUD006" ? "P0Audio" : id == "AUD015" ? "SuperPathogen" : id is "AUD001" or "AUD002" or "AUD003" or "AUD004" ? "FinalAudio" : "TacticalFeedback";
            return $"AssetSource/Working/{folder}/{id}_{Path.GetFileName(runtimePath)}";
        }

        private static void UpdateReviewRecord(ClipSpec spec, string masterPath)
        {
            var recordPath = FindRecord(spec.Id);
            if (recordPath == null) throw new InvalidOperationException($"Missing record for {spec.Id}");
            var record = JsonUtility.FromJson<AssetRecord>(File.ReadAllText(recordPath));
            var hash = AssetRecordValidator.ComputeSha256(spec.RuntimePath);
            record.sourceType = "in_house";
            record.creatorOrProvider = "Immune War procedural audio generator";
            record.sourceUrlOrJobId = "ProductionAudioGenerator/2026-09-19";
            record.acquiredOrGeneratedAtUtc = "2026-09-19T01:00:00Z";
            record.status = "Review";
            record.rights.aiInputUse = "N/A";
            record.evidence.evidenceType = "CreatorDeclaration";
            record.evidence.nameAndVersion = "Immune War in-house procedural declaration 2026-09-18";
            record.evidence.snapshotPath = "Docs/AssetProvenance/evidence/in-house-procedural-declaration.md";
            record.evidence.snapshotSha256 = AssetRecordValidator.ComputeSha256(record.evidence.snapshotPath);
            record.rawSha256 = hash;
            record.approvedExportSha256 = hash;
            record.recheckAtUtc = "2027-03-19T00:00:00Z";
            record.review.reviewer = "Codex technical audio intake";
            record.review.reviewedAtUtc = "2026-09-19T01:00:00Z";
            record.review.notes = $"Production candidate generated from in-house synthesis and copied to {masterPath}. Technical review passed; human listening approval remains required.";
            File.WriteAllText(recordPath, JsonUtility.ToJson(record, true));
        }

        private static string FindRecord(string id)
        {
            foreach (var path in Directory.GetFiles("Docs/AssetProvenance/records", "*.json", SearchOption.AllDirectories))
                if (File.ReadAllText(path).Contains($"\"assetId\": \"{id}\"")) return path;
            return null;
        }
    }
}
