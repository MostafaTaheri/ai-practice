# Week 2 — Structured Architecture Reviews in .NET

**Prepared for:** Mostafa Taheri — Technical Lead | Software Architect  
**Time budget:** 3 hours  
**Prerequisite:** Week 1’s local architecture summarizer works  
**Feature:** `POST /proposals/review`  
**Outcome:** A typed review with explicit assumptions, risks, alternatives, recommendations, and missing information, validated before the API returns it.

## 1. This week’s architectural lesson

A model is an untrusted producer of application data. “Return JSON” is a request, not a sufficient contract. Use three layers:

1. **Schema-guided generation:** ask for specific fields and types.
2. **Application validation:** reject malformed contracts and selected rule violations.
3. **Human review:** assess evidence, reasoning, and architectural usefulness.

Your code can verify that a risk category is permitted and that an evidence quote exists in the proposal. It cannot prove that the quoted passage supports the risk or that the recommendation is correct.

Build on your Week 1 project. Preserve `/proposals/summarize`; add the review capability separately. No Python, vector database, or agent framework is required.

## 2. Three-hour plan

| Session | Minutes | Task |
|---|---:|---|
| Study | 30 | Read structured-output and JSON-validation documentation; explain the three layers above |
| Practice | 60 | Add contracts, schema-guided generation, validator, and endpoint |
| Project | 90 | Try five proposals; exercise deterministic repair/failure cases; document observations |

Suggested project split: 30 minutes for real-model cases, 35 for fake-generator checks, and 25 for notes and architecture reflection. If copying and debugging take longer, repeat this week rather than adding unfinished work to Week 3.

### Selected resources

