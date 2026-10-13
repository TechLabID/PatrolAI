# Standalone Python and D run ownership

Complete README setup with Windows Python 3.12 x64 and install the exact requirements-phase2.txt pins in the project .venv-phase2 before building. A normal public clone contains source and dependency pins; it does not contain an interpreter, packages, credentials, or previous runtime evidence.

## Python discovery and provisioning

PatrolPythonRuntime resolves the executable directory from Application.dataPath/.. (project root in Editor). It validates the agent, vision, and camera source folders, then selects:

1. PATROLAI_PYTHON, if explicitly configured as an absolute existing executable. An invalid override is an error; no silent fallback.
2. Executable-relative .venv-phase2/Scripts/python.exe, preserving the existing convention.
3. Executable-relative .patrolai-python/python.exe, provisioned by the Windows post-build callback.
4. Otherwise throw a setup error. Existing controller failure handling remains FAILED/NOT VERIFIED; discovery never generates a REAL decision.

There is no PATH, registry, parent-project, developer-directory, or arbitrary interpreter search. Existence selects the legacy/override interpreter as before; actual execution can still fail if an explicitly supplied interpreter is incompatible. The provisioned bundle is verified by executing it.

The post-build hook copies only Python source from agent/, vision/, camera/ and requirements-phase2.txt. It creates exchange/ without copying historical observations/results. It invokes tools/provision_player_python.py through the selected project interpreter. The tool:

- requires Windows Python 3.12 x64 and exact installed package versions from requirements-phase2.txt;
- obtains CPython binaries, extension modules, and the standard library from that validated interpreter's base installation;
- vendors only the two pinned distributions and their license/metadata files from their package manifests; excludes CLI scripts outside site-packages, bytecode, direct_url.json, pip configuration, user site-packages, and unrelated packages;
- includes Python's license and uses an application-local python312._pth with relative paths, isolating the interpreter from registry/PYTHONPATH lookup;
- executes the staged interpreter to verify Python architecture, pinned OpenCV/NumPy imports and adjacent application modules before publishing the bundle;
- uses only local installed assets: no download, pip install, credentials, model request, or developer-specific runtime path;
- replaces only an existing recognized phase-owned bundle and validates the final location. A failed prerequisite/import check fails the post-build step; it never reports a ready runtime.

Copy or move the entire output directory, including .patrolai-python/, agent/, vision/, and camera/. A Windows x64 destination with the existing Windows system runtime prerequisites does not need a separate Python installation. Keep NEBIUS_API_KEY external; it is never packaged. Python/OpenCV readiness proves local execution only, not live Nemotron inference.

This follows Python's application-local distribution model. A copied venv is not the packaging mechanism because venvs are generally not portable. References: [Python on Windows](https://docs.python.org/3.12/using/windows.html#the-embeddable-package), [venv portability](https://docs.python.org/3.12/library/venv.html#how-venvs-work).

## D reservation lifecycle

Before this fix, CreateNew wrote demo_started.json with only reservedRequests=2, closed the handle, and never released the marker. Failures, successful completion, timeout, and process exit all left a permanently consumed filename.

The active run now holds an exclusive OS file handle on evaluation/results/phase7c/demo_run.lock from atomic acquisition until terminal work has settled. The empty sidecar remains on disk intentionally; its open handle, not its existence, is the lock. Never unlink the sidecar during use.

The associated demo_started.json contains schema version, two-call budget, PID, UTC process-start ticks, and a random run ID. Metadata is written/flushed to an owned temporary file and atomically published in the same directory. A crash during publication cannot create a partial owner marker. The marker is removed only by its matching owner, while holding the exclusive sidecar lock.

Release paths cover initial handled inference failure, missing adapter, initial exception, successful second result, physical failure/timeout, missing evidence, second-task fault/cancellation, unexpected coroutine exception, and runner disable/destruction. Finally blocks request release on asynchronous and iterator exits. A small lifetime component survives the runner while an uncancellable reasoning task or inherited physical workflow remains active. Presentation failure/abort alone does not authorize another overlapping run. The lease releases once that work settles; process exit also releases the OS handle.

A later process can reclaim valid metadata only after acquiring the exclusive lock and verifying the recorded process has exited or its PID has been reused with a different start time. A live/inaccessible owner fails closed. The exact pre-D2 marker format containing only reservedRequests=2 has a conservative migration rule: reclaim under the exclusive lock only when no other process with the current Player/Editor process name exists. Otherwise ownership is ambiguous and start remains blocked until that process exits. Malformed, unknown, oversized, or externally replaced markers are preserved and blocked; they are never blindly deleted. No fixed timeout declares a live owner stale.

The D safety envelope, action mapping, observations, model decisions, parser/validator, A/B/C behavior, AUTO PATROL, and UI design are unchanged. Each newly admitted D run retains the same two-call bound. Automated tests use explicit offline fixtures and make zero HTTP requests. No benchmark is rerun and no historical evidence is edited.

## Validation commands

From the prepared project root:

~~~powershell
.\.venv-phase2\Scripts\python.exe tests/patrol_ui_state/run_offline.py
.\.venv-phase2\Scripts\python.exe -m unittest discover -s tests -p test_player_python_provisioning.py -v
~~~

The C# regression runner needs the .NET 10 SDK. It writes to tests/artifacts/phase10a_d2, copies only the required read-only stored Phase 7 fixture into scratch, and keeps serialization outputs away from historical evidence. The provisioning test executes a real bundled interpreter after relocation and performs a real OpenCV PNG roundtrip, without PATROLAI_PYTHON or model inference.

Unity-reference compilation and offline tests do not certify a rebuilt Player or manual visual acceptance. Build/run the updated Windows output and inspect concise [PATROLAI-D-CONFLICT] acquisition, start, abort, and release messages to validate the complete Unity runtime.
