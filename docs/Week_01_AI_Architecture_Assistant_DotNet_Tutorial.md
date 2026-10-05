# Week 1 — Build an AI Architecture Proposal Summarizer with .NET

**Prepared for:** Mostafa Taheri — Technical Lead | Software Architect  
**Learning track:** AI for software architecture, through working .NET features  
**Time budget:** 3 hours  
**Feature:** `POST /proposals/summarize`  
**Outcome:** A local API that summarizes a proposal, preserves unknowns, and handles model-service failures.

## 1. What you will build

Build the first feature of an **AI Architecture Review Assistant**. The user supplies a software architecture proposal. Your API asks a local language model to identify:

1. Business goals.
2. Explicit constraints.
3. Proposed components.
4. Unanswered questions and contradictions.

This week’s feature supports requirements clarification. It does not approve an architecture or decide whether a design should use microservices. You will review its output against the supplied evidence.

The flow is: HTTP request → input checks → model-client interface → local model → response checks → summary response.

We use one project and a provider interface to keep the exercise small. The application has no meaningful domain rules yet, so separate Domain and Application projects would add little value at this stage. As the product grows, keep business rules independent of model SDKs and infrastructure.

## 2. Three-hour session plan

| Session | Time | Work | Evidence produced |
|---|---:|---|---|
| Study | 30 min | Read selected API documentation and explain five essential concepts | Short concept notes |
| Practice | 60 min | Set up the local model and implement the endpoint | Buildable .NET API |
| Project | 90 min | Integrate, inspect three proposals, exercise failures, record findings | Working feature and review log |

The 60-minute implementation allowance includes setup. A slow model download may consume much of it. If that happens, stop at the three-hour limit and finish the same lesson next session; do not expand the feature scope.

## 3. Prerequisites and resources

You already know C#, dependency injection, HTTP, and asynchronous programming. No Python or model training is required.

Have a **.NET 8 or newer SDK**, an editor, an HTTP client, and a machine that can run Ollama with a small model. This tutorial uses C# 12 syntax. The SDK selected by any local `global.json` must support it.

Commands use Bash syntax on macOS/Linux. On Windows, use Git Bash or run equivalent requests from an HTTP client; PowerShell quoting differs.