| Resource | Difficulty | Estimated selected time | What to read |
|---|---|---:|---|
| [Structured Outputs — Ollama](https://docs.ollama.com/capabilities/structured-outputs) | Intermediate | 10 min | JSON schema in the `format` field; local examples |
| [Required properties — System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/required-properties) | Intermediate | 5 min | Required property presence during deserialization |
| [Handle unmapped members — System.Text.Json](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members) | Intermediate | 5 min | Rejecting unexpected fields |
| [Generate a chat message — Ollama](https://docs.ollama.com/api/chat) | Beginner–Intermediate | 5 min | Request and response envelope |
| [AI Engineering — Chip Huyen, official contents](https://github.com/chiphuyen/aie-book/blob/main/ToC.md) | Intermediate | Optional replacement for 10 min of study | Book Ch. 2 structured outputs and Ch. 5 explicit instructions, if you own it |

All documentation is free. The optional book is the only paid reference. Times cover selected excerpts, not whole chapters. Use the last five study minutes to explain why schema validity and factual validity differ.

## 3. Contract and feature scope

Your review has six fields:

| Field | Purpose | Important constraint |
|---|---|---|
| `summary` | Brief description of the supplied design | Nonempty, maximum 1,500 characters |
| `assumptions` | Explicitly labeled hypotheses | Array; may be empty |
| `risks` | Risks grounded in proposal excerpts | At most five; permitted category/severity; evidence quote required |
| `alternatives` | Conditional options with trade-offs | Array; no invented requirements |
| `recommendations` | Proposed next actions | Array; human approval remains necessary |
| `missingInformation` | Questions blocking a sound decision | Array; preserve unknown traffic, budget, ownership, etc. |

Risk categories: `reliability`, `security`, `scalability`, `operations`, `cost`, and `team`. Severities: `low`, `medium`, and `high`.

All arrays are required but may be empty. Never force the model to invent a risk or an alternative merely to fill a list.

**Request policy:** up to 12,000 proposal characters.  
**Generation policy:** one initial attempt and at most one repair attempt.  
**Deadline:** 90 seconds for both attempts together.  
**Network retries:** none in this exercise.

The deadline is a learning configuration, not an SLO. Record whether it suits your hardware.

## 4. Try a real proposal

Create `proposal.json` in the project directory:

```json
{
  "proposal": "We are building an insurance back office. Eight engineers will maintain it. We propose a modular monolith with Policy, Claims, Billing, and Customer modules using PostgreSQL. It integrates with two external insurers through HTTP APIs. Claim submissions must tolerate temporary insurer outages. We have not agreed on traffic, availability targets, recovery objectives, or infrastructure budget."
}
```

In a second terminal, from the project directory:

```bash
curl --max-time 100 -i http://localhost:5080/proposals/review \
  -H 'Content-Type: application/json' \
  --data-binary @proposal.json
```

A successful HTTP response has `review`, `attempts`, and `promptVersion`. Inspect whether:

- The modular monolith is preserved as the proposed design.
- External dependency outages are considered without inventing an implemented queue.
- Traffic, SLOs, budget, and recovery objectives remain unknown.
- Suggestions to add durable handling or define outage behavior are framed as proposals.
- Every evidence quote is copied exactly from the original text.

Do not interpret `attempts = 1` as evidence of factual accuracy. It means only that the result passed the implemented checks without repair.

## 5. Five architecture practice cases

Keep all inputs synthetic. Use the same request file, replacing its proposal text.

| Case | Proposal | Human review criterion |
|---|---|---|
| A: External dependencies | The insurance example above | Identify outage-handling questions; do not claim any unmentioned mechanism exists |
| B: Premature distribution | “Two engineers will build and operate twelve microservices. No independent scaling or release requirements have been identified.” | Discuss operational/team trade-offs; any alternative must be conditional |
| C: Conflicting ownership | “All modules must share one database. Each module must also own a separate database.” | Surface the conflict; do not silently resolve it |
| D: Missing authorization | “A staff portal will expose customer financial reports. Authentication is planned. Report authorization rules have not been defined.” | Distinguish authentication from authorization; preserve the unresolved access policy |
| E: Sparse proposal | “We need a portal quickly. Architecture, team capacity, traffic, and budget are undecided.” | Favor clarification; avoid a confident technology prescription |

For each case, record:

| Case | HTTP result | Attempts | Contract valid | Unsupported claims | Useful next action | Manual verdict |
|---|---|---:|---|---|---|---|
| A | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| B | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| C | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| D | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |
| E | Fill in | Fill in | Fill in | Fill in | Fill in | Pass / Needs work |

A small-model failure is useful learning evidence. Preserve the input/output pair; do not hide it by editing the reported result.

## 6. Deterministically test the repair limit

Schema-guided generation may rarely emit malformed JSON. Use a fake generator to exercise your application policy independently of the model.

First, create `valid-review.json` in the project directory. This is a **test fixture**, not a captured model response. Its evidence quote matches Case A exactly.

```json
{
  "summary": "An insurance back office using a proposed modular monolith and external insurer HTTP integrations.",
  "assumptions": [],
  "risks": [
    {
      "category": "reliability",
      "severity": "medium",
      "description": "Outage handling needs a defined acceptance and recovery policy.",
      "evidenceQuote": "Claim submissions must tolerate temporary insurer outages."
    }
  ],
  "alternatives": [
    "If requests must be accepted while insurers are offline, consider durable deferred processing; this adds storage and recovery complexity."
  ],
  "recommendations": [
    "Define claim acceptance and recovery behavior during insurer outages."
  ],
  "missingInformation": [
    "Traffic, availability targets, recovery objectives, and infrastructure budget."
  ]
}
```

Add `ScriptedReviewGenerator.cs`:

```csharp
public sealed class ScriptedReviewGenerator : IReviewGenerator
{
    private int calls;

    public async Task<string> GenerateAsync(
        string proposal,
        string? repairFeedback,
        CancellationToken cancellationToken)
    {
        calls++;
        Console.WriteLine($"Fake generation call: {calls}");

        if (calls == 1)
            return "{}";

        return await File.ReadAllTextAsync(
            "valid-review.json", cancellationToken);
    }
}
```

**Temporarily** add this registration after the normal model-client registration, before `builder.Build()`:

```csharp
builder.Services.AddTransient<IReviewGenerator, ScriptedReviewGenerator>();
```

Run from the project directory, submit **Case A**, and expect:

1. First result rejected because required fields are missing.
2. Second result accepted.
3. HTTP `200` with `attempts = 2`.
4. Exactly two fake-generation log entries.

The fake is transient and the review service keeps one instance for the request, so its counter resets for each request.

Now replace the body of its method with:

```csharp
calls++;
Console.WriteLine($"Fake generation call: {calls}");
return await Task.FromResult("{}");
```

Submit Case A again. Expect `502` with `attempts = 2` and no third generation call. No real model request is made in either fake-generator exercise.

For a one-call success check, return the fixture immediately on every invocation. Expect `attempts = 1`.

**Restore real-model behavior:** remove the temporary fake registration and restart the API. Keep the fixture only as test data.

## 7. Exercise validation beyond JSON syntax

With the fake returning the fixture immediately, make one fixture change at a time:

| Mutation | Expected result |
|---|---|
| Remove `summary` | Required-property deserialization failure; two invalid attempts lead to `502` |
| Add `approved: true` | Unexpected-member rejection; two invalid attempts lead to `502` |
| Set `assumptions` to `null` | Application rejects null list |
| Set category to `performance` | Application rejects category outside its contract |
| Replace evidence with an invented quotation | Application rejects absent excerpt |
| Set description to “This design guarantees 100K RPS,” retaining a real quote | Contract checks may pass; human review must fail the unsupported claim |

The last example is essential: an authentic quote does not establish that the attached conclusion follows from it.

After each mutation, restore the baseline fixture. Use Case A, because the fixture’s evidence belongs to that proposal.

## 8. Architecture trade-offs

| Decision | Benefit | Cost or limitation |
|---|---|---|
| Typed output rather than free text | Predictable downstream fields | Contract/schema maintenance |
| Schema plus explicit validation | Independent acceptance checks | Duplication between schema and application rules |
| One repair attempt | Can recover from a contract failure | Extra latency and model work; no guarantee of improvement |
| Shared deadline | Bounds both calls as one operation | Slow models may leave no time for repair |
| Exact evidence quote | Detects fabricated excerpts | Brittle to wording changes; does not verify inference |
| Read-only recommendations | Supports human decision-making | Cannot autonomously approve or execute a change |

Keep deterministic architecture policies in ordinary application/domain code when they are actual business rules. A prompt is not the place to enforce permissions or architecture governance.

Today’s service performs no external action. Prompt injection can still distort its content; tool permissions and adversarial testing come later.

## 9. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| `JsonUnmappedMemberHandling` does not compile | SDK/target older than .NET 8 | Check project target and selected SDK |
| Endpoint returns `404` | Wrong URL or endpoint added after `app.Run()` | Add mapping before the blocking run call |
| Every real-model call returns `502` | Invalid output, unsupported provider schema, missing model, or bad envelope | Confirm local endpoint/model; inspect safe internal diagnostics |
| `504` before repair | Shared deadline consumed by the first call | Warm the model, shorten output, or adjust the measured learning deadline |
| Evidence quote mismatch | Model paraphrased, changed case, or added punctuation | Keep verbatim-quote instruction; inspect the original passage |
| Fixture cannot be found | Process working directory is not the project directory | Run from the project folder or use an explicit fixture path |
| Fake still handles real requests | Temporary registration remains | Remove it and restart |
| Model output looks correct but is rejected | Contract size, enum, field, or quote rule violation | Inspect validation errors rather than removing checks |
| Output passes but advice is weak | Syntactic checks are insufficient | Record semantic failure for Week 4 evaluation |

Do not strip arbitrary Markdown fences, silently drop unknown fields, or accept partial results to make the demo appear successful. Failures should stay visible.

## 10. Completion checkpoint

- [ ] Week 1’s summarizer still works.
- [ ] `/proposals/review` returns a typed review.
- [ ] Five real proposals have been manually assessed.
- [ ] Required fields and allowed risk values are enforced.
- [ ] Null arrays, unknown fields, and nonexistent evidence quotes are rejected.
- [ ] Fake invalid-then-valid output succeeds in two attempts.
- [ ] Fake always-invalid output fails after two calls.
- [ ] A schema-valid unsupported claim is recognized as a semantic failure.
- [ ] Temporary fake registration has been removed.
- [ ] Model tag, prompt version, cases, and results are recorded.

Passing these checks demonstrates the feature, not production readiness.

### Reflection

Write short answers:

1. Which rules are guaranteed by your code and which merely requested from the model?
2. Why is model-output failure `502`, rather than blaming a valid caller with `400`?
3. What can an evidence quote verify, and what remains uncertain?
4. When does a repair attempt improve user value enough to justify its latency?
5. How would schema changes affect downstream consumers?

## 11. Next week

**Week 3: Review against architecture standards using RAG.** Add ten synthetic/public ADRs or standards, retrieve relevant excerpts, and attach source identifiers. The typed review becomes the contract for that feature.

## 12. Verification and limitations

Official structured-output and .NET serialization documentation was checked on **2026-10-11**. Markdown, embedded JSON, schema/contract consistency, and bounded-control-flow structure were reviewed during document preparation.

The .NET SDK and Ollama are unavailable in the authoring environment, so the application has **not been compiled or executed here**. Run the build, five real cases, and deterministic fake-generator checks on your machine before marking the week complete.

Runtime observations, model quality, and hardware latency remain unverified. Input character limits do not replace server request-body or token limits. The prototype has no authentication, automatic semantic judge, response byte limit, or production telemetry.

The tutorial’s exercises, deadlines, and architecture judgments are learning recommendations. Documentation links above are the primary sources for provider/schema and serializer behavior.

