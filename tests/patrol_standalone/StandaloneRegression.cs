// OFFLINE FIXTURES ONLY. This suite never uses an HTTP transport or invokes a model.
using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using PatrolAI.Runtime;
using PatrolAI.Scenarios;
using PatrolAI.NVIDIA;
using PatrolAI.Competition;
using UnityEngine;
class StandaloneRegression
{
    sealed class QueuedContext : SynchronizationContext
    {
        public readonly System.Collections.Concurrent.ConcurrentQueue<Action> Queue = new System.Collections.Concurrent.ConcurrentQueue<Action>();
        public override void Post(SendOrPostCallback callback, object state) { Queue.Enqueue(() => callback(state)); }
        public void Drain() { while(Queue.TryDequeue(out var action)) action(); }
    }
    static int passed;
    static string scratch;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS: " + name); }
    static void Throws<T>(Action action, string name) where T : Exception
    { try { action(); } catch (T) { Check(true, name); return; } throw new Exception(name); }
    static string Folder(string name) { string p = Path.Combine(scratch, name); Directory.CreateDirectory(p); return p; }
    static string FakePython(string root, string relative) { string p=Path.Combine(root,relative); Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllText(p,"selection fixture only"); return p; }
    static void Invoke(object target,string method) { target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null); }
    static void Poll() { foreach(var monitor in GameObject.Components.OfType<PatrolConflictReservationLifetime>().ToArray()) monitor.Poll(); }
    static PatrolDemoScenarioRunner Runner(string name, string mode, bool adapter=false, bool fresh=false)
    {
        TestObjects.Items.Clear(); TestObjects.Pending.Clear(); TestObjects.BeforeTick=null;
        Application.dataPath=Path.Combine(Folder(name),"Player_Data");
        NebiusNemotronReasoningService.Mode=mode; NebiusNemotronReasoningService.Calls=0;
        PatrolReobservationEvidenceReader.Fresh=fresh;
        if(adapter) {
            var controller=new VisionGuard.Phase3.VGPhase3AgentController();
            TestObjects.Items[controller.GetType()]=controller;
            TestObjects.Items[typeof(PatrolBaselineActionAdapter)]=new PatrolBaselineActionAdapter();
        }
        return new PatrolDemoScenarioRunner();
    }
    static string Marker => Path.GetFullPath(Path.Combine(Application.dataPath,"..","evaluation","results","phase7c","demo_started.json"));
    static string LeaseDirectory(string name) => Folder("leases-"+name);
    static Process Worker(string directory)
    {
        var start=new ProcessStartInfo {
            FileName=Process.GetCurrentProcess().MainModule.FileName,
            Arguments="\""+Assembly.GetExecutingAssembly().Location+"\" --hold \""+directory+"\"",
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardInput=true
        };
        var process=Process.Start(start);
        if(process.StandardOutput.ReadLine()!="LEASE_READY") throw new Exception("Worker did not acquire");
        return process;
    }
    public static int Main(string[] args)
    {
        if(args.Length>0 && args[0]=="--hold") {
            using(var lease=PatrolConflictRunReservation.Acquire(args[1])) {
                Console.WriteLine("LEASE_READY"); Console.Out.Flush(); Console.ReadLine();
            }
            return 0;
        }
        try {
            scratch=Path.GetFullPath(Path.Combine("standalone-cases",Guid.NewGuid().ToString("N"))); Directory.CreateDirectory(scratch);
            string runtime=Folder("python-discovery"); foreach(string p in new[]{"agent","vision","camera"}) Directory.CreateDirectory(Path.Combine(runtime,p));
            string selected;
            string local=FakePython(runtime,Path.Combine(".venv-phase2","Scripts","python.exe"));
            string bundled=FakePython(runtime,Path.Combine(".patrolai-python","python.exe"));
            string configured=FakePython(runtime,"override.exe");
            PatrolPythonRuntime.ResolveOrThrow(runtime,configured,out selected); Check(selected==configured,"valid explicit override wins");
            Throws<FileNotFoundException>(()=>PatrolPythonRuntime.ResolveOrThrow(runtime,Path.Combine(runtime,"missing.exe"),out selected),"invalid override fails without fallback");
            Throws<InvalidOperationException>(()=>PatrolPythonRuntime.ResolveOrThrow(runtime,"relative.exe",out selected),"relative override rejected");
            PatrolPythonRuntime.ResolveOrThrow(runtime,null,out selected); Check(selected==local,"executable-relative venv wins over bundle");
            File.Delete(local); PatrolPythonRuntime.ResolveOrThrow(runtime,null,out selected); Check(selected==bundled,"fresh-build relative bundle selected without override");
            File.Delete(bundled); Throws<FileNotFoundException>(()=>PatrolPythonRuntime.ResolveOrThrow(runtime,null,out selected),"missing runtime throws; no fabricated REAL");
            string clean=LeaseDirectory("clean"); using(var lease=PatrolConflictRunReservation.Acquire(clean)) {
                Check(File.Exists(lease.MarkerPath),"clean atomic acquire");
                Throws<IOException>(()=>PatrolConflictRunReservation.Acquire(clean),"same-process concurrent run blocked");
            }
            Check(!File.Exists(Path.Combine(clean,"demo_started.json")),"dispose releases owned marker");
            string cross=LeaseDirectory("cross-process");
            using(var worker=Worker(cross)) {
                try {
                    Throws<IOException>(()=>PatrolConflictRunReservation.Acquire(cross),"genuine cross-process concurrency blocked");
                    worker.Kill(); worker.WaitForExit();
                } finally { if(!worker.HasExited) { worker.Kill(); worker.WaitForExit(); } }
            }
            using(var recovered=PatrolConflictRunReservation.Acquire(cross)) Check(recovered.RecoveredStaleMarker,"dead process lease recovered safely");
            string unknown=LeaseDirectory("unknown"); File.WriteAllText(Path.Combine(unknown,"demo_started.json"),"unknown owner data");
            Throws<IOException>(()=>PatrolConflictRunReservation.Acquire(unknown),"unknown marker fails closed");
            Check(File.ReadAllText(Path.Combine(unknown,"demo_started.json"))=="unknown owner data","unknown marker preserved");
            string active=LeaseDirectory("active-metadata");
            using(var current=Process.GetCurrentProcess()) File.WriteAllText(Path.Combine(active,"demo_started.json"),
                "{\"schemaVersion\":1,\"reservedRequests\":2,\"processId\":"+current.Id+",\"processStartUtcTicks\":"+current.StartTime.ToUniversalTime().Ticks+",\"runId\":\""+new string('a',32)+"\"}");
            Throws<IOException>(()=>PatrolConflictRunReservation.Acquire(active),"live owner metadata blocks reclamation");
            string legacy=LeaseDirectory("legacy"); File.WriteAllText(Path.Combine(legacy,"demo_started.json"),"{\"reservedRequests\":2}");
            using(var current=Process.GetCurrentProcess()) {
                bool ambiguous=Process.GetProcessesByName(current.ProcessName).Any(p=>p.Id!=current.Id && !p.HasExited);
                if(ambiguous) Throws<IOException>(()=>PatrolConflictRunReservation.Acquire(legacy),"legacy marker protected while same-name owner is ambiguous");
                else using(var recovered=PatrolConflictRunReservation.Acquire(legacy)) Check(recovered.RecoveredStaleMarker,"known dead legacy marker recovered");
            }
            var failed=Runner("handled-failure","FAILED"); failed.RunConflictingEvidenceScenario(); Poll();
            Check(!File.Exists(Marker) && failed.Evidence.FinalState=="STOPPED" && failed.Evidence.PatrolDecision=="NOT VERIFIED","handled D failure releases and preserves STOP/NOT VERIFIED");
            failed.RunConflictingEvidenceScenario(); Poll(); Check(NebiusNemotronReasoningService.Calls==2 && !File.Exists(Marker),"retry after failed run allowed");
            var success=Runner("success","STOP",true,true); success.RunConflictingEvidenceScenario(); Check(File.Exists(Marker),"D lease retained during re-observation"); TestObjects.Finish(); Poll();
            Check(!File.Exists(Marker) && success.LatestResult.secondClosedLoopResult!=null,"successful terminal D releases marker");
            var missing=Runner("missing-evidence","STOP",true,false); missing.RunConflictingEvidenceScenario(); TestObjects.Finish(); Poll();
            Check(!File.Exists(Marker) && missing.Evidence.FinalState=="STOPPED","missing secondary evidence releases marker safely");
            var fault=Runner("coroutine-exception","STOP",true,true); fault.RunConflictingEvidenceScenario();
            typeof(PatrolDemoScenarioRunner).GetField("reobservationSessionId",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(fault, "throw-fixture");
            PatrolReobservationEvidenceReader.Throw = true;
            TestObjects.Finish(); Poll(); PatrolReobservationEvidenceReader.Throw = false;
            Check(!File.Exists(Marker) && fault.Evidence.FinalState=="STOPPED", "unexpected coroutine exception releases marker safely");
            var aborted=Runner("abort", "DELAYED_STOP"); aborted.RunConflictingEvidenceScenario(); Invoke(aborted,"OnDisable"); Poll();
            Check(File.Exists(Marker),"abort retains lease while uncancellable reasoning is pending");
            NebiusNemotronReasoningService.PendingCompletion();
            for(int i=0;i<100 && File.Exists(Marker);i++) { Thread.Sleep(5); Poll(); }
            Check(!File.Exists(Marker) && aborted.Evidence.FinalState=="STOPPED", "aborted D releases after task settles");
            var previousContext=SynchronizationContext.Current;
            var queued=new QueuedContext();
            SynchronizationContext.SetSynchronizationContext(queued);
            try {
                var waiting=Runner("abort-queued-continuation","DELAYED_STOP"); waiting.RunConflictingEvidenceScenario(); Invoke(waiting,"OnDisable");
                NebiusNemotronReasoningService.PendingCompletion();
                for(int i=0;i<100 && queued.Queue.IsEmpty;i++) Thread.Sleep(5);
                Poll(); Check(!queued.Queue.IsEmpty && File.Exists(Marker), "completed task retains ownership until outer continuation exits");
                waiting.RunConflictingEvidenceScenario(); Check(NebiusNemotronReasoningService.Calls==1, "retry cannot overlap queued aborted continuation");
                queued.Drain(); Poll(); Check(!File.Exists(Marker), "queued aborted continuation releases at terminal finally");
            }
            finally { SynchronizationContext.SetSynchronizationContext(previousContext); }
            var timeout=Runner("physical-timeout","STOP",true,true);
            var physical=(VisionGuard.Phase3.VGPhase3AgentController)TestObjects.Items[typeof(VisionGuard.Phase3.VGPhase3AgentController)]; physical.NextRun="RUNNING";
            timeout.RunConflictingEvidenceScenario();
            typeof(PatrolDemoScenarioRunner).GetField("reobservationStartedUtc",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(timeout,DateTime.UtcNow.AddMinutes(-3));
            TestObjects.Tick(); Poll(); Check(File.Exists(Marker),"presentation timeout retains lease while physical workflow runs");
            physical.IsRunning=false; physical.State="FAILED"; Poll(); Check(!File.Exists(Marker),"physical terminal failure finally releases lease");
            ConflictDemoRegression.RunAll();
            Console.WriteLine("STANDALONE SUMMARY: "+passed+"/"+passed+" PASS; D CONFLICT SUITE=PASS; HTTP REQUESTS=0; OFFLINE FIXTURES ONLY");
            return 0;
        }
        catch(Exception e) { Console.Error.WriteLine("FAIL: "+e); return 1; }
    }
}