| Resource | Level | Time for this lesson | Use |
|---|---|---:|---|
| [Ollama Quickstart](https://docs.ollama.com/quickstart) | Beginner | 10 min | Read local-model setup and local requests only |
| [Generate a chat message — Ollama](https://docs.ollama.com/api/chat) | Beginner–Intermediate | 10 min | Inspect request/response fields |
| [Minimal APIs quick reference — Microsoft](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis) | Intermediate | 5 min, within implementation time | Look up routing and binding only if needed |
| [Qwen2.5 3B model page](https://ollama.com/library/qwen2.5:3b) | Beginner | 5 min, within setup time | Check the model tag, size, and license |
| [Download Ollama](https://ollama.com/download) | Beginner | Setup; allow 10–20 min plus download | Install the local runtime |
| [Download .NET](https://dotnet.microsoft.com/download) | Beginner | Only if missing; setup time varies | Install a compatible SDK |

All these resources are free. Estimated times cover selected sections, not entire documentation sites. Chip Huyen’s book is not required this week; retain it as an optional conceptual reference from the wider roadmap.

Use the remaining 10 minutes of study for the concepts below and your own explanation of them.

## 4. Five concepts you need now

| Concept | Plain explanation | What it means for your design |
|---|---|---|
| Inference | Running a trained model on new input | Your application calls a dependency; it is not training a model |
| Tokens | Pieces of text processed by a model | Characters and tokens differ; input/output size affects runtime |
| Context window | The model’s finite input-and-generation capacity | You must bound and select context |
| Hallucination | Plausible text without adequate support | Compare material claims with the proposal |
| System prompt | Instructions describing the task | Useful guidance, but not an enforceable security boundary |

Our prompt tells the model to preserve missing information. This improves task clarity but cannot guarantee compliance. The application still needs validation and evaluation.

For now, distinguish two forms of correctness:

- **Transport correctness:** the request completes and the response envelope can be read.
- **Task correctness:** the summary reflects the proposal, preserves uncertainty, and adds no unsupported claims.

An HTTP `200` establishes neither factual accuracy nor architectural soundness.

## 5. Set up the local model

Install Ollama, open the application, and run:

```bash
ollama pull qwen2.5:3b
ollama list
```

The selected model is approximately 1.9 GB to download. Runtime memory use is higher and depends on context and execution settings. This is a small learning model, not a quality benchmark or a production recommendation. Its model license differs from some other Qwen sizes; inspect the model page before later redistribution or product use.

If no local server is running, start one in a separate terminal:

```bash
ollama serve
```

If you see an address-in-use error, the desktop application may already be running the server. Do not start a second instance.

Verify a local request:

```bash
curl --max-time 120 http://localhost:11434/api/chat \
  -H 'Content-Type: application/json' \
  -d '{
    "model": "qwen2.5:3b",
    "stream": false,
    "messages": [
      {"role": "user", "content": "Say hello in one sentence."}
    ]
  }'
```

Find the answer in `message.content`. Local inference requires no API key. The first request may take longer while the model loads. Resolve this smoke request before debugging the .NET application.

## 6. Create the .NET project

Check your SDK and create the project:

```bash
dotnet --version
dotnet new web -n AIPractice
cd AIPractice
```

Replace `appsettings.json` with this complete configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "localhost;127.0.0.1",
  "Ollama": {
    "BaseUrl": "http://localhost:11434/",
    "Model": "qwen2.5:3b"
  }
}
```

No additional NuGet package is needed for this implementation. It uses ASP.NET Core’s shared framework and .NET HTTP/JSON APIs.

## 7. Understand the implementation

| Element | Why it exists | Current limit |
|---|---|---|
| `IArchitectureModelClient` | Separates application capability from provider transport | One implementation; no model routing |
| Typed `HttpClient` | Centralizes connection configuration and timeout | No retry policy this week |
| Input checks | Reject empty and excessive proposal text | Character bound is not a token budget or an HTTP body-size limit |
| System/user messages | Separate task instructions from proposal content | Prompt separation is not a security guarantee |
| `stream = false` | Simplifies response parsing for the first feature | Caller waits for the complete answer |
| `temperature = 0` | Reduces sampling variation | Does not guarantee accuracy or identical outputs |
| `num_predict = 512` | Bounds generation work | May truncate long answers; truncation is treated as a failure |
| Envelope checks | Reject missing content and incomplete responses | Do not validate summary meaning |
| Cancellation token | Propagates caller cancellation to the dependency | Disconnected callers are not translated into model timeouts |
| Controlled errors | Keep dependency failures distinct from valid results | Detailed telemetry is deferred |

`SummaryResponse` is a typed HTTP wrapper around a string. It is **not** a schema-constrained AI result. Week 2 replaces this free-form content with a structured review contract.

Our `502` is an application mapping for dependency rejection or bad output. A model-not-found error can also map to it; it does not prove the model process is offline.

## 8. Send your first architecture proposal

In the project directory, create `proposal.json`:

```json
{
  "proposal": "We are building an insurance back office. Eight engineers will maintain it. The initial design is a modular monolith with Policy, Claims, Billing, and Customer modules, using PostgreSQL. It integrates with two external insurers through HTTP APIs. Claim submissions must tolerate temporary insurer outages. Traffic, availability targets, infrastructure budget, and recovery objectives have not been agreed."
}
```

From a second terminal in the same directory:

```bash
curl --max-time 75 -i http://localhost:5080/proposals/summarize \
  -H 'Content-Type: application/json' \
  --data-binary @proposal.json
```

The response envelope should contain a `summary` string. Its exact wording will vary. The following is an **illustrative expected summary**, not a captured model response:

```text
1. Business goals
Build an insurance back office.

2. Explicit constraints
An eight-engineer team will maintain the system.
Claim submissions must tolerate temporary insurer outages.

3. Proposed components
A modular monolith containing Policy, Claims, Billing, and Customer modules.
PostgreSQL and HTTP integrations with two external insurers.

4. Unanswered questions and contradictions
Traffic, availability targets, infrastructure budget, and recovery objectives
are not specified. No contradiction is identified in the supplied text.
```

Check whether the actual answer preserves those facts and unknowns. It should not invent a queue, Kubernetes deployment, throughput target, staffing expertise, or migration plan.

## 9. Practice cases and failure drills

Spend 20 minutes on the first request, 30 on the three proposal cases, 20 on input/dependency failures, and 20 on notes.

### Case A — Detailed proposal

Use `proposal.json` above. Review whether all four modules, both insurer integrations, and the outage requirement survive the summary.

### Case B — Incomplete proposal

Replace the proposal text with:

```text
We need a customer portal. We want to deploy quickly. The team is considering
microservices, but no reasons or operational requirements have been recorded.
```

Expected behavior: summarize the portal and delivery goal, preserve the proposed option, and leave team size, data ownership, security, traffic, availability, and deployment budget unknown. It should not endorse microservices based solely on this text.

### Case C — Contradictory proposal

Use:

```text
All modules must share one PostgreSQL database. Each module must also own an
independent database. The release is planned for six weeks from now. Only two
engineers will implement and operate the system.
```

Expected behavior: surface the database conflict without silently choosing one rule. Preserve the stated timeline and team size. Do not turn the summary into a full architecture recommendation.

### Drill D — Empty input

```bash
curl -i http://localhost:5080/proposals/summarize \
  -H 'Content-Type: application/json' \
  -d '{"proposal":""}'
```

Expected: `400`, without invoking the model. `null` and whitespace-only proposals should also be rejected. Malformed request JSON is handled by ASP.NET Core’s binding behavior.

### Drill E — Model service unavailable

Quit the Ollama application and stop any separate `ollama serve` process, then submit a valid proposal. Expected: controlled `502`, or `504` if the connection/request stalls until the timeout.

If background Ollama instances make this difficult, temporarily change the configured base URL to an unused localhost port such as `http://localhost:11435/`, restart the API, and submit the request. Restore the configuration afterwards. Ensure the chosen port really is unused.

### Optional drill — Deadline

To deliberately exercise the deadline branch, temporarily set the HTTP timeout to one millisecond, restart, and submit a valid proposal. This will usually produce `504`, although extremely fast failure may produce another dependency error. Restore 60 seconds before continuing. The primary three-hour lesson does not require a deterministic fault-injection harness.

## 10. Record progress

Create a small learning log. Keep all proposals synthetic.

| Case | Status | Elapsed time | Facts preserved? | Unknowns preserved? | Unsupported claims | Verdict |
|---|---:|---:|---|---|---|---|
| Detailed | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| Incomplete | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| Contradictory | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| Empty | Fill in | Fill in | N/A | N/A | N/A | Pass / Needs work |
| Dependency unavailable | Fill in | Fill in | N/A | N/A | N/A | Pass / Needs work |

Record SDK version, Ollama version (`ollama --version`), model tag, machine RAM, and whether the model was already warm. Three observations cannot establish a reliable p95 or production quality rate.

For a simple manual score, award one point each for: business goal preserved, material constraints preserved, components preserved, unknowns/conflicts preserved, and no unsupported material claim. Report a score out of five for each valid proposal. This is a learning rubric, not a universal evaluation standard.

If a proposal fails, save both input and output. Change only one prompt instruction and rerun the same case. Describe what improved and what regressed rather than declaring the new prompt universally better.

## 11. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| `dotnet` not found | SDK missing or PATH not updated | Install a compatible SDK and reopen your terminal |
| C# syntax compilation errors | Older SDK selected | Check `dotnet --version` and any `global.json` |
| Cannot connect to port 11434 | Local runtime not running | Open Ollama or start `ollama serve`; try the direct smoke request |
| Ollama address already in use | Another instance is running | Use the existing instance |
| Model not found | Tag not downloaded or spelling mismatch | Run `ollama list`; pull the configured tag |
| First request times out | Cold loading or hardware latency | Warm the model using the direct request; choose a smaller model if necessary |
| Answer repeatedly truncated | Generation limit too small for actual output | Ask for a shorter summary first; record any later limit change |
| API port 5080 already in use | Another process owns the port | Use a free local port and update the request URL |
| Too many invented details | Small-model limitations or unclear task boundaries | Inspect evidence, simplify the prompt, and save the failed case |
| Endpoint returns valid JSON but a bad summary | Transport validation succeeded; semantic task failed | Mark the case failed; use it in Week 2/4 evaluations |

Do not solve a quality failure only by increasing timeouts. First determine whether the problem is load time, generation length, connectivity, or incorrect content.

## 12. Completion checklist

- [ ] I can explain inference, tokens, context limits, hallucination, and system instructions.
- [ ] The direct local-model smoke request succeeds.
- [ ] `dotnet build` succeeds on my machine.
- [ ] The endpoint summarizes three proposals.
- [ ] I have reviewed claims against the source text.
- [ ] Missing information remains unknown; contradictions are surfaced.
- [ ] Empty input returns `400`.
- [ ] Dependency failure returns a controlled `502` or `504`.
- [ ] I recorded failures and environment details.
- [ ] I can explain why `200 OK` is different from a trustworthy result.

If a semantic criterion fails, retain the working endpoint and document the failure. Repeat a narrow prompt experiment before marking the lesson complete.

## 13. Architecture reflection and next week

Answer these questions in five sentences or fewer each:

1. Which guarantees are enforced by C# code, and which are merely requested in the prompt?
2. What changes if you replace the model provider?
3. What should a caller do when the model is unavailable?
4. What evidence would you need before applying this feature to actual architecture decisions?

This is a local learning prototype. The three-hour scope intentionally covers a narrow feature: provider isolation, bounded input/output work, cancellation, failure mapping, and manual quality inspection. Production use additionally needs access controls, actual request/token limits, sensitive-data policy, telemetry, and a larger evaluation set.

**Week 2:** develop structured architecture reviews. Replace free-form summary text with explicit fields for assumptions, risks, alternatives, and recommendations; validate both output shape and selected business semantics.

## 14. Verification note

Official documentation and model availability were checked on **2026-10-05**. The .NET SDK and Ollama were not available in the document-generation environment, so this tutorial’s application was not compiled or run here. Commands, JSON examples, code structure, and document completeness were reviewed; the completion checklist establishes the required local runtime verification.

## References

- [Ollama — local setup](https://docs.ollama.com/quickstart)
- [Ollama — chat API contract](https://docs.ollama.com/api/chat)
- [Ollama — Qwen2.5 3B model details and license](https://ollama.com/library/qwen2.5:3b)
- [Microsoft — ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)

The tutorial’s exercise design, time estimates, rubric, and architecture judgments are recommendations for this learning plan.
