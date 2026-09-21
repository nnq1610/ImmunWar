using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ImmunWar.Editor
{
    public static class AudioMixerAssetGenerator
    {
        public static void Generate()
        {
            const string path="Assets/ImmunWar/Audio/Mixers/ImmuneWarAudio.mixer";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController",false)).FirstOrDefault(t=>t!=null);if(type==null)throw new InvalidOperationException("AudioMixerController editor type unavailable.");object controller;if(File.Exists(path))controller=AssetDatabase.LoadMainAssetAtPath(path);else{var create=type.GetMethod("CreateMixerControllerAtPath",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);if(create==null)throw new MissingMethodException(type.FullName,"CreateMixerControllerAtPath");controller=create.Invoke(null,new object[]{path});InvokeNamedFactory(type,controller,"CreateNewGroup","Music");InvokeNamedFactory(type,controller,"CreateNewGroup","SFX");}
            if(!File.ReadAllText(path).Contains("m_Name: Paused"))InvokeNamedFactory(type,controller,"CreateSnapshot","Paused");EditorUtility.SetDirty((UnityEngine.Object)controller);AssetDatabase.SaveAssets();Debug.Log("IMMUNEWAR_AUDIO_MIXER_OK");
        }
        private static void InvokeNamedFactory(Type type,object target,string name,string label){var method=type.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).FirstOrDefault(m=>m.Name==name&&m.GetParameters().Any(p=>p.ParameterType==typeof(string)));if(method==null)return;var ps=method.GetParameters();var args=new object[ps.Length];for(var i=0;i<ps.Length;i++)args[i]=ps[i].ParameterType==typeof(string)?label:ps[i].HasDefaultValue?ps[i].DefaultValue:ps[i].ParameterType.IsValueType?Activator.CreateInstance(ps[i].ParameterType):null;method.Invoke(target,args);}
    }
}
