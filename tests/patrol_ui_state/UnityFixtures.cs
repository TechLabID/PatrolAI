// OFFLINE TEST FIXTURES ONLY. This file is outside Assets and cannot enter a Unity player.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public sealed class SerializeField : Attribute { }
    public class Object { public static T FindAnyObjectByType<T>() where T : class => TestObjects.Get<T>(); }
    public sealed class GameObject
    {
        public static readonly List<MonoBehaviour> Components = new List<MonoBehaviour>();
        public GameObject(string name) { }
        public T AddComponent<T>() where T : MonoBehaviour, new()
        { var component = new T { gameObject = this }; Components.Add(component); return component; }
    }
    public class MonoBehaviour : Object
    {
        public GameObject gameObject;
        protected static void DontDestroyOnLoad(GameObject value) { }
        protected static void Destroy(GameObject value) { GameObject.Components.RemoveAll(c => c.gameObject == value); }
        protected void StopCoroutine(Coroutine value) { if (Routine != null) { PendingDispose(Routine); Routine = null; } }
        private static void PendingDispose(IEnumerator routine) { TestObjects.Pending.Remove(routine); (routine as IDisposable)?.Dispose(); }
        public IEnumerator Routine;
        protected T GetComponent<T>() where T : class => TestObjects.Get<T>();
        protected Coroutine StartCoroutine(IEnumerator routine)
        {
            Routine = routine;
            if (routine.MoveNext()) TestObjects.Pending.Add(routine);
            return new Coroutine();
        }
    }
    public sealed class Coroutine { }
    public static class TestObjects
    {
        public static readonly Dictionary<Type, object> Items = new Dictionary<Type, object>();
        public static readonly List<IEnumerator> Pending = new List<IEnumerator>();
        public static T Get<T>() where T : class => Items.TryGetValue(typeof(T), out var value) ? value as T : null;
        public static Action BeforeTick;
        public static void Tick()
        {
            BeforeTick?.Invoke();
            Time.realtimeSinceStartup++;
            foreach (var routine in Pending.ToArray())
                if (Pending.Contains(routine) && !routine.MoveNext()) Pending.Remove(routine);
        }
        public static void Finish()
        {
            for (int i = 0; i < 200 && Pending.Count > 0; i++) Tick();
            if (Pending.Count > 0) throw new Exception("Unbounded fixture coroutine");
        }
    }
    public static class Application
    {
        public static bool isPlaying => true;
        public static string dataPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Player_Data");
        public static string persistentDataPath => AppContext.BaseDirectory;
    }
    public static class JsonUtility { public static string ToJson(object value, bool prettyPrint) => "{}"; }
    public static class Time { public static float realtimeSinceStartup; }
    public sealed class WaitForSecondsRealtime { public WaitForSecondsRealtime(float seconds) { } }
}
namespace VisionGuard.Phase3
{
    public sealed class VGPhase3AgentController
    {
        public bool IsRunning;
        public string State = "IDLE";
        public string NextRun = "COMPLETE";
        public void RunAgenticInspection()
        {
            IsRunning = NextRun == "RUNNING" || NextRun == "STARTUP";
            if (NextRun != "STARTUP") State = NextRun;
        }
    }
    public sealed class VGMobileInspectorMovement
    {
        public bool IsMoving, IsAtInspectionPose;
        public int Moves;
        public IEnumerator MoveHomeIfNeeded() { IsMoving = true; Moves++; yield return null; IsMoving = false; }
        public IEnumerator MoveToInspectionPose()
        { IsMoving = true; Moves++; yield return null; IsMoving = false; IsAtInspectionPose = true; }
    }
}
namespace VisionGuard.Phase4
{
    public sealed class VGPhase4FusionController
    {
        public bool IsRunning;
        public string State = "IDLE";
        public string FusionStatus, Verification, Action, FinalCondition, Reason, ResultPath;
        public void RunMultiSourceFusion() { State = "COMPLETE"; }
    }
}
namespace VisionGuard.Phase5
{
    public sealed class VGPhase5TemporalController
    {
        public bool IsRunning;
        public string State = "IDLE", Outcome, PreviousCondition, CurrentCondition, ChangeId, IncidentId, Reason, ResultPath;
        public int EvaluationRun;
        public void EvaluateLatestFusion() { State = "COMPLETE"; }
    }
}

namespace UnityEngine { public static class Debug { public static readonly List<string> Messages = new List<string>(); public static void Log(object value) { Messages.Add(value.ToString()); } public static void LogWarning(object value) => Log(value); } }
